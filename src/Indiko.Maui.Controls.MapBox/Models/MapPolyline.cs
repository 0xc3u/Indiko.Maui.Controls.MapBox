namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>
/// A polyline rendered on the map. Requires at least two points.
/// </summary>
public class MapPolyline
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public IList<MapPosition> Points { get; set; } = [];
    public string? Title { get; set; }

    /// <summary>Line color as #RRGGBB hex string.</summary>
    public string Color { get; set; } = "#3B82F6";

    /// <summary>Line width in density-independent pixels.</summary>
    public double Width { get; set; } = 4.0;

    /// <summary>Line opacity between 0.0 and 1.0.</summary>
    public double Opacity { get; set; } = 1.0;

    /// <summary>Optional payload handed back on click via <c>PolylineClicked</c>.</summary>
    public object? Tag { get; set; }
}
