import UIKit
import QuartzCore
import MapboxMaps

/// Global Mapbox configuration. Must be called before the first IKMapView is created.
@objc(IKMapbox)
public class IKMapbox: NSObject
{
    @objc(setAccessToken:)
    public static func setAccessToken(_ token: String)
    {
        MapboxOptions.accessToken = token
    }
}

/// Immutable snapshot of the current camera, handed to the event listener.
@objc(IKCameraState)
public class IKCameraState: NSObject
{
    @objc public let latitude: Double
    @objc public let longitude: Double
    @objc public let zoom: Double
    @objc public let bearing: Double
    @objc public let pitch: Double

    init(latitude: Double, longitude: Double, zoom: Double, bearing: Double, pitch: Double)
    {
        self.latitude = latitude
        self.longitude = longitude
        self.zoom = zoom
        self.bearing = bearing
        self.pitch = pitch
    }
}

/// Events flowing from the native map back to the .NET handler.
@objc(IKMapEventListener)
public protocol IKMapEventListener
{
    @objc(onMapReady) func onMapReady()
    @objc(onStyleLoaded) func onStyleLoaded()
    @objc(onMapClick:longitude:) func onMapClick(latitude: Double, longitude: Double)
    @objc(onMapLongPress:longitude:) func onMapLongPress(latitude: Double, longitude: Double)
    @objc(onMarkerClick:) func onMarkerClick(id: String)
    @objc(onPolylineClick:) func onPolylineClick(id: String)
    @objc(onPolygonClick:) func onPolygonClick(id: String)
    @objc(onCameraChanged:) func onCameraChanged(camera: IKCameraState)
    @objc(onOfflineRegionProgress:progress:) func onOfflineRegionProgress(id: String, progress: Double)
    @objc(onOfflineRegionCompleted:success:error:) func onOfflineRegionCompleted(id: String, success: Bool, error: String?)
    @objc(onFollowPuckChanged:) func onFollowPuckChanged(active: Bool)
    @objc(onMarkerDragEnd:latitude:longitude:) func onMarkerDragEnd(id: String, latitude: Double, longitude: Double)
}

/// Thin facade over MapboxMaps.MapView exposing exactly the surface the
/// Indiko.Maui.Controls.MapBox handler needs. All Mapbox types stay internal
/// so the .NET binding only ever sees this Objective-C API.
@objc(IKMapView)
public class IKMapView: UIView
{
    private var mapView: MapView!
    private var pointManager: PointAnnotationManager?
    private var polylineManager: PolylineAnnotationManager?
    private var polygonManager: PolygonAnnotationManager?
    private var cancelables = Set<AnyCancelable>()

    // Registry of runtime sources/layers — Mapbox drops them on every style
    // reload, so they are re-applied after each onStyleLoaded.
    private var geoJsonSources: [String: String] = [:]
    private var layerConfigs: [(id: String, config: [String: Any])] = []
    private var clusterConfigs: [String: [String: Any]] = [:]

    // Click-consumed semantics: taps handled by an annotation or cluster must not
    // surface as onMapClick. Annotation tap handlers stamp this timestamp; the
    // (deferred) map-click dispatch skips clicks that follow within the window.
    private var lastAnnotationTap: CFTimeInterval = 0
    private static let annotationTapWindow: CFTimeInterval = 0.3

    @objc public weak var listener: IKMapEventListener?

    @objc(initWithFrame:styleUri:latitude:longitude:zoom:bearing:pitch:)
    public init(frame: CGRect, styleUri: String, latitude: Double, longitude: Double,
                zoom: Double, bearing: Double, pitch: Double)
    {
        super.init(frame: frame)

        let camera = CameraOptions(
            center: CLLocationCoordinate2D(latitude: latitude, longitude: longitude),
            zoom: zoom, bearing: bearing, pitch: pitch)

        let options = MapInitOptions(
            cameraOptions: camera,
            styleURI: StyleURI(rawValue: styleUri) ?? .streets)

        mapView = MapView(frame: bounds, mapInitOptions: options)
        mapView.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        addSubview(mapView)

        subscribeEvents()
        mapView.viewport.addStatusObserver(self)
    }

    required init?(coder: NSCoder)
    {
        return nil
    }

