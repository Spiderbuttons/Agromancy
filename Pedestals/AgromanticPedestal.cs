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
using StardewValley.TokenizableStrings;

namespace Agromancy.Pedestals;

[XmlType("Mods_Spiderbuttons.Agromancy_AgromanticPedestal")]
public class AgromanticPedestal : ItemPedestal
{
    public NetStringList requiredItems = [];
    public NetInt minimumQuality = new(0);
    protected int currentlyShownItemIndex;
    protected int lastItemUpdate;

    public NetBool isInRitual = new(false);
    public NetBool isReadyForRitual = new(false);
    public NetBool isJittering = new(false);
    public NetBool hideObject = new(false);

    public static Rectangle silverStarSourceRect = new Rectangle(338, 400, 8, 8);
    public static Rectangle goldStarSourceRect = new Rectangle(346, 400, 8, 8);

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
       if (requiredItemIds is not null)
       {
           requiredItems.AddRange(requiredItemIds);
       }
       Random rng = new Random(Game1.hash.GetDeterministicHashCode(tile.ToString()));
       currentlyShownItemIndex = rng.Next(0, requiredItems.Count);
    }

    public AgromanticPedestal() : base()
    {
    }
    
    public override void UpdateItemMatch()
    {
        bool success = false;
        if (heldObject.Value != null && heldObject.Value.Quality >= minimumQuality.Value && requiredItems.Count > 0 && requiredItems.Contains(heldObject.Value.QualifiedItemId))
        {
            success = true;
        }
        
        match.Value = success;
        if (match.Value && lockOnSuccess.Value)
        {
            locked.Value = true;
        }
    }

    public override bool performToolAction(Tool t)
    {
        if (isInRitual.Value || heldObject.Value != null)
        {
            return false;
        }
        return base.performToolAction(t);
    }

    public override void updateWhenCurrentLocation(GameTime time)
    {
        if (isInRitual.Value) locked.Value = true;
        base.updateWhenCurrentLocation(time);
        if (requiredItems.Count > 0 && time.TotalGameTime.Seconds != lastItemUpdate)
        {
            currentlyShownItemIndex++;
            if (currentlyShownItemIndex >= requiredItems.Count) currentlyShownItemIndex = 0;
            lastItemUpdate = time.TotalGameTime.Seconds;
        } else if (requiredItems.Count <= 0) currentlyShownItemIndex = 0;
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
            heldObject.Value = null;
        }

        if (this is AgromanticAltar altar)
        {
            foreach (var ped in altar.getSurroundingPedestals())
            {
                ped.setRequiredItems([]);
                ped.locked.Value = false;
            }
        }
        
        base.performRemoveAction();
    }

    public override bool onExplosion(Farmer who)
    {
        if (isInRitual.Value || heldObject.Value != null) return false;
        return base.onExplosion(who);
    }

    public void setRequiredItems(IEnumerable<string> newRequiredItems)
    {
        requiredItems.Clear();
        requiredItems.AddRange(newRequiredItems);
    }

    public void setMinimumQualityRequired(int minQuality)
    {
        if (minQuality == 3) minQuality = 4;
        minimumQuality.Value = minQuality;
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
            select ItemRegistry.QualifyItemId(cropData.HarvestItemId)
        );
        
        return possibleCrops;
    }

    public override void draw(SpriteBatch b, int x, int y, float alpha = 1f)
    {
        Vector2 position = new Vector2(x * 64,
            y * 64);
        ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(QualifiedItemId);
        itemData.LoadTextureIfNeeded();
        b.Draw(
            texture: itemData.Texture,
            position: Game1.GlobalToLocal(Game1.viewport, position), sourceRectangle: itemData.GetSourceRect(),
            color: Color.White,
            rotation: 0f,
            origin: new Vector2(0f, 16f),
            scale: 4f,
            effects: SpriteEffects.None,
            layerDepth: Math.Max(0f, (position.Y - 2f) / 10000f)
        );
        
        // if (__instance.match.Value)
        // {
        //     b.Draw(itemData.Texture, Game1.GlobalToLocal(Game1.viewport, position), itemData.GetSourceRect(1), __instance.successColor.Value, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, Math.Max(0f, (position.Y - 1f) / 10000f));
        // }
        
        if (heldObject.Value != null)
        {
            if (!hideObject.Value)
            {
                var heldObjectData = ItemRegistry.GetDataOrErrorItem(heldObject.Value.QualifiedItemId);
                heldObjectData.LoadTextureIfNeeded();
                Vector2 draw_position = position - new Vector2(-8,
                    92f);
                float yOffset = MathUtility.MultiLerp([0f, -8f, 0f],
                    (float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2000 / 2000);
                draw_position.Y += yOffset;
                if (isJittering.Value)
                {
                    draw_position += new Vector2((float)(Game1.random.NextDouble() - 0.5) * 4f,
                        (float)(Game1.random.NextDouble() - 0.5) * 4f);
                }

                b.Draw(
                    texture: heldObjectData.Texture,
                    position: Game1.GlobalToLocal(Game1.viewport,
                        draw_position),
                    sourceRectangle: heldObjectData.GetSourceRect(),
                    color: Color.White,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale: 3f,
                    effects: SpriteEffects.None,
                    layerDepth: Math.Max(0f,
                        (position.Y - 1f) / 10000f)
                );
            }
        }
        else if (requiredItems.Count > 0)
        {
            var requiredItemData = ItemRegistry.GetDataOrErrorItem(requiredItems[currentlyShownItemIndex < requiredItems.Count ? currentlyShownItemIndex : 0]);
            requiredItemData.LoadTextureIfNeeded();
            if (requiredItemData.Texture is null) // This used to happen somehow before I added the LoadTextureIfNeeded() call, so I'm keeping it just in case.
            {
                return;
            }
            Vector2 draw_position = position - new Vector2(-8f,
                92f);
            float yOffset = MathUtility.MultiLerp([
                    0f,
                    -8f,
                    0f
                ],
                (float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2000 / 2000);
            draw_position.Y += yOffset;
            b.Draw(
                texture: requiredItemData.Texture,
                position: Game1.GlobalToLocal(Game1.viewport,
                    draw_position),
                sourceRectangle: requiredItemData.GetSourceRect(),
                color: Color.White * 0.35f,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 3f,
                effects: SpriteEffects.None,
                layerDepth: Math.Max(0f,
                    (position.Y - 1f) / 10000f)
            );

            Rectangle? qualitySourceRect = minimumQuality.Value switch
            {
                1 => silverStarSourceRect,
                2 => goldStarSourceRect,
                _ => null
            };
            if (qualitySourceRect is null) return;
            
            b.Draw(
                texture: Game1.mouseCursors,
                position: Game1.GlobalToLocal(Game1.viewport,
                    draw_position +
                    new Vector2(32,
                        32)),
                sourceRectangle: qualitySourceRect,
                color: Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 2f,
                effects: SpriteEffects.None,
                layerDepth: Math.Max(0f,
                    (position.Y) / 10000f)
            );
        }
    }
    
    public bool areWePreppedForRitual()
    {
        return isReadyForRitual.Value;
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
        return TokenParser.ParseText(Agromancy.TKString("Agromancy"));
    }

    public override bool performObjectDropInAction(Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed = false)
    {
        if (this is not AgromanticAltar && !areWePreppedForRitual() || isInRitual.Value) return false;
        return base.performObjectDropInAction(dropInItem, probe, who, returnFalseIfItemConsumed);
    }

    public override void initNetFields()
    {
        base.initNetFields();
        NetFields.AddField(requiredItems);
        NetFields.AddField(isInRitual);
        NetFields.AddField(isReadyForRitual);
        NetFields.AddField(isJittering);
        NetFields.AddField(hideObject);
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