using System.Collections.Generic;
using System.Linq;
using Agromancy.Models;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Agromancy.Menus;

public partial class AgrometerMenu
{
    private Vector3 PrimitiveNormalize(Vector2 position)
    {
        float x = (position.X / Game1.uiViewport.Width) * 2f - 1f;
        float y = (position.Y / Game1.uiViewport.Height) * 2f - 1f;
        return new Vector3(x, y, 0);
    }

    private List<Item> GetItemsWithAgromancyData()
    {
        var inventory = Game1.player.Items;
        var items = new List<Item>();
        foreach (var item in inventory)
        {
            if (item is not null && item.modData.ContainsKey(Agromancy.Manifest.UniqueID))
            {
                items.Add(item);
            }
        }
        return items;
    }

    public Item? GetCurrentlySelectedCrop()
    {
        return agromancyCrops.ElementAtOrDefault((2 + itemListOffset) % agromancyCrops.Count);
    }

    public CropEssences? GetCurrentlySelectedCropEssences()
    {
        Item? selectedCrop = GetCurrentlySelectedCrop();
        if (selectedCrop is null) return null;
        CropEssences? essences = CropManager.GrabEssences(selectedCrop);
        return essences;
    }

    public bool PointInCircle(Vector2 point, Vector2 center, float radius)
    {
        float dx = point.X - center.X;
        float dy = point.Y - center.Y;
        return (dx * dx + dy * dy) <= radius * radius;
    }
}