    private func subscribeEvents()
    {
        mapView.mapboxMap.onMapLoaded.observeNext { [weak self] _ in
            self?.listener?.onMapReady()
        }.store(in: &cancelables)

        mapView.mapboxMap.onStyleLoaded.observe { [weak self] _ in
            guard let self else { return }
            for (id, geoJson) in self.geoJsonSources
            {
                self.applyGeoJsonSource(id: id, geoJson: geoJson)
            }
            for entry in self.layerConfigs
            {
                self.applyLayer(entry.config)
            }
            for config in self.clusterConfigs.values
            {
                self.applyClusteredSource(config)
            }
            self.listener?.onStyleLoaded()
        }.store(in: &cancelables)

        mapView.mapboxMap.onCameraChanged.observe { [weak self] event in
            guard let self else { return }
            let state = event.cameraState
            self.listener?.onCameraChanged(camera: IKCameraState(
                latitude: state.center.latitude,
                longitude: state.center.longitude,
                zoom: state.zoom,
                bearing: state.bearing,
                pitch: state.pitch))
        }.store(in: &cancelables)

        mapView.gestures.onMapTap.observe { [weak self] context in
            guard let self else { return }
            let latitude = context.coordinate.latitude
            let longitude = context.coordinate.longitude
            self.resolveClusterTap(at: context.point) { [weak self] consumedByCluster in
                guard let self else { return }
                let annotationConsumed =
                    CACurrentMediaTime() - self.lastAnnotationTap < Self.annotationTapWindow
                if !consumedByCluster && !annotationConsumed
                {
                    self.listener?.onMapClick(latitude: latitude, longitude: longitude)
                }
            }
        }.store(in: &cancelables)

        mapView.gestures.onMapLongPress.observe { [weak self] context in
            guard let self else { return }
            let latitude = context.coordinate.latitude
            let longitude = context.coordinate.longitude
            // Deferred like onMapTap: a long-press that starts an annotation drag
            // stamps lastAnnotationTap and must not surface as a map long-press.
            DispatchQueue.main.async { [weak self] in
                guard let self else { return }
                let annotationConsumed =
                    CACurrentMediaTime() - self.lastAnnotationTap < Self.annotationTapWindow
                if !annotationConsumed
                {
                    self.listener?.onMapLongPress(latitude: latitude, longitude: longitude)
                }
            }
        }.store(in: &cancelables)
    }

    // MARK: - Style & camera

    @objc(setStyleUri:)
    public func setStyleUri(_ uri: String)
    {
        mapView.mapboxMap.loadStyle(StyleURI(rawValue: uri) ?? .streets)
    }

    @objc(setCamera:longitude:zoom:bearing:pitch:)
    public func setCamera(latitude: Double, longitude: Double, zoom: Double, bearing: Double, pitch: Double)
    {
        mapView.mapboxMap.setCamera(to: CameraOptions(
            center: CLLocationCoordinate2D(latitude: latitude, longitude: longitude),
            zoom: zoom, bearing: bearing, pitch: pitch))
    }

    @objc(flyTo:longitude:zoom:bearing:pitch:durationMs:)
    public func flyTo(latitude: Double, longitude: Double, zoom: Double, bearing: Double,
                      pitch: Double, durationMs: Double)
    {
        mapView.camera.fly(to: CameraOptions(
            center: CLLocationCoordinate2D(latitude: latitude, longitude: longitude),
            zoom: zoom, bearing: bearing, pitch: pitch),
            duration: durationMs / 1000.0)
    }

    /// Moves the camera so the given bounding box is fully visible, with uniform
    /// padding in points. durationMs 0 jumps instantly.
    @objc(fitBounds:minLng:maxLat:maxLng:padding:durationMs:)
    public func fitBounds(minLat: Double, minLng: Double, maxLat: Double, maxLng: Double,
                          padding: Double, durationMs: Double)
    {
        let bounds = CoordinateBounds(
            southwest: CLLocationCoordinate2D(latitude: minLat, longitude: minLng),
            northeast: CLLocationCoordinate2D(latitude: maxLat, longitude: maxLng))
        let insets = UIEdgeInsets(top: padding, left: padding, bottom: padding, right: padding)

        guard let camera = try? mapView.mapboxMap.camera(
            for: bounds, padding: insets, bearing: nil, pitch: nil, maxZoom: nil, offset: nil)
        else { return }

        if durationMs <= 0
        {
            mapView.mapboxMap.setCamera(to: camera)
        }
        else
        {
            mapView.camera.fly(to: camera, duration: durationMs / 1000.0)
        }
    }

    // MARK: - Markers

