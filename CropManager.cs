using System;
using System.Collections.Generic;
using System.Linq;
using Agromancy.Helpers;
using Agromancy.Models;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Crops;
using StardewValley.GameData.GiantCrops;
using StardewValley.TerrainFeatures;

namespace Agromancy;

public class CropManager
{
    public const int MAX_CROP_YIELD = 3;
    public const int MAX_SEED_YIELD = 3;
    public const int MAX_EXTRA_GROWTH_UPDATES = 1;
    
    public static Dictionary<string, AgroCropReference> SeedIdToCropDataLookup { get; set; } = new();
    public static Dictionary<string, AgroCropReference> CropIdToCropDataLookup { get; set; } = new();
    
    public CropManager()
    {
        FillLookups();
        Agromancy.ModHelper.Events.Player.InventoryChanged += OnInventoryChanged;
        Agromancy.ModHelper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
        Agromancy.ModHelper.Events.GameLoop.DayStarted += OnDayStarted;
    }
    
    public static void EnsureLookups()
    {
        if (!SeedIdToCropDataLookup.Any() || !CropIdToCropDataLookup.Any())
            FillLookups();
    }

    private static void FillLookups()
    {
        var cropData = DataLoader.Crops(Game1.content);
        var giantCropData = DataLoader.GiantCrops(Game1.content);
        foreach (var (key, value) in cropData)
        {
            string seedId = $"(O){key}";
            string cropId = $"(O){value.HarvestItemId}";
            GiantCropData gcData = giantCropData.FirstOrDefault(gcd => gcd.Value.FromItemId.Equals(cropId)).Value;
            AgroCropReference cropRef = new(value, gcData);
            SeedIdToCropDataLookup[seedId] = cropRef;
            CropIdToCropDataLookup[cropId] = cropRef;
        }
    }

    public static CropEssences? GrabEssences(IHaveModData essenceSource)
    {
        if (!essenceSource.modData.TryGetValue(Agromancy.Manifest.UniqueID, out string? serializedEssences))
            return null;

        try
        {
            CropEssences? essences = JsonConvert.DeserializeObject<CropEssences>(serializedEssences);
            return essences;
        }
        catch (JsonException ex)
        {
            Agromancy.ModMonitor.Log($"Failed to deserialize Crop Essences: {ex.Message}", LogLevel.Error);
            return null;
        }
    }

    public static Item ModifyGiantCropDrop(Item drop, GiantCrop crop)
    {
        if (!GiantCrop.TryGetData(crop.Id, out var data)) return drop;
        
        CropEssences essences = GrabEssences(crop) ?? EssenceCalculator.DefaultEssences(GetCropReferenceByCropId(data.FromItemId)) ?? EssenceCalculator.EmptyEssences;
        essences.GiantEssence = (byte)Math.Min(255, essences.GiantEssence * 1.2f);
        drop.ApplyEssences(essences);
        
        return drop;
    }

    public static Item ModifyHarvestedCrop(Item harvest, Crop crop)
    {
        Log.Alert("Modifying crop harvest.");
        Random rng = new Random((int)Game1.stats.DaysPlayed);
        Point pos = crop.tilePosition.ToPoint();
        AgroCropReference? cropRef = GetCropReferenceByCropId($"(O){crop.indexOfHarvest.Value}");
        CropEssences essences = GrabEssences(crop) ?? EssenceCalculator.DefaultEssences(cropRef) ?? EssenceCalculator.EmptyEssences;
        // essences.Mutate(range: 5, positiveOnly: true); // TODO: Config option to allow negative mutations.
        
        /* Quality */
        int qual = essences.QualityEssence;
        int qualityToRaiseTo = 1;
        while (qual > 0)
        {
            Log.Debug($"Applying quality essence. Current essence: {qual}, percent chance to raise to next quality ({qualityToRaiseTo}): {(qual / 85f) * 100}%");
            float percentChance = qual / 85f; // 255 / 3 = 85
            if (rng.NextDouble() < percentChance)
            {
                Log.Debug($"Raising quality to {qualityToRaiseTo}.");
                harvest.Quality = Math.Max(harvest.Quality, qualityToRaiseTo);
            }
            qual -= 85;
            qualityToRaiseTo++;
        }
        harvest.FixQuality();
        essences.QualityEssence = harvest.Quality switch 
        {
            0 => Math.Max(essences.QualityEssence, (byte)0),
            1 => Math.Max(essences.QualityEssence, (byte)85),
            2 => Math.Max(essences.QualityEssence, (byte)170),
            4 => Math.Max(essences.QualityEssence, (byte)255),
            _ => essences.QualityEssence
        };
        
        /* Water Retention */
        float retention = crop.Dirt.GetFertilizerWaterRetentionChance();
        byte waterEssence = Math.Clamp((byte)((retention / 4f) * 255), (byte)0, (byte)255);
        essences.WaterEssence = Math.Max(essences.WaterEssence, waterEssence);
        Log.Debug($"Calculated water essence: {waterEssence}, retention chance: {retention}. Setting crop's water essence to {essences.WaterEssence}.");
        
        /* Giant */
        // (See CropPatches for relevant GiantCrop code.)
        
        /* Growth */
        // (See OnDayUpdated.)
        
        /* Yield */
        /* /!\ This has to be done after quality, since the generated crops need to share the quality and essences after everything's been applied. /!\ */
        int startingYield = harvest.Stack;
        float percentToBump = essences.YieldEssence / 255f;
        int extraYield = 0;
        while (extraYield < MAX_CROP_YIELD && rng.NextDouble() < percentToBump)
        {
            extraYield++;
            for (int i = 0; i < startingYield; i++)
            {
                createObjectDebrisWithEssence(harvest.QualifiedItemId, pos.X, pos.Y, essences, location: crop.currentLocation, itemQuality: harvest.Quality, velocityMultiplyer: 1.1f);
            }

            Log.Debug($"Bumping crop yield by 1. Current extra yield: {extraYield}");
        }
        
        /* Seed */
        float seedChance = essences.SeedEssence / 255f;
        int droppedSeeds = 0;
        while (droppedSeeds < MAX_SEED_YIELD && rng.NextDouble() < seedChance)
        {
            droppedSeeds++;
            createObjectDebrisWithEssence(ItemRegistry.QualifyItemId(crop.isWildSeedCrop() ? crop.whichForageCrop.Value : crop.netSeedIndex.Value), pos.X, pos.Y, essences, location: crop.currentLocation, velocityMultiplyer: 1.1f);
            Log.Debug($"Dropping an extra seed. Current extra seeds: {droppedSeeds}");
        }
        
        return (Item)harvest.ApplyEssences(essences);
    }
    
