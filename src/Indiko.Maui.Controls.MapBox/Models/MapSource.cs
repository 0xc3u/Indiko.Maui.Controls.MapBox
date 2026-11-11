namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>Kind of tile or image source added via <c>MapView.AddSource</c>.</summary>
public enum MapSourceType
{
    /// <summary>Raster tiles (satellite imagery, scanned maps, WMTS).</summary>
    Raster,

    /// <summary>Vector tiles. Layers reading it must set <see cref="MapLayer.SourceLayer"/>.</summary>
    Vector,

    /// <summary>Elevation tiles, rendered with a hillshade layer.</summary>
    RasterDem,

    /// <summary>A single hosted image pinned to four corner coordinates.</summary>
    Image,
}

/// <summary>
/// A tile or image source. Pair it with a <see cref="MapLayer"/> that reads it —
/// a source on its own renders nothing. Sources survive style switches: the native
/// facade re-applies them after every style load, like layers.
/// </summary>
public class MapSource
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public MapSourceType Type { get; set; } = MapSourceType.Raster;

    /// <summary>
    /// TileJSON URL — <c>https://…/tiles.json</c> or <c>mapbox://mapbox.mapbox-streets-v8</c>.
    /// Mutually exclusive with <see cref="Tiles"/>; one of the two is required for tile sources.
    /// For <see cref="MapSourceType.Image"/> this is the URL of the image itself.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Tile URL templates containing <c>{z}</c>/<c>{x}</c>/<c>{y}</c>, e.g.
    /// <c>https://example.com/tiles/{z}/{x}/{y}.png</c>. Use this when the provider has no
    /// TileJSON endpoint.
    /// </summary>
    public IList<string> Tiles { get; } = [];

    /// <summary>Lowest zoom the source has tiles for.</summary>
    public int? MinZoom { get; set; }

    /// <summary>Highest zoom the source has tiles for; higher zooms overzoom these tiles.</summary>
    public int? MaxZoom { get; set; }

    /// <summary>
    /// Tile edge length in pixels for raster and raster-DEM sources — 256 for most third-party
    /// providers, 512 for Mapbox' own. Wrong values make the map look half or double scale.
    /// </summary>
    public int? TileSize { get; set; }

    /// <summary>
    /// Attribution text for the source. Most providers require it; it appears in the map's
    /// attribution dialog.
    /// </summary>
    public string? Attribution { get; set; }

    /// <summary>
    /// The four corners an <see cref="MapSourceType.Image"/> is pinned to, clockwise starting
    /// top-left: top-left, top-right, bottom-right, bottom-left.
    /// </summary>
    public IList<(double Latitude, double Longitude)> Coordinates { get; } = [];
}
