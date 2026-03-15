using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Agromancy.Helpers;
using HarmonyLib;

namespace Agromancy.Patches;

[HarmonyPatch]
public class SpaceCorePatches
{
    [HarmonyPrepare]
    public static bool Prepare()
    {
        return Agromancy.ModHelper.ModRegistry.IsLoaded("spacechase0.SpaceCore");
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch("SpaceCore.VanillaAssetExpansion.CropHarvestOverridePatch", "Prefix")]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        var code = instructions.ToList();
        try
        {
            var matcher = new CodeMatcher(code, il);
            
            matcher.MatchStartForward(
                new CodeMatch(opc => opc.IsStloc() && opc.operand is LocalBuilder { LocalIndex: 10 })
            ).ThrowIfNotMatch($"Could not find proper entry point #1 for {nameof(Transpiler)} (SpaceCore)");
            
            var labels = matcher.Instruction.ExtractLabels();
            
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
                new CodeInstruction(OpCodes.Ldarg_S, 4),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CropManager), nameof(CropManager.ModifyHarvestedCrop)))
            );
            
            matcher.MatchStartForward(
                new CodeMatch(opc => opc.IsStloc() && opc.operand is LocalBuilder { LocalIndex: 23 })
            ).ThrowIfNotMatch($"Could not find proper entry point #2 for {nameof(Transpiler)} (SpaceCore)");
            
            var labels2 = matcher.Instruction.ExtractLabels();
            
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels2),
                new CodeInstruction(OpCodes.Ldarg_S, 4),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CropManager), nameof(CropManager.ModifyHarvestedCrop)))
            );
            
            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error($"Error in {nameof(Transpiler)} (SpaceCore): \n{ex}");
            return code;
        }
    }
}