    /// Replaces all markers. JSON: [{"id":"...","lat":..,"lng":..,"title":"...","color":"#RRGGBB"}]
    @objc(setMarkersJson:)
    public func setMarkers(json: String)
    {
        guard let data = json.data(using: .utf8),
              let items = try? JSONSerialization.jsonObject(with: data) as? [[String: Any]]
        else { return }

        if pointManager == nil
        {
            pointManager = mapView.annotations.makePointAnnotationManager()
        }

        var annotations: [PointAnnotation] = []
        for item in items
        {
            guard let id = item["id"] as? String,
                  let lat = item["lat"] as? Double,
                  let lng = item["lng"] as? Double
            else { continue }

            let colorHex = item["color"] as? String ?? "#E74C3C"
            var annotation = PointAnnotation(
                id: id,
                coordinate: CLLocationCoordinate2D(latitude: lat, longitude: lng))

            if let iconBase64 = item["icon"] as? String, !iconBase64.isEmpty,
               let icon = customIcon(
                   base64: iconBase64,
                   width: item["iconWidth"] as? Double ?? 0,
                   height: item["iconHeight"] as? Double ?? 0)
            {
                annotation.image = .init(image: icon.image, name: icon.name)
            }
            else
            {
                annotation.image = .init(image: Self.pinImage(hex: colorHex), name: "ik-pin-\(colorHex)")
            }
            annotation.iconAnchor = (item["anchor"] as? String == "center") ? .center : .bottom
            annotation.tapHandler = { [weak self] _ in
                self?.lastAnnotationTap = CACurrentMediaTime()
                self?.listener?.onMarkerClick(id: id)
                return true
            }
            if item["draggable"] as? Bool == true
            {
                annotation.isDraggable = true
                annotation.dragBeginHandler = { [weak self] _, _ in
                    self?.lastAnnotationTap = CACurrentMediaTime()
                    return true
                }
                annotation.dragEndHandler = { [weak self] dragged, _ in
                    self?.lastAnnotationTap = CACurrentMediaTime()
                    self?.listener?.onMarkerDragEnd(
                        id: id,
                        latitude: dragged.point.coordinates.latitude,
                        longitude: dragged.point.coordinates.longitude)
                }
            }
            annotations.append(annotation)
        }

        pointManager?.annotations = annotations
    }

    @objc(clearMarkers)
    public func clearMarkers()
    {
        pointManager?.annotations = []
    }

    // MARK: - Polylines & polygons

    /// Replaces all polylines. JSON:
    /// [{"id":"...","points":[[lat,lng],...],"color":"#RRGGBB","width":4.0,"opacity":1.0}]
    @objc(setPolylinesJson:)
    public func setPolylines(json: String)
    {
        guard let data = json.data(using: .utf8),
              let items = try? JSONSerialization.jsonObject(with: data) as? [[String: Any]]
        else { return }

        if polylineManager == nil
        {
            polylineManager = mapView.annotations.makePolylineAnnotationManager()
        }

        var lines: [PolylineAnnotation] = []
        for item in items
        {
            guard let id = item["id"] as? String,
                  let coordinates = Self.coordinates(from: item["points"]), coordinates.count >= 2
            else { continue }

            var line = PolylineAnnotation(id: id, lineCoordinates: coordinates)
            line.lineColor = StyleColor(UIColor(hex: item["color"] as? String ?? "") ?? .systemBlue)
            line.lineWidth = item["width"] as? Double ?? 4.0
            line.lineOpacity = item["opacity"] as? Double ?? 1.0
            line.tapHandler = { [weak self] _ in
                self?.lastAnnotationTap = CACurrentMediaTime()
                self?.listener?.onPolylineClick(id: id)
                return true
            }
            lines.append(line)
        }

        polylineManager?.annotations = lines
    }

    @objc(clearPolylines)
    public func clearPolylines()
    {
        polylineManager?.annotations = []
    }

