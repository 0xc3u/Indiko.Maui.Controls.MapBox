namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>
/// A geographic coordinate (latitude/longitude) used by polylines and polygons.
/// </summary>
public class MapPosition
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public MapPosition()
    {
    }

    public MapPosition(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
}
