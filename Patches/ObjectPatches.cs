using System;
using Agromancy.Helpers;
using Agromancy.Models;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using Object = StardewValley.Object;

namespace Agromancy.Patches;

[HarmonyPatch]
public class ObjectPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Object), nameof(Object.getDescription))]
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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Object), nameof(Object.draw), typeof(SpriteBatch), typeof(int), typeof(int), typeof(float), typeof(float))]
    public static void draw_Prefix(Object __instance, SpriteBatch spriteBatch, int xNonTile, int yNonTile, float layerDepth, float alpha)
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

        float fillPercentage = total / (255f * 6f);
        
        ParsedItemData itemData2 = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId);
        spriteBatch.Draw(
            texture: Game1.staminaRect,
            position: Game1.GlobalToLocal(
                viewport: Game1.viewport,
                globalPosition: new Vector2(
                    xNonTile + 32 + (__instance.shakeTimer > 0 ? Game1.random.Next(-1, 2) : 0),
                    yNonTile + 60 + (__instance.shakeTimer > 0 ? Game1.random.Next(-1, 2) : 0))),
            sourceRectangle: itemData2.GetSourceRect(0, __instance.ParentSheetIndex),
            color: Utility.GetPrismaticColor() * 0.85f,
            rotation: 0f,
            origin: new Vector2(8f, 16f),
            scale: __instance.scale.Y > 1f ? __instance.getScale() : new Vector2(2f, MathHelper.Lerp(0f, 2f, fillPercentage)),
            effects: __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
            layerDepth: layerDepth - 0.0000001f
            );
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Object), nameof(Object.drawInMenu))]
    public static void drawInMenu_Prefix(Object __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
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

        float fillPercentage = total / (255f * 6f);
        
        ParsedItemData itemData2 = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId);
        spriteBatch.Draw(
            texture: Game1.staminaRect,
            position: location + new Vector2(32f, 48f + (8 * scaleSize)),
            sourceRectangle: itemData2.GetSourceRect(0, __instance.ParentSheetIndex),
            color: Utility.GetPrismaticColor() * 0.85f,
            rotation: 0f,
            origin: new Vector2(8f, 16f),
            scale: __instance.scale.Y > 1f ? __instance.getScale() : new Vector2(2f * scaleSize, MathHelper.Lerp(0f, 2f, fillPercentage) * scaleSize),
            effects: __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
            layerDepth: layerDepth - 0.0000001f
            );
    }
    
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(Object), nameof(Object.maximumStackSize))]
    public static void maximumStackSize_Postfix(Object __instance, ref int __result)
    {
        if (__instance.QualifiedItemId.Equals($"(O){Agromancy.UNIQUE_ID}_EssenceVial"))
        {
            __result = 1;
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), nameof(Item.addToStack))]
    public static void addToStack_Postfix(Item __instance, Item otherStack, int __result)
    {
        int amountStacked = Math.Abs(__result - otherStack.Stack);
        if (amountStacked <= 0) return; // Nothing was actually stacked.

        CropEssences? originalEssences = CropManager.GrabEssences(__instance) ?? EssenceCalculator.DefaultEssences(CropManager.GetCropReference(__instance.QualifiedItemId));
        CropEssences? otherEssences = CropManager.GrabEssences(otherStack) ?? EssenceCalculator.DefaultEssences(CropManager.GetCropReference(otherStack.QualifiedItemId));

        if (originalEssences is null || otherEssences is null) return; // Not actually crops/seeds.
        
        CropEssences newEssences = originalEssences;
        while (amountStacked > 0)
        {
            // I don't know why this works when I do a loop like this but not if I just set the stack size in the second argument of the AverageEssences function.
            newEssences = EssenceCalculator.AverageEssences(
                new Tuple<CropEssences, int>(newEssences, __instance.Stack - amountStacked),
                new Tuple<CropEssences, int>(otherEssences, 1));
            amountStacked--;
        }

        __instance.ApplyEssences(newEssences);
    }
}