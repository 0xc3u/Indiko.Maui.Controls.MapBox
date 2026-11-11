namespace Indiko.Maui.Controls.MapBox.Models;

/// <summary>Corner an ornament is anchored to.</summary>
public enum MapOrnamentPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

/// <summary>
/// Where an ornament sits. <see cref="OffsetX"/>/<see cref="OffsetY"/> are measured from
/// the chosen corner inwards, in device-independent units — the same numbers on both
/// platforms. Leave a <c>MapView</c> placement property null to keep the SDK default.
/// </summary>
public class MapOrnamentPlacement
{
    public MapOrnamentPosition Position { get; set; } = MapOrnamentPosition.TopRight;

    public double OffsetX { get; set; }

    public double OffsetY { get; set; }

    public MapOrnamentPlacement()
    {
    }

    public MapOrnamentPlacement(MapOrnamentPosition position, double offsetX = 0, double offsetY = 0)
    {
        Position = position;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }
}
