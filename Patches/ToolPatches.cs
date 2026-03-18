using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Agromancy.Patches;

[HarmonyPatch(typeof(Tool))]
public static class ToolPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Tool.DoFunction))]
    public static void DoFunction_Postfix(Tool __instance)
    {
        if (__instance.IsAgrometer())
        {
            Game1.activeClickableMenu = new Menus.AgrometerMenu(__instance);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Tool.canThisBeAttached), [typeof(Object), typeof(int)])]
    public static void canThisBeAttached_Postfix(Tool __instance, Object o, ref bool __result)
    {
        if (__instance.IsAgrometer())
        {
            __result = o.IsEssenceVial();
        }
    }
    
    internal static bool IsAgrometer(this Tool tool)
    {
        return tool.QualifiedItemId.Equals($"(T){Agromancy.UNIQUE_ID}_Agrometer");
    }
}