namespace Indiko.Maui.Controls.MapBox.Models;

public enum MapIconAnchor
{
    /// <summary>The icon's bottom edge sits on the coordinate (pin-style).</summary>
    Bottom,

    /// <summary>The icon is centered on the coordinate (dot/badge-style).</summary>
    Center,
}

/// <summary>
/// A point annotation (marker) rendered on the map.
/// </summary>
public class MapAnnotation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Title { get; set; }

    /// <summary>Marker color as #RRGGBB hex string (used by the built-in pin glyph).</summary>
    public string Color { get; set; } = "#E74C3C";

    /// <summary>
    /// Custom marker icon as PNG/JPEG bytes. When set, it replaces the built-in pin.
    /// Markers sharing the same bytes share one texture on the map.
    /// </summary>
    public byte[]? IconData { get; set; }

    /// <summary>Icon display width in device-independent units (0 = natural size).</summary>
    public double IconWidth { get; set; }

    /// <summary>Icon display height in device-independent units (0 = derived from width / natural).</summary>
    public double IconHeight { get; set; }

    /// <summary>How the icon anchors to the coordinate. Default Bottom (pin-style).</summary>
    public MapIconAnchor IconAnchor { get; set; } = MapIconAnchor.Bottom;

    /// <summary>
    /// Allows moving the marker via long-press + drag. The final position is written
    /// back to <see cref="Latitude"/>/<see cref="Longitude"/> and reported through
    /// <c>MapView.AnnotationDragged</c>.
    /// </summary>
    public bool IsDraggable { get; set; }

    /// <summary>Optional payload handed back on click via <c>AnnotationClicked</c>.</summary>
    public object? Tag { get; set; }
}
