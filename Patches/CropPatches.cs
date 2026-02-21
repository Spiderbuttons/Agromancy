using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Agromancy.Helpers;
using HarmonyLib;
using StardewValley;
using StardewValley.Characters;

namespace Agromancy.Patches;

[HarmonyPatch(typeof(Crop))]
public class CropPatches
{
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