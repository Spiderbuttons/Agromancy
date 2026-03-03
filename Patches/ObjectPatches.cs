using Agromancy.Helpers;
using Agromancy.Models;
using HarmonyLib;
using Newtonsoft.Json;
using StardewValley;

namespace Agromancy.Patches;

[HarmonyPatch(typeof(Object))]
public class ObjectPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Object.performUseAction))]
    public static bool performUseAction_Prefix(Object __instance, ref bool __result)
    {
        if (!Game1.player.canMove || __instance.isTemporarilyInvisible)
        {
            return true;
        }
        bool normal_gameplay = !Game1.eventUp && !Game1.isFestival() && !Game1.fadeToBlack && !Game1.player.swimming.Value && !Game1.player.bathingClothes.Value && !Game1.player.onBridge.Value;

        if (!normal_gameplay) return true;
        
        if (__instance.QualifiedItemId.Equals($"(O){Agromancy.UNIQUE_ID}_Agrometer"))
        {
            Game1.activeClickableMenu = new Menus.AgrometerMenu();
            __result = false;
            return false;
        }
        
        return true;
    }
    
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