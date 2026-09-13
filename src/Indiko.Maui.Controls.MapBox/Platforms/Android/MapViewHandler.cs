using System.Collections.Specialized;
using Indiko.Maui.Controls.MapBox.Bindings;
using Indiko.Maui.Controls.MapBox.Handlers;
using Indiko.Maui.Controls.MapBox.Models;
using Microsoft.Maui.Handlers;

namespace Indiko.Maui.Controls.MapBox.Platforms.Android;

public class MapViewHandler : ViewHandler<MapView, IKMapView>
{
    private static bool tokenApplied;

    private MapEventListener? listener;
    private INotifyCollectionChanged? observedAnnotations;
    private INotifyCollectionChanged? observedPolylines;
    private INotifyCollectionChanged? observedPolygons;

    public static readonly IPropertyMapper<MapView, MapViewHandler> Mapper =
        new PropertyMapper<MapView, MapViewHandler>(ViewMapper)
        {
            [nameof(MapView.StyleUri)] = MapStyleUri,
            [nameof(MapView.Camera)] = MapCamera,
            [nameof(MapView.Annotations)] = MapAnnotations,
            [nameof(MapView.Polylines)] = MapPolylines,
            [nameof(MapView.Polygons)] = MapPolygons,
            [nameof(MapView.ShowUserLocation)] = MapShowUserLocation,
            [nameof(MapView.ScrollEnabled)] = MapGestures,
            [nameof(MapView.ZoomEnabled)] = MapGestures,
            [nameof(MapView.RotateEnabled)] = MapGestures,
            [nameof(MapView.PitchEnabled)] = MapGestures,
        };

    public static readonly CommandMapper<MapView, MapViewHandler> Commands =
        new(ViewCommandMapper)
        {
            [nameof(MapView.FlyTo)] = MapFlyTo,
            [nameof(MapView.AddGeoJsonSource)] = MapAddGeoJsonSource,
            [nameof(MapView.RemoveGeoJsonSource)] = MapRemoveGeoJsonSource,
            [nameof(MapView.AddLayer)] = MapAddLayer,
            [nameof(MapView.RemoveLayer)] = MapRemoveLayer,
        };

    public MapViewHandler() : base(Mapper, Commands)
    {
    }

    protected override IKMapView CreatePlatformView()
    {
        EnsureAccessToken();

        var camera = VirtualView.Camera ?? new MapCameraPosition(0, 0, 1);
        return new IKMapView(
            Context,
            VirtualView.StyleUri ?? MapStyles.Streets,
            camera.Latitude, camera.Longitude, camera.Zoom, camera.Bearing, camera.Pitch);
    }

    protected override void ConnectHandler(IKMapView platformView)
    {
        base.ConnectHandler(platformView);

        listener = new MapEventListener(VirtualView);
        platformView.Listener = listener;

        ObserveAnnotations(VirtualView.Annotations);
        ObservePolylines(VirtualView.Polylines);
        ObservePolygons(VirtualView.Polygons);
    }

    protected override void DisconnectHandler(IKMapView platformView)
    {
        ObserveAnnotations(null);
        ObservePolylines(null);
        ObservePolygons(null);
        platformView.Listener = null;
        listener?.Dispose();
        listener = null;
        platformView.Destroy();

        base.DisconnectHandler(platformView);
    }

    private static void EnsureAccessToken()
    {
        if (tokenApplied)
            return;

        if (string.IsNullOrWhiteSpace(MapboxConfig.AccessToken))
            throw new InvalidOperationException(
                "Mapbox access token missing. Call builder.UseMapbox(\"pk.…\") in MauiProgram.cs.");

        IKMapbox.SetAccessToken(MapboxConfig.AccessToken);
        tokenApplied = true;
    }

    /* ------------------------------ Property mappers ------------------------------ */

    private static void MapStyleUri(MapViewHandler handler, MapView view)
    {
        handler.PlatformView.SetStyleUri(view.StyleUri ?? MapStyles.Streets);
    }

    private static void MapCamera(MapViewHandler handler, MapView view)
    {
        var camera = view.Camera;
        if (camera is null)
            return;

        handler.PlatformView.SetCamera(camera.Latitude, camera.Longitude, camera.Zoom, camera.Bearing, camera.Pitch);
    }

    private static void MapAnnotations(MapViewHandler handler, MapView view)
    {
        handler.ObserveAnnotations(view.Annotations);
        handler.PushAnnotations();
    }

    private static void MapPolylines(MapViewHandler handler, MapView view)
    {
        handler.ObservePolylines(view.Polylines);
        handler.PushPolylines();
    }

    private static void MapPolygons(MapViewHandler handler, MapView view)
    {
        handler.ObservePolygons(view.Polygons);
        handler.PushPolygons();
    }

    private static void MapShowUserLocation(MapViewHandler handler, MapView view)
    {
        handler.PlatformView.SetUserLocationEnabled(view.ShowUserLocation);
    }

