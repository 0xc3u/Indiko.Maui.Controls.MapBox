namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>
/// How the map is projected onto the screen. <see cref="Default"/> keeps whatever the
/// chosen style ships with — the Mapbox Standard styles use a globe above a certain zoom.
/// </summary>
public enum MapProjection
{
    Default,

    /// <summary>The classic flat web-mercator projection at every zoom.</summary>
    Mercator,

    /// <summary>A 3D globe when zoomed out, flattening into mercator as you zoom in.</summary>
    Globe,
}
