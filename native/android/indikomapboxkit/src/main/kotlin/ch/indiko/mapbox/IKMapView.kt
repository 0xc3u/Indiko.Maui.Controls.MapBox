package ch.indiko.mapbox

import android.annotation.SuppressLint
import android.view.Gravity
import android.content.Context
import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.Path
import android.os.Handler
import android.os.Looper
import android.view.View
import android.widget.FrameLayout
import androidx.lifecycle.DefaultLifecycleObserver
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleOwner
import androidx.lifecycle.LifecycleRegistry
import androidx.lifecycle.ViewTreeLifecycleOwner
import com.mapbox.bindgen.Value
import com.mapbox.common.Cancelable
import com.mapbox.common.MapboxOptions
import com.mapbox.common.NetworkRestriction
import com.mapbox.common.TelemetryUtils
import com.mapbox.common.TileRegionLoadOptions
import com.mapbox.common.TileStore
import com.mapbox.geojson.Point
import com.mapbox.geojson.Feature
import com.mapbox.geojson.Polygon
import com.mapbox.maps.CameraBoundsOptions
import com.mapbox.maps.extension.localization.localizeLabels
import com.mapbox.maps.CameraOptions
import com.mapbox.maps.CoordinateBounds
import com.mapbox.maps.EdgeInsets
import com.mapbox.maps.GlyphsRasterizationMode
import com.mapbox.maps.MapInitOptions
import com.mapbox.maps.MapView
import com.mapbox.maps.OfflineManager
import com.mapbox.maps.Style
import com.mapbox.maps.StylePackLoadOptions
import com.mapbox.maps.TilesetDescriptorOptions
import com.mapbox.maps.RenderedQueryGeometry
import com.mapbox.maps.RenderedQueryOptions
import com.mapbox.maps.ScreenBox
import com.mapbox.maps.ScreenCoordinate
import com.mapbox.maps.ViewAnnotationAnchor
import com.mapbox.maps.extension.style.expressions.generated.Expression
import com.mapbox.maps.extension.style.layers.addLayer
import com.mapbox.maps.extension.style.layers.addLayerBelow
import com.mapbox.maps.extension.style.layers.generated.CircleLayer
import com.mapbox.maps.extension.style.layers.generated.HeatmapLayer
import com.mapbox.maps.extension.style.atmosphere.generated.removeAtmosphere
import com.mapbox.maps.extension.style.layers.generated.FillExtrusionLayer
import com.mapbox.maps.extension.style.layers.generated.FillLayer
import com.mapbox.maps.extension.style.layers.generated.HillshadeLayer
import com.mapbox.maps.extension.style.layers.generated.LineLayer
import com.mapbox.maps.extension.style.layers.generated.RasterLayer
import com.mapbox.maps.extension.style.layers.generated.SymbolLayer
import com.mapbox.maps.extension.style.layers.generated.skyLayer
import com.mapbox.maps.extension.style.layers.properties.generated.SkyType
import com.mapbox.maps.extension.style.layers.properties.generated.IconAnchor
import com.mapbox.maps.extension.style.layers.properties.generated.TextAnchor
import com.mapbox.maps.extension.style.sources.addSource
import com.mapbox.maps.extension.style.sources.generated.GeoJsonSource
import com.mapbox.maps.extension.style.sources.generated.geoJsonSource
import com.mapbox.maps.extension.style.sources.generated.rasterDemSource
import com.mapbox.maps.extension.style.sources.getSourceAs
import com.mapbox.maps.extension.style.terrain.generated.removeTerrain
import com.mapbox.maps.extension.style.terrain.generated.terrain
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
import com.mapbox.maps.plugin.attribution.attribution
import com.mapbox.maps.plugin.compass.compass
import com.mapbox.maps.plugin.gestures.addOnMapClickListener
import com.mapbox.maps.plugin.gestures.addOnMapLongClickListener
import com.mapbox.maps.plugin.gestures.gestures
import com.mapbox.maps.plugin.logo.logo
import com.mapbox.maps.plugin.scalebar.scalebar
import com.mapbox.maps.toCameraOptions
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
import java.util.Locale
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

    /**
     * Turns Mapbox's location telemetry on or off. Disabled means the SDK stops
     * contributing device locations and map-usage events to Mapbox; only the
     * billing turnstile event remains. This is the same switch the attribution
     * dialog offers under "Make Mapbox Maps Better".
     */
    @JvmStatic
    @SuppressLint("RestrictedApi")
    fun setTelemetryEnabled(enabled: Boolean)
    {
        TelemetryUtils.setEventsCollectionState(enabled, null)
    }

    @JvmStatic
    @SuppressLint("RestrictedApi")
    fun isTelemetryEnabled(): Boolean = TelemetryUtils.getEventsCollectionState()

    /**
     * Lists every offline tile region on the device, independent of any map view.
     * The callback receives a JSON array (on an arbitrary thread):
     * [{"id","requiredResourceCount","completedResourceCount","completedResourceSize",
     *   "expires" (epoch ms or null),"metadata" (the region JSON or null)}]
     * On failure the array is empty.
     */
    @JvmStatic
    fun listOfflineRegions(callback: IKStringCallback)
    {
        val tileStore = TileStore.create()
        tileStore.getAllTileRegions { expected ->
            val regions = expected.value
            if (regions == null || regions.isEmpty())
            {
                callback.onResult("[]")
                return@getAllTileRegions
            }

            // Metadata is fetched per region; the callback fires once all are in.
            val results = arrayOfNulls<JSONObject>(regions.size)
            val remaining = java.util.concurrent.atomic.AtomicInteger(regions.size)
            regions.forEachIndexed { index, region ->
                tileStore.getTileRegionMetadata(region.id) { metadata ->
                    val entry = JSONObject()
                        .put("id", region.id)
                        .put("requiredResourceCount", region.requiredResourceCount)
                        .put("completedResourceCount", region.completedResourceCount)
                        .put("completedResourceSize", region.completedResourceSize)
                        .put("expires", region.expires?.time ?: JSONObject.NULL)
                    val json = metadata.value?.toJson()
                    entry.put("metadata", json?.let { runCatching { JSONObject(it) }.getOrNull() } ?: JSONObject.NULL)
                    results[index] = entry

                    if (remaining.decrementAndGet() == 0)
                    {
                        val array = JSONArray()
                        results.forEach { if (it != null) array.put(it) }
                        callback.onResult(array.toString())
                    }
                }
            }
        }
    }

    /**
     * Removes an offline tile region by id, independent of any map view. Does not
     * cancel a download still running in a map view — use IKMapView.removeOfflineRegion
     * for that. Tiles shared with other regions stay on disk.
     */
    @JvmStatic
    fun removeOfflineRegion(id: String)
    {
        TileStore.create().removeTileRegion(id)
    }
}

