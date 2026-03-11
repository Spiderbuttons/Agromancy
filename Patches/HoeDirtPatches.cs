using Agromancy.Helpers;
using Agromancy.Models;
using HarmonyLib;
using Newtonsoft.Json;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Agromancy.Patches;

[HarmonyPatch(typeof(HoeDirt))]
public class HoeDirtPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(HoeDirt.plant))]
    public static void plant_Postfix(HoeDirt __instance, Farmer who, bool isFertilizer, bool __result)
    {
        if (__result && !isFertilizer)
        {
            Log.Debug("Crop planted, adding Agromancy essences.");
            
            CropEssences essences = CropManager.GrabEssences(who.ActiveObject) ?? EssenceCalculator.DefaultEssences(CropManager.GetCropReferenceBySeedId(who.ActiveObject.QualifiedItemId)) ?? EssenceCalculator.EmptyEssences;
            
            __instance.crop.modData[Agromancy.Manifest.UniqueID] = JsonConvert.SerializeObject(essences);
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(HoeDirt.GetFertilizerWaterRetentionChance))]
    public static void GetFertilizerWaterRetentionChance_Postfix(HoeDirt __instance, ref float __result)
    {
        if (__instance.crop?.indexOfHarvest.Value is null) return;
        
        CropEssences essences = CropManager.GrabEssences(__instance.crop) ?? EssenceCalculator.DefaultEssences(CropManager.GetCropReferenceBySeedId(__instance.crop.indexOfHarvest.Value)) ?? EssenceCalculator.EmptyEssences;
        
        Log.Debug($"Checking water use for hoedirt on tile {__instance.Tile}. Initial result: {__result}, adding {essences.WaterEssence / 255f} from WaterEssence.");
        
        __result += essences.WaterEssence / 255f;
    }
}