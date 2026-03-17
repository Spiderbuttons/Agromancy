using System;
using System.Collections.Generic;
using System.Linq;
using Agromancy.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;

namespace Agromancy.Pedestals;

public class AgromanticPedestal : ItemPedestal
{
    private Item randomPossibleItem;
    private HashSet<string> requiredItems;
    private int lastItemUpdate;

    public AgromanticPedestal(Vector2 tile,
        StardewValley.Object? requiredItem,
        bool lockOnSuccess,
        Color successColor,
        string itemId) : base(tile, requiredItem, lockOnSuccess, successColor, itemId)
    {
        randomPossibleItem = chooseRandomCrop();
    }

    public AgromanticPedestal(Vector2 tile, IEnumerable<string> requiredItemIds, bool lockOnSuccess, Color successColor,
        string itemId) : base(tile, null, lockOnSuccess, successColor, itemId)
    {
       requiredItems = new HashSet<string>(requiredItemIds);
    }

    public override void updateWhenCurrentLocation(GameTime time)
    {
        base.updateWhenCurrentLocation(time);
        if (time.TotalGameTime.Seconds != lastItemUpdate)
        {
            string currentlySelectedItemId = randomPossibleItem.QualifiedItemId;
            int numAttempts = 0;
            while (randomPossibleItem.QualifiedItemId.Equals(currentlySelectedItemId) && numAttempts < 10)
            {
                randomPossibleItem = chooseRandomCrop(Season.Winter);
                numAttempts++;
            }
            lastItemUpdate = time.TotalGameTime.Seconds;
        }
    }

    public Item chooseRandomCrop(Season? season = null)
    {
        List<string> possibleCrops = [];
        
        possibleCrops.AddRange(
            from cropData in Game1.cropData.Values
            where season is null || cropData.Seasons.Contains(season.Value)
            select cropData.HarvestItemId
        );
        
        string selectedId = possibleCrops[Game1.random.Next(possibleCrops.Count)];
        return ItemRegistry.Create(ItemRegistry.QualifyItemId(selectedId));
    }

    public override void draw(SpriteBatch b, int x, int y, float alpha = 1f)
    {
        Vector2 position = new Vector2(x * 64, y * 64);
        ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(QualifiedItemId);
        b.Draw(itemData.Texture, Game1.GlobalToLocal(Game1.viewport, position), itemData.GetSourceRect(0, 0), Color.White, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, Math.Max(0f, (position.Y - 2f) / 10000f));
        
        // if (__instance.match.Value)
        // {
        //     b.Draw(itemData.Texture, Game1.GlobalToLocal(Game1.viewport, position), itemData.GetSourceRect(1), __instance.successColor.Value, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, Math.Max(0f, (position.Y - 1f) / 10000f));
        // }
        
        if (heldObject.Value != null)
        {
            Vector2 draw_position = new Vector2(x, y);
            if (heldObject.Value.bigCraftable.Value)
            {
                draw_position.Y -= 1f;
            }
            heldObject.Value.draw(b, (int)draw_position.X * 64, (int)((draw_position.Y - 0.2f) * 64f) - 64, position.Y / 10000f, 1f);
        } else if (requiredItem.Value != null)
        {
            var requiredItemData = ItemRegistry.GetDataOrErrorItem(randomPossibleItem.QualifiedItemId);
            requiredItemData.LoadTextureIfNeeded();
            if (requiredItemData.Texture is null) // This used to happen somehow before I added the LoadTextureIfNeeded() call, so I'm keeping it just in case.
            {
                return;
            }
            Vector2 draw_position = position - new Vector2(-8f, 90f);
            float yOffset = MathUtility.MultiLerp([0f, -8f, 0f], (float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2000 / 2000);
            draw_position.Y += yOffset;
            b.Draw(
                texture: requiredItemData.Texture,
                position: Game1.GlobalToLocal(Game1.viewport, draw_position),
                sourceRectangle: requiredItemData.GetSourceRect(),
                color: Color.White * 0.35f,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 3f,
                effects: SpriteEffects.None,
                layerDepth: Math.Max(0f, (position.Y - 1f) / 10000f)
            );
        }
    }

    public override Item GetOneNew()
    {
        return new AgromanticPedestal(TileLocation, (StardewValley.Object?)requiredItem.Value?.getOne(), lockOnSuccess.Value, successColor.Value, ItemId);
    }
    
    public override void GetOneCopyFrom(Item source)
    {
        base.GetOneCopyFrom(source);
        if (source is ItemPedestal fromPedestal)
        {
            isIslandShrinePedestal.Value = fromPedestal.isIslandShrinePedestal.Value;
        }
    }
}