/**
 * Single-string result callback for asynchronous static calls (JSON payloads).
 */
interface IKStringCallback
{
    fun onResult(json: String)
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

    /**
     * A resource the map needed could not be loaded. [type] is one of
     * "style", "sprite", "source", "tile", "glyphs" or "unknown"; [sourceId] is set for
     * source and tile errors. Tile errors fire once per failed tile, so they can arrive in
     * bursts — offline, every tile outside a downloaded region produces one.
     */
    fun onMapLoadError(type: String, message: String, sourceId: String?)
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

    // Our own lifecycle, deliberately NOT the one from the view tree. In MAUI the
    // ViewTreeLifecycleOwner is the Activity, which outlives the page by far — the
    // Mapbox lifecycle plugin would then never see ON_DESTROY, so the map had to be
    // torn down by hand, and a hand-destroyed MapView can still be restarted by a
    // later attach. That is a permanently black map: a Mapbox MapView is strictly
    // single-use, its render thread cannot be started again after onDestroy.
    private val lifecycleOwner: LifecycleOwner = object : LifecycleOwner
    {
        override fun getLifecycle(): Lifecycle = lifecycleRegistry
    }
    private val lifecycleRegistry: LifecycleRegistry = LifecycleRegistry(lifecycleOwner)

    // Lifecycle of the hosting Activity/Fragment, mirrored into lifecycleRegistry so
    // the map stops rendering while the app is backgrounded and dies with its host.
    private var hostLifecycle: Lifecycle? = null
    private val hostObserver = object : DefaultLifecycleObserver
    {
        override fun onStart(owner: LifecycleOwner) = syncLifecycle()
        override fun onResume(owner: LifecycleOwner) = syncLifecycle()
        override fun onPause(owner: LifecycleOwner) = syncLifecycle()
        override fun onStop(owner: LifecycleOwner) = syncLifecycle()
        override fun onDestroy(owner: LifecycleOwner) = performDestroy()
    }

    private val mainHandler = Handler(Looper.getMainLooper())
    private val cancelables = mutableListOf<Cancelable>()
    private var attached = false
    private var everAttached = false
    private var disconnected = false
    private var destroyed = false
    private var pendingDestroy: Runnable? = null

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
    private var terrainExaggeration: Double? = null

    // Click-consumed semantics: taps handled by an annotation or cluster must not
    // surface as onMapClick. Annotation click listeners stamp this timestamp; the
    // (deferred) map-click dispatch skips clicks that follow within the window.
    private var lastAnnotationTapMs = 0L