    /// Replaces all polygons. JSON:
    /// [{"id":"...","points":[[lat,lng],...],"fillColor":"#RRGGBB","fillOpacity":0.4,"strokeColor":"#RRGGBB"}]
    @objc(setPolygonsJson:)
    public func setPolygons(json: String)
    {
        guard let data = json.data(using: .utf8),
              let items = try? JSONSerialization.jsonObject(with: data) as? [[String: Any]]
        else { return }

        if polygonManager == nil
        {
            polygonManager = mapView.annotations.makePolygonAnnotationManager()
        }

        var polygons: [PolygonAnnotation] = []
        for item in items
        {
            guard let id = item["id"] as? String,
                  let coordinates = Self.coordinates(from: item["points"]), coordinates.count >= 3
            else { continue }

            var polygon = PolygonAnnotation(id: id, polygon: Polygon([coordinates]))
            polygon.fillColor = StyleColor(UIColor(hex: item["fillColor"] as? String ?? "") ?? .systemBlue)
            polygon.fillOpacity = item["fillOpacity"] as? Double ?? 0.4
            if let stroke = UIColor(hex: item["strokeColor"] as? String ?? "")
            {
                polygon.fillOutlineColor = StyleColor(stroke)
            }
            polygon.tapHandler = { [weak self] _ in
                self?.lastAnnotationTap = CACurrentMediaTime()
                self?.listener?.onPolygonClick(id: id)
                return true
            }
            polygons.append(polygon)
        }

        polygonManager?.annotations = polygons
    }

    @objc(clearPolygons)
    public func clearPolygons()
    {
        polygonManager?.annotations = []
    }

    // MARK: - GeoJSON sources & layers

    /// Adds a GeoJSON source or replaces the data of an existing one.
    @objc(addGeoJsonSource:geoJson:)
    public func addGeoJsonSource(id: String, geoJson: String)
    {
        if var config = clusterConfigs[id]
        {
            // Clustered source: only its data is replaced, cluster config stays.
            config["geoJson"] = geoJson
            clusterConfigs[id] = config
        }
        else
        {
            geoJsonSources[id] = geoJson
        }
        applyGeoJsonSource(id: id, geoJson: geoJson)
    }

    @objc(removeGeoJsonSource:)
    public func removeGeoJsonSource(id: String)
    {
        geoJsonSources.removeValue(forKey: id)
        try? mapView.mapboxMap.removeSource(withId: id)
    }

    /// Adds (or replaces) a style layer. JSON:
    /// {"id","sourceId","type":"fill|line|circle","color","opacity","lineWidth","circleRadius","belowLayerId"}
    @objc(addLayerJson:)
    public func addLayer(json: String)
    {
        guard let data = json.data(using: .utf8),
              let config = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
              let id = config["id"] as? String
        else { return }

        layerConfigs.removeAll { $0.id == id }
        layerConfigs.append((id, config))
        applyLayer(config)
    }

    @objc(removeLayer:)
    public func removeLayer(id: String)
    {
        layerConfigs.removeAll { $0.id == id }
        try? mapView.mapboxMap.removeLayer(withId: id)
    }

    private func applyGeoJsonSource(id: String, geoJson: String)
    {
        if mapView.mapboxMap.sourceExists(withId: id)
        {
            mapView.mapboxMap.updateGeoJSONSource(withId: id, data: .string(geoJson))
        }
        else
        {
            var source = GeoJSONSource(id: id)
            source.data = .string(geoJson)
            try? mapView.mapboxMap.addSource(source)
        }
    }

    private func applyLayer(_ config: [String: Any])
    {
        guard let id = config["id"] as? String,
              let sourceId = config["sourceId"] as? String,
              let type = config["type"] as? String
        else { return }

        if mapView.mapboxMap.layerExists(withId: id)
        {
            try? mapView.mapboxMap.removeLayer(withId: id)
        }

        let color = StyleColor(UIColor(hex: config["color"] as? String ?? "") ?? .systemBlue)
        let opacity = config["opacity"] as? Double ?? 1.0

        let layer: Layer
        switch type
        {
        case "fill":
            var fill = FillLayer(id: id, source: sourceId)
            fill.fillColor = .constant(color)
            fill.fillOpacity = .constant(opacity)
            layer = fill
        case "line":
            var line = LineLayer(id: id, source: sourceId)
            line.lineColor = .constant(color)
            line.lineOpacity = .constant(opacity)
            line.lineWidth = .constant(config["lineWidth"] as? Double ?? 3.0)
            layer = line
        case "circle":
            var circle = CircleLayer(id: id, source: sourceId)
            circle.circleColor = .constant(color)
            circle.circleOpacity = .constant(opacity)
            circle.circleRadius = .constant(config["circleRadius"] as? Double ?? 6.0)
            layer = circle
        case "symbol":
            layer = buildSymbolLayer(id: id, sourceId: sourceId, config: config)
        default:
            return
        }

        let position = (config["belowLayerId"] as? String).map { LayerPosition.below($0) }
        try? mapView.mapboxMap.addLayer(layer, layerPosition: position)
    }

    // MARK: - View annotations

    private var viewAnnotationsById: [String: ViewAnnotation] = [:]

