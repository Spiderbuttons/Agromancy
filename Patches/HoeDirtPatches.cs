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
    public static void plant_Postfix(HoeDirt __instance, Farmer who, bool __result)
    {
        if (__result)
        {
            Log.Debug("Crop planted, adding Agromancy essences.");
            
            CropManager.EnsureLookups();
            CropEssences essences = CropManager.GrabEssences(who.ActiveObject) ??
                                    EssenceCalculator.DefaultEssences(CropManager.GetCropReferenceBySeedId(who.ActiveObject.QualifiedItemId));
            
            __instance.crop.modData[Agromancy.Manifest.UniqueID] = JsonConvert.SerializeObject(essences);
        }
    }
}