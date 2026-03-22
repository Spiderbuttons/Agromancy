using System;
using System.Collections.Generic;
using Agromancy.Helpers;
using Agromancy.Models;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.ModHelpers;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TokenizableStrings;
using Object = StardewValley.Object;

namespace Agromancy.Patches;

[HarmonyPatch]
public static class ObjectPatches
{
    public static bool IsEssenceVial(this Item? obj)
    {
        if (obj is null) return false;
        return obj.QualifiedItemId.Equals($"(O){Agromancy.UNIQUE_ID}_T1EssenceVial") ||
               obj.QualifiedItemId.Equals($"(O){Agromancy.UNIQUE_ID}_T2EssenceVial") ||
               obj.QualifiedItemId.Equals($"(O){Agromancy.UNIQUE_ID}_T3EssenceVial");
    }

    public static int GetEssenceVialTier(Item obj)
    {
        if (!obj.IsEssenceVial()) return -1;
        return obj.QualifiedItemId switch
        {
            var id when id.Equals($"(O){Agromancy.UNIQUE_ID}_T1EssenceVial") => 1,
            var id when id.Equals($"(O){Agromancy.UNIQUE_ID}_T2EssenceVial") => 2,
            var id when id.Equals($"(O){Agromancy.UNIQUE_ID}_T3EssenceVial") => 3,
            _ => -1
        };
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Object), nameof(Object.getCategoryName))]
    public static void getCategoryName_Postfix(Item __instance, ref string __result)
    {
        if (__instance.IsEssenceVial())
        {
            __result = TokenParser.ParseText(Agromancy.TKString("Agromancy"));
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Object), nameof(Object.getCategoryColor))]
    public static void getCategoryName_Postfix(Item __instance, ref Color __result)
    {
        if (__instance.IsEssenceVial())
        {
            __result = Utility.GetPrismaticColor();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Object), nameof(Object._PopulateContextTags))]
    public static void PopulateContextTags_Postfix(Object __instance, HashSet<string> tags)
    {
        if (CropManager.GetCropReferenceByCropId(__instance.QualifiedItemId) is not null)
        {
            tags.Add("agromantic_crop");
        }
        else if (CropManager.GetCropReferenceBySeedId(__instance.QualifiedItemId) is not null)
        {
            tags.Add("agromantic_seed");
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), nameof(Item.GetContextTags))] // I don't want to regenerate every context tag on every item in an existing save, so this patch handles existing items.
    public static void GetContextTags_Postfix(Item __instance, ref HashSet<string> __result)
    {
        if (CropManager.GetCropReferenceByCropId(__instance.QualifiedItemId) is not null)
        {
            __result.Add("agromantic_crop");
        }
        else if (CropManager.GetCropReferenceBySeedId(__instance.QualifiedItemId) is not null)
        {
            __result.Add("agromantic_seed");
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Object), nameof(Object.getDescription))]
    public static void getDescription_Postfix(Object __instance, ref string __result)
    {
        if (!__instance.IsEssenceVial()) return;
        
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
    public static void draw_Prefix(Object __instance, SpriteBatch spriteBatch)
    {
        if (!__instance.IsEssenceVial()) return;
        
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

        float fillPercentage = Math.Clamp(total / (10f * GetEssenceVialTier(__instance) * 6f), 0f, 1f);
        Agromancy.EssenceVialFx.Parameters["PerlinNoise"].SetValue(Agromancy.PerlinNoise);
        Agromancy.EssenceVialFx.Parameters["Waviness"].SetValue(fillPercentage > 0f ? 0.5f : 0f);
        Agromancy.EssenceVialFx.Parameters["FillPercentage"].SetValue(fillPercentage > 0f ? fillPercentage + 0.5f : 0f);
        Agromancy.EssenceVialFx.Parameters["BottomOfVial"].SetValue(1f - 0.125f);
        Agromancy.EssenceVialFx.Parameters["TopOfVial"].SetValue(0.5f);
        Agromancy.EssenceVialFx.Parameters["Time"].SetValue((float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500f);
        Agromancy.EssenceVialFx.Parameters["PrismaticColour"].SetValue(new Vector4(Utility.GetPrismaticColor().R / 255f, Utility.GetPrismaticColor().G / 255f, Utility.GetPrismaticColor().B / 255f, 0.9f));
        Agromancy.EssenceVialFx.Parameters["GlassShineColour"].SetValue(new Vector4(219, 211, 206, 255) / 255f);
        Agromancy.EssenceVialFx.Parameters["Flipped"].SetValue(false);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, effect: Agromancy.EssenceVialFx);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Object), nameof(Object.draw), typeof(SpriteBatch), typeof(int), typeof(int), typeof(float), typeof(float))]
    public static void draw_Postfix(Object __instance, SpriteBatch spriteBatch)
    {
        if (!__instance.IsEssenceVial()) return;
        
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Object), nameof(Object.drawInMenu))]
    public static void drawInMenu_Prefix(Object __instance, SpriteBatch spriteBatch)
    {
        if (!__instance.IsEssenceVial()) return;
        
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

        float fillPercentage = Math.Clamp(total / (10f * GetEssenceVialTier(__instance) * 6f), 0f, 1f);
        Agromancy.EssenceVialFx.Parameters["PerlinNoise"].SetValue(Agromancy.PerlinNoise);
        Agromancy.EssenceVialFx.Parameters["Waviness"].SetValue(fillPercentage > 0f ? 0.5f : 0f);
        Agromancy.EssenceVialFx.Parameters["FillPercentage"].SetValue(fillPercentage > 0f ? fillPercentage + 0.5f : 0f);
        Agromancy.EssenceVialFx.Parameters["BottomOfVial"].SetValue(1f - 0.125f);
        Agromancy.EssenceVialFx.Parameters["TopOfVial"].SetValue(0.5f);
        Agromancy.EssenceVialFx.Parameters["Time"].SetValue((float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500f);
        Agromancy.EssenceVialFx.Parameters["PrismaticColour"].SetValue(new Vector4(Utility.GetPrismaticColor().R / 255f, Utility.GetPrismaticColor().G / 255f, Utility.GetPrismaticColor().B / 255f, 0.9f));
        Agromancy.EssenceVialFx.Parameters["GlassShineColour"].SetValue(new Vector4(219, 211, 206, 255) / 255f);
        Agromancy.EssenceVialFx.Parameters["Flipped"].SetValue(false);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, effect: Agromancy.EssenceVialFx);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Object), nameof(Object.drawInMenu))]
    public static void drawInMenu_Postfix(Object __instance, SpriteBatch spriteBatch)
    {
        if (!__instance.IsEssenceVial()) return;
        
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Object), nameof(Object.drawWhenHeld))]
    public static void drawWhenHeld_Prefix(Object __instance, ref SpriteBatch spriteBatch, ref Vector2 objectPosition, ref (SpriteBatch, RenderTarget2D?, RenderTarget2D, Vector2) __state)
    {
        if (!__instance.IsEssenceVial()) return;
        
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
        
        float fillPercentage = Math.Clamp(total / (10f * GetEssenceVialTier(__instance) * 6f), 0f, 1f);
        Agromancy.EssenceVialFx.Parameters["PerlinNoise"].SetValue(Agromancy.PerlinNoise);
        Agromancy.EssenceVialFx.Parameters["Waviness"].SetValue(fillPercentage > 0f ? 0.5f : 0f);
        Agromancy.EssenceVialFx.Parameters["FillPercentage"].SetValue(fillPercentage > 0f ? fillPercentage + 0.5f : 0f);
        Agromancy.EssenceVialFx.Parameters["BottomOfVial"].SetValue(1f - 0.125f);
        Agromancy.EssenceVialFx.Parameters["TopOfVial"].SetValue(0.5f);
        Agromancy.EssenceVialFx.Parameters["Time"].SetValue((float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500f);
        Agromancy.EssenceVialFx.Parameters["PrismaticColour"].SetValue(new Vector4(Utility.GetPrismaticColor().R / 255f, Utility.GetPrismaticColor().G / 255f, Utility.GetPrismaticColor().B / 255f, 0.9f));
        Agromancy.EssenceVialFx.Parameters["GlassShineColour"].SetValue(new Vector4(219, 211, 206, 255) / 255f);
        Agromancy.EssenceVialFx.Parameters["Flipped"].SetValue(false);
        
        // Thank you to mushymato aka chu my lifesaver
        ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId);
        Rectangle sourceRect = dataOrErrorItem.GetSourceRect();
        RenderTarget2D? wasRenderTarget;
        {
            RenderTargetBinding[] wasRenderTargets = Game1.graphics.GraphicsDevice.GetRenderTargets();
            wasRenderTarget = wasRenderTargets.Length > 0 ? wasRenderTargets[0].RenderTarget as RenderTarget2D : null;
        }
        RenderTarget2D renderTarget = new(
            Game1.graphics.GraphicsDevice,
            sourceRect.Width * 4,
            sourceRect.Height * 4,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents
        );
        Game1.SetRenderTarget(renderTarget);
        SpriteBatch batch = Agromancy.VialSpriteBatch ??= new SpriteBatch(Game1.graphics.GraphicsDevice);
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, effect: Agromancy.EssenceVialFx);
        Game1.graphics.GraphicsDevice.Clear(Color.Transparent);
        __state = new ValueTuple<SpriteBatch, RenderTarget2D?, RenderTarget2D, Vector2>(spriteBatch, wasRenderTarget, renderTarget, objectPosition);
        spriteBatch = batch;
        objectPosition = Vector2.Zero;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Object), nameof(Object.drawWhenHeld))]
    public static void drawWhenHeld_Postfix(Object __instance, SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f, ref (SpriteBatch, RenderTarget2D?, RenderTarget2D, Vector2) __state)
    {
        if (!__instance.IsEssenceVial()) return;
        
        SpriteBatch realBatch = __state.Item1;
        RenderTarget2D? wasRenderTarget = __state.Item2;
        RenderTarget2D renderTarget = __state.Item3;
        Vector2 realPos = __state.Item4;
        spriteBatch.End();
        Game1.SetRenderTarget(wasRenderTarget);

        realBatch.Draw(
            texture: renderTarget,
            position: realPos,
            sourceRectangle: renderTarget.Bounds,
            color: Color.White,
            rotation: 0f,
            origin: Vector2.Zero,
            scale: 1f,
            effects: SpriteEffects.None,
            layerDepth: Math.Max(0f, (f.StandingPixel.Y + 3) / 10000f)
        );
    }
    
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(Object), nameof(Object.maximumStackSize))]
    public static void maximumStackSize_Postfix(Object __instance, ref int __result)
    {
        if (__instance.IsEssenceVial())
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