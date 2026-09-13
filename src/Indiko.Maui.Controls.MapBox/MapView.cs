using System.Windows.Input;
using Indiko.Maui.Controls.MapBox.Models;

namespace Indiko.Maui.Controls.MapBox;

/// <summary>
/// Cross-platform Mapbox map control. Rendered natively via the Mapbox Maps SDK v11
/// on Android and iOS through the IndikoMapboxKit facade.
/// </summary>
public class MapView : View
{
    /* ---------------------------- Bindable properties ---------------------------- */

    public static readonly BindableProperty StyleUriProperty = BindableProperty.Create(
        nameof(StyleUri), typeof(string), typeof(MapView), MapStyles.Streets);

    public string StyleUri
    {
        get => (string)GetValue(StyleUriProperty);
        set => SetValue(StyleUriProperty, value);
    }

    public static readonly BindableProperty CameraProperty = BindableProperty.Create(
        nameof(Camera), typeof(MapCameraPosition), typeof(MapView),
        defaultValueCreator: _ => new MapCameraPosition(0, 0, 1));

    /// <summary>
    /// Desired camera position (one-way, instant). Use <see cref="FlyTo"/> for animated moves and
    /// <see cref="CurrentCamera"/> / <see cref="CameraChanged"/> to observe the live camera.
    /// </summary>
    public MapCameraPosition Camera
    {
        get => (MapCameraPosition)GetValue(CameraProperty);
        set => SetValue(CameraProperty, value);
    }

    public static readonly BindableProperty AnnotationsProperty = BindableProperty.Create(
        nameof(Annotations), typeof(ObservableRangeCollection<MapAnnotation>), typeof(MapView),
        defaultValueCreator: _ => new ObservableRangeCollection<MapAnnotation>());

    public ObservableRangeCollection<MapAnnotation> Annotations
    {
        get => (ObservableRangeCollection<MapAnnotation>)GetValue(AnnotationsProperty);
        set => SetValue(AnnotationsProperty, value);
    }

    public static readonly BindableProperty PolylinesProperty = BindableProperty.Create(
        nameof(Polylines), typeof(ObservableRangeCollection<MapPolyline>), typeof(MapView),
        defaultValueCreator: _ => new ObservableRangeCollection<MapPolyline>());

    public ObservableRangeCollection<MapPolyline> Polylines
    {
        get => (ObservableRangeCollection<MapPolyline>)GetValue(PolylinesProperty);
        set => SetValue(PolylinesProperty, value);
    }

    public static readonly BindableProperty PolygonsProperty = BindableProperty.Create(
        nameof(Polygons), typeof(ObservableRangeCollection<MapPolygon>), typeof(MapView),
        defaultValueCreator: _ => new ObservableRangeCollection<MapPolygon>());

    public ObservableRangeCollection<MapPolygon> Polygons
    {
        get => (ObservableRangeCollection<MapPolygon>)GetValue(PolygonsProperty);
        set => SetValue(PolygonsProperty, value);
    }

    public static readonly BindableProperty ViewAnnotationsProperty = BindableProperty.Create(
        nameof(ViewAnnotations), typeof(ObservableRangeCollection<MapViewAnnotation>), typeof(MapView),
        defaultValueCreator: _ => new ObservableRangeCollection<MapViewAnnotation>());

    /// <summary>MAUI views anchored to coordinates. Synced by id — adding/removing
    /// items creates/removes native view annotations.</summary>
    public ObservableRangeCollection<MapViewAnnotation> ViewAnnotations
    {
        get => (ObservableRangeCollection<MapViewAnnotation>)GetValue(ViewAnnotationsProperty);
        set => SetValue(ViewAnnotationsProperty, value);
    }

    public static readonly BindableProperty ShowUserLocationProperty = BindableProperty.Create(
        nameof(ShowUserLocation), typeof(bool), typeof(MapView), false);

    public bool ShowUserLocation
    {
        get => (bool)GetValue(ShowUserLocationProperty);
        set => SetValue(ShowUserLocationProperty, value);
    }

    public static readonly BindableProperty ScrollEnabledProperty = BindableProperty.Create(
        nameof(ScrollEnabled), typeof(bool), typeof(MapView), true);

