package ch.indiko.mapbox

import android.content.Context
import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.Path
import android.widget.FrameLayout
import com.mapbox.common.MapboxOptions
import com.mapbox.geojson.Point
import com.mapbox.maps.CameraOptions
import com.mapbox.maps.MapInitOptions
import com.mapbox.maps.MapView
import com.mapbox.maps.extension.style.layers.properties.generated.IconAnchor
import com.mapbox.maps.plugin.animation.MapAnimationOptions
import com.mapbox.maps.plugin.animation.camera
import com.mapbox.maps.plugin.annotation.annotations
import com.mapbox.maps.plugin.annotation.generated.PointAnnotationManager
import com.mapbox.maps.plugin.annotation.generated.PointAnnotationOptions
import com.mapbox.maps.plugin.annotation.generated.createPointAnnotationManager
import com.mapbox.maps.plugin.gestures.addOnMapClickListener
import com.mapbox.maps.plugin.gestures.addOnMapLongClickListener
import com.mapbox.maps.plugin.gestures.gestures
import com.mapbox.maps.plugin.locationcomponent.createDefault2DPuck
import com.mapbox.maps.plugin.locationcomponent.location
import org.json.JSONArray

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
    fun onCameraChanged(camera: IKCameraState)
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
    private val markerIdsByAnnotationId = HashMap<String, String>()

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
        mapView.mapboxMap.subscribeStyleLoaded { listener?.onStyleLoaded() }
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
            listener?.onMapClick(point.latitude(), point.longitude())
            false
        }
        mapView.gestures.addOnMapLongClickListener { point ->
            listener?.onMapLongPress(point.latitude(), point.longitude())
            false
        }
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

    // endregion

    // region Markers

    /**
     * Replaces all markers. JSON: [{"id":"...","lat":..,"lng":..,"title":"...","color":"#RRGGBB"}]
     */
    fun setMarkersJson(json: String)
    {
        val manager = pointManager ?: mapView.annotations.createPointAnnotationManager().also { created ->
            created.addClickListener { annotation ->
                markerIdsByAnnotationId[annotation.id]?.let { listener?.onMarkerClick(it) }
                true
            }
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
            val options = PointAnnotationOptions()
                .withPoint(Point.fromLngLat(lng, lat))
                .withIconImage(pinBitmap(color))
                .withIconAnchor(IconAnchor.BOTTOM)

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
        pointManager = null
        markerIdsByAnnotationId.clear()
        removeAllViews()
        mapView.onStop()
        mapView.onDestroy()
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
