namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>
/// An offline region stored on the device, as reported by
/// <see cref="MapboxOffline.GetRegionsAsync"/>.
/// </summary>
public sealed class MapOfflineRegionInfo
{
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// The region as it was requested (name, style, bounds, zoom range, terrain), or
    /// null for regions created by other code or by library versions before 1.9.
    /// </summary>
    public MapOfflineRegion? Region { get; init; }

    /// <summary>Number of resources (tiles) the region needs in total.</summary>
    public long RequiredResourceCount { get; init; }

    /// <summary>Number of resources already on the device.</summary>
    public long CompletedResourceCount { get; init; }

    /// <summary>Bytes on disk for the completed resources (shared tiles count for every region).</summary>
    public long CompletedResourceSize { get; init; }

    /// <summary>When the earliest stored resource expires, if known.</summary>
    public DateTimeOffset? Expires { get; init; }

    /// <summary>True once every required resource has been downloaded.</summary>
    public bool IsComplete => RequiredResourceCount > 0 && CompletedResourceCount >= RequiredResourceCount;
}
