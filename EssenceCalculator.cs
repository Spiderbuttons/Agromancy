using System;
using System.Linq;
using Agromancy.Models;
using Newtonsoft.Json;
using StardewValley;

namespace Agromancy;

public static class EssenceCalculator
{
    public const int YIELD_INDEX = 0;
    public const int QUALITY_INDEX = 1;
    public const int GROWTH_INDEX = 2;
    public const int GIANT_INDEX = 3;
    public const int WATER_INDEX = 4;
    public const int SEED_INDEX = 5;
    
    // These won't be used for essence calculations but will be used as limiters when the essences are "applied" to crops being harvested/watered/etc.
    private const int MAX_CROP_YIELD = 12;
    private const int MAX_SEED_YIELD = 4;
    private const float MAX_EXTRA_GROWTH_CHANCE = 0.5f;
    private const float MAX_QUALITY_BUMP_CHANCE = 1f;
    private const float MAX_GIANT_CHANCE = 1f;
    private const float MIN_WATER_CHANCE = 0f;

    public static void Mutate(this CropEssences essences, int range = 10, bool positiveOnly = false)
    {
        essences.YieldEssence = MutateEssence(essences.YieldEssence, range, positiveOnly);
        for (int i = 0; i < essences.QualityEssence.Length; i++)
        {
            essences.QualityEssence[i] = MutateEssence(essences.QualityEssence[i], range, positiveOnly);
        }
        essences.GrowthEssence = MutateEssence(essences.GrowthEssence, range, positiveOnly);
        essences.GiantEssence = MutateEssence(essences.GiantEssence, range, positiveOnly);
        essences.WaterEssence = MutateEssence(essences.WaterEssence, range, positiveOnly);
        essences.SeedEssence = MutateEssence(essences.SeedEssence, range, positiveOnly);
    }

    public static IHaveModData ApplyEssences(this IHaveModData essenceTarget, CropEssences essences)
    {
        essenceTarget.modData[Agromancy.Manifest.UniqueID] = JsonConvert.SerializeObject(essences);
        return essenceTarget;
    }
    
    public static byte MutateEssence(byte original, int range = 10, bool positiveOnly = false)
    {
        int mutation = Game1.random.Next(-range, range + 1);
        if (positiveOnly) mutation = Math.Abs(mutation);
        int mutatedValue = Math.Clamp(original + mutation, 0, 255);
        return (byte)mutatedValue;
    }

    public static float PercentToPerfectCrop(CropEssences essences)
    {
        float yieldPercent = essences.YieldEssence / 255f;
        float qualityPercent = (float)essences.QualityEssence.Average(x => x) / 255f;
        float growthPercent = essences.GrowthEssence / 255f;
        float giantPercent = essences.GiantEssence / 255f;
        float waterPercent = essences.WaterEssence / 255f;
        float seedPercent = essences.SeedEssence / 255f;
        
        float overallPercent = (yieldPercent + qualityPercent + growthPercent + giantPercent + waterPercent + seedPercent) / 6f;
        return overallPercent;
    }

    public static float GetEssencePercent(CropEssences? essences, int idx)
    {
        if (essences is null) return 0f;
        return idx switch
        {
            YIELD_INDEX => essences.YieldEssence / 255f,
            QUALITY_INDEX => essences.QualityEssence.Sum(x => x) / (255f * essences.QualityEssence.Length),
            GROWTH_INDEX => essences.GrowthEssence / 255f,
            GIANT_INDEX => essences.GiantEssence / 255f,
            WATER_INDEX => essences.WaterEssence / 255f,
            SEED_INDEX => essences.SeedEssence / 255f,
            _ => throw new ArgumentOutOfRangeException(nameof(idx), idx, null)
        };
    }

    public static CropEssences DefaultEssences(AgroCropReference? cropRef)
    {
        if (cropRef is null)
        {
            return new CropEssences
            {
                YieldEssence = 0,
                QualityEssence = [0, 0, 0],
                GrowthEssence = 0,
                GiantEssence = 0,
                WaterEssence = 0,
                SeedEssence = 0
            };
        }
        
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
        var harvestEssence = (byte)(255 * percentageOfMonthToGrow);
        
        return harvestEssence;
    }
    
    public static byte DefaultGiantEssence(AgroCropReference cropRef)
    {
        if (!cropRef.CanBeGiant) return 0;
        return (byte)(cropRef.GiantCropData!.Chance * 255);
    }
    
    public static byte DefaultWaterEssence(AgroCropReference cropRef)
    {
        return (byte)(cropRef.CropData.NeedsWatering ? 0 : 255);
    }
    
    public static byte DefaultSeedEssence(AgroCropReference cropRef)
    {
        return cropRef.CropData.HarvestItemId is "421" ? (byte)(0.15f * 255) : (byte)0;
    }
}