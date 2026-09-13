namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>
/// A MAUI view anchored (bottom-center) to a map coordinate. The content is rendered
/// natively and moves with the map; gesture recognizers on the content keep working.
/// A fixed size is required by the native view-annotation system.
/// </summary>
public class MapViewAnnotation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>The MAUI view rendered as annotation content.</summary>
    public View? Content { get; set; }

    /// <summary>Fixed width in device-independent units.</summary>
    public double Width { get; set; } = 120;

    /// <summary>Fixed height in device-independent units.</summary>
    public double Height { get; set; } = 48;
}
