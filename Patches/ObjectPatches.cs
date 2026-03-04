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

        __result = string.Format(__result, __instance.modData[$"{Agromancy.UNIQUE_ID}_{0}"],
            __instance.modData[$"{Agromancy.UNIQUE_ID}_{1}"], __instance.modData[$"{Agromancy.UNIQUE_ID}_{2}"],
            __instance.modData[$"{Agromancy.UNIQUE_ID}_{3}"], __instance.modData[$"{Agromancy.UNIQUE_ID}_{4}"],
            __instance.modData[$"{Agromancy.UNIQUE_ID}_{5}"], __instance.modData[$"{Agromancy.UNIQUE_ID}_{6}"]);
    }
}