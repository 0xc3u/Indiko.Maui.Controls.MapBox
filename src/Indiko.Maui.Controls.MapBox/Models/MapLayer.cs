namespace Indiko.Maui.Controls.MapBox.Models;

public enum MapLayerType
{
    Fill,
    Line,
    Circle,
    Symbol,
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

    /* ------------------------- Symbol layer options ------------------------- */

    /// <summary>Feature property whose value is rendered as the label (symbol layers).</summary>
    public string? TextField { get; set; }

    /// <summary>Label size in device-independent units (symbol layers).</summary>
    public double TextSize { get; set; } = 14;

    /// <summary>Label color as #RRGGBB hex string (symbol layers).</summary>
    public string TextColor { get; set; } = "#000000";

    /// <summary>Optional halo color behind the label for readability (symbol layers).</summary>
    public string? TextHaloColor { get; set; }

    /// <summary>Halo width in device-independent units (symbol layers).</summary>
    public double TextHaloWidth { get; set; } = 1.2;

    /// <summary>Optional icon (PNG/JPEG bytes) rendered per feature (symbol layers).
    /// With a TextField set, the label is placed below the icon.</summary>
    public byte[]? IconData { get; set; }

    /// <summary>Icon display width in device-independent units (0 = natural size).</summary>
    public double IconWidth { get; set; }

    /// <summary>Render all symbols even when they collide (symbol layers).</summary>
    public bool AllowOverlap { get; set; }
}
