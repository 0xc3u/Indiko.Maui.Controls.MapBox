using System.Text;
using Indiko.Maui.Controls.MapBox.Models;

namespace Indiko.Maui.Controls.MapBox.Handlers;

/// <summary>
/// Serializes annotations into the compact JSON contract shared with both native facades:
/// [{"id":"...","lat":..,"lng":..,"title":"...","color":"#RRGGBB"}]
/// </summary>
internal static class AnnotationSerializer
{
    public static string ToJson(IEnumerable<MapAnnotation>? annotations)
    {
        if (annotations is null)
            return "[]";

        var builder = new StringBuilder("[");
        var first = true;

        foreach (var annotation in annotations)
        {
            if (!first)
                builder.Append(',');
            first = false;

            builder.Append("{\"id\":").Append(Quote(annotation.Id));
            builder.Append(",\"lat\":").Append(annotation.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.Append(",\"lng\":").Append(annotation.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.Append(",\"color\":").Append(Quote(annotation.Color));
            if (annotation.IconData is { Length: > 0 })
            {
                builder.Append(",\"icon\":").Append(Quote(Convert.ToBase64String(annotation.IconData)));
                if (annotation.IconWidth > 0)
                    builder.Append(",\"iconWidth\":").Append(Invariant(annotation.IconWidth));
                if (annotation.IconHeight > 0)
                    builder.Append(",\"iconHeight\":").Append(Invariant(annotation.IconHeight));
            }
            if (annotation.IconAnchor == MapIconAnchor.Center)
                builder.Append(",\"anchor\":\"center\"");
            if (annotation.IsDraggable)
                builder.Append(",\"draggable\":true");
            if (!string.IsNullOrEmpty(annotation.Title))
                builder.Append(",\"title\":").Append(Quote(annotation.Title));
            builder.Append('}');
        }

        return builder.Append(']').ToString();
    }

    public static string ToPolylinesJson(IEnumerable<MapPolyline>? polylines)
    {
        if (polylines is null)
            return "[]";

        var builder = new StringBuilder("[");
        var first = true;

        foreach (var polyline in polylines)
        {
            if (!first)
                builder.Append(',');
            first = false;

            builder.Append("{\"id\":").Append(Quote(polyline.Id));
            AppendPoints(builder, polyline.Points);
            builder.Append(",\"color\":").Append(Quote(polyline.Color));
            builder.Append(",\"width\":").Append(Invariant(polyline.Width));
            builder.Append(",\"opacity\":").Append(Invariant(polyline.Opacity));
            builder.Append('}');
        }

        return builder.Append(']').ToString();
    }

    public static string ToPolygonsJson(IEnumerable<MapPolygon>? polygons)
    {
        if (polygons is null)
            return "[]";

        var builder = new StringBuilder("[");
        var first = true;

        foreach (var polygon in polygons)
        {
            if (!first)
                builder.Append(',');
            first = false;

            builder.Append("{\"id\":").Append(Quote(polygon.Id));
            AppendPoints(builder, polygon.Points);
            builder.Append(",\"fillColor\":").Append(Quote(polygon.FillColor));
            builder.Append(",\"fillOpacity\":").Append(Invariant(polygon.FillOpacity));
            builder.Append(",\"strokeColor\":").Append(Quote(polygon.StrokeColor));
            builder.Append('}');
        }

        return builder.Append(']').ToString();
    }

    /// <summary>
    /// Serializes a tile or image source. Optional values are omitted rather than defaulted,
    /// so the native SDK keeps its own defaults for anything the caller did not set.
    /// </summary>
    public static string ToSourceJson(MapSource source)
    {
        var builder = new StringBuilder("{");
        builder.Append("\"id\":").Append(Quote(source.Id));
        builder.Append(",\"type\":").Append(Quote(source.Type switch
        {
            MapSourceType.Vector => "vector",
            MapSourceType.RasterDem => "raster-dem",
            MapSourceType.Image => "image",
            _ => "raster",
        }));

        if (!string.IsNullOrEmpty(source.Url))
            builder.Append(",\"url\":").Append(Quote(source.Url));

        if (source.Tiles.Count > 0)
        {
            builder.Append(",\"tiles\":[");
            for (var index = 0; index < source.Tiles.Count; index++)
            {
                if (index > 0)
                    builder.Append(',');
                builder.Append(Quote(source.Tiles[index]));
            }
            builder.Append(']');
        }

        if (source.MinZoom is { } minZoom)
            builder.Append(",\"minZoom\":").Append(Invariant(minZoom));
        if (source.MaxZoom is { } maxZoom)
            builder.Append(",\"maxZoom\":").Append(Invariant(maxZoom));
        if (source.TileSize is { } tileSize)
            builder.Append(",\"tileSize\":").Append(Invariant(tileSize));
        if (!string.IsNullOrEmpty(source.Attribution))
            builder.Append(",\"attribution\":").Append(Quote(source.Attribution));

        if (source.Coordinates.Count > 0)
        {
            // [[lng,lat], …] clockwise from the top-left, the order Mapbox expects.
            builder.Append(",\"coordinates\":[");
            for (var index = 0; index < source.Coordinates.Count; index++)
            {
                if (index > 0)
                    builder.Append(',');
                var (latitude, longitude) = source.Coordinates[index];
                builder.Append('[').Append(Invariant(longitude)).Append(',').Append(Invariant(latitude)).Append(']');
            }
            builder.Append(']');
        }

        return builder.Append('}').ToString();
    }

    public static string ToLayerJson(MapLayer layer)
    {
        var builder = new StringBuilder("{");
        builder.Append("\"id\":").Append(Quote(layer.Id));
        builder.Append(",\"sourceId\":").Append(Quote(layer.SourceId));
        builder.Append(",\"type\":").Append(Quote(layer.Type switch
        {
            MapLayerType.Line => "line",
            MapLayerType.Circle => "circle",
            MapLayerType.Symbol => "symbol",
            MapLayerType.Raster => "raster",
            MapLayerType.Hillshade => "hillshade",
            MapLayerType.Heatmap => "heatmap",
            MapLayerType.FillExtrusion => "fill-extrusion",
            _ => "fill",
        }));
        builder.Append(",\"color\":").Append(Quote(layer.Color));
        builder.Append(",\"opacity\":").Append(Invariant(layer.Opacity));
        builder.Append(",\"lineWidth\":").Append(Invariant(layer.LineWidth));
        builder.Append(",\"circleRadius\":").Append(Invariant(layer.CircleRadius));
        if (!string.IsNullOrEmpty(layer.BelowLayerId))
            builder.Append(",\"belowLayerId\":").Append(Quote(layer.BelowLayerId));
        if (!string.IsNullOrEmpty(layer.SourceLayer))
            builder.Append(",\"sourceLayer\":").Append(Quote(layer.SourceLayer));
        if (layer.Properties.Count > 0)
        {
            // Raw style-spec values pass through verbatim; the facade hands each one to
            // Mapbox' own property setter, so expressions and scalars take the same path.
            builder.Append(",\"properties\":{");
            var first = true;
            foreach (var property in layer.Properties)
            {
                if (!first)
                    builder.Append(',');
                builder.Append(Quote(property.Key)).Append(':').Append(MapExpression.Encode(property.Value));
                first = false;
            }
            builder.Append('}');
        }
        if (layer.Type == MapLayerType.Symbol)
        {
            if (!string.IsNullOrEmpty(layer.TextField))
            {
                builder.Append(",\"textField\":").Append(Quote(layer.TextField));
                builder.Append(",\"textSize\":").Append(Invariant(layer.TextSize));
                builder.Append(",\"textColor\":").Append(Quote(layer.TextColor));
                if (!string.IsNullOrEmpty(layer.TextHaloColor))
                {
                    builder.Append(",\"textHaloColor\":").Append(Quote(layer.TextHaloColor));
                    builder.Append(",\"textHaloWidth\":").Append(Invariant(layer.TextHaloWidth));
                }
            }
            if (layer.IconData is { Length: > 0 })
            {
                builder.Append(",\"icon\":").Append(Quote(Convert.ToBase64String(layer.IconData)));
                if (layer.IconWidth > 0)
                    builder.Append(",\"iconWidth\":").Append(Invariant(layer.IconWidth));
            }
            if (layer.AllowOverlap)
                builder.Append(",\"allowOverlap\":true");
        }
        return builder.Append('}').ToString();
    }

    public static string ToClusterJson(MapClusterSource source)
    {
        var builder = new StringBuilder("{");
        builder.Append("\"sourceId\":").Append(Quote(source.SourceId));
        builder.Append(",\"geoJson\":").Append(Quote(source.GeoJson));
        builder.Append(",\"clusterRadius\":").Append(Invariant(source.ClusterRadius));
        builder.Append(",\"clusterMaxZoom\":").Append(Invariant(source.ClusterMaxZoom));
        builder.Append(",\"clusterColor\":").Append(Quote(source.ClusterColor));
        builder.Append(",\"clusterTextColor\":").Append(Quote(source.ClusterTextColor));
        builder.Append(",\"pointColor\":").Append(Quote(source.PointColor));
        builder.Append(",\"pointRadius\":").Append(Invariant(source.PointRadius));
        return builder.Append('}').ToString();
    }

    public static string ToOfflineRegionJson(MapOfflineRegion region)
    {
        var builder = new StringBuilder("{");
        builder.Append("\"id\":").Append(Quote(region.Id));
        if (region.Name is not null)
            builder.Append(",\"name\":").Append(Quote(region.Name));
        builder.Append(",\"styleUri\":").Append(Quote(region.StyleUri));
        builder.Append(",\"minZoom\":").Append(region.MinZoom);
        builder.Append(",\"maxZoom\":").Append(region.MaxZoom);
        builder.Append(",\"minLat\":").Append(Invariant(region.MinLatitude));
        builder.Append(",\"minLng\":").Append(Invariant(region.MinLongitude));
        builder.Append(",\"maxLat\":").Append(Invariant(region.MaxLatitude));
        builder.Append(",\"maxLng\":").Append(Invariant(region.MaxLongitude));
        builder.Append(",\"includeTerrain\":").Append(region.IncludeTerrain ? "true" : "false");
        return builder.Append('}').ToString();
    }

    /// <summary>
    /// Parses the facade's offline-region list:
    /// [{"id","requiredResourceCount","completedResourceCount","completedResourceSize","expires","metadata"}].
    /// Malformed entries are skipped; malformed input yields an empty list.
    /// </summary>
    /// <summary>
    /// Serializes the four ornament placements. Ornaments left null are omitted from the
    /// payload, which the facade reads as "restore the SDK default".
    /// </summary>
    public static string SerializeOrnamentPlacement(
        MapOrnamentPlacement? compass, MapOrnamentPlacement? scaleBar,
        MapOrnamentPlacement? logo, MapOrnamentPlacement? attribution)
    {
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            WriteOrnament(writer, "compass", compass);
            WriteOrnament(writer, "scaleBar", scaleBar);
            WriteOrnament(writer, "logo", logo);
            WriteOrnament(writer, "attribution", attribution);
            writer.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteOrnament(System.Text.Json.Utf8JsonWriter writer, string name,
        MapOrnamentPlacement? placement)
    {
        if (placement is null)
            return;

        writer.WriteStartObject(name);
        writer.WriteString("position", placement.Position switch
        {
            MapOrnamentPosition.TopLeft => "topLeft",
            MapOrnamentPosition.BottomLeft => "bottomLeft",
            MapOrnamentPosition.BottomRight => "bottomRight",
            _ => "topRight",
        });
        writer.WriteNumber("x", placement.OffsetX);
        writer.WriteNumber("y", placement.OffsetY);
        writer.WriteEndObject();
    }

    /// <summary>Serializes layer ids for the query facade; an empty list means "all layers".</summary>
    public static string ToStringArrayJson(IEnumerable<string>? values)
    {
        if (values is null)
            return "[]";

        var builder = new StringBuilder("[");
        var first = true;
        foreach (var value in values)
        {
            if (string.IsNullOrEmpty(value))
                continue;
            if (!first)
                builder.Append(',');
            builder.Append(Quote(value));
            first = false;
        }
        return builder.Append(']').ToString();
    }

    /// <summary>Parses the feature array both facades produce for a rendered-feature query.</summary>
    public static IReadOnlyList<MapQueriedFeature> ParseQueriedFeatures(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                return [];

            var result = new List<MapQueriedFeature>();
            foreach (var entry in document.RootElement.EnumerateArray())
            {
                if (entry.ValueKind != System.Text.Json.JsonValueKind.Object)
                    continue;

                var properties = new Dictionary<string, object?>();
                if (entry.TryGetProperty("properties", out var propertyElement)
                    && propertyElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var property in propertyElement.EnumerateObject())
                        properties[property.Name] = ReadJsonScalar(property.Value);
                }

                result.Add(new MapQueriedFeature
                {
                    Id = GetString(entry, "id"),
                    LayerId = GetString(entry, "layerId"),
                    Source = GetString(entry, "source") ?? string.Empty,
                    SourceLayer = GetString(entry, "sourceLayer"),
                    GeometryType = GetString(entry, "geometryType"),
                    Properties = properties,
                });
            }
            return result;
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private static string? GetString(System.Text.Json.JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.String
            ? value.GetString()
            : element.TryGetProperty(name, out var raw) && raw.ValueKind == System.Text.Json.JsonValueKind.Number
                ? raw.GetRawText()
                : null;

    /// <summary>Nested objects and arrays keep their JSON text — callers rarely need them typed.</summary>
    private static object? ReadJsonScalar(System.Text.Json.JsonElement element) => element.ValueKind switch
    {
        System.Text.Json.JsonValueKind.String => element.GetString(),
        System.Text.Json.JsonValueKind.Number => element.GetDouble(),
        System.Text.Json.JsonValueKind.True => true,
        System.Text.Json.JsonValueKind.False => false,
        System.Text.Json.JsonValueKind.Null => null,
        _ => element.GetRawText(),
    };

    public static IReadOnlyList<MapOfflineRegionInfo> ParseOfflineRegions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                return [];

            var result = new List<MapOfflineRegionInfo>();
            foreach (var entry in document.RootElement.EnumerateArray())
            {
                if (entry.ValueKind != System.Text.Json.JsonValueKind.Object
                    || !entry.TryGetProperty("id", out var idElement)
                    || idElement.GetString() is not { Length: > 0 } id)
                    continue;

                long? expires = GetLong(entry, "expires");
                result.Add(new MapOfflineRegionInfo
                {
                    Id = id,
                    RequiredResourceCount = GetLong(entry, "requiredResourceCount") ?? 0,
                    CompletedResourceCount = GetLong(entry, "completedResourceCount") ?? 0,
                    CompletedResourceSize = GetLong(entry, "completedResourceSize") ?? 0,
                    Expires = expires is { } ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : null,
                    Region = entry.TryGetProperty("metadata", out var metadata)
                        ? ParseOfflineRegionMetadata(id, metadata)
                        : null,
                });
            }
            return result;
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private static MapOfflineRegion? ParseOfflineRegionMetadata(string id, System.Text.Json.JsonElement metadata)
    {
        if (metadata.ValueKind != System.Text.Json.JsonValueKind.Object)
            return null;

        double? minLat = GetDouble(metadata, "minLat");
        double? minLng = GetDouble(metadata, "minLng");
        double? maxLat = GetDouble(metadata, "maxLat");
        double? maxLng = GetDouble(metadata, "maxLng");
        if (minLat is null || minLng is null || maxLat is null || maxLng is null)
            return null;

        return new MapOfflineRegion
        {
            Id = id,
            Name = metadata.TryGetProperty("name", out var name) && name.ValueKind == System.Text.Json.JsonValueKind.String
                ? name.GetString()
                : null,
            StyleUri = metadata.TryGetProperty("styleUri", out var style) && style.ValueKind == System.Text.Json.JsonValueKind.String
                ? style.GetString() ?? MapStyles.Streets
                : MapStyles.Streets,
            MinLatitude = minLat.Value,
            MinLongitude = minLng.Value,
            MaxLatitude = maxLat.Value,
            MaxLongitude = maxLng.Value,
            MinZoom = (int)(GetDouble(metadata, "minZoom") ?? 6),
            MaxZoom = (int)(GetDouble(metadata, "maxZoom") ?? 14),
            IncludeTerrain = metadata.TryGetProperty("includeTerrain", out var terrain)
                && terrain.ValueKind == System.Text.Json.JsonValueKind.True,
        };
    }

    private static double? GetDouble(System.Text.Json.JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.Number
            ? value.GetDouble()
            : null;

    private static long? GetLong(System.Text.Json.JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.Number
            ? (value.TryGetInt64(out var integer) ? integer : (long)value.GetDouble())
            : null;

    /// <summary>Parses the facade's visible-bounds JSON: {"minLat","minLng","maxLat","maxLng"}.</summary>
    public static MapBounds? ParseBounds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            var root = document.RootElement;
            return new MapBounds(
                root.GetProperty("minLat").GetDouble(),
                root.GetProperty("minLng").GetDouble(),
                root.GetProperty("maxLat").GetDouble(),
                root.GetProperty("maxLng").GetDouble());
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    private static void AppendPoints(StringBuilder builder, IEnumerable<MapPosition>? points)
    {
        builder.Append(",\"points\":[");
        var first = true;
        foreach (var point in points ?? [])
        {
            if (!first)
                builder.Append(',');
            first = false;
            builder.Append('[').Append(Invariant(point.Latitude)).Append(',')
                .Append(Invariant(point.Longitude)).Append(']');
        }
        builder.Append(']');
    }

    private static string Invariant(double value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string Quote(string value)
    {
        return "\"" + value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t") + "\"";
    }
}
