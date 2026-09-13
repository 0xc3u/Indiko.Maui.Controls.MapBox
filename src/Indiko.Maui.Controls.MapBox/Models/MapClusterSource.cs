namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>
/// A clustered GeoJSON point source. The native facade manages three layers
/// ("&lt;SourceId&gt;-clusters", "&lt;SourceId&gt;-cluster-count", "&lt;SourceId&gt;-points")
/// and zooms into a cluster when it is tapped. Update the point data later via
/// <c>MapView.AddGeoJsonSource(SourceId, geoJson)</c>; remove everything via
/// <c>MapView.RemoveClusteredSource(SourceId)</c>.
/// </summary>
public class MapClusterSource
{
    public string SourceId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>GeoJSON FeatureCollection of points to cluster.</summary>
    public string GeoJson { get; set; } = string.Empty;

    /// <summary>Cluster radius in pixels.</summary>
    public double ClusterRadius { get; set; } = 50;

    /// <summary>Max zoom on which points are clustered.</summary>
    public double ClusterMaxZoom { get; set; } = 14;

    /// <summary>Fill color of cluster circles as #RRGGBB hex string.</summary>
    public string ClusterColor { get; set; } = "#3B82F6";

    /// <summary>Color of the point-count text as #RRGGBB hex string.</summary>
    public string ClusterTextColor { get; set; } = "#FFFFFF";

    /// <summary>Color of unclustered points as #RRGGBB hex string.</summary>
    public string PointColor { get; set; } = "#E74C3C";

    /// <summary>Radius of unclustered points in density-independent pixels.</summary>
    public double PointRadius { get; set; } = 6;
}
