namespace Indiko.Maui.Controls.MapBox;

/// <summary>
/// Holds the Mapbox public access token (pk.…) provided via UseMapbox().
/// The platform handlers apply it to the native SDK before the first map is created.
/// </summary>
public static class MapboxConfig
{
    public static string? AccessToken { get; set; }
}