    /// Anchors a native view (bottom-center) to a coordinate. The caller provides the
    /// fixed size in points; an existing annotation with the same id is replaced.
    @objc(addViewAnnotation:view:latitude:longitude:width:height:)
    public func addViewAnnotation(id: String, view: UIView, latitude: Double, longitude: Double,
                                  width: Double, height: Double)
    {
        removeViewAnnotation(id: id)

        view.frame = CGRect(x: 0, y: 0, width: width, height: height)
        let annotation = ViewAnnotation(
            coordinate: CLLocationCoordinate2D(latitude: latitude, longitude: longitude),
            view: view)
        annotation.allowOverlap = true
        annotation.variableAnchors = [ViewAnnotationAnchorConfig(anchor: .bottom)]
        mapView.viewAnnotations.add(annotation)
        viewAnnotationsById[id] = annotation
    }

    @objc(removeViewAnnotation:)
    public func removeViewAnnotation(id: String)
    {
        viewAnnotationsById.removeValue(forKey: id)?.remove()
    }

    // MARK: - Offline regions

    private lazy var offlineManager = OfflineManager()
    private var tileRegionCancelables: [String: Cancelable] = [:]

    /// Downloads a style pack plus the tile region for a bounding box. JSON:
    /// {"id","styleUri","minZoom","maxZoom","minLat","minLng","maxLat","maxLng"}
    /// Progress and completion are reported through the listener.
    @objc(downloadOfflineRegionJson:)
    public func downloadOfflineRegion(json: String)
    {
        guard let data = json.data(using: .utf8),
              let config = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
              let id = config["id"] as? String,
              let minLat = config["minLat"] as? Double,
              let minLng = config["minLng"] as? Double,
              let maxLat = config["maxLat"] as? Double,
              let maxLng = config["maxLng"] as? Double
        else { return }

        let styleUri = StyleURI(rawValue: config["styleUri"] as? String ?? "") ?? .streets
        let minZoom = UInt8(config["minZoom"] as? Double ?? 6)
        let maxZoom = UInt8(config["maxZoom"] as? Double ?? 14)

        // 1. Style pack (style JSON, sprites, glyphs) — required for offline rendering.
        if let stylePackOptions = StylePackLoadOptions(
            glyphsRasterizationMode: .ideographsRasterizedLocally, metadata: nil)
        {
            _ = offlineManager.loadStylePack(for: styleUri, loadOptions: stylePackOptions,
                                             completion: { _ in })
        }

        // 2. Tile region for the bounding box.
        let descriptorOptions = TilesetDescriptorOptions(
            styleURI: styleUri, zoomRange: minZoom...maxZoom, tilesets: nil)
        let descriptor = offlineManager.createTilesetDescriptor(for: descriptorOptions)

        let ring = Ring(coordinates: [
            CLLocationCoordinate2D(latitude: minLat, longitude: minLng),
            CLLocationCoordinate2D(latitude: minLat, longitude: maxLng),
            CLLocationCoordinate2D(latitude: maxLat, longitude: maxLng),
            CLLocationCoordinate2D(latitude: maxLat, longitude: minLng),
            CLLocationCoordinate2D(latitude: minLat, longitude: minLng),
        ])

        guard let loadOptions = TileRegionLoadOptions(
            geometry: .polygon(Polygon(outerRing: ring)),
            descriptors: [descriptor],
            acceptExpired: true)
        else { return }

        let cancelable = TileStore.default.loadTileRegion(forId: id, loadOptions: loadOptions)
        { [weak self] progress in
            let fraction = progress.requiredResourceCount > 0
                ? Double(progress.completedResourceCount) / Double(progress.requiredResourceCount)
                : 0
            DispatchQueue.main.async
            {
                self?.listener?.onOfflineRegionProgress(id: id, progress: fraction)
            }
        }
        completion: { [weak self] result in
            DispatchQueue.main.async
            {
                self?.tileRegionCancelables.removeValue(forKey: id)
                switch result
                {
                case .success:
                    self?.listener?.onOfflineRegionCompleted(id: id, success: true, error: nil)
                case .failure(let error):
                    self?.listener?.onOfflineRegionCompleted(
                        id: id, success: false, error: error.localizedDescription)
                }
            }
        }
        tileRegionCancelables[id] = cancelable
    }

    @objc(removeOfflineRegion:)
    public func removeOfflineRegion(id: String)
    {
        tileRegionCancelables.removeValue(forKey: id)?.cancel()
        TileStore.default.removeTileRegion(forId: id)
    }

