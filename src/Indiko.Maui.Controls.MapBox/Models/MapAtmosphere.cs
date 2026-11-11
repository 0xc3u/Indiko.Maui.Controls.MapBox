namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>
/// The atmosphere (fog) drawn around the map — most visible on the globe projection,
/// where it is the glow at the limb and the space behind it. Assign one to
/// <c>MapView.Atmosphere</c>; null removes it and lets the style decide.
/// </summary>
public class MapAtmosphere
{
    /// <summary>Colour of the lower atmosphere, as #RRGGBB or rgba(…).</summary>
    public string? Color { get; set; }

    /// <summary>Colour of the upper atmosphere.</summary>
    public string? HighColor { get; set; }

    /// <summary>Colour of the space behind the globe.</summary>
    public string? SpaceColor { get; set; }

    /// <summary>
    /// How sharply the atmosphere fades into space, 0–1. Lower is a crisper edge.
    /// </summary>
    public double? HorizonBlend { get; set; }

    /// <summary>Brightness of the stars in space, 0–1.</summary>
    public double? StarIntensity { get; set; }
}