    public bool ScrollEnabled
    {
        get => (bool)GetValue(ScrollEnabledProperty);
        set => SetValue(ScrollEnabledProperty, value);
    }

    public static readonly BindableProperty ZoomEnabledProperty = BindableProperty.Create(
        nameof(ZoomEnabled), typeof(bool), typeof(MapView), true);

    public bool ZoomEnabled
    {
        get => (bool)GetValue(ZoomEnabledProperty);
        set => SetValue(ZoomEnabledProperty, value);
    }

    public static readonly BindableProperty RotateEnabledProperty = BindableProperty.Create(
        nameof(RotateEnabled), typeof(bool), typeof(MapView), true);

    public bool RotateEnabled
    {
        get => (bool)GetValue(RotateEnabledProperty);
        set => SetValue(RotateEnabledProperty, value);
    }

    public static readonly BindableProperty PitchEnabledProperty = BindableProperty.Create(
        nameof(PitchEnabled), typeof(bool), typeof(MapView), true);

    public bool PitchEnabled
    {
        get => (bool)GetValue(PitchEnabledProperty);
        set => SetValue(PitchEnabledProperty, value);
    }

    /* --------------------------------- Commands ---------------------------------- */

    public static readonly BindableProperty MapReadyCommandProperty = BindableProperty.Create(
        nameof(MapReadyCommand), typeof(ICommand), typeof(MapView));

    public ICommand? MapReadyCommand
    {
        get => (ICommand?)GetValue(MapReadyCommandProperty);
        set => SetValue(MapReadyCommandProperty, value);
    }

    public static readonly BindableProperty StyleLoadedCommandProperty = BindableProperty.Create(
        nameof(StyleLoadedCommand), typeof(ICommand), typeof(MapView));

    public ICommand? StyleLoadedCommand
    {
        get => (ICommand?)GetValue(StyleLoadedCommandProperty);
        set => SetValue(StyleLoadedCommandProperty, value);
    }

    public static readonly BindableProperty MapClickedCommandProperty = BindableProperty.Create(
        nameof(MapClickedCommand), typeof(ICommand), typeof(MapView));

    public ICommand? MapClickedCommand
    {
        get => (ICommand?)GetValue(MapClickedCommandProperty);
        set => SetValue(MapClickedCommandProperty, value);
    }

    public static readonly BindableProperty MapLongPressedCommandProperty = BindableProperty.Create(
        nameof(MapLongPressedCommand), typeof(ICommand), typeof(MapView));

    public ICommand? MapLongPressedCommand
    {
        get => (ICommand?)GetValue(MapLongPressedCommandProperty);
        set => SetValue(MapLongPressedCommandProperty, value);
    }

    public static readonly BindableProperty AnnotationClickedCommandProperty = BindableProperty.Create(
        nameof(AnnotationClickedCommand), typeof(ICommand), typeof(MapView));

    public ICommand? AnnotationClickedCommand
    {
        get => (ICommand?)GetValue(AnnotationClickedCommandProperty);
        set => SetValue(AnnotationClickedCommandProperty, value);
    }

    public static readonly BindableProperty PolylineClickedCommandProperty = BindableProperty.Create(
        nameof(PolylineClickedCommand), typeof(ICommand), typeof(MapView));

    public ICommand? PolylineClickedCommand
    {
        get => (ICommand?)GetValue(PolylineClickedCommandProperty);
        set => SetValue(PolylineClickedCommandProperty, value);
    }

    public static readonly BindableProperty PolygonClickedCommandProperty = BindableProperty.Create(
        nameof(PolygonClickedCommand), typeof(ICommand), typeof(MapView));

    public ICommand? PolygonClickedCommand
    {
        get => (ICommand?)GetValue(PolygonClickedCommandProperty);
        set => SetValue(PolygonClickedCommandProperty, value);
    }

    public static readonly BindableProperty CameraChangedCommandProperty = BindableProperty.Create(
        nameof(CameraChangedCommand), typeof(ICommand), typeof(MapView));

    public ICommand? CameraChangedCommand
    {
        get => (ICommand?)GetValue(CameraChangedCommandProperty);
        set => SetValue(CameraChangedCommandProperty, value);
    }

