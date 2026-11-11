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

public class AnnotationDraggedEventArgs : EventArgs
{
    /// <summary>The dragged annotation; Latitude/Longitude already hold the new position.</summary>
    public MapAnnotation Annotation { get; }
    public double Latitude { get; }
    public double Longitude { get; }

    public AnnotationDraggedEventArgs(MapAnnotation annotation, double latitude, double longitude)
    {
        Annotation = annotation;
        Latitude = latitude;
        Longitude = longitude;
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

public class FollowPuckChangedEventArgs : EventArgs
{
    /// <summary>True while the camera is following the puck.</summary>
    public bool IsActive { get; }

    public FollowPuckChangedEventArgs(bool isActive)
    {
        IsActive = isActive;
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

/// <summary>Which kind of resource the map failed to load.</summary>
public enum MapLoadErrorType
{
    /// <summary>The style document itself — the map stays blank.</summary>
    Style,

    /// <summary>The sprite sheet; icons are missing.</summary>
    Sprite,

    /// <summary>A source's metadata; everything from that source is missing.</summary>
    Source,

    /// <summary>A single tile. Fires once per failed tile, so these arrive in bursts.</summary>
    Tile,

    /// <summary>A glyph range; labels in that range are missing.</summary>
    Glyphs,

    /// <summary>A kind this library does not know yet.</summary>
    Unknown,
}

/// <summary>
/// A resource the map needed could not be loaded — most often "offline and outside any
/// downloaded region". See <c>MapView.MapLoadError</c>.
/// </summary>
public class MapLoadErrorEventArgs : EventArgs
{
    public MapLoadErrorType Type { get; }

    /// <summary>The SDK's message, e.g. "Online connectivity is disabled."</summary>
    public string Message { get; }

    /// <summary>The source that failed, for <see cref="MapLoadErrorType.Source"/> and
    /// <see cref="MapLoadErrorType.Tile"/>; null otherwise.</summary>
    public string? SourceId { get; }

    public MapLoadErrorEventArgs(MapLoadErrorType type, string message, string? sourceId)
    {
        Type = type;
        Message = message;
        SourceId = sourceId;
    }
}
