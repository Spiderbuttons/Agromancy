using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Agromancy.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;

namespace Agromancy.Pedestals;

[XmlType("Mods_Spiderbuttons.Agromancy_AgromanticPedestal")]
public class AgromanticPedestal : ItemPedestal
{
    protected string[] requiredItems;
    protected int currentlyShownItemIndex;
    protected int lastItemUpdate;

    private int vialTier = -1;

    public NetBool isInRitual = new(false);
    public NetBool isReadyForRitual = new(false);

    public AgromanticPedestal(Vector2 tile,
        StardewValley.Object? requiredItem,
        bool lockOnSuccess,
        Color successColor,
        string itemId) : base(tile, null, lockOnSuccess, successColor, itemId)
    {
        requiredItems = requiredItem is null ? [] : [requiredItem.QualifiedItemId];
    }

    public AgromanticPedestal(Vector2 tile, IEnumerable<string>? requiredItemIds, bool lockOnSuccess, Color successColor,
        string itemId) : base(tile, null, lockOnSuccess, successColor, itemId)
    {
       requiredItems = requiredItemIds is null ? [] : requiredItemIds.ToArray();
       Random rng = new Random(Game1.hash.GetDeterministicHashCode(tile.ToString()));
       currentlyShownItemIndex = rng.Next(0, requiredItems.Length);
    }

    public AgromanticPedestal() : base()
    {
        requiredItems = [];
    }
    
    public override void UpdateItemMatch()
    {
        bool success = false;
        if (heldObject.Value != null && requiredItems.Length > 0 && requiredItems.Contains(heldObject.Value.QualifiedItemId))
        {
            success = true;
        }
        
        match.Value = success;
        if (match.Value && lockOnSuccess.Value)
        {
            locked.Value = true;
        }
    }

    public override void updateWhenCurrentLocation(GameTime time)
    {
        if (isInRitual.Value) locked.Value = true;
        base.updateWhenCurrentLocation(time);
        if (requiredItems.Length > 0 && time.TotalGameTime.Seconds != lastItemUpdate)
        {
            currentlyShownItemIndex++;
            if (currentlyShownItemIndex >= requiredItems.Length) currentlyShownItemIndex = 0;
            lastItemUpdate = time.TotalGameTime.Seconds;
        }
    }

    public override void performRemoveAction()
    {
        if (heldObject.Value != null)
        {
            Game1.createItemDebris(
                item: heldObject.Value,
                pixelOrigin: TileLocation * 64f,
                direction: 0,
                Location
            );
        }

        if (this is AgromanticAltar altar)
        {
            foreach (var ped in altar.getSurroundingPedestals())
            {
                ped.setRequiredItems([], -1);
            }
        }

        base.performRemoveAction();
    }

    public override bool onExplosion(Farmer who)
    {
        if (isInRitual.Value || requiredItems.Length > 0) return false;
        return base.onExplosion(who);
    }

    public void setRequiredItems(IEnumerable<string> newRequiredItems, int tier)
    {
        requiredItems = newRequiredItems.ToArray();
        vialTier = tier;
    }

    public static Item chooseRandomCrop(Season? season = null)
    {
        var crops = getCropsFromSeason(season);
        string selectedId = crops[Game1.random.Next(crops.Count)];
        return ItemRegistry.Create(ItemRegistry.QualifyItemId(selectedId));
    }

    public static List<string> getCropsFromSeason(Season? season = null)
    {
        List<string> possibleCrops = [];
        
        possibleCrops.AddRange(
            from cropData in Game1.cropData.Values
            where season is null || cropData.Seasons.Contains(season.Value)
            select cropData.HarvestItemId
        );
        
        return possibleCrops;
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
            var heldObjectData = ItemRegistry.GetDataOrErrorItem(heldObject.Value.QualifiedItemId);
            heldObjectData.LoadTextureIfNeeded();
            Vector2 draw_position = position - new Vector2(-8, 92f);
            float yOffset = MathUtility.MultiLerp([0f, -8f, 0f], (float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2000 / 2000);
            draw_position.Y += yOffset;
            b.Draw(
                texture: heldObjectData.Texture,
                position: Game1.GlobalToLocal(Game1.viewport, draw_position),
                sourceRectangle: heldObjectData.GetSourceRect(),
                color: Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 3f,
                effects: SpriteEffects.None,
                layerDepth: Math.Max(0f, (position.Y - 1f) / 10000f)
            );
        }
        else if (requiredItems.Length > 0)
        {
            var requiredItemData = ItemRegistry.GetDataOrErrorItem(requiredItems[currentlyShownItemIndex]);
            requiredItemData.LoadTextureIfNeeded();
            if (requiredItemData.Texture is null) // This used to happen somehow before I added the LoadTextureIfNeeded() call, so I'm keeping it just in case.
            {
                return;
            }
            Vector2 draw_position = position - new Vector2(-8f, 92f);
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
    
    public override bool placementAction(GameLocation location, int x, int y, Farmer who = null)
    {
        return base.placementAction(location, x, y, who);
    }
    
    public override Color getCategoryColor()
    {
        return Utility.GetPrismaticColor();
    }

    public override string getCategoryName()
    {
        return "Agromancy";
    }
    
    public override void initNetFields()
    {
        base.initNetFields();
        NetFields.AddField(isInRitual);
        NetFields.AddField(isReadyForRitual);
    }

    public override Item GetOneNew()
    {
        return new AgromanticPedestal(TileLocation, requiredItems, lockOnSuccess.Value, successColor.Value, ItemId);
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