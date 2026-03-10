using System;
using System.Collections.Generic;
using System.Linq;
using Agromancy.Helpers;
using Agromancy.Models;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.GameData.GiantCrops;
using StardewValley.TerrainFeatures;

namespace Agromancy;

public class CropManager
{
    public static Dictionary<string, AgroCropReference> SeedIdToCropDataLookup { get; set; } = new();
    public static Dictionary<string, AgroCropReference> CropIdToCropDataLookup { get; set; } = new();

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
    
    public CropManager()
    {
        FillLookups();
        Agromancy.ModHelper.Events.Player.InventoryChanged += OnInventoryChanged;
        Agromancy.ModHelper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
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

    public static Item ModifyHarvestedCrop(Item harvest, Crop crop)
    {
        Log.Alert("Modifying crop harvest.");
        Random rng = new Random((int)Game1.stats.DaysPlayed);
        CropEssences essences = GrabEssences(crop) ?? EssenceCalculator.DefaultEssences(GetCropReferenceByCropId($"(O){crop.indexOfHarvest.Value}")) ?? EssenceCalculator.EmptyEssences;
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
        
        return (Item)harvest.ApplyEssences(essences);
    }
    
    public static AgroCropReference? GetCropReferenceBySeedId(string seedId)
    {
        EnsureLookups();
        return SeedIdToCropDataLookup.GetValueOrDefault(seedId);
    }
    
    public static AgroCropReference? GetCropReferenceByCropId(string cropId)
    {
        EnsureLookups();
        return CropIdToCropDataLookup.GetValueOrDefault(cropId);
    }

    public static AgroCropReference? GetCropReference(string itemId)
    {
        EnsureLookups();
        if (SeedIdToCropDataLookup.TryGetValue(itemId, out AgroCropReference? cropRef) ||
            CropIdToCropDataLookup.TryGetValue(itemId, out cropRef))
            return cropRef;

        return null;
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