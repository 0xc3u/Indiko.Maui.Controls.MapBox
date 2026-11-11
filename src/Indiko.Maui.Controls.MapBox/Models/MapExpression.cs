using System.Globalization;
using System.Text;

namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>
/// A Mapbox style-spec expression — a value computed per feature or per zoom level
/// instead of a constant. Assign one to a <see cref="MapLayer.Properties"/> entry to
/// make a layer data-driven.
/// </summary>
/// <remarks>
/// The factory methods cover the common shapes; <see cref="Raw"/> takes any expression
/// from the Mapbox style specification verbatim, so nothing is out of reach.
/// </remarks>
public sealed class MapExpression
{
    /// <summary>The expression as style-spec JSON.</summary>
    public string Json { get; }

    private MapExpression(string json)
    {
        Json = json;
    }

    /// <summary>
    /// Any style-spec expression, given as raw JSON — e.g.
    /// <c>["interpolate",["linear"],["zoom"],8,1,16,8]</c>. Not validated here; an
    /// invalid expression is rejected by the native SDK and the property keeps its
    /// previous value.
    /// </summary>
    public static MapExpression Raw(string json) => new(json);

    /// <summary>Reads a feature property: <c>["get", name]</c>.</summary>
    public static MapExpression Get(string name) => new($"[\"get\",{EncodeString(name)}]");

    /// <summary>The current zoom level: <c>["zoom"]</c>.</summary>
    public static MapExpression Zoom() => new("[\"zoom\"]");

    /// <summary>
    /// Smoothly interpolates between stops: <c>["interpolate",["linear"],input,…]</c>.
    /// Outputs may be numbers or colors.
    /// </summary>
    public static MapExpression Interpolate(MapExpression input, params (double Stop, object Output)[] stops)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (stops is null || stops.Length < 2)
            throw new ArgumentException("An interpolation needs at least two stops.", nameof(stops));

        var builder = new StringBuilder("[\"interpolate\",[\"linear\"],").Append(input.Json);
        foreach (var (stop, output) in stops)
            builder.Append(',').Append(Encode(stop)).Append(',').Append(Encode(output));
        return new MapExpression(builder.Append(']').ToString());
    }

    /// <summary>
    /// Picks the output for the first stop the input is greater than or equal to:
    /// <c>["step",input,fallback,…]</c>. Used for buckets rather than gradients.
    /// </summary>
    public static MapExpression Step(MapExpression input, object fallback, params (double Stop, object Output)[] stops)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (stops is null || stops.Length == 0)
            throw new ArgumentException("A step expression needs at least one stop.", nameof(stops));

        var builder = new StringBuilder("[\"step\",").Append(input.Json).Append(',').Append(Encode(fallback));
        foreach (var (stop, output) in stops)
            builder.Append(',').Append(Encode(stop)).Append(',').Append(Encode(output));
        return new MapExpression(builder.Append(']').ToString());
    }

    /// <summary>
    /// Maps discrete input values onto outputs: <c>["match",input,label,output,…,fallback]</c>.
    /// </summary>
    public static MapExpression Match(MapExpression input, IEnumerable<(object Label, object Output)> cases, object fallback)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(cases);

        var builder = new StringBuilder("[\"match\",").Append(input.Json);
        var any = false;
        foreach (var (label, output) in cases)
        {
            builder.Append(',').Append(Encode(label)).Append(',').Append(Encode(output));
            any = true;
        }
        if (!any)
            throw new ArgumentException("A match expression needs at least one case.", nameof(cases));

        return new MapExpression(builder.Append(',').Append(Encode(fallback)).Append(']').ToString());
    }

    /// <summary>
    /// Encodes a scalar or nested expression as style-spec JSON. Used by the serializer
    /// for <see cref="MapLayer.Properties"/> values too.
    /// </summary>
    internal static string Encode(object? value) => value switch
    {
        null => "null",
        MapExpression expression => expression.Json,
        string text => EncodeString(text),
        bool flag => flag ? "true" : "false",
        float number => number.ToString(CultureInfo.InvariantCulture),
        double number => number.ToString(CultureInfo.InvariantCulture),
        decimal number => number.ToString(CultureInfo.InvariantCulture),
        IFormattable number when value is sbyte or byte or short or ushort or int or uint or long or ulong
            => number.ToString(null, CultureInfo.InvariantCulture),
        _ => EncodeString(value.ToString() ?? string.Empty),
    };

    private static string EncodeString(string text)
    {
        var builder = new StringBuilder("\"");
        foreach (var character in text)
        {
            switch (character)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                default:
                    if (character < ' ')
                        builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        builder.Append(character);
                    break;
            }
        }
        return builder.Append('"').ToString();
    }

    /// <inheritdoc />
    public override string ToString() => Json;
}
