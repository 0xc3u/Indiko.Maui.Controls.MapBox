namespace Indiko.Maui.Controls.MapBox.Models;

public class MapClickedEventArgs : EventArgs
{
    public double Latitude { get; }
    public double Longitude { get; }

    public MapClickedEventArgs(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
}

public class AnnotationClickedEventArgs : EventArgs
{
    public MapAnnotation Annotation { get; }

    public AnnotationClickedEventArgs(MapAnnotation annotation)
    {
        Annotation = annotation;
    }
}

public class PolylineClickedEventArgs : EventArgs
{
    public MapPolyline Polyline { get; }

    public PolylineClickedEventArgs(MapPolyline polyline)
    {
        Polyline = polyline;
    }
}

public class PolygonClickedEventArgs : EventArgs
{
    public MapPolygon Polygon { get; }

    public PolygonClickedEventArgs(MapPolygon polygon)
    {
        Polygon = polygon;
    }
}

public class OfflineRegionProgressEventArgs : EventArgs
{
    public string RegionId { get; }

    /// <summary>Download progress between 0.0 and 1.0.</summary>
    public double Progress { get; }

    public OfflineRegionProgressEventArgs(string regionId, double progress)
    {
        RegionId = regionId;
        Progress = progress;
    }
}

public class OfflineRegionCompletedEventArgs : EventArgs
{
    public string RegionId { get; }
    public bool Success { get; }
    public string? ErrorMessage { get; }

    public OfflineRegionCompletedEventArgs(string regionId, bool success, string? errorMessage)
    {
        RegionId = regionId;
        Success = success;
        ErrorMessage = errorMessage;
    }
}

public class CameraChangedEventArgs : EventArgs
{
    public MapCameraPosition Camera { get; }

    public CameraChangedEventArgs(MapCameraPosition camera)
    {
        Camera = camera;
    }
}