    private func buildSymbolLayer(id: String, sourceId: String, config: [String: Any]) -> SymbolLayer
    {
        var layer = SymbolLayer(id: id, source: sourceId)
        let textField = config["textField"] as? String ?? ""

        if !textField.isEmpty, let expr = Self.expression("[\"get\",\"\(textField)\"]")
        {
            layer.textField = .expression(expr)
            layer.textSize = .constant(config["textSize"] as? Double ?? 14)
            layer.textColor = .constant(StyleColor(UIColor(hex: config["textColor"] as? String ?? "") ?? .black))

            if let halo = UIColor(hex: config["textHaloColor"] as? String ?? "")
            {
                layer.textHaloColor = .constant(StyleColor(halo))
                layer.textHaloWidth = .constant(config["textHaloWidth"] as? Double ?? 1.2)
            }
        }

        if let iconBase64 = config["icon"] as? String, !iconBase64.isEmpty,
           let icon = customIcon(base64: iconBase64, width: config["iconWidth"] as? Double ?? 0, height: 0)
        {
            let imageId = "ik-layer-icon-\(id)"
            try? mapView.mapboxMap.addImage(icon.image, id: imageId)
            layer.iconImage = .constant(.name(imageId))
            if !textField.isEmpty
            {
                // Icon above, label below
                layer.textOffset = .constant([0, 1.2])
                layer.textAnchor = .constant(.top)
            }
        }

        if config["allowOverlap"] as? Bool == true
        {
            layer.iconAllowOverlap = .constant(true)
            layer.iconIgnorePlacement = .constant(true)
            layer.textAllowOverlap = .constant(true)
            layer.textIgnorePlacement = .constant(true)
        }

        return layer
    }

    // MARK: - Clustering

    /// Adds a clustered GeoJSON source plus three managed layers
    /// ("<id>-clusters", "<id>-cluster-count", "<id>-points"). JSON:
    /// {"sourceId","geoJson","clusterRadius","clusterMaxZoom","clusterColor",
    ///  "clusterTextColor","pointColor","pointRadius"}
    @objc(addClusteredSourceJson:)
    public func addClusteredSource(json: String)
    {
        guard let data = json.data(using: .utf8),
              let config = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
              let id = config["sourceId"] as? String
        else { return }

        clusterConfigs[id] = config
        applyClusteredSource(config)
    }

    @objc(removeClusteredSource:)
    public func removeClusteredSource(id: String)
    {
        clusterConfigs.removeValue(forKey: id)
        for suffix in ["-clusters", "-cluster-count", "-points"]
        {
            try? mapView.mapboxMap.removeLayer(withId: id + suffix)
        }
        try? mapView.mapboxMap.removeSource(withId: id)
    }

    private func applyClusteredSource(_ config: [String: Any])
    {
        guard let id = config["sourceId"] as? String,
              let geoJson = config["geoJson"] as? String
        else { return }

        if mapView.mapboxMap.sourceExists(withId: id)
        {
            mapView.mapboxMap.updateGeoJSONSource(withId: id, data: .string(geoJson))
        }
        else
        {
            var source = GeoJSONSource(id: id)
            source.data = .string(geoJson)
            source.cluster = true
            source.clusterRadius = config["clusterRadius"] as? Double ?? 50
            source.clusterMaxZoom = config["clusterMaxZoom"] as? Double ?? 14
            try? mapView.mapboxMap.addSource(source)
        }

        guard let hasCount = Self.expression("[\"has\",\"point_count\"]"),
              let hasNoCount = Self.expression("[\"!\",[\"has\",\"point_count\"]]"),
              let radiusSteps = Self.expression("[\"step\",[\"get\",\"point_count\"],15,25,20,100,25]"),
              let countText = Self.expression("[\"get\",\"point_count_abbreviated\"]")
        else { return }

        for suffix in ["-clusters", "-cluster-count", "-points"]
        {
            try? mapView.mapboxMap.removeLayer(withId: id + suffix)
        }

        let clusterColor = StyleColor(UIColor(hex: config["clusterColor"] as? String ?? "") ?? .systemBlue)
        let textColor = StyleColor(UIColor(hex: config["clusterTextColor"] as? String ?? "") ?? .white)
        let pointColor = StyleColor(UIColor(hex: config["pointColor"] as? String ?? "") ?? .systemRed)
        let pointRadius = config["pointRadius"] as? Double ?? 6.0

        var points = CircleLayer(id: id + "-points", source: id)
        points.filter = hasNoCount
        points.circleColor = .constant(pointColor)
        points.circleRadius = .constant(pointRadius)
        try? mapView.mapboxMap.addLayer(points)

        var clusters = CircleLayer(id: id + "-clusters", source: id)
        clusters.filter = hasCount
        clusters.circleColor = .constant(clusterColor)
        clusters.circleOpacity = .constant(0.85)
        clusters.circleRadius = .expression(radiusSteps)
        try? mapView.mapboxMap.addLayer(clusters)

        var counts = SymbolLayer(id: id + "-cluster-count", source: id)
        counts.filter = hasCount
        counts.textField = .expression(countText)
        counts.textSize = .constant(12)
        counts.textColor = .constant(textColor)
        counts.textAllowOverlap = .constant(true)
        counts.textIgnorePlacement = .constant(true)
        try? mapView.mapboxMap.addLayer(counts)
    }

