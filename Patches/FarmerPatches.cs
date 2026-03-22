using System.Collections.Generic;
using Agromancy.Helpers;
using Agromancy.Menus;
using HarmonyLib;
using StardewValley;
using StardewValley.TokenizableStrings;

namespace Agromancy.Patches;

[HarmonyPatch(typeof(Farmer))]
public static class FarmerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Farmer.draw))]
    public static bool draw_Prefix(Farmer __instance)
    {
        return !__instance.UniqueMultiplayerID.Equals(Game1.player.UniqueMultiplayerID) ||
               Game1.activeClickableMenu is not AgrometerMenu;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Farmer.DrawShadow))]
    public static bool drawShadow_Prefix(Farmer __instance)
    {
        return !__instance.UniqueMultiplayerID.Equals(Game1.player.UniqueMultiplayerID) ||
               Game1.activeClickableMenu is not AgrometerMenu;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Farmer.OnItemReceived))]
    public static void OnItemReceived_Postfix(Farmer __instance, Item item)
    {
        if (item is not Tool agrometer || !agrometer.IsAgrometer()) return;
        
        if (__instance.mailReceived.Contains($"{Agromancy.UNIQUE_ID}_FoundAgrometer")) return;
        
        Game1.PerformActionWhenPlayerFree(delegate
        {
            if (!__instance.hasOrWillReceiveMail($"{Agromancy.UNIQUE_ID}_FoundAgrometer"))
            {
                __instance.mailReceived.Add($"{Agromancy.UNIQUE_ID}_FoundAgrometer");
                __instance.craftingRecipes.TryAdd($"{Agromancy.UNIQUE_ID}_Pedestal_Recipe", 0);
                __instance.craftingRecipes.TryAdd($"{Agromancy.UNIQUE_ID}_Altar_Recipe", 0);
                __instance.holdUpItemWithCustomMessage(item, [TokenParser.ParseText(Agromancy.TKString("FoundAgrometer_One")), TokenParser.ParseText(Agromancy.TKString("FoundAgrometer_Two"))]);
            }
        });
    }

    public static void holdUpItemWithCustomMessage(this Farmer who, Item item, List<string> message, bool showMessage = true)
    {
        who.completelyStopAnimatingOrDoingAction();
        if (showMessage)
        {
            Game1.MusicDuckTimer = 2000f;
            DelayedAction.playSoundAfterDelay("getNewSpecialItem", 750);
        }
        who.faceDirection(2);
        who.freezePause = 4000;
        who.FarmerSprite.animateOnce([
            new FarmerSprite.AnimationFrame(57, 0),
            new FarmerSprite.AnimationFrame(57, 2500, secondaryArm: false, flip: false, delegate(Farmer farmer)
            {
                Farmer.showHoldingItem(farmer, item);
            }),
            showMessage ? new FarmerSprite.AnimationFrame((short)who.FarmerSprite.CurrentFrame, 500, secondaryArm: false, flip: false, delegate
            {
                Game1.drawObjectDialogue(message);
                who.completelyStopAnimatingOrDoingAction();
            }, behaviorAtEndOfFrame: true) : new FarmerSprite.AnimationFrame((short)who.FarmerSprite.CurrentFrame, 500, secondaryArm: false, flip: false)
        ]);
        who.mostRecentlyGrabbedItem = item;
        who.canMove = false;
    }
}