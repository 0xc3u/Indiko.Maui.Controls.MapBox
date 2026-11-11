namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>
/// An offline region: style pack plus map tiles for a bounding box, downloaded to the
/// device via <c>MapView.DownloadOfflineRegion</c>. Progress is reported through
/// <c>MapView.OfflineRegionProgress</c> / <c>OfflineRegionCompleted</c>. Downloaded
/// regions can be listed and removed without a map via <see cref="MapboxOffline"/>.
/// </summary>
public class MapOfflineRegion
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Optional display name. Stored with the region and returned by
    /// <see cref="MapboxOffline.GetRegionsAsync"/>.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>Style to download for offline use.</summary>
    public string StyleUri { get; set; } = MapStyles.Streets;

    public double MinLatitude { get; set; }
    public double MinLongitude { get; set; }
    public double MaxLatitude { get; set; }
    public double MaxLongitude { get; set; }

    /// <summary>Lowest zoom level to download (smaller = wider area context).</summary>
    public int MinZoom { get; set; } = 6;

    /// <summary>Highest zoom level to download (higher = more detail, much more data).</summary>
    public int MaxZoom { get; set; } = 14;

    /// <summary>
    /// Also downloads the Mapbox terrain elevation model (DEM), so that
    /// <c>MapView.TerrainEnabled</c> and <c>MapView.QueryTerrainElevation</c> work
    /// offline. The DEM ends at zoom 14; higher zoom levels reuse that data.
    /// </summary>
    public bool IncludeTerrain { get; set; }
}
