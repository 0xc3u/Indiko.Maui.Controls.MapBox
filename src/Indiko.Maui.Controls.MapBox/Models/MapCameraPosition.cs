namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>
/// Describes a camera position on the map.
/// </summary>
public class MapCameraPosition
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Zoom { get; set; } = 12;
    public double Bearing { get; set; }
    public double Pitch { get; set; }

    public MapCameraPosition()
    {
    }

    public MapCameraPosition(double latitude, double longitude, double zoom = 12, double bearing = 0, double pitch = 0)
    {
        Latitude = latitude;
        Longitude = longitude;
        Zoom = zoom;
        Bearing = bearing;
        Pitch = pitch;
    }
}