    private companion object
    {
        const val ANNOTATION_TAP_WINDOW_MS = 300L
        const val DEM_TILESET_URL = "mapbox://mapbox.mapbox-terrain-dem-v1"
        const val DEM_MAX_ZOOM = 14
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

        // Has to be in place before the inner MapView can ever attach: the Mapbox
        // lifecycle plugin resolves its owner from the view tree on its first attach.
        ViewTreeLifecycleOwner.set(this, lifecycleOwner)
        lifecycleRegistry.currentState = Lifecycle.State.CREATED

        mapView = MapView(context, MapInitOptions(context = context, cameraOptions = camera, styleUri = styleUri))
        addView(mapView, LayoutParams(LayoutParams.MATCH_PARENT, LayoutParams.MATCH_PARENT))

        cancelables += mapView.mapboxMap.subscribeMapLoaded { listener?.onMapReady() }
        cancelables += mapView.mapboxMap.subscribeMapLoadingError { error ->
            listener?.onMapLoadError(
                error.type.name.lowercase(),
                error.message,
                error.sourceId,
            )
        }
        cancelables += mapView.mapboxMap.subscribeStyleLoaded {
            if (destroyed)
                return@subscribeStyleLoaded

            tileSources.values.forEach { applyTileSource(it) }
            geoJsonSources.forEach { (id, geoJson) -> applyGeoJsonSource(id, geoJson) }
            layerConfigs.values.forEach { applyLayer(it) }
            clusterConfigs.values.forEach { applyClusteredSource(it) }
            terrainExaggeration?.let { applyTerrain(it) }
            applyMapLanguage()
            applyStyleImportConfig()
            applyProjection()
            applyAtmosphere()
            if (skyJson != null) applySky()
            listener?.onStyleLoaded()
        }
        cancelables += mapView.mapboxMap.subscribeCameraChanged { event ->
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
        if (destroyed)
            return

        mapView.mapboxMap.loadStyle(uri)
    }

    fun setCamera(latitude: Double, longitude: Double, zoom: Double, bearing: Double, pitch: Double)
    {
        if (destroyed)
            return

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
        if (destroyed)
            return

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
        if (destroyed)
            return

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
        if (destroyed)
            return

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
        if (destroyed)
            return

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
        if (destroyed)
            return

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
        if (destroyed)
            return

        polylineManager?.deleteAll()
        polylineIdsByAnnotationId.clear()
    }

    /**
     * Replaces all polygons. JSON:
     * [{"id":"...","points":[[lat,lng],...],"fillColor":"#RRGGBB","fillOpacity":0.4,"strokeColor":"#RRGGBB"}]
     */
    fun setPolygonsJson(json: String)
    {
        if (destroyed)
            return

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
        if (destroyed)
            return

        polygonManager?.deleteAll()
        polygonIdsByAnnotationId.clear()
    }

    // region GeoJSON sources & layers

    /**
     * Adds a GeoJSON source or replaces the data of an existing one.
     */
    /**
     * Adds or replaces a raster, vector, raster-dem or image source. JSON:
     * {"id","type","url"|"tiles",[minZoom],[maxZoom],[tileSize],[attribution],
     *  [image] (base64, image sources), [coordinates] ([[lng,lat] x4], image sources)}
     * The source is handed to Mapbox as raw style-spec JSON, so nothing has to be
     * modelled per property. Re-applied after every style load.
     */
    fun addSourceJson(json: String)
    {
        if (destroyed)
            return

        val config = runCatching { JSONObject(json) }.getOrNull() ?: return
        val id = config.optString("id")
        if (id.isEmpty())
            return

        tileSources[id] = config
        applyTileSource(config)
    }

    fun removeSource(id: String)
    {
        if (destroyed)
            return

        tileSources.remove(id)
        mapView.mapboxMap.style?.removeStyleSource(id)
    }

    private val tileSources = linkedMapOf<String, JSONObject>()

    private fun applyTileSource(config: JSONObject)
    {
        val style = mapView.mapboxMap.style ?: return
        val id = config.optString("id")
        if (style.styleSourceExists(id))
            style.removeStyleSource(id)

        val type = config.optString("type")
        val spec = JSONObject().put("type", type)

        config.optString("url").takeIf { it.isNotEmpty() }?.let { spec.put("url", it) }
        config.optJSONArray("tiles")?.let { spec.put("tiles", it) }
        if (config.has("minZoom")) spec.put("minzoom", config.optInt("minZoom"))
        if (config.has("maxZoom")) spec.put("maxzoom", config.optInt("maxZoom"))
        if (config.has("tileSize")) spec.put("tileSize", config.optInt("tileSize"))
        config.optString("attribution").takeIf { it.isNotEmpty() }?.let { spec.put("attribution", it) }
        config.optJSONArray("coordinates")?.let { spec.put("coordinates", it) }

        val value = Value.fromJson(spec.toString()).value ?: return
        style.addStyleSource(id, value)
    }

    fun addGeoJsonSource(id: String, geoJson: String)
    {
        if (destroyed)
            return

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
        if (destroyed)
            return

        geoJsonSources.remove(id)
        mapView.mapboxMap.style?.removeStyleSource(id)
    }

    /**
     * Adds (or replaces) a style layer. JSON:
     * {"id","sourceId","type":"fill|line|circle","color","opacity","lineWidth","circleRadius","belowLayerId"}
     */
    fun addLayerJson(json: String)
    {
        if (destroyed)
            return

        val config = JSONObject(json)
        val id = config.optString("id")
        if (id.isEmpty()) return

        layerConfigs.remove(id)
        layerConfigs[id] = config
        applyLayer(config)
    }

    fun removeLayer(id: String)
    {
        if (destroyed)
            return

        layerConfigs.remove(id)
        mapView.mapboxMap.style?.removeStyleLayer(id)
    }

    private fun buildSymbolLayer(
        style: com.mapbox.maps.Style,
        id: String,
        sourceId: String,
        config: JSONObject
    ): SymbolLayer
    {
        val layer = SymbolLayer(id, sourceId)
        val textField = config.optString("textField")

        if (textField.isNotEmpty())
        {
            layer.textField(Expression.fromRaw("[\"get\",\"$textField\"]"))
                .textSize(config.optDouble("textSize", 14.0))
                .textColor(config.optString("textColor", "#000000"))

            val halo = config.optString("textHaloColor")
            if (halo.isNotEmpty())
            {
                layer.textHaloColor(halo).textHaloWidth(config.optDouble("textHaloWidth", 1.2))
            }
        }

        val iconBase64 = config.optString("icon")
        if (iconBase64.isNotEmpty())
        {
            val bitmap = customIconBitmap(iconBase64, config.optDouble("iconWidth", 0.0), 0.0)
            if (bitmap != null)
            {
                val imageId = "ik-layer-icon-$id"
                style.addImage(imageId, bitmap)
                layer.iconImage(imageId)
                if (textField.isNotEmpty())
                {
                    // Icon above, label below
                    layer.textOffset(listOf(0.0, 1.2)).textAnchor(TextAnchor.TOP)
                }
            }
        }

        if (config.optBoolean("allowOverlap", false))
        {
            layer.iconAllowOverlap(true).iconIgnorePlacement(true)
                .textAllowOverlap(true).textIgnorePlacement(true)
        }

        return layer
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

        // Vector sources need the tileset's internal layer name; every other kind ignores it.
        val sourceLayer = config.optString("sourceLayer").takeIf { it.isNotEmpty() }

        val layer = when (config.optString("type"))
        {
            "fill" -> FillLayer(id, sourceId)
                .fillColor(color)
                .fillOpacity(opacity)
                .also { l -> sourceLayer?.let { l.sourceLayer(it) } }
            "line" -> LineLayer(id, sourceId)
                .lineColor(color)
                .lineOpacity(opacity)
                .lineWidth(config.optDouble("lineWidth", 3.0))
                .also { l -> sourceLayer?.let { l.sourceLayer(it) } }
            "circle" -> CircleLayer(id, sourceId)
                .circleColor(color)
                .circleOpacity(opacity)
                .circleRadius(config.optDouble("circleRadius", 6.0))
                .also { l -> sourceLayer?.let { l.sourceLayer(it) } }
            "symbol" -> buildSymbolLayer(style, id, sourceId, config)
                .also { l -> sourceLayer?.let { l.sourceLayer(it) } }
            // These carry no typed options — style them via "properties".
            "raster" -> RasterLayer(id, sourceId)
            "hillshade" -> HillshadeLayer(id, sourceId)
            "heatmap" -> HeatmapLayer(id, sourceId)
                .also { l -> sourceLayer?.let { l.sourceLayer(it) } }
            "fill-extrusion" -> FillExtrusionLayer(id, sourceId)
                .also { l -> sourceLayer?.let { l.sourceLayer(it) } }
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

        applyLayerProperties(style, id, config.optJSONObject("properties"))
    }

    /**
     * Applies raw style-spec properties on top of the typed ones. Values arrive as JSON,
     * so a scalar and an expression take the same path — Mapbox parses both.
     */
    private fun applyLayerProperties(style: Style, id: String, properties: JSONObject?)
    {
        if (properties == null)
            return

        for (key in properties.keys())
        {
            val node = properties.get(key)
            // org.json hands back a bare String; everything else already renders as JSON.
            val json = if (node is String) JSONObject.quote(node) else node.toString()
            val value = Value.fromJson(json).value ?: continue
            style.setStyleLayerProperty(id, key, value)
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
        if (destroyed)
            return

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
        if (destroyed)
            return

        viewAnnotationViews.remove(id)?.let { mapView.viewAnnotationManager.removeViewAnnotation(it) }
    }

    // endregion

    // region Offline regions

    private val offlineManager by lazy { OfflineManager() }
    private val tileStore by lazy { TileStore.create() }

    /**
     * Downloads a style pack plus the tile region for a bounding box. JSON:
     * {"id","name","styleUri","minZoom","maxZoom","minLat","minLng","maxLat","maxLng","includeTerrain"}
     * With includeTerrain the Mapbox terrain DEM is downloaded too (capped at its
     * native max zoom 14), so 3D terrain and elevation queries work offline.
     * The JSON itself is stored as the region's metadata (see IKMapbox.listOfflineRegions).
     * Progress and completion are reported through the listener.
     */
    fun downloadOfflineRegionJson(json: String)
    {
        if (destroyed)
            return

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
        val descriptors = mutableListOf(
            offlineManager.createTilesetDescriptor(
                TilesetDescriptorOptions.Builder()
                    .styleURI(styleUri)
                    .minZoom(minZoom)
                    .maxZoom(maxZoom)
                    .build()
            )
        )

        // 2b. Terrain DEM (the runtime "ik-dem" source is not part of the style, so
        // the style descriptor alone never covers it). The DEM ends at zoom 14.
        if (config.optBoolean("includeTerrain", false))
        {
            val demMax = minOf(maxZoom.toInt(), DEM_MAX_ZOOM).toByte()
            val demMin = minOf(minZoom.toInt(), demMax.toInt()).toByte()
            descriptors += offlineManager.createTilesetDescriptor(
                TilesetDescriptorOptions.Builder()
                    .styleURI(styleUri)
                    .minZoom(demMin)
                    .maxZoom(demMax)
                    .tilesets(listOf(DEM_TILESET_URL))
                    .build()
            )
        }

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
                .descriptors(descriptors)
                .metadata(Value.fromJson(json).value ?: Value.nullValue())
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
        if (destroyed)
            return

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
        if (destroyed)
            return

        val config = JSONObject(json)
        val id = config.optString("sourceId")
        if (id.isEmpty()) return

        clusterConfigs[id] = config
        applyClusteredSource(config)
    }

    fun removeClusteredSource(id: String)
    {
        if (destroyed)
            return

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
        if (destroyed)
            return

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
        if (destroyed)
            return

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

    /**
     * Enables/disables 3D terrain (Mapbox DEM + sky atmosphere). Survives style
     * switches — re-applied after every style load. Tilt the camera (pitch) to
     * actually see the relief.
     */
    fun setTerrain(enabled: Boolean, exaggeration: Double)
    {
        if (destroyed)
            return

        if (enabled)
        {
            terrainExaggeration = exaggeration
            applyTerrain(exaggeration)
        }
        else
        {
            terrainExaggeration = null
            val style = mapView.mapboxMap.style ?: return
            style.removeTerrain()
            style.removeStyleLayer("ik-sky")
            style.removeStyleSource("ik-dem")
        }
    }

    private fun applyTerrain(exaggeration: Double)
    {
        val style = mapView.mapboxMap.style ?: return
        if (!style.styleSourceExists("ik-dem"))
        {
            style.addSource(rasterDemSource("ik-dem") {
                url(DEM_TILESET_URL)
                tileSize(514)
                maxzoom(DEM_MAX_ZOOM.toLong())
            })
        }

        terrain("ik-dem") { exaggeration(exaggeration) }.bindTo(style)

        applySky()
    }

    /**
     * Shows/hides the map ornaments. Note: hiding logo/attribution may require a
     * Mapbox license that permits it — the consuming app is responsible for compliance.
     */
    /**
     * Places the ornaments. JSON, every entry optional — omitted ornaments keep the
     * SDK default:
     * {"compass":{"position":"topRight","x":8,"y":8}, "scaleBar":{...}, "logo":{...},
     *  "attribution":{...}}
     * position is topLeft|topRight|bottomLeft|bottomRight; x/y are the offset in dp
     * from that corner.
     */
    /**
     * Shifts the camera's anchor inside the viewport, in dp. Use it when part of the map
     * is covered by other UI: the centre of the camera then lands in the visible part
     * instead of the middle of the view.
     */
    /**
     * Translates the style's name labels into [language] (a BCP-47 tag such as "de" or
     * "fr-CH"). An empty string leaves the style's own labels alone. Re-applied after
     * every style load, because a style reload resets the labels.
     */
    fun setMapLanguage(language: String)
    {
        if (destroyed)
            return

        mapLanguage = language.ifEmpty { null }
        applyMapLanguage()
    }

    private var mapLanguage: String? = null

    private fun applyMapLanguage()
    {
        val tag = mapLanguage ?: return
        mapView.mapboxMap.style?.localizeLabels(Locale.forLanguageTag(tag))
    }

    fun setCameraPadding(left: Double, top: Double, right: Double, bottom: Double)
    {
        if (destroyed)
            return

        val d = resources.displayMetrics.density
        mapView.mapboxMap.setCamera(
            CameraOptions.Builder()
                .padding(EdgeInsets(top * d, left * d, bottom * d, right * d))
                .build()
        )
    }

    fun setOrnamentPlacementJson(json: String)
    {
        if (destroyed)
            return

        val config = runCatching { JSONObject(json) }.getOrNull() ?: return
        val density = resources.displayMetrics.density
        captureOrnamentDefaults()

        val compass = config.optJSONObject("compass")
        mapView.compass.updateSettings {
            val d = ornamentDefaults.getValue("compass")
            position = if (compass != null) gravityOf(compass.optString("position")) else d.position
            applyMargins(compass, d, density) { l, t, r, b ->
                marginLeft = l; marginTop = t; marginRight = r; marginBottom = b
            }
        }
        val scaleBar = config.optJSONObject("scaleBar")
        mapView.scalebar.updateSettings {
            val d = ornamentDefaults.getValue("scaleBar")
            position = if (scaleBar != null) gravityOf(scaleBar.optString("position")) else d.position
            applyMargins(scaleBar, d, density) { l, t, r, b ->
                marginLeft = l; marginTop = t; marginRight = r; marginBottom = b
            }
        }
        val logo = config.optJSONObject("logo")
        mapView.logo.updateSettings {
            val d = ornamentDefaults.getValue("logo")
            position = if (logo != null) gravityOf(logo.optString("position")) else d.position
            applyMargins(logo, d, density) { l, t, r, b ->
                marginLeft = l; marginTop = t; marginRight = r; marginBottom = b
            }
        }
        val attribution = config.optJSONObject("attribution")
        mapView.attribution.updateSettings {
            val d = ornamentDefaults.getValue("attribution")
            position = if (attribution != null) gravityOf(attribution.optString("position")) else d.position
            applyMargins(attribution, d, density) { l, t, r, b ->
                marginLeft = l; marginTop = t; marginRight = r; marginBottom = b
            }
        }
    }

    private class OrnamentDefaults(
        val position: Int,
        val left: Float, val top: Float, val right: Float, val bottom: Float,
    )

    private val ornamentDefaults = mutableMapOf<String, OrnamentDefaults>()

    /**
     * Remembers the SDK's own placement the first time a placement is applied, so that
     * dropping an ornament from the JSON restores the default instead of leaving the
     * previous override in place.
     */
    private fun captureOrnamentDefaults()
    {
        if (ornamentDefaults.isNotEmpty())
            return

        mapView.compass.also {
            ornamentDefaults["compass"] =
                OrnamentDefaults(it.position, it.marginLeft, it.marginTop, it.marginRight, it.marginBottom)
        }
        mapView.scalebar.also {
            ornamentDefaults["scaleBar"] =
                OrnamentDefaults(it.position, it.marginLeft, it.marginTop, it.marginRight, it.marginBottom)
        }
        mapView.logo.also {
            ornamentDefaults["logo"] =
                OrnamentDefaults(it.position, it.marginLeft, it.marginTop, it.marginRight, it.marginBottom)
        }
        mapView.attribution.also {
            ornamentDefaults["attribution"] =
                OrnamentDefaults(it.position, it.marginLeft, it.marginTop, it.marginRight, it.marginBottom)
        }
    }

    private fun gravityOf(position: String): Int = when (position)
    {
        "topLeft" -> Gravity.TOP or Gravity.START
        "bottomLeft" -> Gravity.BOTTOM or Gravity.START
        "bottomRight" -> Gravity.BOTTOM or Gravity.END
        else -> Gravity.TOP or Gravity.END
    }

    /**
     * Turns the corner-relative x/y offset into the four Android margins: only the two
     * that belong to the chosen corner are set, the opposite ones stay at 0.
     */
    private inline fun applyMargins(
        o: JSONObject?,
        defaults: OrnamentDefaults,
        density: Float,
        set: (left: Float, top: Float, right: Float, bottom: Float) -> Unit,
    )
    {
        if (o == null)
        {
            set(defaults.left, defaults.top, defaults.right, defaults.bottom)
            return
        }

        val x = (o.optDouble("x", 0.0) * density).toFloat()
        val y = (o.optDouble("y", 0.0) * density).toFloat()
        when (o.optString("position"))
        {
            "topLeft" -> set(x, y, 0f, 0f)
            "bottomLeft" -> set(x, 0f, 0f, y)
            "bottomRight" -> set(0f, 0f, x, y)
            else -> set(0f, y, x, 0f)
        }
    }

    fun setOrnaments(compass: Boolean, scaleBar: Boolean, logo: Boolean, attribution: Boolean)
    {
        if (destroyed)
            return

        mapView.compass.enabled = compass
        mapView.scalebar.enabled = scaleBar
        mapView.logo.enabled = logo
        mapView.attribution.enabled = attribution
    }

    /**
     * Current viewport as a bounding box. JSON: {"minLat","minLng","maxLat","maxLng"}
     */
    fun visibleBoundsJson(): String
    {
        if (destroyed)
            return ""

        val bounds = mapView.mapboxMap.coordinateBoundsForCamera(
            mapView.mapboxMap.cameraState.toCameraOptions()
        )
        return JSONObject()
            .put("minLat", bounds.southwest.latitude())
            .put("minLng", bounds.southwest.longitude())
            .put("maxLat", bounds.northeast.latitude())
            .put("maxLng", bounds.northeast.longitude())
            .toString()
    }

    /**
     * Terrain elevation in meters above sea level at a coordinate, or NaN when it
     * is not available: terrain disabled, exaggeration 0, or the DEM tile for the
     * coordinate not loaded (typically: not on screen, or offline without a region
     * that includes terrain). Mapbox reports the elevation multiplied by the
     * terrain exaggeration; this returns the real value.
     */
    fun queryTerrainElevation(latitude: Double, longitude: Double): Double
    {
        if (destroyed)
            return Double.NaN

        val exaggeration = terrainExaggeration ?: return Double.NaN
        if (exaggeration <= 0.0)
            return Double.NaN

        val elevation = mapView.mapboxMap.getElevation(Point.fromLngLat(longitude, latitude))
            ?: return Double.NaN
        return elevation / exaggeration
    }

    /**
     * Constrains the camera. Any argument may be NaN, which means "leave at the SDK
     * default". The four bounds values are all-or-nothing: pass NaN for all of them to
     * lift a pan restriction.
     */
    fun setCameraBounds(
        minLat: Double, minLng: Double, maxLat: Double, maxLng: Double,
        minZoom: Double, maxZoom: Double, minPitch: Double, maxPitch: Double,
    )
    {
        if (destroyed)
            return

        val options = CameraBoundsOptions.Builder()
        if (!minLat.isNaN() && !minLng.isNaN() && !maxLat.isNaN() && !maxLng.isNaN())
        {
            options.bounds(
                CoordinateBounds(
                    Point.fromLngLat(minLng, minLat),
                    Point.fromLngLat(maxLng, maxLat),
                )
            )
        }
        else
        {
            // Mapbox keeps the previous restriction unless it is explicitly reset.
            options.bounds(CoordinateBounds.world())
        }

        options.minZoom(if (minZoom.isNaN()) null else minZoom)
        options.maxZoom(if (maxZoom.isNaN()) null else maxZoom)
        options.minPitch(if (minPitch.isNaN()) null else minPitch)
        options.maxPitch(if (maxPitch.isNaN()) null else maxPitch)

        mapView.mapboxMap.setBounds(options.build())
    }

    /**
     * Reports the rendered features under a coordinate. [layerIdsJson] is a JSON array of
     * layer ids to restrict the query to, or empty for every layer; [radius] widens the hit
     * area in dp, which matters for thin lines and small circles. The callback receives a
     * JSON array (on an arbitrary thread):
     * [{"id","layerId","source","sourceLayer","geometryType","properties"}]
     */
    fun queryRenderedFeatures(
        latitude: Double,
        longitude: Double,
        layerIdsJson: String,
        radius: Double,
        callback: IKStringCallback,
    )
    {
        if (destroyed)
        {
            callback.onResult("[]")
            return
        }

        val layerIds = runCatching {
            val array = JSONArray(layerIdsJson)
            (0 until array.length()).map { array.getString(it) }
        }.getOrNull()?.takeIf { it.isNotEmpty() }

        val screen = mapView.mapboxMap.pixelForCoordinate(Point.fromLngLat(longitude, latitude))
        val density = resources.displayMetrics.density
        val padding = radius * density
        val geometry = if (padding > 0)
        {
            RenderedQueryGeometry(
                ScreenBox(
                    ScreenCoordinate(screen.x - padding, screen.y - padding),
                    ScreenCoordinate(screen.x + padding, screen.y + padding),
                )
            )
        }
        else
        {
            RenderedQueryGeometry(screen)
        }

        mapView.mapboxMap.queryRenderedFeatures(geometry, RenderedQueryOptions(layerIds, null)) { result ->
            val array = JSONArray()
            result.value?.forEach { item ->
                val queried = item.queriedFeature
                // Round-trip the feature through GeoJSON so properties and geometry type
                // come out in the same shape the iOS side produces.
                val geo = runCatching { JSONObject(queried.feature.toJson()) }.getOrNull()
                array.put(
                    JSONObject()
                        .put("id", geo?.opt("id") ?: JSONObject.NULL)
                        .put("layerId", item.layers?.firstOrNull() ?: JSONObject.NULL)
                        .put("source", queried.source)
                        .put("sourceLayer", queried.sourceLayer ?: JSONObject.NULL)
                        .put("geometryType", geo?.optJSONObject("geometry")?.optString("type") ?: JSONObject.NULL)
                        .put("properties", geo?.optJSONObject("properties") ?: JSONObject())
                )
            }
            callback.onResult(array.toString())
        }
    }

    /**
     * Attaches state to a feature, readable from expressions as
     * ["feature-state", "<key>"]. The feature must carry an id — GeoJSON without one is
     * invisible to this. [sourceLayer] is only used by vector sources; pass "" otherwise.
     */
    fun setFeatureState(sourceId: String, sourceLayer: String, featureId: String, stateJson: String)
    {
        if (destroyed)
            return

        val state = Value.fromJson(stateJson).value ?: return
        // Mapbox has separate overloads with and without a source layer.
        if (sourceLayer.isEmpty())
        {
            mapView.mapboxMap.setFeatureState(sourceId, featureId, state) { }
        }
        else
        {
            mapView.mapboxMap.setFeatureState(sourceId, sourceLayer, featureId, state) { }
        }
    }

    /** Removes one state key, or the feature's whole state when [stateKey] is empty. */
    fun removeFeatureState(sourceId: String, sourceLayer: String, featureId: String, stateKey: String)
    {
        if (destroyed)
            return

        // Four overloads: with/without a source layer, with/without a single key.
        when
        {
            sourceLayer.isEmpty() && stateKey.isEmpty() ->
                mapView.mapboxMap.removeFeatureState(sourceId, featureId) { }
            sourceLayer.isEmpty() ->
                mapView.mapboxMap.removeFeatureState(sourceId, featureId, stateKey) { }
            stateKey.isEmpty() ->
                mapView.mapboxMap.removeFeatureState(sourceId, sourceLayer, featureId) { }
            else ->
                mapView.mapboxMap.removeFeatureState(sourceId, sourceLayer, featureId, stateKey) { }
        }
    }

    /**
     * Full gesture configuration in one call, so the order properties arrive in does not
     * matter. JSON: {"scroll","zoom","rotate","pitch","doubleTapToZoom","quickZoom",
     * "pinchToZoom","rotateAndZoom"} — all booleans, missing keys default to true.
     */
    /**
     * Configures the style's "basemap" import — the Mapbox Standard style's own options
     * such as lightPreset, theme and show3dObjects. JSON: {"<key>": <value>, …}.
     * Classic styles have no basemap import and ignore this. Re-applied after every
     * style load, because a style reload resets the configuration.
     */
    /**
     * Switches the map projection: "globe", "mercator", or "" to keep whatever the style
     * ships with. Re-applied after every style load, because a style reload resets it.
     */
    /**
     * Sets the atmosphere (fog) as raw style-spec JSON — keys such as "color",
     * "high-color", "space-color", "horizon-blend", "star-intensity". An empty payload
     * removes it. Re-applied after every style load.
     */
    /**
     * Configures the sky layer. JSON: {"type":"atmosphere"|"gradient","sunAzimuth",
     * "sunElevation","sunIntensity","opacity"} — all optional. An empty payload drops
     * back to the defaults terrain uses, and removes the layer entirely when terrain is
     * off. The sky is most visible with a pitched camera.
     */
    fun setSkyJson(json: String)
    {
        if (destroyed)
            return

        skyJson = json.takeIf { it.isNotEmpty() && it != "{}" }
        if (skyJson == null && terrainExaggeration == null)
        {
            mapView.mapboxMap.style?.removeStyleLayer("ik-sky")
            return
        }
        applySky()
    }

    private var skyJson: String? = null

    /** Shared by the terrain feature and the public setter, so there is one sky layer. */
    private fun applySky()
    {
        val style = mapView.mapboxMap.style ?: return
        val config = skyJson?.let { runCatching { JSONObject(it) }.getOrNull() }

        if (style.styleLayerExists("ik-sky"))
            style.removeStyleLayer("ik-sky")

        style.addLayer(skyLayer("ik-sky") {
            skyType(
                if (config?.optString("type") == "gradient") SkyType.GRADIENT else SkyType.ATMOSPHERE
            )
            skyAtmosphereSun(
                listOf(
                    config?.optDouble("sunAzimuth", 0.0) ?: 0.0,
                    config?.optDouble("sunElevation", 0.0) ?: 0.0,
                )
            )
            skyAtmosphereSunIntensity(config?.optDouble("sunIntensity", 15.0) ?: 15.0)
            if (config != null && config.has("opacity"))
                skyOpacity(config.optDouble("opacity", 1.0))
        })
    }

    fun setAtmosphereJson(json: String)
    {
        if (destroyed)
            return

        atmosphereJson = json.takeIf { it.isNotEmpty() && it != "{}" }
        applyAtmosphere()
    }

    private var atmosphereJson: String? = null

    private fun applyAtmosphere()
    {
        val style = mapView.mapboxMap.style ?: return
        val json = atmosphereJson
        if (json == null)
        {
            style.removeAtmosphere()
            return
        }

        val value = Value.fromJson(json).value ?: return
        style.setStyleAtmosphere(value)
    }

    fun setProjection(name: String)
    {
        if (destroyed)
            return

        projectionName = name.takeIf { it.isNotEmpty() }
        applyProjection()
    }

    private var projectionName: String? = null

    private fun applyProjection()
    {
        val name = projectionName ?: return
        val style = mapView.mapboxMap.style ?: return
        val value = Value.fromJson("{\"name\":\"$name\"}").value ?: return
        style.setStyleProjection(value)
    }

    fun setStyleImportConfigJson(json: String)
    {
        if (destroyed)
            return

        styleImportConfig = json.takeIf { it.isNotEmpty() && it != "{}" }
        applyStyleImportConfig()
    }

    private var styleImportConfig: String? = null

    private fun applyStyleImportConfig()
    {
        val json = styleImportConfig ?: return
        val config = runCatching { JSONObject(json) }.getOrNull() ?: return
        val style = mapView.mapboxMap.style ?: return

        for (key in config.keys())
        {
            val node = config.get(key)
            val text = if (node is String) JSONObject.quote(node) else node.toString()
            val value = Value.fromJson(text).value ?: continue
            style.setStyleImportConfigProperty("basemap", key, value)
        }
    }

    fun setGestureOptionsJson(json: String)
    {
        if (destroyed)
            return

        val config = runCatching { JSONObject(json) }.getOrNull() ?: return
        val zoom = config.optBoolean("zoom", true)

        mapView.gestures.updateSettings {
            scrollEnabled = config.optBoolean("scroll", true)
            rotateEnabled = config.optBoolean("rotate", true)
            pitchEnabled = config.optBoolean("pitch", true)

            // The fine flags only ever narrow the coarse "zoom" switch.
            pinchToZoomEnabled = zoom && config.optBoolean("pinchToZoom", true)
            quickZoomEnabled = zoom && config.optBoolean("quickZoom", true)
            doubleTapToZoomInEnabled = zoom && config.optBoolean("doubleTapToZoom", true)
            doubleTouchToZoomOutEnabled = zoom && config.optBoolean("doubleTapToZoom", true)
            simultaneousRotateAndPinchToZoomEnabled = config.optBoolean("rotateAndZoom", true)
        }
    }

    fun setGestures(scroll: Boolean, zoom: Boolean, rotate: Boolean, pitch: Boolean)
    {
        if (destroyed)
            return

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

    override fun onAttachedToWindow()
    {
        attached = true
        everAttached = true

        // A teardown requested while the map was still on screen is cancelled when it
        // comes back. MAUI disconnects handlers late (Page.SendNavigatedFrom defers to
        // OnUnloaded, and an Android fragment swap creates the incoming view before it
        // destroys the outgoing one), so a disconnect can arrive for a map that is
        // being shown again — destroying it there is exactly what turns it black.
        pendingDestroy?.let { mainHandler.removeCallbacks(it) }
        pendingDestroy = null

        if (!destroyed && hostLifecycle == null)
        {
            // Read from the parent, not from us: our own owner is already installed here.
            hostLifecycle = (parent as? View)?.let { ViewTreeLifecycleOwner.get(it) }?.lifecycle
            hostLifecycle?.addObserver(hostObserver)
        }

        syncLifecycle()
        super.onAttachedToWindow()
    }

    override fun onDetachedFromWindow()
    {
        attached = false
        syncLifecycle()
        super.onDetachedFromWindow()
        scheduleDestroyIfIdle()
    }

    /**
     * Teardown request from the MAUI handler's DisconnectHandler. The map is not
     * destroyed while it is still attached to a window, and not before the main loop
     * has turned once — see onAttachedToWindow for why.
     */
    fun destroy()
    {
        if (destroyed)
            return

        disconnected = true
        listener = null
        scheduleDestroyIfIdle()
    }

    private fun syncLifecycle()
    {
        if (destroyed)
            return

        // Mirrors the host, capped by our own attachment: detached means the page is
        // not on screen, so the map must stop rendering but stay alive.
        val hostState = hostLifecycle?.currentState ?: Lifecycle.State.RESUMED
        lifecycleRegistry.currentState = when
        {
            !attached || !hostState.isAtLeast(Lifecycle.State.STARTED) -> Lifecycle.State.CREATED
            hostState.isAtLeast(Lifecycle.State.RESUMED) -> Lifecycle.State.RESUMED
            else -> Lifecycle.State.STARTED
        }
    }

    private fun scheduleDestroyIfIdle()
    {
        if (destroyed || !disconnected || attached || pendingDestroy != null)
            return

        val runnable = Runnable {
            pendingDestroy = null
            if (!attached)
                performDestroy()
        }
        pendingDestroy = runnable
        mainHandler.post(runnable)
    }

    private fun performDestroy()
    {
        if (destroyed)
            return

        destroyed = true
        pendingDestroy?.let { mainHandler.removeCallbacks(it) }
        pendingDestroy = null
        listener = null

        cancelables.forEach { it.cancel() }
        cancelables.clear()
        hostLifecycle?.removeObserver(hostObserver)
        hostLifecycle = null

        mapView.viewAnnotationManager.removeAllViewAnnotations()
        viewAnnotationViews.clear()
        pointManager = null
        polylineManager = null
        polygonManager = null
        markerIdsByAnnotationId.clear()
        polylineIdsByAnnotationId.clear()
        polygonIdsByAnnotationId.clear()
        geoJsonSources.clear()
        layerConfigs.clear()
        clusterConfigs.clear()

        // Driving our own registry to DESTROYED is what tears the map down: the Mapbox
        // lifecycle plugin forwards ON_DESTROY to MapView.onDestroy() and runs its own
        // cleanup, which unregisters the ComponentCallbacks2 it put on the Activity
        // context. Calling MapView.onDestroy() by hand skips that and leaks the map.
        if (lifecycleRegistry.currentState.isAtLeast(Lifecycle.State.CREATED))
            lifecycleRegistry.currentState = Lifecycle.State.DESTROYED

        // Never attached means the plugin never bound — nothing else will destroy it.
        if (!everAttached)
            mapView.onDestroy()

        removeAllViews()
        ViewTreeLifecycleOwner.set(this, null)
        customIconCache.clear()
        pinCache.clear()
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
