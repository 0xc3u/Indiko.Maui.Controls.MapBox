using System.Collections.Specialized;
using CoreGraphics;
using Indiko.Maui.Controls.MapBox.Bindings;
using Indiko.Maui.Controls.MapBox.Handlers;
using Indiko.Maui.Controls.MapBox.Models;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Indiko.Maui.Controls.MapBox.Platforms.iOS;

public class MapViewHandler : ViewHandler<MapView, IKMapView>
{
    private static bool tokenApplied;

    private MapEventListener? listener;
    private INotifyCollectionChanged? observedAnnotations;
    private INotifyCollectionChanged? observedPolylines;
    private INotifyCollectionChanged? observedPolygons;
    private INotifyCollectionChanged? observedViewAnnotations;
    private readonly Dictionary<string, MapViewAnnotation> shownViewAnnotations = [];

    public static readonly IPropertyMapper<MapView, MapViewHandler> Mapper =
        new PropertyMapper<MapView, MapViewHandler>(ViewMapper)
        {
            [nameof(MapView.StyleUri)] = MapStyleUri,
            [nameof(MapView.Camera)] = MapCamera,
            [nameof(MapView.Annotations)] = MapAnnotations,
            [nameof(MapView.Polylines)] = MapPolylines,
            [nameof(MapView.Polygons)] = MapPolygons,
            [nameof(MapView.ViewAnnotations)] = MapViewAnnotations,
            [nameof(MapView.ShowUserLocation)] = MapShowUserLocation,
            [nameof(MapView.FollowPuck)] = MapFollowPuck,
            [nameof(MapView.FollowPuckZoom)] = MapFollowPuck,
            [nameof(MapView.FollowPuckTrackBearing)] = MapFollowPuck,
            [nameof(MapView.ScrollEnabled)] = MapGestures,
            [nameof(MapView.ZoomEnabled)] = MapGestures,
            [nameof(MapView.RotateEnabled)] = MapGestures,
            [nameof(MapView.PitchEnabled)] = MapGestures,
        };

    public static readonly CommandMapper<MapView, MapViewHandler> Commands =
        new(ViewCommandMapper)
        {
            [nameof(MapView.FlyTo)] = MapFlyTo,
            [nameof(MapView.FitBounds)] = MapFitBounds,
            [nameof(MapView.AddGeoJsonSource)] = MapAddGeoJsonSource,
            [nameof(MapView.RemoveGeoJsonSource)] = MapRemoveGeoJsonSource,
            [nameof(MapView.AddLayer)] = MapAddLayer,
            [nameof(MapView.RemoveLayer)] = MapRemoveLayer,
            [nameof(MapView.AddClusteredSource)] = MapAddClusteredSource,
            [nameof(MapView.RemoveClusteredSource)] = MapRemoveClusteredSource,
            [nameof(MapView.DownloadOfflineRegion)] = MapDownloadOfflineRegion,
            [nameof(MapView.RemoveOfflineRegion)] = MapRemoveOfflineRegion,
        };

    public MapViewHandler() : base(Mapper, Commands)
    {
    }

