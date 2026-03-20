using System;
using System.Drawing;
using Agromancy.Helpers;
using Agromancy.Pedestals;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using Color = Microsoft.Xna.Framework.Color;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Agromancy.Patches;

[HarmonyPatch]
public class ItemPedestalPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BigCraftableDataDefinition), nameof(BigCraftableDataDefinition.CreateItem))]
    public static bool CreateBC_Prefix(BigCraftableDataDefinition __instance, ParsedItemData? data, ref Item __result)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));

        if (!data.QualifiedItemId.Equals($"(BC){Agromancy.UNIQUE_ID}_Pedestal") && !data.QualifiedItemId.Equals($"(BC){Agromancy.UNIQUE_ID}_Altar")) return true;
        
        StardewValley.Object? requiredItem = null;
        if (data.RawData is BigCraftableData { CustomFields: not null } bData && bData.CustomFields.TryGetValue(Agromancy.UNIQUE_ID, out string? itemId))
        {
            requiredItem = ItemRegistry.Create<StardewValley.Object>(ItemRegistry.QualifyItemId(itemId));
        }
        
        if (data.QualifiedItemId.Equals($"(BC){Agromancy.UNIQUE_ID}_Pedestal")) __result = new AgromanticPedestal(Vector2.Zero, requiredItem, lockOnSuccess: false, successColor: Color.White, itemId: data.ItemId);
        else if (data.QualifiedItemId.Equals($"(BC){Agromancy.UNIQUE_ID}_Altar")) __result = new AgromanticAltar(Vector2.Zero, lockOnSuccess: false, successColor: Color.White, itemId: data.ItemId);
        return false;

    }
}