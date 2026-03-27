using System;
using System.Collections.Generic;
using System.Linq;
using Agromancy.Models;
using Agromancy.Patches;
using Microsoft.Xna.Framework;
using StardewValley;
using Object = StardewValley.Object;

namespace Agromancy.Menus;

public partial class AgrometerMenu
{
    private Vector3 PrimitiveNormalize(Vector2 position)
    {
        float x = (position.X / Game1.uiViewport.Width) * 2f - 1f;
        float y = (position.Y / Game1.uiViewport.Height) * 2f - 1f;
        return new Vector3(x, y, 0);
    }

    private void poofCurrentItem()
    {
        alreadyCreatedSuckedDryParticles = false;
        
        Item? crop = GetCurrentlySelectedCrop();
        if (crop is null) return;
        Game1.player.Items[agromancyCrops[crop]] = null;
    }

    private Dictionary<Item, int> GetItemsWithAgromancyData()
    {
        var inventory = Game1.player.Items;
        var items = new Dictionary<Item, int>();
        for (var index = 0; index < inventory.Count; index++)
        {
            var item = inventory[index];
            if (item is not null && !item.IsEssenceVial() &&
                item.modData.ContainsKey(Agromancy.Manifest.UniqueID))
            {
                if (isExtractMode && CropManager.GetCropReferenceByCropId(item.QualifiedItemId) is not null)
                {
                    items.Add(item, index);
                }
                else if (!isExtractMode && CropManager.GetCropReferenceBySeedId(item.QualifiedItemId) is not null)
                {
                    items.Add(item, index);
                }
            }
        }

        return items;
    }

    private Item? GetEssenceVial()
    {
        return Agrometer.attachments.Count > 0 ? Agrometer.attachments[0] : null;
    }

    private Vector2 GetEssenceVialSlotPosition()
    {
        return GetPointOnCircle(GetAgrometerCenter(), (agrometerFrame.Height / 2.225f) * GetAgrometerScale().Y, 90 + currentMenuRotation);
    }
    
    private Vector2 GetExtractAllPosition()
    {
        return GetPointOnCircle(GetAgrometerCenter(), (agrometerFrame.Height / 2.215f) * GetAgrometerScale().Y, -90 + currentMenuRotation);
    }

    public Item? GetCurrentlySelectedCrop()
    {
        return agromancyCrops.Count == 0 ? null : agromancyCrops.Keys.ElementAtOrDefault((2 + itemListOffset) % agromancyCrops.Count);
    }

    public void UpdateCropInitials()
    {
        if (GetCurrentlySelectedCrop() is not Object crop)
        {
            initialCropPrice = -1;
            initialCropEssences = null;
            return;
        }

        initialCropPrice = crop.Price;
        initialCropEssences = CropManager.GrabEssences(crop) ??
                              EssenceCalculator.DefaultEssences(CropManager.GetCropReferenceByCropId(crop.QualifiedItemId)) ??
                              EssenceCalculator.EmptyEssences;
    }
    
    public Vector2 GetPointOnCircle(Vector2 circleCenter, float radius, float angleDegrees)
    {
        float angleRadians = MathHelper.ToRadians(angleDegrees);
        float x = circleCenter.X + radius * (float)Math.Cos(angleRadians);
        float y = circleCenter.Y + radius * (float)Math.Sin(angleRadians);
        return new Vector2(x, y);
    }

    public CropEssences? GetCurrentlySelectedCropEssences()
    {
        Item? selectedCrop = GetCurrentlySelectedCrop();
        if (selectedCrop is null) return null;
        CropEssences? essences = CropManager.GrabEssences(selectedCrop);
        return essences;
    }

    private float GetEssenceInVial(int essenceIdx)
    {
        return EssenceVial?.modData[$"{Agromancy.UNIQUE_ID}_{essenceIdx}"] is not { } s ? 0 : float.Parse(s);
    }

    private Vector2 GetEssenceCenter(int essenceIdx)
    {
        Vector2 center = GetAgrometerCenter();
        float radius = agrometerFrame.Width / 2.35f * GetAgrometerScale().X;
        return essenceIdx switch {
            0 => GetPointOnCircle(center, radius, -30f + 7.5f + currentMenuRotation),
            1 => GetPointOnCircle(center, radius, 0 + 7.5f + currentMenuRotation),
            2 => GetPointOnCircle(center, radius, 30 + 7.5f + currentMenuRotation),
            3 => GetPointOnCircle(center, radius, 150 - 7.5f + currentMenuRotation),
            4 => GetPointOnCircle(center, radius, 180 - 7.5f + currentMenuRotation),
            5 => GetPointOnCircle(center, radius, 210 - 7.5f + currentMenuRotation),
            _ => Vector2.Zero
        };
    }

    private Rectangle GetEssenceIconSourceRect(int essenceIdx)
    {
        return new Rectangle(
            x: essenceIdx * 32,
            y: 0,
            width: 32,
            height: 32
        );
    }

    private string GetEssenceTooltip(int essenceIdx)
    {
        return essenceIdx switch {
            0 => $"{Agromancy.TKString("Yield")} {Agromancy.TKString("Essence")}:{Agromancy.TKString("YieldTip")}",
            1 => $"{Agromancy.TKString("Quality")} {Agromancy.TKString("Essence")}:{Agromancy.TKString("QualityTip")}",
            2 => $"{Agromancy.TKString("Growth")} {Agromancy.TKString("Essence")}:{Agromancy.TKString("GrowthTip")}",
            3 => $"{Agromancy.TKString("Giant")} {Agromancy.TKString("Essence")}:{Agromancy.TKString("GiantTip")}",
            4 => $"{Agromancy.TKString("Retention")} {Agromancy.TKString("Essence")}:{Agromancy.TKString("RetentionTip")}",
            5 => $"{Agromancy.TKString("Seed")} {Agromancy.TKString("Essence")}:{Agromancy.TKString("SeedTip")}",
            _ => ""
        };
    }

    private Vector2 GetRotateArrowPosition()
    {
        return GetPointOnCircle(GetAgrometerCenter(), (agrometerFrame.Height / 1.85f) * GetAgrometerScale().Y,
            currentMenuRotation + 90);
    }

    public bool IsPointInCircle(Vector2 point, Vector2 center, float radius)
    {
        float dx = point.X - center.X;
        float dy = point.Y - center.Y;
        return (dx * dx + dy * dy) <= radius * radius;
    }
}