    protected override IKMapView CreatePlatformView()
    {
        EnsureAccessToken();

        var camera = VirtualView.Camera ?? new MapCameraPosition(0, 0, 1);
        return new IKMapView(
            CGRect.Empty,
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
        ObserveViewAnnotations(VirtualView.ViewAnnotations);
    }

    protected override void DisconnectHandler(IKMapView platformView)
    {
        ObserveAnnotations(null);
        ObservePolylines(null);
        ObservePolygons(null);
        ObserveViewAnnotations(null);
        shownViewAnnotations.Clear();
        platformView.WeakListener = null;
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

    private static void MapViewAnnotations(MapViewHandler handler, MapView view)
    {
        handler.ObserveViewAnnotations(view.ViewAnnotations);
        handler.PushViewAnnotations();
    }

    private static void MapShowUserLocation(MapViewHandler handler, MapView view)
    {
        handler.PlatformView.SetUserLocationEnabled(view.ShowUserLocation);
    }

    private static void MapFollowPuck(MapViewHandler handler, MapView view)
    {
        if (view.SuppressFollowPuckSync)
            return;

        handler.PlatformView.SetFollowPuck(view.FollowPuck, view.FollowPuckZoom, view.FollowPuckTrackBearing);
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

    private static void MapAddClusteredSource(MapViewHandler handler, MapView view, object? args)
    {
        if (args is MapClusterSource source)
            handler.PlatformView.AddClusteredSourceJson(AnnotationSerializer.ToClusterJson(source));
    }

    private static void MapRemoveClusteredSource(MapViewHandler handler, MapView view, object? args)
    {
        if (args is string sourceId)
            handler.PlatformView.RemoveClusteredSource(sourceId);
    }

    private static void MapDownloadOfflineRegion(MapViewHandler handler, MapView view, object? args)
    {
        if (args is MapOfflineRegion region)
            handler.PlatformView.DownloadOfflineRegionJson(AnnotationSerializer.ToOfflineRegionJson(region));
    }

    private static void MapRemoveOfflineRegion(MapViewHandler handler, MapView view, object? args)
    {
        if (args is string regionId)
            handler.PlatformView.RemoveOfflineRegion(regionId);
    }

    private static void MapFitBounds(MapViewHandler handler, MapView view, object? args)
    {
        if (args is not FitBoundsRequest request)
            return;

        handler.PlatformView.FitBounds(
            request.Bounds.MinLatitude, request.Bounds.MinLongitude,
            request.Bounds.MaxLatitude, request.Bounds.MaxLongitude,
            request.Padding, request.DurationMs);
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
        PlatformView.SetMarkers(AnnotationSerializer.ToJson(VirtualView.Annotations));
        VirtualView.TryAutoFit();
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
        PlatformView.SetPolylines(AnnotationSerializer.ToPolylinesJson(VirtualView.Polylines));
        VirtualView.TryAutoFit();
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
        PlatformView.SetPolygons(AnnotationSerializer.ToPolygonsJson(VirtualView.Polygons));
        VirtualView.TryAutoFit();
    }

    private void ObserveViewAnnotations(INotifyCollectionChanged? viewAnnotations)
    {
        if (ReferenceEquals(observedViewAnnotations, viewAnnotations))
            return;

        if (observedViewAnnotations is not null)
            observedViewAnnotations.CollectionChanged -= OnViewAnnotationsChanged;

        observedViewAnnotations = viewAnnotations;

        if (observedViewAnnotations is not null)
            observedViewAnnotations.CollectionChanged += OnViewAnnotationsChanged;
    }

    private void OnViewAnnotationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PushViewAnnotations();
    }

    private void PushViewAnnotations()
    {
        if (MauiContext is null)
            return;

        var current = VirtualView.ViewAnnotations?.ToList() ?? [];
        var currentIds = new HashSet<string>(current.Select(a => a.Id));

        foreach (var staleId in shownViewAnnotations.Keys.Where(id => !currentIds.Contains(id)).ToList())
        {
            PlatformView.RemoveViewAnnotation(staleId);
            shownViewAnnotations.Remove(staleId);
        }

        foreach (var annotation in current)
        {
            if (annotation.Content is null || shownViewAnnotations.ContainsKey(annotation.Id))
                continue;

            var platformContent = annotation.Content.ToPlatform(MauiContext);
            var content = (IView)annotation.Content;
            content.Measure(annotation.Width, annotation.Height);
            content.Arrange(new Rect(0, 0, annotation.Width, annotation.Height));

            PlatformView.AddViewAnnotation(
                annotation.Id, platformContent, annotation.Latitude, annotation.Longitude,
                annotation.Width, annotation.Height);
            shownViewAnnotations[annotation.Id] = annotation;
        }
        VirtualView.TryAutoFit();
    }

    /* --------------------------------- Listener ----------------------------------- */

    /// <summary>
    /// NSObject-based listener holding only a WeakReference to the virtual view — C# objects
    /// subclassing NSObject are not collected when circular references exist on Apple platforms.
    /// </summary>
    private sealed class MapEventListener : IKMapEventListener
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

        public override void OnMapReady() => Dispatch(v => v.SendMapReady());

        public override void OnStyleLoaded() => Dispatch(v => v.SendStyleLoaded());

        public override void OnMapClick(double latitude, double longitude) =>
            Dispatch(v => v.SendMapClicked(latitude, longitude));

        public override void OnMapLongPress(double latitude, double longitude) =>
            Dispatch(v => v.SendMapLongPressed(latitude, longitude));

        public override void OnMarkerClick(string id) => Dispatch(v => v.SendAnnotationClicked(id));

        public override void OnPolylineClick(string id) => Dispatch(v => v.SendPolylineClicked(id));

        public override void OnPolygonClick(string id) => Dispatch(v => v.SendPolygonClicked(id));

        public override void OnCameraChanged(IKCameraState camera)
        {
            var position = new MapCameraPosition(camera.Latitude, camera.Longitude, camera.Zoom, camera.Bearing, camera.Pitch);
            Dispatch(v => v.SendCameraChanged(position));
        }

        public override void OnOfflineRegionProgress(string id, double progress) =>
            Dispatch(v => v.SendOfflineRegionProgress(id, progress));

        public override void OnOfflineRegionCompleted(string id, bool success, string? error) =>
            Dispatch(v => v.SendOfflineRegionCompleted(id, success, error));

        public override void OnFollowPuckChanged(bool active) =>
            Dispatch(v => v.SendFollowPuckChanged(active));
    }
}
