package ch.indiko.mapbox

import android.content.Context
import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.Path
import android.widget.FrameLayout
import com.mapbox.common.MapboxOptions
import com.mapbox.common.NetworkRestriction
import com.mapbox.common.TileRegionLoadOptions
import com.mapbox.common.TileStore
import com.mapbox.geojson.Point
import com.mapbox.geojson.Feature
import com.mapbox.geojson.Polygon
import com.mapbox.maps.CameraOptions
import com.mapbox.maps.CoordinateBounds
import com.mapbox.maps.EdgeInsets
import com.mapbox.maps.GlyphsRasterizationMode
import com.mapbox.maps.MapInitOptions
import com.mapbox.maps.MapView
import com.mapbox.maps.OfflineManager
import com.mapbox.maps.StylePackLoadOptions
import com.mapbox.maps.TilesetDescriptorOptions
import com.mapbox.maps.RenderedQueryGeometry
import com.mapbox.maps.RenderedQueryOptions
import com.mapbox.maps.ViewAnnotationAnchor
import com.mapbox.maps.extension.style.expressions.generated.Expression
import com.mapbox.maps.extension.style.layers.addLayer
import com.mapbox.maps.extension.style.layers.addLayerBelow
import com.mapbox.maps.extension.style.layers.generated.CircleLayer
import com.mapbox.maps.extension.style.layers.generated.FillLayer
import com.mapbox.maps.extension.style.layers.generated.LineLayer
import com.mapbox.maps.extension.style.layers.generated.SymbolLayer
import com.mapbox.maps.extension.style.layers.properties.generated.IconAnchor
import com.mapbox.maps.extension.style.sources.addSource
import com.mapbox.maps.extension.style.sources.generated.GeoJsonSource
import com.mapbox.maps.extension.style.sources.generated.geoJsonSource
import com.mapbox.maps.extension.style.sources.getSourceAs
import com.mapbox.maps.plugin.animation.MapAnimationOptions
import com.mapbox.maps.plugin.animation.camera
import com.mapbox.maps.plugin.annotation.annotations
import com.mapbox.maps.plugin.annotation.generated.OnPointAnnotationDragListener
import com.mapbox.maps.plugin.annotation.generated.PointAnnotation
import com.mapbox.maps.plugin.annotation.generated.PointAnnotationManager
import com.mapbox.maps.plugin.annotation.generated.PointAnnotationOptions
import com.mapbox.maps.plugin.annotation.generated.PolygonAnnotationManager
import com.mapbox.maps.plugin.annotation.generated.PolygonAnnotationOptions
import com.mapbox.maps.plugin.annotation.generated.PolylineAnnotationManager
import com.mapbox.maps.plugin.annotation.generated.PolylineAnnotationOptions
import com.mapbox.maps.plugin.annotation.generated.createPointAnnotationManager
import com.mapbox.maps.plugin.annotation.generated.createPolygonAnnotationManager
import com.mapbox.maps.plugin.annotation.generated.createPolylineAnnotationManager
import com.mapbox.maps.plugin.gestures.addOnMapClickListener
import com.mapbox.maps.plugin.gestures.addOnMapLongClickListener
import com.mapbox.maps.plugin.gestures.gestures
import com.mapbox.maps.plugin.locationcomponent.createDefault2DPuck
import com.mapbox.maps.plugin.locationcomponent.location
import com.mapbox.maps.plugin.viewport.ViewportStatus
import com.mapbox.maps.plugin.viewport.ViewportStatusObserver
import com.mapbox.maps.plugin.viewport.data.FollowPuckViewportStateBearing
import com.mapbox.maps.plugin.viewport.data.FollowPuckViewportStateOptions
import com.mapbox.maps.plugin.viewport.state.FollowPuckViewportState
import com.mapbox.maps.plugin.viewport.viewport
import com.mapbox.maps.viewannotation.annotationAnchor
import com.mapbox.maps.viewannotation.geometry
import com.mapbox.maps.viewannotation.viewAnnotationOptions
import org.json.JSONArray
import org.json.JSONObject

/**
 * Global Mapbox configuration. Must be called before the first IKMapView is created.
 */
object IKMapbox
{
    @JvmStatic
    fun setAccessToken(token: String)
    {
        MapboxOptions.accessToken = token
    }
}

