namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>How the sky is painted.</summary>
public enum MapSkyType
{
    /// <summary>Physically-based sky lit by a sun position.</summary>
    Atmosphere,

    /// <summary>A plain colour gradient.</summary>
    Gradient,
}

/// <summary>
/// The sky above the horizon. Assign one to <c>MapView.Sky</c>; null falls back to the
/// defaults the terrain feature uses, and removes the sky entirely when terrain is off.
/// </summary>
/// <remarks>
/// The sky is only visible with a pitched camera — at pitch 0 you are looking straight
/// down and there is no horizon on screen.
/// </remarks>
public class MapSky
{
    public MapSkyType Type { get; set; } = MapSkyType.Atmosphere;

    /// <summary>Sun azimuth in degrees, 0 = north.</summary>
    public double? SunAzimuth { get; set; }

    /// <summary>Sun elevation in degrees, 0 = at the horizon, 90 = overhead.</summary>
    public double? SunElevation { get; set; }

    /// <summary>Brightness of the sun's glow.</summary>
    public double? SunIntensity { get; set; }

    /// <summary>Opacity of the whole sky layer, 0–1.</summary>
    public double? Opacity { get; set; }
}
