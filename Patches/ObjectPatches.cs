using Agromancy.Helpers;
using Agromancy.Models;
using HarmonyLib;
using Newtonsoft.Json;
using StardewValley;

namespace Agromancy.Patches;

[HarmonyPatch(typeof(Object))]
public class ObjectPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Object.getDescription))]
    public static void getDescription_Postfix(Object __instance, ref string __result)
    {
        if (!__instance.QualifiedItemId.Equals($"(O){Agromancy.UNIQUE_ID}_EssenceVial")) return;
        
        for (int i = 0; i < 7; i++)
        {
            __instance.modData.TryAdd($"{Agromancy.UNIQUE_ID}_{i}", "0");
        }
        
        float yield = float.Parse(__instance.modData[$"{Agromancy.UNIQUE_ID}_0"]) / 255f;
        float quality = float.Parse(__instance.modData[$"{Agromancy.UNIQUE_ID}_1"]) / 255f;
        float growth = float.Parse(__instance.modData[$"{Agromancy.UNIQUE_ID}_2"]) / 255f;
        float giant = float.Parse(__instance.modData[$"{Agromancy.UNIQUE_ID}_3"]) / 255f;
        float water = float.Parse(__instance.modData[$"{Agromancy.UNIQUE_ID}_4"]) / 255f;
        float seed = float.Parse(__instance.modData[$"{Agromancy.UNIQUE_ID}_5"]) / 255f;
        float total = yield + quality + growth + giant + water + seed;

        __result = string.Format(__result, yield.ToString("0.##"), quality.ToString("0.##"), growth.ToString("0.##"), giant.ToString("0.##"), water.ToString("0.##"), seed.ToString("0.##"), total.ToString("0.##"));
    }
}