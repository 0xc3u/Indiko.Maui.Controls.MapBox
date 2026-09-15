namespace Indiko.Maui.Controls.MapBox;

/// <summary>
/// Holds the Mapbox public access token (pk.…) provided via UseMapbox().
/// The platform handlers apply it to the native SDK before the first map is created.
/// </summary>
public static class MapboxConfig
{
    public static string? AccessToken { get; set; }

    /// <summary>
    /// Whether Mapbox may collect location telemetry. Defaults to true, which is the
    /// Mapbox SDK's own default. Set it to false via UseMapbox(token, enableTelemetry: false)
    /// if your app must not send device locations to Mapbox — the platform handlers apply
    /// this before the first map is created.
    /// </summary>
    public static bool TelemetryEnabled { get; set; } = true;
}