    public static readonly BindableProperty OfflineRegionProgressCommandProperty = BindableProperty.Create(
        nameof(OfflineRegionProgressCommand), typeof(ICommand), typeof(MapView));

    public ICommand? OfflineRegionProgressCommand
    {
        get => (ICommand?)GetValue(OfflineRegionProgressCommandProperty);
        set => SetValue(OfflineRegionProgressCommandProperty, value);
    }

    public static readonly BindableProperty OfflineRegionCompletedCommandProperty = BindableProperty.Create(
        nameof(OfflineRegionCompletedCommand), typeof(ICommand), typeof(MapView));

    public ICommand? OfflineRegionCompletedCommand
    {
        get => (ICommand?)GetValue(OfflineRegionCompletedCommandProperty);
        set => SetValue(OfflineRegionCompletedCommandProperty, value);
    }

    /* ---------------------------------- Events ----------------------------------- */

    public event EventHandler? MapReady;
    public event EventHandler? StyleLoaded;
    public event EventHandler<MapClickedEventArgs>? MapClicked;
    public event EventHandler<MapClickedEventArgs>? MapLongPressed;
    public event EventHandler<AnnotationClickedEventArgs>? AnnotationClicked;
    public event EventHandler<PolylineClickedEventArgs>? PolylineClicked;
    public event EventHandler<PolygonClickedEventArgs>? PolygonClicked;
    public event EventHandler<CameraChangedEventArgs>? CameraChanged;
    public event EventHandler<OfflineRegionProgressEventArgs>? OfflineRegionProgress;
    public event EventHandler<OfflineRegionCompletedEventArgs>? OfflineRegionCompleted;

    /// <summary>Live camera position, updated on every camera change.</summary>
    public MapCameraPosition? CurrentCamera { get; private set; }

    /* ------------------------------ Imperative API -------------------------------- */

    /// <summary>Animates the camera to the target position.</summary>
    public void FlyTo(MapCameraPosition camera, int durationMs = 2000)
    {
        Handler?.Invoke(nameof(FlyTo), new FlyToRequest(camera, durationMs));
    }

    /// <summary>
    /// Adds a GeoJSON source or replaces the data of an existing one. Sources survive
    /// style switches — the native facade re-applies them after every style load.
    /// </summary>
    public void AddGeoJsonSource(string sourceId, string geoJson)
    {
        Handler?.Invoke(nameof(AddGeoJsonSource), new GeoJsonSourceRequest(sourceId, geoJson));
    }

    public void RemoveGeoJsonSource(string sourceId)
    {
        Handler?.Invoke(nameof(RemoveGeoJsonSource), sourceId);
    }

    /// <summary>Adds (or replaces) a style layer rendering a GeoJSON source.</summary>
    public void AddLayer(MapLayer layer)
    {
        Handler?.Invoke(nameof(AddLayer), layer);
    }

    public void RemoveLayer(string layerId)
    {
        Handler?.Invoke(nameof(RemoveLayer), layerId);
    }

    /// <summary>
    /// Adds a clustered point source with managed cluster/count/point layers.
    /// Tapping a cluster zooms to its expansion level automatically.
    /// </summary>
    public void AddClusteredSource(MapClusterSource source)
    {
        Handler?.Invoke(nameof(AddClusteredSource), source);
    }

    /// <summary>Removes a clustered source including its managed layers.</summary>
    public void RemoveClusteredSource(string sourceId)
    {
        Handler?.Invoke(nameof(RemoveClusteredSource), sourceId);
    }

    /// <summary>
    /// Downloads an offline region (style pack + tiles). Progress is reported via
    /// <see cref="OfflineRegionProgress"/> and <see cref="OfflineRegionCompleted"/>.
    /// </summary>
    public void DownloadOfflineRegion(MapOfflineRegion region)
    {
        Handler?.Invoke(nameof(DownloadOfflineRegion), region);
    }

    /// <summary>Cancels a running download and removes the region's tiles.</summary>
    public void RemoveOfflineRegion(string regionId)
    {
        Handler?.Invoke(nameof(RemoveOfflineRegion), regionId);
    }