    /// Determines whether the tap hit a cluster circle; if so, eases the camera to the
    /// cluster's expansion zoom and reports the tap as consumed. The completion always
    /// runs asynchronously on the main queue — after any synchronous annotation
    /// tap handlers of the same gesture have stamped their timestamp.
    private func resolveClusterTap(at point: CGPoint, completion: @escaping (Bool) -> Void)
    {
        guard !clusterConfigs.isEmpty else
        {
            DispatchQueue.main.async { completion(false) }
            return
        }

        let layerIds = clusterConfigs.keys.map { "\($0)-clusters" }
        let options = RenderedQueryOptions(layerIds: Array(layerIds), filter: nil)

        mapView.mapboxMap.queryRenderedFeatures(with: point, options: options) { [weak self] result in
            guard let self,
                  case let .success(features) = result,
                  let queried = features.first?.queriedFeature
            else
            {
                DispatchQueue.main.async { completion(false) }
                return
            }

            let feature = queried.feature
            self.mapView.mapboxMap.getGeoJsonClusterExpansionZoom(
                forSourceId: queried.source, feature: feature)
            { [weak self] zoomResult in
                guard let self,
                      case let .success(extensionValue) = zoomResult,
                      let zoom = (extensionValue.value as? NSNumber)?.doubleValue,
                      case let .point(centerPoint) = feature.geometry
                else
                {
                    DispatchQueue.main.async { completion(false) }
                    return
                }

                self.mapView.camera.ease(
                    to: CameraOptions(center: centerPoint.coordinates, zoom: zoom + 0.5),
                    duration: 0.4)
                DispatchQueue.main.async { completion(true) }
            }
        }
    }

    private static func expression(_ json: String) -> Exp?
    {
        guard let data = json.data(using: .utf8) else { return nil }
        return try? JSONDecoder().decode(Exp.self, from: data)
    }

    private static func coordinates(from value: Any?) -> [CLLocationCoordinate2D]?
    {
        guard let points = value as? [[Double]] else { return nil }
        return points.compactMap
        {
            $0.count >= 2 ? CLLocationCoordinate2D(latitude: $0[0], longitude: $0[1]) : nil
        }
    }

    // MARK: - Location & gestures

    @objc(setUserLocationEnabled:)
    public func setUserLocationEnabled(_ enabled: Bool)
    {
        mapView.location.options.puckType = enabled ? .puck2D(.makeDefault(showBearing: true)) : nil
    }

    /// Enables/disables the follow-puck viewport mode. Enabling implicitly shows the
    /// location puck. The mode ends natively when the user pans the map — reported
    /// through onFollowPuckChanged.
    @objc(setFollowPuck:zoom:trackBearing:)
    public func setFollowPuck(enabled: Bool, zoom: Double, trackBearing: Bool)
    {
        if enabled
        {
            if mapView.location.options.puckType == nil
            {
                mapView.location.options.puckType = .puck2D(.makeDefault(showBearing: true))
            }

            let options = FollowPuckViewportStateOptions(
                zoom: zoom,
                bearing: trackBearing ? .heading : .constant(0),
                pitch: 0)
            let state = mapView.viewport.makeFollowPuckViewportState(options: options)
            mapView.viewport.transition(to: state)
        }
        else
        {
            mapView.viewport.idle()
        }
    }