    private static void MapGestures(MapViewHandler handler, MapView view)
    {
        handler.PlatformView.SetGestures(view.ScrollEnabled, view.ZoomEnabled, view.RotateEnabled, view.PitchEnabled);
    }

    /* ------------------------------ Command mappers ------------------------------- */

    private static void MapFlyTo(MapViewHandler handler, MapView view, object? args)
    {
        if (args is not FlyToRequest request)
            return;

        handler.PlatformView.FlyTo(
            request.Camera.Latitude, request.Camera.Longitude, request.Camera.Zoom,
            request.Camera.Bearing, request.Camera.Pitch, request.DurationMs);
    }

    private static void MapAddGeoJsonSource(MapViewHandler handler, MapView view, object? args)
    {
        if (args is GeoJsonSourceRequest request)
            handler.PlatformView.AddGeoJsonSource(request.SourceId, request.GeoJson);
    }

    private static void MapRemoveGeoJsonSource(MapViewHandler handler, MapView view, object? args)
    {
        if (args is string sourceId)
            handler.PlatformView.RemoveGeoJsonSource(sourceId);
    }

    private static void MapAddLayer(MapViewHandler handler, MapView view, object? args)
    {
        if (args is MapLayer layer)
            handler.PlatformView.AddLayerJson(AnnotationSerializer.ToLayerJson(layer));
    }

    private static void MapRemoveLayer(MapViewHandler handler, MapView view, object? args)
    {
        if (args is string layerId)
            handler.PlatformView.RemoveLayer(layerId);
    }

    /* -------------------------------- Annotations --------------------------------- */

    private void ObserveAnnotations(INotifyCollectionChanged? annotations)
    {
        if (ReferenceEquals(observedAnnotations, annotations))
            return;

        if (observedAnnotations is not null)
            observedAnnotations.CollectionChanged -= OnAnnotationsChanged;

        observedAnnotations = annotations;

        if (observedAnnotations is not null)
            observedAnnotations.CollectionChanged += OnAnnotationsChanged;
    }

    private void OnAnnotationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PushAnnotations();
    }

    private void PushAnnotations()
    {
        PlatformView.SetMarkersJson(AnnotationSerializer.ToJson(VirtualView.Annotations));
    }

    private void ObservePolylines(INotifyCollectionChanged? polylines)
    {
        if (ReferenceEquals(observedPolylines, polylines))
            return;

        if (observedPolylines is not null)
            observedPolylines.CollectionChanged -= OnPolylinesChanged;

        observedPolylines = polylines;

        if (observedPolylines is not null)
            observedPolylines.CollectionChanged += OnPolylinesChanged;
    }

    private void OnPolylinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PushPolylines();
    }

    private void PushPolylines()
    {
        PlatformView.SetPolylinesJson(AnnotationSerializer.ToPolylinesJson(VirtualView.Polylines));
    }

    private void ObservePolygons(INotifyCollectionChanged? polygons)
    {
        if (ReferenceEquals(observedPolygons, polygons))
            return;

        if (observedPolygons is not null)
            observedPolygons.CollectionChanged -= OnPolygonsChanged;

        observedPolygons = polygons;

        if (observedPolygons is not null)
            observedPolygons.CollectionChanged += OnPolygonsChanged;
    }

    private void OnPolygonsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PushPolygons();
    }

    private void PushPolygons()
    {
        PlatformView.SetPolygonsJson(AnnotationSerializer.ToPolygonsJson(VirtualView.Polygons));
    }

    /* --------------------------------- Listener ----------------------------------- */

    private sealed class MapEventListener : Java.Lang.Object, IKMapEventListener
    {
        private readonly WeakReference<MapView> virtualView;

        public MapEventListener(MapView mapView)
        {
            virtualView = new WeakReference<MapView>(mapView);
        }

        private void Dispatch(Action<MapView> action)
        {
            if (virtualView.TryGetTarget(out var view))
                view.Dispatcher.Dispatch(() => action(view));
        }

        public void OnMapReady() => Dispatch(v => v.SendMapReady());

        public void OnStyleLoaded() => Dispatch(v => v.SendStyleLoaded());

        public void OnMapClick(double latitude, double longitude) =>
            Dispatch(v => v.SendMapClicked(latitude, longitude));

        public void OnMapLongPress(double latitude, double longitude) =>
            Dispatch(v => v.SendMapLongPressed(latitude, longitude));

        public void OnMarkerClick(string id) => Dispatch(v => v.SendAnnotationClicked(id));

        public void OnPolylineClick(string id) => Dispatch(v => v.SendPolylineClicked(id));

        public void OnPolygonClick(string id) => Dispatch(v => v.SendPolygonClicked(id));

        public void OnCameraChanged(IKCameraState camera)
        {
            var position = new MapCameraPosition(camera.Latitude, camera.Longitude, camera.Zoom, camera.Bearing, camera.Pitch);
            Dispatch(v => v.SendCameraChanged(position));
        }
    }
}
