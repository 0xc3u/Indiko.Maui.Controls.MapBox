namespace Indiko.Maui.Controls.MapBox.Models;

public enum MapLayerType
{
    Fill,
    Line,
    Circle,
}

/// <summary>
/// A style layer rendering features of a GeoJSON source added via
/// <c>MapView.AddGeoJsonSource</c>. Layers survive style switches — the native
/// facade re-applies them after every style load.
/// </summary>
public class MapLayer
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Id of the GeoJSON source this layer renders.</summary>
    public string SourceId { get; set; } = string.Empty;

    public MapLayerType Type { get; set; } = MapLayerType.Fill;

    /// <summary>Fill/line/circle color as #RRGGBB hex string.</summary>
    public string Color { get; set; } = "#3B82F6";

    /// <summary>Opacity between 0.0 and 1.0.</summary>
    public double Opacity { get; set; } = 1.0;

    /// <summary>Line width in density-independent pixels (line layers only).</summary>
    public double LineWidth { get; set; } = 3.0;

    /// <summary>Circle radius in density-independent pixels (circle layers only).</summary>
    public double CircleRadius { get; set; } = 6.0;

    /// <summary>Optional id of an existing layer to insert this layer below.</summary>
    public string? BelowLayerId { get; set; }
}
