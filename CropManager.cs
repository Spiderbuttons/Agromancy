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
            
            CropEssences newCropEssences = EssenceCalculator.DefaultEssences(cropRef);
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