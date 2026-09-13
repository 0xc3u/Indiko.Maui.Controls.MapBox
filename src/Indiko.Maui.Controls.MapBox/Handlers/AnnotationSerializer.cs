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
            if (!string.IsNullOrEmpty(annotation.Title))
                builder.Append(",\"title\":").Append(Quote(annotation.Title));
            builder.Append('}');
        }

        return builder.Append(']').ToString();
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
