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

[HarmonyPatch(typeof(Crop))]
public class CropPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Crop.TryGrowGiantCrop))]
    public static void TryGrowGiantCrop_Postfix(Crop __instance, bool checkPreconditions, ref bool __result)
    {
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
            float totalEssence = 0f;
            int cropsChecked = 0;
            for (int y = (int)tile.Y; y < tile.Y + giantCrop.TileSize.Y; y++)
            {
                for (int x = (int)tile.X; x < tile.X + giantCrop.TileSize.X; x++)
                {
                    if (loc.terrainFeatures.GetValueOrDefault(new Vector2(x, y)) is HoeDirt dirt &&
                        dirt.crop?.indexOfHarvest.Value == __instance.indexOfHarvest.Value)
                    {
                        CropEssences essences = CropManager.GrabEssences(dirt.crop) ?? EssenceCalculator.DefaultEssences(CropManager.GetCropReferenceBySeedId(dirt.crop.indexOfHarvest.Value)) ?? EssenceCalculator.EmptyEssences;
                        totalEssence += EssenceCalculator.GetEssencePercent(essences, EssenceCalculator.GIANT_INDEX);
                        cropsChecked++;
                        continue;
                    }
                    
                    valid = false;
                    break;
                }
                
                if (!valid) break;
            }
            
            if (!valid) continue;

            float averageEssencePct = totalEssence / cropsChecked;
            Log.Debug($"Checking giant crop growth for crop {__instance.indexOfHarvest.Value} at tile {tile}. Average essence percent: {averageEssencePct}");
            if (rng.NextDouble() >= averageEssencePct) continue;
            
            for (int i = (int)tile.Y; i < tile.Y + giantCrop.TileSize.Y; i++)
            {
                for (int j = (int)tile.X; j < tile.X + giantCrop.TileSize.X; j++)
                {
                    Vector2 v = new Vector2(j, i);
                    ((HoeDirt)loc.terrainFeatures[v]).crop = null;
                }
            }
            loc.resourceClumps.Add(new GiantCrop(giantCropId, tile));
            __result = true;
        }
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Crop.harvest))]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        var code = instructions.ToList();
        try
        {
            var matcher = new CodeMatcher(code, il);

            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Farmer), nameof(Farmer.addItemToInventoryBool)))
            ).ThrowIfNotMatch($"Could not find proper entry point #1 for {nameof(Transpiler)}");

            matcher.Advance(-1);

            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CropManager), nameof(CropManager.ModifyHarvestedCrop)))
            );

            matcher.End();
            
            matcher.MatchStartBackwards(
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Farmer), nameof(Farmer.addItemToInventoryBool)))
            ).ThrowIfNotMatch($"Could not find proper entry point #2 for {nameof(Transpiler)}");

            matcher.Advance(-1);

            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CropManager), nameof(CropManager.ModifyHarvestedCrop)))
            );

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error($"Error in {nameof(Transpiler)}: \n{ex}");
            return code;
        }
    }
}