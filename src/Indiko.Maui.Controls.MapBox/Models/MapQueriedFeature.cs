namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>
/// A rendered feature found under a coordinate by <c>MapView.QueryFeaturesAsync</c>.
/// </summary>
public sealed class MapQueriedFeature
{
    /// <summary>The feature's own id, when the data carries one.</summary>
    public string? Id { get; init; }

    /// <summary>The style layer that drew it — useful to tell your layers apart.</summary>
    public string? LayerId { get; init; }

    /// <summary>The source the feature came from.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>The layer inside a vector tileset, for vector sources.</summary>
    public string? SourceLayer { get; init; }

    /// <summary>"Point", "LineString", "Polygon", …</summary>
    public string? GeometryType { get; init; }

    /// <summary>
    /// The feature's properties. Values are <see cref="string"/>, <see cref="double"/>,
    /// <see cref="bool"/> or null; nested objects and arrays arrive as their JSON text.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; init; } =
        new Dictionary<string, object?>();
}
