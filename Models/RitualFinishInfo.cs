using Microsoft.Xna.Framework;

namespace Agromancy.Models;

public class RitualFinishInfo(string? location, Vector2 tilePosition)
{
    public string? Location { get; set; } = location;
    public Vector2 TilePosition { get; set; } = tilePosition;
}