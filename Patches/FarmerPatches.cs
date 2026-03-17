using System.Collections.Generic;
using Agromancy.Helpers;
using HarmonyLib;
using StardewValley;

namespace Agromancy.Patches;

[HarmonyPatch(typeof(Farmer))]
public static class FarmerPatches
{
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
                __instance.holdUpItemWithCustomMessage(item, ["You found an Agrometer! It looks like there's something attached...", "Why don't you take a look at your crops with it?"]);
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