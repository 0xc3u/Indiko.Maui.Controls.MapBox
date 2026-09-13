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

public class CameraChangedEventArgs : EventArgs
{
    public MapCameraPosition Camera { get; }

    public CameraChangedEventArgs(MapCameraPosition camera)
    {
        Camera = camera;
    }
}
