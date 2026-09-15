namespace Indiko.Maui.Controls.MapBox;

public static class BuilderExtension
{
    /// <summary>
    /// Registers the MapView handler and configures the Mapbox public access token (pk.…).
    /// Call this in MauiProgram.cs before the first MapView is created.
    /// </summary>
    /// <param name="accessToken">Mapbox public access token (pk.…).</param>
    /// <param name="enableTelemetry">
    /// Whether Mapbox may collect location telemetry. The Mapbox SDK has this on by default and
    /// sends device locations to Mapbox; pass false to switch it off for every user. Apps that
    /// advertise "no location leaves the device" — or that would otherwise have to declare shared
    /// location data in App Store / Play data-safety forms — want false here.
    /// </param>
    public static MauiAppBuilder UseMapbox(
        this MauiAppBuilder builder,
        string accessToken,
        bool enableTelemetry = true)
    {
        MapboxConfig.AccessToken = accessToken;
        MapboxConfig.TelemetryEnabled = enableTelemetry;

        builder.ConfigureMauiHandlers(handlers =>
        {
#if ANDROID
            handlers.AddHandler<MapView, Platforms.Android.MapViewHandler>();
#elif IOS
            handlers.AddHandler<MapView, Platforms.iOS.MapViewHandler>();
#endif
        });

        return builder;
    }
}