    public static AgroCropReference? GetCropReferenceBySeedId(string? seedId)
    {
        EnsureLookups();
        return seedId is null ? null : SeedIdToCropDataLookup.GetValueOrDefault(ItemRegistry.QualifyItemId(seedId));
    }
    
    public static AgroCropReference? GetCropReferenceByCropId(string? cropId)
    {
        EnsureLookups();
        return cropId is null ? null : CropIdToCropDataLookup.GetValueOrDefault(ItemRegistry.QualifyItemId(cropId));
    }

    public static AgroCropReference? GetCropReference(string itemId)
    {
        EnsureLookups();
        if (SeedIdToCropDataLookup.TryGetValue(itemId, out AgroCropReference? cropRef) ||
            CropIdToCropDataLookup.TryGetValue(itemId, out cropRef))
            return cropRef;

        return null;
    }
    
    public static void createObjectDebrisWithEssence(string id, int xTile, int yTile, CropEssences essences, int groundLevel = -1, int itemQuality = 0, float velocityMultiplyer = 1f, GameLocation? location = null)
    {
        if (location == null)
        {
            location = Game1.currentLocation;
        }
        Debris d = new Debris(id, new Vector2(xTile * 64 + 32, yTile * 64 + 32), Game1.player.getStandingPosition())
        {
            itemQuality = itemQuality,
            item = ItemRegistry.Create(id, quality: itemQuality).ApplyEssences(essences) as Item
        };
        foreach (Chunk chunk in d.Chunks)
        {
            chunk.xVelocity.Value *= velocityMultiplyer;
            chunk.yVelocity.Value *= velocityMultiplyer;
        }
        if (groundLevel != -1)
        {
            d.chunkFinalYLevel = groundLevel;
        }
        location.debris.Add(d);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        Utility.ForEachCrop(crop =>
        {
            if (crop.indexOfHarvest.Value is null || crop.dead.Value || crop.fullyGrown.Value) return true;
            
            Random rng = Utility.CreateDaySaveRandom();
            CropEssences essences = GrabEssences(crop) ?? EssenceCalculator.DefaultEssences(GetCropReferenceByCropId(crop.indexOfHarvest.Value)) ?? EssenceCalculator.EmptyEssences;
            float extraGrowthChance = EssenceCalculator.GetEssencePercent(essences, EssenceCalculator.GROWTH_INDEX);
            int extraGrowths = 0;
            while (extraGrowths < MAX_EXTRA_GROWTH_UPDATES && rng.NextDouble() < extraGrowthChance)
            {
                crop.newDay(crop.Dirt.state.Value);
                extraGrowths++;
                Log.Debug($"Applying extra growth to crop at {crop.tilePosition}. Current phase: {crop.currentPhase}, extra growths applied: {extraGrowths}");
            }
            
            return true;
        });
    }

    private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
    {
        EnsureLookups();
        foreach (var item in e.Added)
        {
            if (!SeedIdToCropDataLookup.TryGetValue(item.QualifiedItemId, out AgroCropReference? cropRef) &&
                !CropIdToCropDataLookup.TryGetValue(item.QualifiedItemId, out cropRef))
                continue;

            if (item.modData.ContainsKey(Agromancy.Manifest.UniqueID))
                continue;
            
            Log.Error("Adding Crop Essences to " + item.DisplayName);
            
            CropEssences newCropEssences = EssenceCalculator.DefaultEssences(cropRef) ?? EssenceCalculator.EmptyEssences;
            Log.Info(newCropEssences.ToString());
            item.modData[Agromancy.Manifest.UniqueID] = JsonConvert.SerializeObject(newCropEssences);
        }
    }

    private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (e.NamesWithoutLocale.Any(asset => asset.IsEquivalentTo("Data/Crops") || asset.IsEquivalentTo("Data/GiantCrops")))
        {
            SeedIdToCropDataLookup.Clear();
            CropIdToCropDataLookup.Clear();
        }
    }
}