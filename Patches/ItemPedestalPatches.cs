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
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemPedestal), nameof(ItemPedestal.draw))]
    public static bool draw_Prefix(ItemPedestal __instance, SpriteBatch b, int x, int y, float alpha)
    {
        return true;
        
        if (!__instance.QualifiedItemId.Equals($"(BC){Agromancy.UNIQUE_ID}_Pedestal")) return true;
        
        Vector2 position = new Vector2(x * 64, y * 64);
        ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId);
        b.Draw(itemData.Texture, Game1.GlobalToLocal(Game1.viewport, position), itemData.GetSourceRect(), Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, Math.Max(0f, (position.Y - 2f) / 10000f));
        // if (__instance.match.Value)
        // {
        //     b.Draw(itemData.Texture, Game1.GlobalToLocal(Game1.viewport, position), itemData.GetSourceRect(1), __instance.successColor.Value, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, Math.Max(0f, (position.Y - 1f) / 10000f));
        // }
        if (__instance.heldObject.Value != null)
        {
            Vector2 draw_position = new Vector2(x, y);
            if (__instance.heldObject.Value.bigCraftable.Value)
            {
                draw_position.Y -= 1f;
            }
            __instance.heldObject.Value.draw(b, (int)draw_position.X * 64, (int)((draw_position.Y - 0.2f) * 64f) - 64, position.Y / 10000f, 1f);
        }
    
        return false;
    }
}