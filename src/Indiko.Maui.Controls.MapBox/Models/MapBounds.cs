namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>
/// A geographic bounding box (south-west / north-east corners).
/// </summary>
public class MapBounds
{
    public double MinLatitude { get; set; }
    public double MinLongitude { get; set; }
    public double MaxLatitude { get; set; }
    public double MaxLongitude { get; set; }

    public MapBounds()
    {
    }

    public MapBounds(double minLatitude, double minLongitude, double maxLatitude, double maxLongitude)
    {
        MinLatitude = minLatitude;
        MinLongitude = minLongitude;
        MaxLatitude = maxLatitude;
        MaxLongitude = maxLongitude;
    }

    /// <summary>Computes the bounding box of a set of positions; null when empty.</summary>
    public static MapBounds? FromPositions(IEnumerable<(double Latitude, double Longitude)> positions)
    {
        double minLat = double.MaxValue, minLng = double.MaxValue;
        double maxLat = double.MinValue, maxLng = double.MinValue;
        var any = false;

        foreach (var (lat, lng) in positions)
        {
            any = true;
            if (lat < minLat) minLat = lat;
            if (lng < minLng) minLng = lng;
            if (lat > maxLat) maxLat = lat;
            if (lng > maxLng) maxLng = lng;
        }

        return any ? new MapBounds(minLat, minLng, maxLat, maxLng) : null;
    }
}
