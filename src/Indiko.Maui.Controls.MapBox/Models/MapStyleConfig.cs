namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>
/// Time-of-day lighting of the Mapbox Standard style. <see cref="Default"/> leaves the
/// style's own setting alone.
/// </summary>
public enum MapLightPreset
{
    Default,
    Dawn,
    Day,
    Dusk,
    Night,
}

/// <summary>
/// Colour treatment of the Mapbox Standard style. <see cref="Default"/> leaves the
/// style's own setting alone.
/// </summary>
public enum MapStyleTheme
{
    Default,
    Faded,
    Monochrome,
}
