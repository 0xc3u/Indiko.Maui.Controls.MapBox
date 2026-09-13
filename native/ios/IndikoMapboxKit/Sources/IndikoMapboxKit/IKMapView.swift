import UIKit
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
            self?.handleClusterTap(at: context.point)
            self?.listener?.onMapClick(
                latitude: context.coordinate.latitude,
                longitude: context.coordinate.longitude)
        }.store(in: &cancelables)

        mapView.gestures.onMapLongPress.observe { [weak self] context in
            self?.listener?.onMapLongPress(
                latitude: context.coordinate.latitude,
                longitude: context.coordinate.longitude)
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
            annotation.image = .init(image: Self.pinImage(hex: colorHex), name: "ik-pin-\(colorHex)")
            annotation.iconAnchor = .bottom
            annotation.tapHandler = { [weak self] _ in
                self?.listener?.onMarkerClick(id: id)
                return true
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
        default:
            return
        }

        let position = (config["belowLayerId"] as? String).map { LayerPosition.below($0) }
        try? mapView.mapboxMap.addLayer(layer, layerPosition: position)
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

    /// Tap on a cluster circle eases the camera to the cluster's expansion zoom.
    private func handleClusterTap(at point: CGPoint)
    {
        guard !clusterConfigs.isEmpty else { return }

        let layerIds = clusterConfigs.keys.map { "\($0)-clusters" }
        let options = RenderedQueryOptions(layerIds: Array(layerIds), filter: nil)

        mapView.mapboxMap.queryRenderedFeatures(with: point, options: options) { [weak self] result in
            guard let self,
                  case let .success(features) = result,
                  let queried = features.first?.queriedFeature
            else { return }

            let feature = queried.feature
            self.mapView.mapboxMap.getGeoJsonClusterExpansionZoom(
                forSourceId: queried.source, feature: feature)
            { [weak self] zoomResult in
                guard let self,
                      case let .success(extensionValue) = zoomResult,
                      let zoom = (extensionValue.value as? NSNumber)?.doubleValue,
                      case let .point(centerPoint) = feature.geometry
                else { return }

                self.mapView.camera.ease(
                    to: CameraOptions(center: centerPoint.coordinates, zoom: zoom + 0.5),
                    duration: 0.4)
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
        mapView.removeFromSuperview()
        mapView = nil
    }

    // MARK: - Helpers

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
