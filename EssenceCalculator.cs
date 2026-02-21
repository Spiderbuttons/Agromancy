using System;
using System.Linq;
using Agromancy.Models;
using StardewValley;

namespace Agromancy;

public static class EssenceCalculator
{
    // These won't be used for essence calculations but will be used as limiters when the essences are "applied" to crops being harvested/watered/etc.
    private const int MAX_CROP_YIELD = 12;
    private const int MAX_SEED_YIELD = 4;
    private const float MAX_EXTRA_GROWTH_CHANCE = 0.5f;
    private const float MAX_QUALITY_BUMP_CHANCE = 1f;
    private const float MAX_GIANT_CHANCE = 1f;
    private const float MIN_WATER_CHANCE = 0f;

    public static void Mutate(this CropEssences essences, bool positiveOnly = false)
    {
        essences.YieldEssence = MutateEssence(essences.YieldEssence, positiveOnly);
        for (int i = 0; i < essences.QualityEssence.Length; i++)
        {
            essences.QualityEssence[i] = MutateEssence(essences.QualityEssence[i], positiveOnly);
        }
        essences.GrowthEssence = MutateEssence(essences.GrowthEssence, positiveOnly);
        essences.GiantEssence = MutateEssence(essences.GiantEssence, positiveOnly);
        essences.WaterEssence = MutateEssence(essences.WaterEssence, positiveOnly);
        essences.SeedEssence = MutateEssence(essences.SeedEssence, positiveOnly);
    }
    
    public static byte MutateEssence(byte original, bool positiveOnly = false)
    {
        int mutationRange = 10;
        int mutation = Game1.random.Next(-mutationRange, mutationRange + 1);
        if (positiveOnly) mutation = Math.Abs(mutation);
        int mutatedValue = Math.Clamp(original + mutation, 0, 255);
        return (byte)mutatedValue;
    }

    public static CropEssences DefaultEssences(AgroCropReference cropRef)
    {
        return new CropEssences
        {
            YieldEssence = DefaultYieldEssence(cropRef),
            QualityEssence = DefaultQualityEssence(cropRef),
            GrowthEssence = DefaultGrowthEssence(cropRef),
            GiantEssence = DefaultGiantEssence(cropRef),
            WaterEssence = DefaultWaterEssence(cropRef),
            SeedEssence = DefaultSeedEssence(cropRef)
        };
    }
    
    public static byte DefaultYieldEssence(AgroCropReference cropRef)
    {
        float minimumYieldByData = cropRef.CropData.HarvestMinStack;
        float extraYieldChance = (float)cropRef.CropData.ExtraHarvestChance;
        byte yieldEssence = (byte)(255 * extraYieldChance + minimumYieldByData / MAX_CROP_YIELD * 255);
        return yieldEssence;
    }
    
    public static byte[] DefaultQualityEssence(AgroCropReference cropRef)
    {
        byte[] qualityBump = new byte[3];
        for (int i = 0; i < 3; i++)
        {
            if (cropRef.CropData.HarvestMinQuality > i)
                qualityBump[i] = 255;
            else qualityBump[i] = 0;
        }
        return qualityBump;
    }
    
    public static byte DefaultGrowthEssence(AgroCropReference cropRef)
    {
        int totalGrowthDays = cropRef.CropData.DaysInPhase.Sum();
        
        // RegrowDays is -1 for non-regrowable crops, but I actually think that works in our favour here.
        totalGrowthDays += cropRef.CropData.RegrowDays;
        // The intent is to make slower growing crops have less harvest essence anyway, so regrowable crops should have a buff.
        
        float percentageOfMonthToGrow = totalGrowthDays / 28f;
        var harvestEssence = (byte)(255 * (1 - percentageOfMonthToGrow));
        
        return harvestEssence;
    }
    
    public static byte DefaultGiantEssence(AgroCropReference cropRef)
    {
        if (!cropRef.CanBeGiant) return 0;
        return (byte)(cropRef.GiantCropData!.Chance * 255);
    }
    
    public static byte DefaultWaterEssence(AgroCropReference cropRef)
    {
        return (byte)(cropRef.CropData.NeedsWatering ? 255 : 0);
    }
    
    public static byte DefaultSeedEssence(AgroCropReference cropRef)
    {
        return cropRef.CropData.HarvestItemId is "421" ? (byte)(0.15f * 255) : (byte)0;
    }
}