/**
 * Immutable snapshot of the current camera, handed to the event listener.
 */
class IKCameraState(
    val latitude: Double,
    val longitude: Double,
    val zoom: Double,
    val bearing: Double,
    val pitch: Double
)

/**
 * Events flowing from the native map back to the .NET handler.
 */
interface IKMapEventListener
{
    fun onMapReady()
    fun onStyleLoaded()
    fun onMapClick(latitude: Double, longitude: Double)
    fun onMapLongPress(latitude: Double, longitude: Double)
    fun onMarkerClick(id: String)
    fun onPolylineClick(id: String)
    fun onPolygonClick(id: String)
    fun onCameraChanged(camera: IKCameraState)
    fun onOfflineRegionProgress(id: String, progress: Double)
    fun onOfflineRegionCompleted(id: String, success: Boolean, error: String?)
    fun onFollowPuckChanged(active: Boolean)
    fun onMarkerDragEnd(id: String, latitude: Double, longitude: Double)
}

/**
 * Thin facade over com.mapbox.maps.MapView exposing exactly the surface the
 * Indiko.Maui.Controls.MapBox handler needs. Mirrors the iOS IKMapView API.
 */
class IKMapView(
    context: Context,
    styleUri: String,
    latitude: Double,
    longitude: Double,
    zoom: Double,
    bearing: Double,
    pitch: Double
) : FrameLayout(context)
{
    private val mapView: MapView
    private var pointManager: PointAnnotationManager? = null
    private var polylineManager: PolylineAnnotationManager? = null
    private var polygonManager: PolygonAnnotationManager? = null
    private val markerIdsByAnnotationId = HashMap<String, String>()
    private val polylineIdsByAnnotationId = HashMap<String, String>()
    private val polygonIdsByAnnotationId = HashMap<String, String>()

    // Registry of runtime sources/layers — Mapbox drops them on every style
    // reload, so they are re-applied after each StyleLoaded event.
    private val geoJsonSources = LinkedHashMap<String, String>()
    private val layerConfigs = LinkedHashMap<String, JSONObject>()
    private val clusterConfigs = LinkedHashMap<String, JSONObject>()

    // Click-consumed semantics: taps handled by an annotation or cluster must not
    // surface as onMapClick. Annotation click listeners stamp this timestamp; the
    // (deferred) map-click dispatch skips clicks that follow within the window.
    private var lastAnnotationTapMs = 0L

    private companion object
    {
        const val ANNOTATION_TAP_WINDOW_MS = 300L
    }

    var listener: IKMapEventListener? = null

    init
    {
        val camera = CameraOptions.Builder()
            .center(Point.fromLngLat(longitude, latitude))
            .zoom(zoom)
            .bearing(bearing)
            .pitch(pitch)
            .build()

        mapView = MapView(context, MapInitOptions(context = context, cameraOptions = camera, styleUri = styleUri))
        addView(mapView, LayoutParams(LayoutParams.MATCH_PARENT, LayoutParams.MATCH_PARENT))

        mapView.mapboxMap.subscribeMapLoaded { listener?.onMapReady() }
        mapView.mapboxMap.subscribeStyleLoaded {
            geoJsonSources.forEach { (id, geoJson) -> applyGeoJsonSource(id, geoJson) }
            layerConfigs.values.forEach { applyLayer(it) }
            clusterConfigs.values.forEach { applyClusteredSource(it) }
            listener?.onStyleLoaded()
        }
        mapView.mapboxMap.subscribeCameraChanged { event ->
            val state = event.cameraState
            listener?.onCameraChanged(
                IKCameraState(
                    state.center.latitude(),
                    state.center.longitude(),
                    state.zoom,
                    state.bearing,
                    state.pitch
                )
            )
        }

        mapView.gestures.addOnMapClickListener { point ->
            val latitude = point.latitude()
            val longitude = point.longitude()
            resolveClusterTap(point) { consumedByCluster ->
                // Posted to the main queue after the synchronous gesture dispatch, so
                // annotation click listeners of the same tap have already stamped.
                post {
                    val annotationConsumed =
                        android.os.SystemClock.uptimeMillis() - lastAnnotationTapMs < ANNOTATION_TAP_WINDOW_MS
                    if (!consumedByCluster && !annotationConsumed)
                    {
                        listener?.onMapClick(latitude, longitude)
                    }
                }
            }
            false
        }
        mapView.gestures.addOnMapLongClickListener { point ->
            val latitude = point.latitude()
            val longitude = point.longitude()
            // Deferred like onMapClick: a long-press that starts an annotation drag
            // stamps lastAnnotationTapMs and must not surface as a map long-press.
            post {
                val annotationConsumed =
                    android.os.SystemClock.uptimeMillis() - lastAnnotationTapMs < ANNOTATION_TAP_WINDOW_MS
                if (!annotationConsumed)
                {
                    listener?.onMapLongPress(latitude, longitude)
                }
            }
            false
        }

        mapView.viewport.addStatusObserver(ViewportStatusObserver { _, to, _ ->
            val active = isFollowPuck(to)
            post { listener?.onFollowPuckChanged(active) }
        })
    }

    private fun isFollowPuck(status: ViewportStatus): Boolean = when (status)
    {
        is ViewportStatus.State -> status.state is FollowPuckViewportState
        is ViewportStatus.Transition -> status.toState is FollowPuckViewportState
        else -> false
    }

    // region Style & camera

    fun setStyleUri(uri: String)
    {
        mapView.mapboxMap.loadStyle(uri)
    }

    fun setCamera(latitude: Double, longitude: Double, zoom: Double, bearing: Double, pitch: Double)
    {
        mapView.mapboxMap.setCamera(
            CameraOptions.Builder()
                .center(Point.fromLngLat(longitude, latitude))
                .zoom(zoom)
                .bearing(bearing)
                .pitch(pitch)
                .build()
        )
    }

    fun flyTo(latitude: Double, longitude: Double, zoom: Double, bearing: Double, pitch: Double, durationMs: Double)
    {
        mapView.camera.flyTo(
            CameraOptions.Builder()
                .center(Point.fromLngLat(longitude, latitude))
                .zoom(zoom)
                .bearing(bearing)
                .pitch(pitch)
                .build(),
            MapAnimationOptions.mapAnimationOptions { duration(durationMs.toLong()) }
        )
    }

    /**
     * Moves the camera so the given bounding box is fully visible, with uniform
     * padding in dp. durationMs 0 jumps instantly.
     */
    fun fitBounds(minLat: Double, minLng: Double, maxLat: Double, maxLng: Double,
                  paddingDp: Double, durationMs: Double)
    {
        val paddingPx = (paddingDp * resources.displayMetrics.density)
        val camera = mapView.mapboxMap.cameraForCoordinateBounds(
            CoordinateBounds(
                Point.fromLngLat(minLng, minLat),
                Point.fromLngLat(maxLng, maxLat)
            ),
            EdgeInsets(paddingPx, paddingPx, paddingPx, paddingPx),
            null,
            null
        )

        if (durationMs <= 0)
        {
            mapView.mapboxMap.setCamera(camera)
        }
        else
        {
            mapView.camera.flyTo(
                camera,
                MapAnimationOptions.mapAnimationOptions { duration(durationMs.toLong()) }
            )
        }
    }

    // endregion

    // region Markers

    /**
     * Replaces all markers. JSON: [{"id":"...","lat":..,"lng":..,"title":"...","color":"#RRGGBB"}]
     */
    fun setMarkersJson(json: String)
    {
        val manager = pointManager ?: mapView.annotations.createPointAnnotationManager().also { created ->
            created.addClickListener { annotation ->
                lastAnnotationTapMs = android.os.SystemClock.uptimeMillis()
                markerIdsByAnnotationId[annotation.id]?.let { listener?.onMarkerClick(it) }
                true
            }
            created.addDragListener(object : OnPointAnnotationDragListener {
                override fun onAnnotationDragStarted(annotation: com.mapbox.maps.plugin.annotation.Annotation<*>)
                {
                    lastAnnotationTapMs = android.os.SystemClock.uptimeMillis()
                }

                override fun onAnnotationDrag(annotation: com.mapbox.maps.plugin.annotation.Annotation<*>)
                {
                }

                override fun onAnnotationDragFinished(annotation: com.mapbox.maps.plugin.annotation.Annotation<*>)
                {
                    lastAnnotationTapMs = android.os.SystemClock.uptimeMillis()
                    val dragged = annotation as? PointAnnotation ?: return
                    markerIdsByAnnotationId[dragged.id]?.let {
                        listener?.onMarkerDragEnd(it, dragged.point.latitude(), dragged.point.longitude())
                    }
                }
            })
            pointManager = created
        }

        manager.deleteAll()
        markerIdsByAnnotationId.clear()

        val items = JSONArray(json)
        for (i in 0 until items.length())
        {
            val item = items.getJSONObject(i)
            val id = item.optString("id") ?: continue
            val lat = item.optDouble("lat", Double.NaN)
            val lng = item.optDouble("lng", Double.NaN)
            if (lat.isNaN() || lng.isNaN()) continue

            val color = item.optString("color", "#E74C3C")
            val iconBase64 = item.optString("icon")
            val bitmap =
                if (iconBase64.isNotEmpty())
                    customIconBitmap(iconBase64, item.optDouble("iconWidth", 0.0), item.optDouble("iconHeight", 0.0))
                else
                    pinBitmap(color)
            val anchor =
                if (item.optString("anchor") == "center") IconAnchor.CENTER else IconAnchor.BOTTOM

            val options = PointAnnotationOptions()
                .withPoint(Point.fromLngLat(lng, lat))
                .withIconImage(bitmap ?: pinBitmap(color))
                .withIconAnchor(anchor)
                .withDraggable(item.optBoolean("draggable", false))

            val annotation = manager.create(options)
            markerIdsByAnnotationId[annotation.id] = id
        }
    }

    fun clearMarkers()
    {
        pointManager?.deleteAll()
        markerIdsByAnnotationId.clear()
    }

    // endregion

    // region Polylines & polygons

    /**
     * Replaces all polylines. JSON:
     * [{"id":"...","points":[[lat,lng],...],"color":"#RRGGBB","width":4.0,"opacity":1.0}]
     */
    fun setPolylinesJson(json: String)
    {
        val manager = polylineManager ?: mapView.annotations.createPolylineAnnotationManager().also { created ->
            created.addClickListener { annotation ->
                lastAnnotationTapMs = android.os.SystemClock.uptimeMillis()
                polylineIdsByAnnotationId[annotation.id]?.let { listener?.onPolylineClick(it) }
                true
            }
            polylineManager = created
        }

        manager.deleteAll()
        polylineIdsByAnnotationId.clear()

        val items = JSONArray(json)
        for (i in 0 until items.length())
        {
            val item = items.getJSONObject(i)
            val id = item.optString("id") ?: continue
            val points = parsePoints(item.optJSONArray("points")) ?: continue
            if (points.size < 2) continue

            val options = PolylineAnnotationOptions()
                .withPoints(points)
                .withLineColor(item.optString("color", "#3B82F6"))
                .withLineWidth(item.optDouble("width", 4.0))
                .withLineOpacity(item.optDouble("opacity", 1.0))

            val annotation = manager.create(options)
            polylineIdsByAnnotationId[annotation.id] = id
        }
    }

    fun clearPolylines()
    {
        polylineManager?.deleteAll()
        polylineIdsByAnnotationId.clear()
    }

    /**
     * Replaces all polygons. JSON:
     * [{"id":"...","points":[[lat,lng],...],"fillColor":"#RRGGBB","fillOpacity":0.4,"strokeColor":"#RRGGBB"}]
     */
    fun setPolygonsJson(json: String)
    {
        val manager = polygonManager ?: mapView.annotations.createPolygonAnnotationManager().also { created ->
            created.addClickListener { annotation ->
                lastAnnotationTapMs = android.os.SystemClock.uptimeMillis()
                polygonIdsByAnnotationId[annotation.id]?.let { listener?.onPolygonClick(it) }
                true
            }
            polygonManager = created
        }

        manager.deleteAll()
        polygonIdsByAnnotationId.clear()

        val items = JSONArray(json)
        for (i in 0 until items.length())
        {
            val item = items.getJSONObject(i)
            val id = item.optString("id") ?: continue
            val points = parsePoints(item.optJSONArray("points")) ?: continue
            if (points.size < 3) continue

            val options = PolygonAnnotationOptions()
                .withPoints(listOf(points))
                .withFillColor(item.optString("fillColor", "#3B82F6"))
                .withFillOpacity(item.optDouble("fillOpacity", 0.4))
                .withFillOutlineColor(item.optString("strokeColor", "#1D4ED8"))

            val annotation = manager.create(options)
            polygonIdsByAnnotationId[annotation.id] = id
        }
    }

    fun clearPolygons()
    {
        polygonManager?.deleteAll()
        polygonIdsByAnnotationId.clear()
    }

    // region GeoJSON sources & layers

    /**
     * Adds a GeoJSON source or replaces the data of an existing one.
     */
    fun addGeoJsonSource(id: String, geoJson: String)
    {
        val clusterConfig = clusterConfigs[id]
        if (clusterConfig != null)
        {
            // Clustered source: only its data is replaced, cluster config stays.
            clusterConfig.put("geoJson", geoJson)
        }
        else
        {
            geoJsonSources[id] = geoJson
        }
        applyGeoJsonSource(id, geoJson)
    }

    fun removeGeoJsonSource(id: String)
    {
        geoJsonSources.remove(id)
        mapView.mapboxMap.style?.removeStyleSource(id)
    }

    /**
     * Adds (or replaces) a style layer. JSON:
     * {"id","sourceId","type":"fill|line|circle","color","opacity","lineWidth","circleRadius","belowLayerId"}
     */
    fun addLayerJson(json: String)
    {
        val config = JSONObject(json)
        val id = config.optString("id")
        if (id.isEmpty()) return

        layerConfigs.remove(id)
        layerConfigs[id] = config
        applyLayer(config)
    }

    fun removeLayer(id: String)
    {
        layerConfigs.remove(id)
        mapView.mapboxMap.style?.removeStyleLayer(id)
    }

    private fun applyGeoJsonSource(id: String, geoJson: String)
    {
        val style = mapView.mapboxMap.style ?: return
        if (style.styleSourceExists(id))
        {
            style.getSourceAs<GeoJsonSource>(id)?.data(geoJson)
        }
        else
        {
            style.addSource(geoJsonSource(id) { data(geoJson) })
        }
    }

    private fun applyLayer(config: JSONObject)
    {
        val style = mapView.mapboxMap.style ?: return
        val id = config.optString("id")
        val sourceId = config.optString("sourceId")
        if (id.isEmpty() || sourceId.isEmpty()) return

        if (style.styleLayerExists(id))
        {
            style.removeStyleLayer(id)
        }

        val color = config.optString("color", "#3B82F6")
        val opacity = config.optDouble("opacity", 1.0)

        val layer = when (config.optString("type"))
        {
            "fill" -> FillLayer(id, sourceId)
                .fillColor(color)
                .fillOpacity(opacity)
            "line" -> LineLayer(id, sourceId)
                .lineColor(color)
                .lineOpacity(opacity)
                .lineWidth(config.optDouble("lineWidth", 3.0))
            "circle" -> CircleLayer(id, sourceId)
                .circleColor(color)
                .circleOpacity(opacity)
                .circleRadius(config.optDouble("circleRadius", 6.0))
            else -> return
        }

        val below = config.optString("belowLayerId")
        if (below.isNotEmpty())
        {
            style.addLayerBelow(layer, below)
        }
        else
        {
            style.addLayer(layer)
        }
    }

    // endregion

    // region View annotations

    private val viewAnnotationViews = HashMap<String, android.view.View>()

    /**
     * Anchors a native view (bottom-center) to a coordinate. The caller provides the
     * fixed size in dp; an existing annotation with the same id is replaced.
     */
    fun addViewAnnotation(id: String, view: android.view.View, latitude: Double, longitude: Double,
                          widthDp: Double, heightDp: Double)
    {
        removeViewAnnotation(id)

        val density = resources.displayMetrics.density
        if (view.layoutParams == null)
        {
            // Mapbox's ViewAnnotationManager casts layoutParams unconditionally —
            // freshly created (e.g. MAUI) views don't have any yet.
            view.layoutParams = LayoutParams((widthDp * density).toInt(), (heightDp * density).toInt())
        }
        mapView.viewAnnotationManager.addViewAnnotation(view, viewAnnotationOptions {
            geometry(Point.fromLngLat(longitude, latitude))
            width(widthDp * density)
            height(heightDp * density)
            allowOverlap(true)
            annotationAnchor {
                anchor(ViewAnnotationAnchor.BOTTOM)
            }
        })
        viewAnnotationViews[id] = view
    }

    fun removeViewAnnotation(id: String)
    {
        viewAnnotationViews.remove(id)?.let { mapView.viewAnnotationManager.removeViewAnnotation(it) }
    }

    // endregion

    // region Offline regions

    private val offlineManager by lazy { OfflineManager() }
    private val tileStore by lazy { TileStore.create() }

    /**
     * Downloads a style pack plus the tile region for a bounding box. JSON:
     * {"id","styleUri","minZoom","maxZoom","minLat","minLng","maxLat","maxLng"}
     * Progress and completion are reported through the listener.
     */
    fun downloadOfflineRegionJson(json: String)
    {
        val config = JSONObject(json)
        val id = config.optString("id")
        if (id.isEmpty()) return

        val styleUri = config.optString("styleUri", "mapbox://styles/mapbox/streets-v12")
        val minZoom = config.optInt("minZoom", 6).toByte()
        val maxZoom = config.optInt("maxZoom", 14).toByte()
        val minLat = config.optDouble("minLat")
        val minLng = config.optDouble("minLng")
        val maxLat = config.optDouble("maxLat")
        val maxLng = config.optDouble("maxLng")
        if (minLat.isNaN() || minLng.isNaN() || maxLat.isNaN() || maxLng.isNaN()) return

        // 1. Style pack (style JSON, sprites, glyphs) — required for offline rendering.
        offlineManager.loadStylePack(
            styleUri,
            StylePackLoadOptions.Builder()
                .glyphsRasterizationMode(GlyphsRasterizationMode.IDEOGRAPHS_RASTERIZED_LOCALLY)
                .build(),
            { /* style pack progress ignored */ },
            { /* completion reported via the tile region below */ }
        )

        // 2. Tile region for the bounding box.
        val descriptor = offlineManager.createTilesetDescriptor(
            TilesetDescriptorOptions.Builder()
                .styleURI(styleUri)
                .minZoom(minZoom)
                .maxZoom(maxZoom)
                .build()
        )

        val ring = listOf(
            com.mapbox.geojson.Point.fromLngLat(minLng, minLat),
            com.mapbox.geojson.Point.fromLngLat(maxLng, minLat),
            com.mapbox.geojson.Point.fromLngLat(maxLng, maxLat),
            com.mapbox.geojson.Point.fromLngLat(minLng, maxLat),
            com.mapbox.geojson.Point.fromLngLat(minLng, minLat),
        )

        tileStore.loadTileRegion(
            id,
            TileRegionLoadOptions.Builder()
                .geometry(Polygon.fromLngLats(listOf(ring)))
                .descriptors(listOf(descriptor))
                .acceptExpired(true)
                .networkRestriction(NetworkRestriction.NONE)
                .build(),
            { progress ->
                val fraction =
                    if (progress.requiredResourceCount > 0)
                        progress.completedResourceCount.toDouble() / progress.requiredResourceCount
                    else 0.0
                post { listener?.onOfflineRegionProgress(id, fraction) }
            }
        ) { expected ->
            post {
                if (expected.isValue)
                {
                    listener?.onOfflineRegionCompleted(id, true, null)
                }
                else
                {
                    listener?.onOfflineRegionCompleted(id, false, expected.error?.message)
                }
            }
        }
    }

    fun removeOfflineRegion(id: String)
    {
        tileStore.removeTileRegion(id)
    }

    // endregion

    // region Clustering

    /**
     * Adds a clustered GeoJSON source plus three managed layers
     * ("<id>-clusters", "<id>-cluster-count", "<id>-points"). JSON:
     * {"sourceId","geoJson","clusterRadius","clusterMaxZoom","clusterColor",
     *  "clusterTextColor","pointColor","pointRadius"}
     */
    fun addClusteredSourceJson(json: String)
    {
        val config = JSONObject(json)
        val id = config.optString("sourceId")
        if (id.isEmpty()) return

        clusterConfigs[id] = config
        applyClusteredSource(config)
    }

    fun removeClusteredSource(id: String)
    {
        clusterConfigs.remove(id)
        val style = mapView.mapboxMap.style ?: return
        for (suffix in listOf("-clusters", "-cluster-count", "-points"))
        {
            style.removeStyleLayer(id + suffix)
        }
        style.removeStyleSource(id)
    }

    private fun applyClusteredSource(config: JSONObject)
    {
        val style = mapView.mapboxMap.style ?: return
        val id = config.optString("sourceId")
        val geoJson = config.optString("geoJson")
        if (id.isEmpty() || geoJson.isEmpty()) return

        if (style.styleSourceExists(id))
        {
            style.getSourceAs<GeoJsonSource>(id)?.data(geoJson)
        }
        else
        {
            style.addSource(geoJsonSource(id) {
                data(geoJson)
                cluster(true)
                clusterRadius(config.optLong("clusterRadius", 50))
                clusterMaxZoom(config.optLong("clusterMaxZoom", 14))
            })
        }

        for (suffix in listOf("-clusters", "-cluster-count", "-points"))
        {
            if (style.styleLayerExists(id + suffix))
            {
                style.removeStyleLayer(id + suffix)
            }
        }

        val hasCount = Expression.fromRaw("[\"has\",\"point_count\"]")
        val hasNoCount = Expression.fromRaw("[\"!\",[\"has\",\"point_count\"]]")
        val radiusSteps = Expression.fromRaw("[\"step\",[\"get\",\"point_count\"],15,25,20,100,25]")
        val countText = Expression.fromRaw("[\"get\",\"point_count_abbreviated\"]")

        style.addLayer(
            CircleLayer(id + "-points", id)
                .filter(hasNoCount)
                .circleColor(config.optString("pointColor", "#E74C3C"))
                .circleRadius(config.optDouble("pointRadius", 6.0))
        )
        style.addLayer(
            CircleLayer(id + "-clusters", id)
                .filter(hasCount)
                .circleColor(config.optString("clusterColor", "#3B82F6"))
                .circleOpacity(0.85)
                .circleRadius(radiusSteps)
        )
        style.addLayer(
            SymbolLayer(id + "-cluster-count", id)
                .filter(hasCount)
                .textField(countText)
                .textSize(12.0)
                .textColor(config.optString("clusterTextColor", "#FFFFFF"))
                .textAllowOverlap(true)
                .textIgnorePlacement(true)
        )
    }

    /**
     * Determines whether the tap hit a cluster circle; if so, eases the camera to the
     * cluster's expansion zoom and reports the tap as consumed via [completion].
     */
    private fun resolveClusterTap(point: Point, completion: (Boolean) -> Unit)
    {
        if (clusterConfigs.isEmpty())
        {
            completion(false)
            return
        }

        val layerIds = clusterConfigs.keys.map { "$it-clusters" }
        val screen = mapView.mapboxMap.pixelForCoordinate(point)

        mapView.mapboxMap.queryRenderedFeatures(
            RenderedQueryGeometry(screen),
            RenderedQueryOptions(layerIds, null)
        ) { queryResult ->
            val queried = queryResult.value?.firstOrNull()?.queriedFeature
            val feature: Feature? = queried?.feature
            val center = feature?.geometry() as? Point
            if (queried == null || feature == null || center == null)
            {
                completion(false)
                return@queryRenderedFeatures
            }

            mapView.mapboxMap.getGeoJsonClusterExpansionZoom(queried.source, feature) { zoomResult ->
                val contents = zoomResult.value?.value?.contents
                val zoom = when (contents)
                {
                    is Double -> contents
                    is Long -> contents.toDouble()
                    else -> null
                }
                if (zoom == null)
                {
                    completion(false)
                    return@getGeoJsonClusterExpansionZoom
                }

                post {
                    mapView.camera.easeTo(
                        CameraOptions.Builder().center(center).zoom(zoom + 0.5).build(),
                        MapAnimationOptions.mapAnimationOptions { duration(400) }
                    )
                }
                completion(true)
            }
        }
    }

    // endregion

    private fun parsePoints(array: org.json.JSONArray?): List<Point>?
    {
        if (array == null) return null
        val points = ArrayList<Point>(array.length())
        for (i in 0 until array.length())
        {
            val pair = array.optJSONArray(i) ?: continue
            if (pair.length() < 2) continue
            points.add(Point.fromLngLat(pair.getDouble(1), pair.getDouble(0)))
        }
        return points
    }

    // endregion

    // region Location & gestures

    fun setUserLocationEnabled(enabled: Boolean)
    {
        mapView.location.updateSettings {
            this.enabled = enabled
            if (enabled)
            {
                locationPuck = createDefault2DPuck(withBearing = true)
            }
        }
    }

    /**
     * Enables/disables the follow-puck viewport mode. Enabling implicitly shows the
     * location puck. The mode ends natively when the user pans the map — reported
     * through onFollowPuckChanged.
     */
    fun setFollowPuck(enabled: Boolean, zoom: Double, trackBearing: Boolean)
    {
        if (enabled)
        {
            mapView.location.updateSettings {
                this.enabled = true
                locationPuck = createDefault2DPuck(withBearing = true)
            }

            val state = mapView.viewport.makeFollowPuckViewportState(
                FollowPuckViewportStateOptions.Builder()
                    .zoom(zoom)
                    .bearing(
                        if (trackBearing) FollowPuckViewportStateBearing.SyncWithLocationPuck
                        else FollowPuckViewportStateBearing.Constant(0.0)
                    )
                    .pitch(0.0)
                    .build()
            )
            mapView.viewport.transitionTo(state)
        }
        else
        {
            mapView.viewport.idle()
        }
    }

    fun setGestures(scroll: Boolean, zoom: Boolean, rotate: Boolean, pitch: Boolean)
    {
        mapView.gestures.updateSettings {
            scrollEnabled = scroll
            pinchToZoomEnabled = zoom
            doubleTapToZoomInEnabled = zoom
            doubleTouchToZoomOutEnabled = zoom
            quickZoomEnabled = zoom
            rotateEnabled = rotate
            pitchEnabled = pitch
        }
    }

    // endregion

    // region Lifecycle & helpers

    fun destroy()
    {
        listener = null
        mapView.viewAnnotationManager.removeAllViewAnnotations()
        viewAnnotationViews.clear()
        pointManager = null
        polylineManager = null
        polygonManager = null
        markerIdsByAnnotationId.clear()
        polylineIdsByAnnotationId.clear()
        polygonIdsByAnnotationId.clear()
        removeAllViews()
        mapView.onStop()
        mapView.onDestroy()
    }

    private val customIconCache = HashMap<String, Bitmap>()

    /**
     * Decodes a base64 PNG/JPEG and scales it to the requested size in dp
     * (0 = natural pixel size treated as dp, matching iOS point semantics).
     */
    private fun customIconBitmap(base64: String, widthDp: Double, heightDp: Double): Bitmap?
    {
        val key = "${base64.hashCode()}-${widthDp}x$heightDp"
        customIconCache[key]?.let { return it }

        val bytes = try { android.util.Base64.decode(base64, android.util.Base64.DEFAULT) }
        catch (_: IllegalArgumentException) { return null }
        val decoded = android.graphics.BitmapFactory.decodeByteArray(bytes, 0, bytes.size) ?: return null

        val density = resources.displayMetrics.density
        val targetW = ((if (widthDp > 0) widthDp else decoded.width.toDouble()) * density).toInt()
        val targetH = ((if (heightDp > 0) heightDp
            else decoded.height.toDouble() * (if (widthDp > 0) widthDp / decoded.width else 1.0)) * density).toInt()
        if (targetW <= 0 || targetH <= 0) return null

        val scaled = Bitmap.createScaledBitmap(decoded, targetW, targetH, true)
        customIconCache[key] = scaled
        return scaled
    }

    private val pinCache = HashMap<String, Bitmap>()

    private fun pinBitmap(colorHex: String): Bitmap
    {
        pinCache[colorHex]?.let { return it }

        val color = try { Color.parseColor(colorHex) } catch (_: IllegalArgumentException) { Color.rgb(231, 76, 60) }
        val density = resources.displayMetrics.density
        val width = (27 * density).toInt()
        val height = (41 * density).toInt()
        val bitmap = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888)
        val canvas = Canvas(bitmap)
        val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply { this.color = color }

        // Teardrop: circle head + triangle tail
        val headRadius = 12f * density
        val centerX = width / 2f
        val headCenterY = 13.5f * density
        canvas.drawCircle(centerX, headCenterY, headRadius, paint)

        val path = Path().apply {
            moveTo(4.5f * density, 20f * density)
            lineTo(22.5f * density, 20f * density)
            lineTo(centerX, 41f * density)
            close()
        }
        canvas.drawPath(path, paint)

        // White inner dot
        paint.color = Color.WHITE
        canvas.drawCircle(centerX, headCenterY, 4.5f * density, paint)

        pinCache[colorHex] = bitmap
        return bitmap
    }

    // endregion
}
