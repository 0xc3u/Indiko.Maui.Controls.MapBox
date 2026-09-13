namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>
/// A filled polygon rendered on the map. Requires at least three points; the ring
/// is closed automatically.
/// </summary>
public class MapPolygon
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public IList<MapPosition> Points { get; set; } = [];
    public string? Title { get; set; }

    /// <summary>Fill color as #RRGGBB hex string.</summary>
    public string FillColor { get; set; } = "#3B82F6";

    /// <summary>Fill opacity between 0.0 and 1.0.</summary>
    public double FillOpacity { get; set; } = 0.4;

    /// <summary>Outline color as #RRGGBB hex string.</summary>
    public string StrokeColor { get; set; } = "#1D4ED8";

    /// <summary>Optional payload handed back on click via <c>PolygonClicked</c>.</summary>
    public object? Tag { get; set; }
}
