using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Agromancy.Helpers;
using Agromancy.Patches;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;

namespace Agromancy.Pedestals;

[XmlType("Mods_Spiderbuttons.Agromancy_AgromanticAltar")]
public class AgromanticAltar : AgromanticPedestal
{
    
    private NetBool shouldShowPedestals = new(true);
    private NetInt puffTimer = new(0);

    public AgromanticAltar(Vector2 tile, bool lockOnSuccess, Color successColor, string itemId) : base(tile, [$"(O){Agromancy.UNIQUE_ID}_T1EssenceVial"], lockOnSuccess, successColor, itemId)
    {
        
    }

    public AgromanticAltar() : base()
    {
        
    }

    public override bool checkForAction(Farmer who, bool checking_for_activity = false)
    {
        return base.checkForAction(who, checking_for_activity);
    }

    public override void updateWhenCurrentLocation(GameTime time)
    {
        base.updateWhenCurrentLocation(time);
        var pedestals = getSurroundingPedestals();
        if (pedestals.Count < 4 && heldObject.Value == null) locked.Value = true;
        else locked.Value = false;
        puffTimer.Value += time.ElapsedGameTime.Milliseconds;
        if (puffTimer.Value >= 500f)
        {
            puffTimer.Value = 0;
            if (heldObject.Value != null && isInRitual.Value)
            {
                foreach (var ped in pedestals)
                {
                    if (ped.heldObject.Value == null || !ped.match.Value) continue;
                    Vector2 pedSpot = ped.TileLocation - new Vector2(-0.25f, 1.25f);
                    Vector2 directionToPedestal = TileLocation - new Vector2(-0.5f, 1.25f) - pedSpot;
                    Vector2 puffVelocity = directionToPedestal * 0.25f;
                    Location.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors",
                        new Rectangle(372, 1956, 10, 10), pedSpot * 64f, flipped: false, 0.003f,
                        Utility.GetPrismaticColor())
                    {
                        alpha = 0.75f,
                        motion = puffVelocity,
                        acceleration = new Vector2(0f, 0f),
                        interval = 99999f,
                        layerDepth = Math.Max(0f, (pedSpot.Y - 2f) / 100f), //0.144f - (float)Game1.random.Next(100) / 10000f,
                        scale = 3f,
                        scaleChange = -0.01f,
                        rotationChange = Game1.random.Next(-5, 6) * (float)Math.PI / 256f
                    });
                }
            }
        }

        checkForRitualStart();
    }

    public void setPedestalsForRitual()
    {
        List<string> cropList = getCropsFromSeason(Season.Spring);
        foreach (var ped in getSurroundingPedestals())
        {
            ped.setRequiredItems(cropList.OrderBy(_ => Guid.NewGuid()).ToList(), ObjectPatches.GetEssenceVialTier(heldObject.Value));
            ped.isReadyForRitual.Value = true;
        }
    }

    public override void draw(SpriteBatch b, int x, int y, float alpha = 1f)
    {
        Vector2 position = new Vector2(x * 64, y * 64);
        ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(QualifiedItemId);
        itemData.LoadTextureIfNeeded();
        b.Draw(
            texture: itemData.Texture,
            position: Game1.GlobalToLocal(Game1.viewport, position),
            sourceRectangle: itemData.GetSourceRect(),
            color: Color.White,
            rotation: 0f,
            origin: new Vector2(0f, 16f),
            scale: 4f,
            effects: SpriteEffects.None,
            layerDepth: Math.Max(0f, (position.Y - 2f) / 10000f));
        
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
        else if (requiredItems.Length > 0 && !locked.Value)
        {
            var requiredItemData = ItemRegistry.GetDataOrErrorItem(requiredItems[currentlyShownItemIndex]);
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
                color: Color.White * 0.3f,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 3f,
                effects: SpriteEffects.None,
                layerDepth: Math.Max(0f, (position.Y - 1f) / 10000f)
            );
        }

        if (Location is null || !shouldShowPedestals.Value) return;
        
        ParsedItemData pedData = ItemRegistry.GetDataOrErrorItem($"(BC){Agromancy.UNIQUE_ID}_Pedestal");
        pedData.LoadTextureIfNeeded();
        foreach (var spot in getSurroundingPedestalSpots())
        {
            bool isPlaceable = Location.isTilePlaceable(spot);
            bool isTileOccupied = Location.IsTileOccupiedBy(spot);
            StardewValley.Object? objAtTile = Location.getObjectAtTile((int)spot.X, (int)spot.Y);
            bool isOccupyingTileAPedestal = isTileOccupied && objAtTile is not null && objAtTile.QualifiedItemId.Equals($"(BC){Agromancy.UNIQUE_ID}_Pedestal");
            if (isOccupyingTileAPedestal) continue;
            Vector2 drawLocation = spot * 64f;
            b.Draw(
                texture: pedData.Texture,
                position: Game1.GlobalToLocal(Game1.viewport, drawLocation),
                sourceRectangle: pedData.GetSourceRect(),
                color: (isPlaceable && !isTileOccupied ? Color.White : Color.Red) * 0.35f,
                rotation: 0f,
                origin: new Vector2(0f, 16f),
                scale: 4f,
                effects: SpriteEffects.None,
                layerDepth: Math.Max(0f, (position.Y - 3f) / 1000f));
        }
    }

    public List<AgromanticPedestal> getSurroundingPedestals()
    {
        List<AgromanticPedestal> pedestals = [];
        foreach (var spot in getSurroundingPedestalSpots())
        {
            var obj = Location.getObjectAtTile((int)spot.X, (int)spot.Y);
            if (obj?.QualifiedItemId.Equals($"(BC){Agromancy.UNIQUE_ID}_Pedestal") == true)
            {
                pedestals.Add((obj as AgromanticPedestal)!);
            }
        }

        return pedestals;
    }

    public List<Vector2> getSurroundingPedestalSpots()
    {
        return
        [
            new Vector2(TileLocation.X - 2, TileLocation.Y - 2),
            new Vector2(TileLocation.X + 2, TileLocation.Y - 2),
            new Vector2(TileLocation.X - 2, TileLocation.Y + 2),
            new Vector2(TileLocation.X + 2, TileLocation.Y + 2)
        ];
    }

    private bool doWeHaveEnoughPedestals()
    {
        return getSurroundingPedestals().Count >= 4;
    }

    public void checkForRitualStart()
    {
        if (match.Value && doWeHaveEnoughPedestals() && !areWePreppedForRitual())
        {
            isReadyForRitual.Value = true;
            setPedestalsForRitual();
        }
        else if (!match.Value || !doWeHaveEnoughPedestals())
        {
            isReadyForRitual.Value = false;
            foreach (var ped in getSurroundingPedestals())
            {
                ped.isInRitual.Value = false;
                ped.isReadyForRitual.Value = false;
                ped.setRequiredItems([], -1);
            }
        }
    }

    public override bool performObjectDropInAction(Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed = false)
    {
        if (getSurroundingPedestals().Count < 4) return false;
        return base.performObjectDropInAction(dropInItem, probe, who, returnFalseIfItemConsumed);
    }
    
    public override void initNetFields()
    {
        base.initNetFields();
        NetFields.AddField(shouldShowPedestals);
        NetFields.AddField(puffTimer);
    }

    public override bool clicked(Farmer who)
    {
        shouldShowPedestals.Value = !shouldShowPedestals.Value;
        return base.clicked(who);
    }

    public override Item GetOneNew()
    {
        return new AgromanticAltar(TileLocation, lockOnSuccess.Value, successColor.Value, ItemId);
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