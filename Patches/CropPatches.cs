using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Agromancy.Helpers;
using Agromancy.Models;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.GameData.GiantCrops;
using StardewValley.TerrainFeatures;

namespace Agromancy.Patches;

[HarmonyPatch]
public class CropPatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GiantCrop), nameof(GiantCrop.performToolAction))]
    public static IEnumerable<CodeInstruction> performToolAction_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        var code = instructions.ToList();
        try
        {
            var matcher = new CodeMatcher(code, il);

            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GiantCrop), nameof(GiantCrop.TryGetDrop)))
            ).ThrowIfNotMatch($"Could not find proper entry point #1 for {nameof(performToolAction_Transpiler)}");

            matcher.Advance(1);

            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CropManager), nameof(CropManager.ModifyGiantCropDrop)))
            );

            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GiantCrop), nameof(GiantCrop.TryGetDrop)))
            ).ThrowIfNotMatch($"Could not find proper entry point #2 for {nameof(performToolAction_Transpiler)}");

            matcher.Advance(1);

            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CropManager), nameof(CropManager.ModifyGiantCropDrop)))
            );

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error($"Error in {nameof(performToolAction_Transpiler)}: \n{ex}");
            return code;
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Crop), nameof(Crop.TryGrowGiantCrop))]
    public static void TryGrowGiantCrop_Prefix(Crop __instance, bool checkPreconditions, ref Tuple<CropEssences, string>? __state)
    {
        __state = null;
        
        GameLocation loc = __instance.currentLocation;
        Vector2 tile = __instance.tilePosition;
        if (checkPreconditions)
        {
            if (loc is not Farm && !loc.HasMapPropertyWithValue("AllowGiantCrops"))
            {
                return;
            }
            if (__instance.currentPhase.Value != __instance.phaseDays.Count - 1)
            {
                return;
            }
        }
        if (!__instance.TryGetGiantCrops(out var possibleGiantCrops))
        {
            return;
        }
        
        Random rng = Utility.CreateDaySaveRandom(tile.X, tile.Y, Game1.hash.GetDeterministicHashCode(__instance.indexOfHarvest.Value));
        foreach (var (giantCropId, giantCrop) in possibleGiantCrops)
        {
            if (!GameStateQuery.CheckConditions(giantCrop.Condition, loc)) continue;
            
            bool valid = true;
            int cropsChecked = 0;
            CropEssences averagedEssences = CropManager.GrabEssences(__instance) ?? EssenceCalculator.DefaultEssences(CropManager.GetCropReferenceBySeedId(__instance.indexOfHarvest.Value)) ?? EssenceCalculator.EmptyEssences;
            for (int y = (int)tile.Y; y < tile.Y + giantCrop.TileSize.Y; y++)
            {
                for (int x = (int)tile.X; x < tile.X + giantCrop.TileSize.X; x++)
                {
                    if (loc.terrainFeatures.GetValueOrDefault(new Vector2(x, y)) is HoeDirt dirt &&
                        dirt.crop?.indexOfHarvest.Value == __instance.indexOfHarvest.Value)
                    {
                        CropEssences essences = CropManager.GrabEssences(dirt.crop) ?? EssenceCalculator.DefaultEssences(CropManager.GetCropReferenceBySeedId(dirt.crop.indexOfHarvest.Value)) ?? EssenceCalculator.EmptyEssences;
                        cropsChecked++;
                        averagedEssences = EssenceCalculator.AverageEssences(new Tuple<CropEssences, int>(averagedEssences, cropsChecked), new Tuple<CropEssences, int>(essences, 1));
                        continue;
                    }
                    
                    valid = false;
                    break;
                }
                
                if (!valid) break;
            }
            
            if (!valid) continue;
            
            if (rng.NextDouble() >= EssenceCalculator.GetEssencePercent(averagedEssences, EssenceCalculator.GIANT_INDEX)) continue;
            
            __state = new Tuple<CropEssences, string>(averagedEssences, giantCropId);
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Crop), nameof(Crop.TryGrowGiantCrop))]
    public static void TryGrowGiantCrop_Postfix(Crop __instance, bool checkPreconditions, ref bool __result, Tuple<CropEssences, string>? __state)
    {
        if (__state is null) return; // If state was never assigned, it means that a giant crop didn't pass any growth checks this time.
        
        GameLocation loc = __instance.currentLocation;
        Vector2 tile = __instance.tilePosition;
        
        if (__result) // This means a giant crop must've already grown even before our own essence chance check, so we need to apply essences to the already-there GiantCrop
        {
            var giantCrop = loc.resourceClumps.FirstOrDefault(clump => clump is GiantCrop && clump.occupiesTile((int)tile.X, (int)tile.Y));
            giantCrop?.ApplyEssences(__state.Item1);
        }
        else // If the result was false, then the game didn't succeed in growing a giant crop, but since we have __state, that means WE succeeded. So now we make our own.
        {
            if (!__instance.TryGetGiantCrops(out var possibleGiantCrops))
            {
                return; // This shouldn't actually be possible, but y'know. I guess someone can null out the dictionary between the prefix and now if they hate people.
            }

            if (!possibleGiantCrops.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    .TryGetValue(__state.Item2, out var gcData))
            {
                return; // This should also not be possible but I've been bitten by WAY too many "impossible" NREs lmao
            }
            
            for (int i = (int)tile.Y; i < tile.Y + gcData.TileSize.Y; i++)
            {
                for (int j = (int)tile.X; j < tile.X + gcData.TileSize.X; j++)
                {
                    Vector2 v = new Vector2(j, i);
                    ((HoeDirt)loc.terrainFeatures[v]).crop = null;
                }
            }

            GiantCrop newGiantCrop = new GiantCrop(__state.Item2, tile);
            newGiantCrop.ApplyEssences(__state.Item1);
            loc.resourceClumps.Add(newGiantCrop);
            __result = true;
        }
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Crop), nameof(Crop.harvest))]
    public static IEnumerable<CodeInstruction> harvest_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        var code = instructions.ToList();
        try
        {
            var matcher = new CodeMatcher(code, il);

            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Farmer), nameof(Farmer.addItemToInventoryBool)))
            ).ThrowIfNotMatch($"Could not find proper entry point #1 for {nameof(harvest_Transpiler)}");

            matcher.Advance(-1);

            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CropManager), nameof(CropManager.ModifyHarvestedCrop)))
            );

            matcher.End();
            
            matcher.MatchStartBackwards(
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Farmer), nameof(Farmer.addItemToInventoryBool)))
            ).ThrowIfNotMatch($"Could not find proper entry point #2 for {nameof(harvest_Transpiler)}");

            matcher.Advance(-1);

            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CropManager), nameof(CropManager.ModifyHarvestedCrop)))
            );

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error($"Error in {nameof(harvest_Transpiler)}: \n{ex}");
            return code;
        }
    }
}