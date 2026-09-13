namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>
/// A point annotation (marker) rendered on the map.
/// </summary>
public class MapAnnotation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Title { get; set; }

    /// <summary>Marker color as #RRGGBB hex string.</summary>
    public string Color { get; set; } = "#E74C3C";

    /// <summary>Optional payload handed back on click via <c>AnnotationClicked</c>.</summary>
    public object? Tag { get; set; }
}
