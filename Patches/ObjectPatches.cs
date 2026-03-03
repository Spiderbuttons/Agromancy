using Agromancy.Helpers;
using HarmonyLib;
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
}