    @objc(setGesturesScroll:zoom:rotate:pitch:)
    public func setGestures(scroll: Bool, zoom: Bool, rotate: Bool, pitch: Bool)
    {
        mapView.gestures.options.panEnabled = scroll
        mapView.gestures.options.pinchZoomEnabled = zoom
        mapView.gestures.options.doubleTapToZoomInEnabled = zoom
        mapView.gestures.options.doubleTouchToZoomOutEnabled = zoom
        mapView.gestures.options.quickZoomEnabled = zoom
        mapView.gestures.options.rotateEnabled = rotate
        mapView.gestures.options.pitchEnabled = pitch
    }

    // MARK: - Lifecycle

    @objc(destroy)
    public func destroy()
    {
        cancelables.removeAll()
        listener = nil
        pointManager = nil
        polylineManager = nil
        polygonManager = nil
        geoJsonSources.removeAll()
        layerConfigs.removeAll()
        clusterConfigs.removeAll()
        for annotation in viewAnnotationsById.values
        {
            annotation.remove()
        }
        viewAnnotationsById.removeAll()
        mapView.removeFromSuperview()
        mapView = nil
    }

    // MARK: - Helpers

    private var customIconCache: [String: UIImage] = [:]

    /// Decodes a base64 PNG/JPEG and scales it to the requested size in points
    /// (0 = natural pixel size treated as points, matching Android dp semantics).
    private func customIcon(base64: String, width: Double, height: Double) -> (image: UIImage, name: String)?
    {
        let name = "ik-custom-\(base64.hashValue)-\(Int(width))x\(Int(height))"
        if let cached = customIconCache[name]
        {
            return (cached, name)
        }

        guard let data = Data(base64Encoded: base64), let decoded = UIImage(data: data) else { return nil }

        let naturalW = decoded.size.width * decoded.scale
        let naturalH = decoded.size.height * decoded.scale
        let targetW = width > 0 ? width : Double(naturalW)
        let targetH = height > 0 ? height
            : Double(naturalH) * (width > 0 ? width / Double(naturalW) : 1.0)
        guard targetW > 0, targetH > 0 else { return nil }

        let size = CGSize(width: targetW, height: targetH)
        let scaled = UIGraphicsImageRenderer(size: size).image { _ in
            decoded.draw(in: CGRect(origin: .zero, size: size))
        }
        customIconCache[name] = scaled
        return (scaled, name)
    }

    private static var pinCache: [String: UIImage] = [:]

    private static func pinImage(hex: String) -> UIImage
    {
        if let cached = pinCache[hex] { return cached }

        let color = UIColor(hex: hex) ?? .systemRed
        let size = CGSize(width: 27, height: 41)
        let image = UIGraphicsImageRenderer(size: size).image { ctx in
            let c = ctx.cgContext
            // Teardrop: circle head + triangle tail
            let headRect = CGRect(x: 1.5, y: 1.5, width: 24, height: 24)
            c.setFillColor(color.cgColor)
            c.addEllipse(in: headRect)
            c.fillPath()
            c.move(to: CGPoint(x: 4.5, y: 20))
            c.addLine(to: CGPoint(x: 22.5, y: 20))
            c.addLine(to: CGPoint(x: 13.5, y: 41))
            c.closePath()
            c.setFillColor(color.cgColor)
            c.fillPath()
            // White inner dot
            c.setFillColor(UIColor.white.cgColor)
            c.addEllipse(in: CGRect(x: 9, y: 9, width: 9, height: 9))
            c.fillPath()
        }
        pinCache[hex] = image
        return image
    }
}

extension IKMapView: ViewportStatusObserver
{
    public func viewportStatusDidChange(from fromStatus: ViewportStatus, to toStatus: ViewportStatus,
                                        reason: ViewportStatusChangeReason)
    {
        let active = Self.isFollowPuck(toStatus)
        listener?.onFollowPuckChanged(active: active)
    }

    private static func isFollowPuck(_ status: ViewportStatus) -> Bool
    {
        switch status
        {
        case .state(let state):
            return state is FollowPuckViewportState
        case let .transition(_, toState):
            return toState is FollowPuckViewportState
        case .idle:
            return false
        }
    }
}

private extension UIColor
{
    convenience init?(hex: String)
    {
        var value = hex.trimmingCharacters(in: .whitespacesAndNewlines)
        if value.hasPrefix("#") { value.removeFirst() }
        guard value.count == 6, let rgb = UInt64(value, radix: 16) else { return nil }
        self.init(
            red: CGFloat((rgb & 0xFF0000) >> 16) / 255.0,
            green: CGFloat((rgb & 0x00FF00) >> 8) / 255.0,
            blue: CGFloat(rgb & 0x0000FF) / 255.0,
            alpha: 1.0)
    }
}
