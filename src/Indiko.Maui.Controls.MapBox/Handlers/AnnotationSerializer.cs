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

    public static string ToLayerJson(MapLayer layer)
    {
        var builder = new StringBuilder("{");
        builder.Append("\"id\":").Append(Quote(layer.Id));
        builder.Append(",\"sourceId\":").Append(Quote(layer.SourceId));
        builder.Append(",\"type\":").Append(Quote(layer.Type switch
        {
            MapLayerType.Line => "line",
            MapLayerType.Circle => "circle",
            _ => "fill",
        }));
        builder.Append(",\"color\":").Append(Quote(layer.Color));
        builder.Append(",\"opacity\":").Append(Invariant(layer.Opacity));
        builder.Append(",\"lineWidth\":").Append(Invariant(layer.LineWidth));
        builder.Append(",\"circleRadius\":").Append(Invariant(layer.CircleRadius));
        if (!string.IsNullOrEmpty(layer.BelowLayerId))
            builder.Append(",\"belowLayerId\":").Append(Quote(layer.BelowLayerId));
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
        builder.Append(",\"styleUri\":").Append(Quote(region.StyleUri));
        builder.Append(",\"minZoom\":").Append(region.MinZoom);
        builder.Append(",\"maxZoom\":").Append(region.MaxZoom);
        builder.Append(",\"minLat\":").Append(Invariant(region.MinLatitude));
        builder.Append(",\"minLng\":").Append(Invariant(region.MinLongitude));
        builder.Append(",\"maxLat\":").Append(Invariant(region.MaxLatitude));
        builder.Append(",\"maxLng\":").Append(Invariant(region.MaxLongitude));
        return builder.Append('}').ToString();
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
