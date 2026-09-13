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

    public static readonly BindableProperty CameraChangedCommandProperty = BindableProperty.Create(
        nameof(CameraChangedCommand), typeof(ICommand), typeof(MapView));

    public ICommand? CameraChangedCommand
    {
        get => (ICommand?)GetValue(CameraChangedCommandProperty);
        set => SetValue(CameraChangedCommandProperty, value);
    }

    /* ---------------------------------- Events ----------------------------------- */

    public event EventHandler? MapReady;
    public event EventHandler? StyleLoaded;
    public event EventHandler<MapClickedEventArgs>? MapClicked;
    public event EventHandler<MapClickedEventArgs>? MapLongPressed;
    public event EventHandler<AnnotationClickedEventArgs>? AnnotationClicked;
    public event EventHandler<CameraChangedEventArgs>? CameraChanged;

    /// <summary>Live camera position, updated on every camera change.</summary>
    public MapCameraPosition? CurrentCamera { get; private set; }

    /* ------------------------------ Imperative API -------------------------------- */

    /// <summary>Animates the camera to the target position.</summary>
    public void FlyTo(MapCameraPosition camera, int durationMs = 2000)
    {
        Handler?.Invoke(nameof(FlyTo), new FlyToRequest(camera, durationMs));
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

    internal void SendCameraChanged(MapCameraPosition camera)
    {
        CurrentCamera = camera;
        var args = new CameraChangedEventArgs(camera);
        CameraChanged?.Invoke(this, args);
        if (CameraChangedCommand?.CanExecute(args) == true)
            CameraChangedCommand.Execute(args);
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
