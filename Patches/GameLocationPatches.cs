using System;
using System.Linq;
using Agromancy.Helpers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using Object = StardewValley.Object;

namespace Agromancy.Patches;

[HarmonyPatch(typeof(GameLocation))]
public class GameLocationPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(GameLocation.checkForBuriedItem))]
    public static void makeHoeDirt_Postfix(GameLocation __instance, int xLocation, int yLocation, bool explosion, bool detectOnly, Farmer who)
    {
        if (explosion || detectOnly || !__instance.IsFarm) return;

        if (who.mailReceived.Contains($"{Agromancy.UNIQUE_ID}_FoundAgrometer")) return;

        Random rng = Utility.CreateDaySaveRandom(who.stats.DirtHoed);
        float chance = 0.001f + who.stats.DaysPlayed * 0.01f;
        float bonusChance = who.activeDialogueEvents.Keys.Any(s => s.StartsWith("cropMatured")) || who.previousActiveDialogueEvents.Keys.Any(s => s.StartsWith("cropMatured"))
            ? 0.1f
            : 0.0f;
        chance += bonusChance;
        if (rng.NextDouble() > chance) return;
        
        Tool agrometer = ItemRegistry.Create<Tool>($"(T){Agromancy.UNIQUE_ID}_Agrometer");
        Object essenceVial = ItemRegistry.Create<Object>($"(O){Agromancy.UNIQUE_ID}_EssenceVial");
        agrometer.attachments[0] = essenceVial;
        Game1.createItemDebris(agrometer, new Vector2(xLocation * 64, yLocation * 64), Game1.random.Next(4), location: __instance);
    }
}