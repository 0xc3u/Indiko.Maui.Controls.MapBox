using System.Windows.Input;
using Indiko.Maui.Controls.MapBox.Handlers;
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

    public static readonly BindableProperty AutoFitBoundsProperty = BindableProperty.Create(
        nameof(AutoFitBounds), typeof(bool), typeof(MapView), false,
        propertyChanged: (bindable, _, newValue) =>
        {
            if (newValue is true)
                ((MapView)bindable).TryAutoFit();
        });

    /// <summary>
    /// When enabled, the camera automatically fits all map content (annotations,
    /// polylines, polygons, view annotations) whenever it changes.
    /// </summary>
    public bool AutoFitBounds
    {
        get => (bool)GetValue(AutoFitBoundsProperty);
        set => SetValue(AutoFitBoundsProperty, value);
    }

    public static readonly BindableProperty FitBoundsPaddingProperty = BindableProperty.Create(
        nameof(FitBoundsPadding), typeof(double), typeof(MapView), 40.0);

    /// <summary>Uniform edge padding (device-independent units) used by FitBounds/AutoFitBounds.</summary>
    public double FitBoundsPadding
    {
        get => (double)GetValue(FitBoundsPaddingProperty);
        set => SetValue(FitBoundsPaddingProperty, value);
    }

    public static readonly BindableProperty FollowPuckProperty = BindableProperty.Create(
        nameof(FollowPuck), typeof(bool), typeof(MapView), false);

    /// <summary>
    /// When enabled, the camera follows the user-location puck (the puck is shown
    /// implicitly). The mode ends when the user pans the map — the property is updated
    /// and <see cref="FollowPuckChanged"/> is raised.
    /// </summary>
    public bool FollowPuck
    {
        get => (bool)GetValue(FollowPuckProperty);
        set => SetValue(FollowPuckProperty, value);
    }

    public static readonly BindableProperty FollowPuckZoomProperty = BindableProperty.Create(
        nameof(FollowPuckZoom), typeof(double), typeof(MapView), 16.0);

    /// <summary>Zoom level used while following the puck.</summary>
    public double FollowPuckZoom
    {
        get => (double)GetValue(FollowPuckZoomProperty);
        set => SetValue(FollowPuckZoomProperty, value);
    }

    public static readonly BindableProperty FollowPuckTrackBearingProperty = BindableProperty.Create(
        nameof(FollowPuckTrackBearing), typeof(bool), typeof(MapView), false);

    /// <summary>Rotate the map with the puck's heading instead of keeping north up.</summary>
    public bool FollowPuckTrackBearing
    {
        get => (bool)GetValue(FollowPuckTrackBearingProperty);
        set => SetValue(FollowPuckTrackBearingProperty, value);
    }

    /// <summary>Guards the FollowPuck property mapper while syncing state from native.</summary>
    internal bool SuppressFollowPuckSync;

    public static readonly BindableProperty DoubleTapToZoomEnabledProperty = BindableProperty.Create(
        nameof(DoubleTapToZoomEnabled), typeof(bool), typeof(MapView), true);

    /// <summary>Double-tap to zoom in and two-finger-tap to zoom out. Narrows <see cref="ZoomEnabled"/>; it cannot re-enable a disabled gesture.</summary>
    public bool DoubleTapToZoomEnabled
    {
        get => (bool)GetValue(DoubleTapToZoomEnabledProperty);
        set => SetValue(DoubleTapToZoomEnabledProperty, value);
    }

    public static readonly BindableProperty QuickZoomEnabledProperty = BindableProperty.Create(
        nameof(QuickZoomEnabled), typeof(bool), typeof(MapView), true);

    /// <summary>Double-tap-and-drag to zoom with one finger. Narrows <see cref="ZoomEnabled"/>; it cannot re-enable a disabled gesture.</summary>
    public bool QuickZoomEnabled
    {
        get => (bool)GetValue(QuickZoomEnabledProperty);
        set => SetValue(QuickZoomEnabledProperty, value);
    }

    public static readonly BindableProperty PinchToZoomEnabledProperty = BindableProperty.Create(
        nameof(PinchToZoomEnabled), typeof(bool), typeof(MapView), true);

    /// <summary>Two-finger pinch to zoom. Narrows <see cref="ZoomEnabled"/>; it cannot re-enable a disabled gesture.</summary>
    public bool PinchToZoomEnabled
    {
        get => (bool)GetValue(PinchToZoomEnabledProperty);
        set => SetValue(PinchToZoomEnabledProperty, value);
    }

    public static readonly BindableProperty SimultaneousRotateAndZoomEnabledProperty = BindableProperty.Create(
        nameof(SimultaneousRotateAndZoomEnabled), typeof(bool), typeof(MapView), true);

    /// <summary>Allow rotating and pinch-zooming in the same gesture. Narrows <see cref="ZoomEnabled"/>; it cannot re-enable a disabled gesture.</summary>
    public bool SimultaneousRotateAndZoomEnabled
    {
        get => (bool)GetValue(SimultaneousRotateAndZoomEnabledProperty);
        set => SetValue(SimultaneousRotateAndZoomEnabledProperty, value);
    }

    public static readonly BindableProperty ShowUserLocationProperty = BindableProperty.Create(
        nameof(ShowUserLocation), typeof(bool), typeof(MapView), false);

    public bool ShowUserLocation
    {
        get => (bool)GetValue(ShowUserLocationProperty);
        set => SetValue(ShowUserLocationProperty, value);
    }

    public static readonly BindableProperty MapLanguageProperty = BindableProperty.Create(
        nameof(MapLanguage), typeof(string), typeof(MapView));

    /// <summary>
    /// Translates the style's name labels into this language — a BCP-47 tag such as
    /// "de", "fr" or "fr-CH". Null or empty keeps the style's own labels, which are
    /// local names. Only names Mapbox has a translation for change; the rest stay local.
    /// </summary>
    public string? MapLanguage
    {
        get => (string?)GetValue(MapLanguageProperty);
        set => SetValue(MapLanguageProperty, value);
    }

    public static readonly BindableProperty AtmosphereProperty = BindableProperty.Create(
        nameof(Atmosphere), typeof(MapAtmosphere), typeof(MapView));

    /// <summary>
    /// The atmosphere (fog) around the map; null leaves the style's own. Most visible with
    /// <see cref="MapProjection.Globe"/>, where it is the glow at the limb and the space
    /// behind the planet.
    /// </summary>
    public MapAtmosphere? Atmosphere
    {
        get => (MapAtmosphere?)GetValue(AtmosphereProperty);
        set => SetValue(AtmosphereProperty, value);
    }

    public static readonly BindableProperty ProjectionProperty = BindableProperty.Create(
        nameof(Projection), typeof(MapProjection), typeof(MapView), MapProjection.Default);

    /// <summary>
    /// Map projection. <see cref="MapProjection.Globe"/> renders a 3D globe when zoomed
    /// out; <see cref="MapProjection.Default"/> keeps the style's own choice.
    /// </summary>
    public MapProjection Projection
    {
        get => (MapProjection)GetValue(ProjectionProperty);
        set => SetValue(ProjectionProperty, value);
    }

    public static readonly BindableProperty LightPresetProperty = BindableProperty.Create(
        nameof(LightPreset), typeof(MapLightPreset), typeof(MapView), MapLightPreset.Default);

    /// <summary>
    /// Time-of-day lighting — only the Mapbox Standard styles support it; classic styles
    /// ignore it. See <see cref="MapStyles.Standard"/>.
    /// </summary>
    public MapLightPreset LightPreset
    {
        get => (MapLightPreset)GetValue(LightPresetProperty);
        set => SetValue(LightPresetProperty, value);
    }

    public static readonly BindableProperty StyleThemeProperty = BindableProperty.Create(
        nameof(StyleTheme), typeof(MapStyleTheme), typeof(MapView), MapStyleTheme.Default);

    /// <summary>Colour treatment of the Mapbox Standard styles.</summary>
    public MapStyleTheme StyleTheme
    {
        get => (MapStyleTheme)GetValue(StyleThemeProperty);
        set => SetValue(StyleThemeProperty, value);
    }

    public static readonly BindableProperty Show3dObjectsProperty = BindableProperty.Create(
        nameof(Show3dObjects), typeof(bool?), typeof(MapView));

    /// <summary>
    /// Whether the Mapbox Standard styles draw their 3D buildings and landmarks;
    /// null keeps the style's own setting.
    /// </summary>
    public bool? Show3dObjects
    {
        get => (bool?)GetValue(Show3dObjectsProperty);
        set => SetValue(Show3dObjectsProperty, value);
    }

    public static readonly BindableProperty CameraPaddingProperty = BindableProperty.Create(
        nameof(CameraPadding), typeof(Thickness), typeof(MapView), new Thickness(0));

    /// <summary>
    /// Shifts the camera's anchor inside the viewport (device-independent units). Set the
    /// bottom inset to the height of a bottom sheet and the camera centre — and everything
    /// positioned by it, like <see cref="FlyTo"/> and the follow-puck — lands in the
    /// visible part of the map instead of behind the sheet.
    /// </summary>
    public Thickness CameraPadding
    {
        get => (Thickness)GetValue(CameraPaddingProperty);
        set => SetValue(CameraPaddingProperty, value);
    }

    public static readonly BindableProperty CompassPlacementProperty = BindableProperty.Create(
        nameof(CompassPlacement), typeof(MapOrnamentPlacement), typeof(MapView));

    /// <summary>Where the compass sits; null keeps the SDK default.</summary>
    public MapOrnamentPlacement? CompassPlacement
    {
        get => (MapOrnamentPlacement?)GetValue(CompassPlacementProperty);
        set => SetValue(CompassPlacementProperty, value);
    }

    public static readonly BindableProperty ScaleBarPlacementProperty = BindableProperty.Create(
        nameof(ScaleBarPlacement), typeof(MapOrnamentPlacement), typeof(MapView));

    /// <summary>Where the scale bar sits; null keeps the SDK default.</summary>
    public MapOrnamentPlacement? ScaleBarPlacement
    {
        get => (MapOrnamentPlacement?)GetValue(ScaleBarPlacementProperty);
        set => SetValue(ScaleBarPlacementProperty, value);
    }

    public static readonly BindableProperty LogoPlacementProperty = BindableProperty.Create(
        nameof(LogoPlacement), typeof(MapOrnamentPlacement), typeof(MapView));

    /// <summary>Where the Mapbox logo sits; null keeps the SDK default.</summary>
    public MapOrnamentPlacement? LogoPlacement
    {
        get => (MapOrnamentPlacement?)GetValue(LogoPlacementProperty);
        set => SetValue(LogoPlacementProperty, value);
    }

    public static readonly BindableProperty AttributionPlacementProperty = BindableProperty.Create(
        nameof(AttributionPlacement), typeof(MapOrnamentPlacement), typeof(MapView));

    /// <summary>Where the attribution button sits; null keeps the SDK default.</summary>
    public MapOrnamentPlacement? AttributionPlacement
    {
        get => (MapOrnamentPlacement?)GetValue(AttributionPlacementProperty);
        set => SetValue(AttributionPlacementProperty, value);
    }

    public static readonly BindableProperty CameraBoundsProperty = BindableProperty.Create(
        nameof(CameraBounds), typeof(MapBounds), typeof(MapView));

    /// <summary>
    /// Restricts panning to a bounding box. Null lifts the restriction. The camera is
    /// kept inside the box, so the visible area can still extend past it when zoomed out.
    /// </summary>
    public MapBounds? CameraBounds
    {
        get => (MapBounds?)GetValue(CameraBoundsProperty);
        set => SetValue(CameraBoundsProperty, value);
    }

    public static readonly BindableProperty MinZoomProperty = BindableProperty.Create(
        nameof(MinZoom), typeof(double?), typeof(MapView));

    /// <summary>Lowest zoom the user can reach; null = SDK default (0).</summary>
    public double? MinZoom
    {
        get => (double?)GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public static readonly BindableProperty MaxZoomProperty = BindableProperty.Create(
        nameof(MaxZoom), typeof(double?), typeof(MapView));

    /// <summary>Highest zoom the user can reach; null = SDK default (22).</summary>
    public double? MaxZoom
    {
        get => (double?)GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    public static readonly BindableProperty MinPitchProperty = BindableProperty.Create(
        nameof(MinPitch), typeof(double?), typeof(MapView));

    /// <summary>Lowest camera pitch in degrees; null = SDK default (0).</summary>
    public double? MinPitch
    {
        get => (double?)GetValue(MinPitchProperty);
        set => SetValue(MinPitchProperty, value);
    }

    public static readonly BindableProperty MaxPitchProperty = BindableProperty.Create(
        nameof(MaxPitch), typeof(double?), typeof(MapView));

    /// <summary>Highest camera pitch in degrees; null = SDK default (85).</summary>
    public double? MaxPitch
    {
        get => (double?)GetValue(MaxPitchProperty);
        set => SetValue(MaxPitchProperty, value);
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

    public static readonly BindableProperty TerrainEnabledProperty = BindableProperty.Create(
        nameof(TerrainEnabled), typeof(bool), typeof(MapView), false);

    /// <summary>
    /// Renders 3D terrain (Mapbox DEM + sky atmosphere). Survives style switches.
    /// Tilt the camera (<see cref="MapCameraPosition.Pitch"/>) to see the relief.
    /// </summary>
    public bool TerrainEnabled
    {
        get => (bool)GetValue(TerrainEnabledProperty);
        set => SetValue(TerrainEnabledProperty, value);
    }

    public static readonly BindableProperty TerrainExaggerationProperty = BindableProperty.Create(
        nameof(TerrainExaggeration), typeof(double), typeof(MapView), 1.5);

    /// <summary>Vertical exaggeration of the terrain (1.0 = realistic, default 1.5).</summary>
    public double TerrainExaggeration
    {
        get => (double)GetValue(TerrainExaggerationProperty);
        set => SetValue(TerrainExaggerationProperty, value);
    }

    public static readonly BindableProperty ShowCompassProperty = BindableProperty.Create(
        nameof(ShowCompass), typeof(bool), typeof(MapView), true);

    /// <summary>Shows the compass ornament while the map is rotated (default true).</summary>
    public bool ShowCompass
    {
        get => (bool)GetValue(ShowCompassProperty);
        set => SetValue(ShowCompassProperty, value);
    }

    public static readonly BindableProperty ShowScaleBarProperty = BindableProperty.Create(
        nameof(ShowScaleBar), typeof(bool), typeof(MapView), true);

    /// <summary>Shows the scale bar ornament (default true).</summary>
    public bool ShowScaleBar
    {
        get => (bool)GetValue(ShowScaleBarProperty);
        set => SetValue(ShowScaleBarProperty, value);
    }

    public static readonly BindableProperty ShowMapboxLogoProperty = BindableProperty.Create(
        nameof(ShowMapboxLogo), typeof(bool), typeof(MapView), true);

    /// <summary>
    /// Shows the Mapbox logo ornament (default true). Hiding it may require a Mapbox
    /// plan that permits white-labeling — the app is responsible for compliance.
    /// </summary>
    public bool ShowMapboxLogo
    {
        get => (bool)GetValue(ShowMapboxLogoProperty);
        set => SetValue(ShowMapboxLogoProperty, value);
    }

    public static readonly BindableProperty ShowAttributionProperty = BindableProperty.Create(
        nameof(ShowAttribution), typeof(bool), typeof(MapView), true);

    /// <summary>
    /// Shows the attribution ornament (default true). Hiding it may require a Mapbox
    /// plan that permits it — the app is responsible for compliance.
    /// </summary>
    public bool ShowAttribution
    {
        get => (bool)GetValue(ShowAttributionProperty);
        set => SetValue(ShowAttributionProperty, value);
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

    public static readonly BindableProperty AnnotationDraggedCommandProperty = BindableProperty.Create(
        nameof(AnnotationDraggedCommand), typeof(ICommand), typeof(MapView));

    public ICommand? AnnotationDraggedCommand
    {
        get => (ICommand?)GetValue(AnnotationDraggedCommandProperty);
        set => SetValue(AnnotationDraggedCommandProperty, value);
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

    public static readonly BindableProperty FollowPuckChangedCommandProperty = BindableProperty.Create(
        nameof(FollowPuckChangedCommand), typeof(ICommand), typeof(MapView));

    public ICommand? FollowPuckChangedCommand
    {
        get => (ICommand?)GetValue(FollowPuckChangedCommandProperty);
        set => SetValue(FollowPuckChangedCommandProperty, value);
    }

    public static readonly BindableProperty MapLoadErrorCommandProperty = BindableProperty.Create(
        nameof(MapLoadErrorCommand), typeof(ICommand), typeof(MapView));

    public ICommand? MapLoadErrorCommand
    {
        get => (ICommand?)GetValue(MapLoadErrorCommandProperty);
        set => SetValue(MapLoadErrorCommandProperty, value);
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
    public event EventHandler<AnnotationDraggedEventArgs>? AnnotationDragged;
    public event EventHandler<PolylineClickedEventArgs>? PolylineClicked;
    public event EventHandler<PolygonClickedEventArgs>? PolygonClicked;
    public event EventHandler<CameraChangedEventArgs>? CameraChanged;
    public event EventHandler<OfflineRegionProgressEventArgs>? OfflineRegionProgress;
    public event EventHandler<OfflineRegionCompletedEventArgs>? OfflineRegionCompleted;
    public event EventHandler<FollowPuckChangedEventArgs>? FollowPuckChanged;

    /// <summary>
    /// A resource the map needed could not be loaded — most often "offline and outside any
    /// downloaded region". Without this the only symptom is a map that silently stays blank.
    /// <see cref="MapLoadErrorType.Tile"/> fires once per failed tile and arrives in bursts;
    /// filter on <see cref="MapLoadErrorEventArgs.Type"/> before showing anything to a user.
    /// </summary>
    public event EventHandler<MapLoadErrorEventArgs>? MapLoadError;

    /// <summary>Live camera position, updated on every camera change.</summary>
    public MapCameraPosition? CurrentCamera { get; private set; }

    /* ------------------------------ Imperative API -------------------------------- */

    // Imperative calls issued before the handler is attached (e.g. from a page
    // constructor) are queued and replayed in order once the handler connects.
    private readonly List<(string Command, object? Args)> pendingCommands = [];

    private void InvokeOrQueue(string command, object? args)
    {
        if (Handler is null)
        {
            pendingCommands.Add((command, args));
            return;
        }

        Handler.Invoke(command, args);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler is null || pendingCommands.Count == 0)
            return;

        var queued = pendingCommands.ToArray();
        pendingCommands.Clear();
        foreach (var (command, args) in queued)
            Handler.Invoke(command, args);
    }

    /// <summary>Animates the camera to the target position.</summary>
    public void FlyTo(MapCameraPosition camera, int durationMs = 2000)
    {
        InvokeOrQueue(nameof(FlyTo), new FlyToRequest(camera, durationMs));
    }

    /// <summary>Moves the camera so the bounding box is fully visible. durationMs 0 jumps instantly.</summary>
    public void FitBounds(MapBounds bounds, double? padding = null, int durationMs = 1000)
    {
        InvokeOrQueue(nameof(FitBounds),
            new FitBoundsRequest(bounds, padding ?? FitBoundsPadding, durationMs));
    }

    /// <summary>Fits the camera to all current map content (no-op when the map is empty).</summary>
    public void FitBoundsToContent(int durationMs = 1000)
    {
        var bounds = ComputeContentBounds();
        if (bounds is not null)
            FitBounds(bounds, durationMs: durationMs);
    }

    internal void TryAutoFit()
    {
        if (AutoFitBounds)
            FitBoundsToContent(600);
    }

    private MapBounds? ComputeContentBounds()
    {
        IEnumerable<(double, double)> Positions()
        {
            foreach (var annotation in Annotations ?? [])
                yield return (annotation.Latitude, annotation.Longitude);
            foreach (var polyline in Polylines ?? [])
                foreach (var point in polyline.Points)
                    yield return (point.Latitude, point.Longitude);
            foreach (var polygon in Polygons ?? [])
                foreach (var point in polygon.Points)
                    yield return (point.Latitude, point.Longitude);
            foreach (var viewAnnotation in ViewAnnotations ?? [])
                yield return (viewAnnotation.Latitude, viewAnnotation.Longitude);
        }

        return MapBounds.FromPositions(Positions());
    }

    /// <summary>
    /// Adds a GeoJSON source or replaces the data of an existing one. Sources survive
    /// style switches — the native facade re-applies them after every style load.
    /// </summary>
    public void AddGeoJsonSource(string sourceId, string geoJson)
    {
        InvokeOrQueue(nameof(AddGeoJsonSource), new GeoJsonSourceRequest(sourceId, geoJson));
    }

    /// <summary>
    /// Adds or replaces a raster, vector, raster-DEM or image source. A source renders
    /// nothing on its own — add a <see cref="MapLayer"/> that reads it.
    /// </summary>
    public void AddSource(MapSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        InvokeOrQueue(nameof(AddSource), source);
    }

    /// <summary>Removes a source added with <see cref="AddSource"/>.</summary>
    public void RemoveSource(string sourceId)
    {
        InvokeOrQueue(nameof(RemoveSource), sourceId);
    }

    public void RemoveGeoJsonSource(string sourceId)
    {
        InvokeOrQueue(nameof(RemoveGeoJsonSource), sourceId);
    }

    /// <summary>Adds (or replaces) a style layer rendering a GeoJSON source.</summary>
    public void AddLayer(MapLayer layer)
    {
        InvokeOrQueue(nameof(AddLayer), layer);
    }

    public void RemoveLayer(string layerId)
    {
        InvokeOrQueue(nameof(RemoveLayer), layerId);
    }

    /// <summary>
    /// Adds a clustered point source with managed cluster/count/point layers.
    /// Tapping a cluster zooms to its expansion level automatically.
    /// </summary>
    public void AddClusteredSource(MapClusterSource source)
    {
        InvokeOrQueue(nameof(AddClusteredSource), source);
    }

    /// <summary>Removes a clustered source including its managed layers.</summary>
    public void RemoveClusteredSource(string sourceId)
    {
        InvokeOrQueue(nameof(RemoveClusteredSource), sourceId);
    }

    /// <summary>
    /// Downloads an offline region (style pack + tiles). Progress is reported via
    /// <see cref="OfflineRegionProgress"/> and <see cref="OfflineRegionCompleted"/>.
    /// </summary>
    public void DownloadOfflineRegion(MapOfflineRegion region)
    {
        InvokeOrQueue(nameof(DownloadOfflineRegion), region);
    }

    /// <summary>Cancels a running download and removes the region's tiles.</summary>
    public void RemoveOfflineRegion(string regionId)
    {
        InvokeOrQueue(nameof(RemoveOfflineRegion), regionId);
    }

    /// <summary>
    /// Returns the currently visible viewport as a bounding box, or null while the
    /// handler is not attached yet.
    /// </summary>
    public MapBounds? GetVisibleBounds()
    {
        if (Handler is null)
            return null;

        var request = new VisibleBoundsRequest();
        Handler.Invoke(nameof(GetVisibleBounds), request);
        return request.Bounds;
    }

    /// <summary>
    /// Returns the terrain elevation in meters above sea level at a coordinate, or null
    /// when it is not available. Requires <see cref="TerrainEnabled"/> with a
    /// <see cref="TerrainExaggeration"/> above 0 (the result is corrected for the
    /// exaggeration), and the elevation tile for the coordinate must be loaded —
    /// in practice the coordinate is on screen, and offline it lies in a region
    /// downloaded with <see cref="MapOfflineRegion.IncludeTerrain"/>. With a camera
    /// pitch of 0 the map still looks flat, so terrain can stay enabled just for
    /// elevation queries. Typical use: query inside a <see cref="MapClicked"/> handler.
    /// </summary>
    public double? QueryTerrainElevation(double latitude, double longitude)
    {
        if (Handler is null)
            return null;

        var request = new TerrainElevationRequest(latitude, longitude);
        Handler.Invoke(nameof(QueryTerrainElevation), request);
        return request.Elevation;
    }

    /// <summary>
    /// Reports the rendered features under a coordinate — the way to find out *which*
    /// feature a <see cref="MapClicked"/> tap hit, for data drawn by your own layers.
    /// Pass <paramref name="layerIds"/> to restrict the query to your layers, otherwise
    /// the basemap's own features are included too.
    /// </summary>
    /// <param name="radius">
    /// Hit radius in device-independent units. A few units make thin lines and small
    /// circles practical to hit; 0 queries the exact point.
    /// </param>
    public Task<IReadOnlyList<MapQueriedFeature>> QueryFeaturesAsync(double latitude, double longitude,
        IEnumerable<string>? layerIds = null, double radius = 8)
    {
        var completion = new TaskCompletionSource<IReadOnlyList<MapQueriedFeature>>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        if (Handler is null)
        {
            completion.TrySetResult([]);
            return completion.Task;
        }

        var request = new QueryFeaturesRequest(
            latitude, longitude, AnnotationSerializer.ToStringArrayJson(layerIds), radius,
            json => completion.TrySetResult(AnnotationSerializer.ParseQueriedFeatures(json)));
        Handler.Invoke(nameof(QueryFeaturesAsync), request);
        return completion.Task;
    }

    /// <summary>
    /// Attaches state to a feature — selection, hover, anything your layers should react
    /// to. Layers read it with the <c>["feature-state", "key"]</c> expression, so the look
    /// changes without touching the source data.
    /// </summary>
    /// <remarks>
    /// The feature must carry an <c>id</c>: GeoJSON features without one are invisible to
    /// this. <paramref name="sourceLayer"/> is only needed for vector sources.
    /// </remarks>
    public void SetFeatureState(string sourceId, string featureId, IDictionary<string, object?> state,
        string? sourceLayer = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId);
        ArgumentException.ThrowIfNullOrEmpty(featureId);
        ArgumentNullException.ThrowIfNull(state);

        InvokeOrQueue(nameof(SetFeatureState), new FeatureStateRequest(
            sourceId, sourceLayer ?? string.Empty, featureId, AnnotationSerializer.ToStateJson(state)));
    }

    /// <summary>
    /// Removes one state key from a feature, or its whole state when
    /// <paramref name="stateKey"/> is null.
    /// </summary>
    public void RemoveFeatureState(string sourceId, string featureId, string? stateKey = null,
        string? sourceLayer = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId);
        ArgumentException.ThrowIfNullOrEmpty(featureId);

        InvokeOrQueue(nameof(RemoveFeatureState), new FeatureStateRequest(
            sourceId, sourceLayer ?? string.Empty, featureId, stateKey ?? string.Empty));
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

    internal void SendAnnotationDragged(string annotationId, double latitude, double longitude)
    {
        var annotation = Annotations?.FirstOrDefault(a => a.Id == annotationId);
        if (annotation is null)
            return;

        // Keep the model in sync so a later collection push re-creates the marker
        // at its dragged position.
        annotation.Latitude = latitude;
        annotation.Longitude = longitude;

        var args = new AnnotationDraggedEventArgs(annotation, latitude, longitude);
        AnnotationDragged?.Invoke(this, args);
        if (AnnotationDraggedCommand?.CanExecute(args) == true)
            AnnotationDraggedCommand.Execute(args);
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

    internal void SendFollowPuckChanged(bool isActive)
    {
        if (FollowPuck != isActive)
        {
            SuppressFollowPuckSync = true;
            FollowPuck = isActive;
            SuppressFollowPuckSync = false;
        }

        var args = new FollowPuckChangedEventArgs(isActive);
        FollowPuckChanged?.Invoke(this, args);
        if (FollowPuckChangedCommand?.CanExecute(args) == true)
            FollowPuckChangedCommand.Execute(args);
    }

    internal void SendMapLoadError(string type, string message, string? sourceId)
    {
        var parsed = type switch
        {
            "style" => MapLoadErrorType.Style,
            "sprite" => MapLoadErrorType.Sprite,
            "source" => MapLoadErrorType.Source,
            "tile" => MapLoadErrorType.Tile,
            "glyphs" => MapLoadErrorType.Glyphs,
            _ => MapLoadErrorType.Unknown,
        };

        var args = new MapLoadErrorEventArgs(parsed, message, sourceId);
        MapLoadError?.Invoke(this, args);
        if (MapLoadErrorCommand?.CanExecute(args) == true)
            MapLoadErrorCommand.Execute(args);
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

/// <summary>Payload for the FitBounds command mapper.</summary>
public sealed class FitBoundsRequest
{
    public MapBounds Bounds { get; }
    public double Padding { get; }
    public int DurationMs { get; }

    public FitBoundsRequest(MapBounds bounds, double padding, int durationMs)
    {
        Bounds = bounds;
        Padding = padding;
        DurationMs = durationMs;
    }
}

/// <summary>Payload for the GetVisibleBounds command mapper — the handler fills Bounds.</summary>
public sealed class VisibleBoundsRequest
{
    public MapBounds? Bounds { get; set; }
}

/// <summary>Payload for the feature-state command mappers. Payload is the state JSON for
/// SetFeatureState and the state key (possibly empty) for RemoveFeatureState.</summary>
public sealed class FeatureStateRequest
{
    public string SourceId { get; }
    public string SourceLayer { get; }
    public string FeatureId { get; }
    public string Payload { get; }

    public FeatureStateRequest(string sourceId, string sourceLayer, string featureId, string payload)
    {
        SourceId = sourceId;
        SourceLayer = sourceLayer;
        FeatureId = featureId;
        Payload = payload;
    }
}

/// <summary>Payload for the QueryFeaturesAsync command mapper.</summary>
public sealed class QueryFeaturesRequest
{
    public double Latitude { get; }
    public double Longitude { get; }
    public string LayerIdsJson { get; }
    public double Radius { get; }

    /// <summary>Invoked with the facade's JSON array, on an arbitrary thread.</summary>
    public Action<string> OnResult { get; }

    public QueryFeaturesRequest(double latitude, double longitude, string layerIdsJson, double radius,
        Action<string> onResult)
    {
        Latitude = latitude;
        Longitude = longitude;
        LayerIdsJson = layerIdsJson;
        Radius = radius;
        OnResult = onResult;
    }
}

/// <summary>Payload for the QueryTerrainElevation command mapper — the handler fills Elevation.</summary>
public sealed class TerrainElevationRequest
{
    public double Latitude { get; }
    public double Longitude { get; }
    public double? Elevation { get; set; }

    public TerrainElevationRequest(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
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