    /* ------------------------- Internal event dispatchers ------------------------- */

    internal void SendMapReady()
    {
        MapReady?.Invoke(this, EventArgs.Empty);
        if (MapReadyCommand?.CanExecute(null) == true)
            MapReadyCommand.Execute(null);
    }

    internal void SendStyleLoaded()
    {
        StyleLoaded?.Invoke(this, EventArgs.Empty);
        if (StyleLoadedCommand?.CanExecute(null) == true)
            StyleLoadedCommand.Execute(null);
    }

    internal void SendMapClicked(double latitude, double longitude)
    {
        var args = new MapClickedEventArgs(latitude, longitude);
        MapClicked?.Invoke(this, args);
        if (MapClickedCommand?.CanExecute(args) == true)
            MapClickedCommand.Execute(args);
    }

    internal void SendMapLongPressed(double latitude, double longitude)
    {
        var args = new MapClickedEventArgs(latitude, longitude);
        MapLongPressed?.Invoke(this, args);
        if (MapLongPressedCommand?.CanExecute(args) == true)
            MapLongPressedCommand.Execute(args);
    }

    internal void SendAnnotationClicked(string annotationId)
    {
        var annotation = Annotations?.FirstOrDefault(a => a.Id == annotationId);
        if (annotation is null)
            return;

        var args = new AnnotationClickedEventArgs(annotation);
        AnnotationClicked?.Invoke(this, args);
        if (AnnotationClickedCommand?.CanExecute(args) == true)
            AnnotationClickedCommand.Execute(args);
    }

    internal void SendPolylineClicked(string polylineId)
    {
        var polyline = Polylines?.FirstOrDefault(p => p.Id == polylineId);
        if (polyline is null)
            return;

        var args = new PolylineClickedEventArgs(polyline);
        PolylineClicked?.Invoke(this, args);
        if (PolylineClickedCommand?.CanExecute(args) == true)
            PolylineClickedCommand.Execute(args);
    }

    internal void SendPolygonClicked(string polygonId)
    {
        var polygon = Polygons?.FirstOrDefault(p => p.Id == polygonId);
        if (polygon is null)
            return;

        var args = new PolygonClickedEventArgs(polygon);
        PolygonClicked?.Invoke(this, args);
        if (PolygonClickedCommand?.CanExecute(args) == true)
            PolygonClickedCommand.Execute(args);
    }

    internal void SendOfflineRegionProgress(string regionId, double progress)
    {
        var args = new OfflineRegionProgressEventArgs(regionId, progress);
        OfflineRegionProgress?.Invoke(this, args);
        if (OfflineRegionProgressCommand?.CanExecute(args) == true)
            OfflineRegionProgressCommand.Execute(args);
    }

    internal void SendOfflineRegionCompleted(string regionId, bool success, string? errorMessage)
    {
        var args = new OfflineRegionCompletedEventArgs(regionId, success, errorMessage);
        OfflineRegionCompleted?.Invoke(this, args);
        if (OfflineRegionCompletedCommand?.CanExecute(args) == true)
            OfflineRegionCompletedCommand.Execute(args);
    }

    internal void SendCameraChanged(MapCameraPosition camera)
    {
        CurrentCamera = camera;
        var args = new CameraChangedEventArgs(camera);
        CameraChanged?.Invoke(this, args);
        if (CameraChangedCommand?.CanExecute(args) == true)
            CameraChangedCommand.Execute(args);
    }
}

/// <summary>Payload for the AddGeoJsonSource command mapper.</summary>
public sealed class GeoJsonSourceRequest
{
    public string SourceId { get; }
    public string GeoJson { get; }

    public GeoJsonSourceRequest(string sourceId, string geoJson)
    {
        SourceId = sourceId;
        GeoJson = geoJson;
    }
}

/// <summary>Payload for the FlyTo command mapper.</summary>
public sealed class FlyToRequest
{
    public MapCameraPosition Camera { get; }
    public int DurationMs { get; }

    public FlyToRequest(MapCameraPosition camera, int durationMs)
    {
        Camera = camera;
        DurationMs = durationMs;
    }
}
