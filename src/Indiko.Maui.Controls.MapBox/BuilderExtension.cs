namespace Indiko.Maui.Controls.MapBox;

public static class BuilderExtension
{
    /// <summary>
    /// Registers the MapView handler and configures the Mapbox public access token (pk.…).
    /// Call this in MauiProgram.cs before the first MapView is created.
    /// </summary>
    public static MauiAppBuilder UseMapbox(this MauiAppBuilder builder, string accessToken)
    {
        MapboxConfig.AccessToken = accessToken;

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
