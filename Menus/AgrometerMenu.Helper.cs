using System;
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
            if (item is not null && !item.QualifiedItemId.Equals($"(O){Agromancy.UNIQUE_ID}_EssenceVial") && item.modData.ContainsKey(Agromancy.Manifest.UniqueID))
            {
                items.Add(item);
            }
        }
        return items;
    }

    private Item? GetEssenceVial()
    {
        var inventory = Game1.player.Items;
        return inventory.FirstOrDefault(item => item.QualifiedItemId.Equals($"(O){Agromancy.UNIQUE_ID}_EssenceVial"));
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

    private Dictionary<int, Vector3> GetEssenceCenters()
    {
        Dictionary<int, Vector3> centers = new();
        int essenceIndex = 0;
        for (int i = -1; i < 8; i++)
        {
            if (i is >= 2 and <= 4) continue;
            float x = (float)Math.Cos(MathHelper.ToRadians(i * 30) - MathHelper.ToRadians(7.5f * (i < 2 ? -1 : 1))) *
                      (agrometerFrame.Width / 2.35f) * GetAgrometerScale().X;
            float y = (float)Math.Sin(MathHelper.ToRadians(i * 30) - MathHelper.ToRadians(7.5f * (i < 2 ? -1 : 1))) *
                      (agrometerFrame.Height / 2.35f) * GetAgrometerScale().Y;
            float z = GetEssenceContainerRadius() * 0.1f;
            Vector3 position = new Vector3(GetAgrometerCenter().X + x, GetAgrometerCenter().Y + y, z);
            centers.Add(essenceIndex, position);
            essenceIndex++;
        }

        return centers;
    }

    public bool PointInCircle(Vector2 point, Vector2 center, float radius)
    {
        float dx = point.X - center.X;
        float dy = point.Y - center.Y;
        return (dx * dx + dy * dy) <= radius * radius;
    }
}