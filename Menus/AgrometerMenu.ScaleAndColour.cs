using Microsoft.Xna.Framework;
using StardewValley;

namespace Agromancy.Menus;

public partial class AgrometerMenu
{
    private Color GetAgrometerMainColour()
    {
        return Color.BlueViolet;
    }

    private Color GetItemSlotColour()
    {
        return Color.PaleVioletRed;
    }

    public Vector2 GetAgrometerCenter()
    {
        float x = Game1.uiViewport.Width / 2f;
        float y = Game1.uiViewport.Height / 2f;
        return new Vector2(x, y);
    }

    public Vector2 GetAgrometerScale()
    {
        float scale = (Game1.uiViewport.Height / 1.25f) / agrometerFrame.Height;
        return new Vector2(scale, scale);
    }

    private Vector2 GetAgrometerRingScale()
    {
        float scale = (Game1.uiViewport.Height / 1.55f) / agrometerCircles.Height;
        return new Vector2(scale, scale);
    }

    private Vector2 GetItemSlotScale(int index)
    {
        Vector2 baseScale = index switch
        {
            0 or 4 => new Vector2(.5f, .5f),
            1 or 3 => new Vector2(1f, 1f),
            2 => new Vector2(1.5f, 1.5f),
            _ => new Vector2(1f, 1f)
        };
        float targetHeight = Game1.uiViewport.Height / 12f;
        float scaleFactor = targetHeight / (Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 16).Height);
        return baseScale * scaleFactor;
    }

    private Vector2 GetItemSlotVerticalOffset(int index)
    {
        Vector2 baseOffset = index switch
        {
            0 => new Vector2(0, -275),
            1 => new Vector2(0, -85),
            2 => new Vector2(0, 0),
            3 => new Vector2(0, 85),
            4 => new Vector2(0, 275),
            _ => new Vector2(0, 0)
        };
        return baseOffset * GetItemSlotScale(index);
    }
}