using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Agromancy.Helpers;
using Agromancy.Models;
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
    private NetInt suckTimer = new(0);

    public AgromanticAltar(Vector2 tile, bool lockOnSuccess, Color successColor, string itemId) : base(tile, [$"(O){Agromancy.UNIQUE_ID}_T1EssenceVial", $"(O){Agromancy.UNIQUE_ID}_T2EssenceVial"], lockOnSuccess, successColor, itemId)
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
        if (Game1.IsMasterGame)
        {
            puffTimer.Value += time.ElapsedGameTime.Milliseconds;
            if (isInRitual.Value) suckTimer.Value += time.ElapsedGameTime.Milliseconds;
            else suckTimer.Value = 0;
        }

        if (puffTimer.Value >= 250f || (isInRitual.Value && puffTimer.Value >= 100))
        {
            puffTimer.Value = 0;
            if (heldObject.Value != null && isReadyForRitual.Value)
            {
                foreach (var ped in pedestals)
                {
                    if (ped.heldObject.Value == null || !ped.match.Value || ped.hideObject.Value) continue;
                    Vector2 pedSpot = ped.TileLocation - new Vector2(-0.25f, 1.25f);
                    Vector2 directionToPedestal = TileLocation - new Vector2(-0.5f, 1.25f) - pedSpot;
                    Vector2 puffVelocity = directionToPedestal * 0.75f;
                    Game1.Multiplayer.broadcastSprites(Location, new TemporaryAnimatedSprite("LooseSprites\\Cursors",
                        new Rectangle(372, 1956, 10, 10), pedSpot * 64f, flipped: false, 0.003f,
                        Utility.GetPrismaticColor())
                    {
                        alpha = 0.75f,
                        motion = puffVelocity,
                        acceleration = new Vector2(0f, 0f),
                        interval = 99999f,
                        layerDepth = Math.Max(0f, (pedSpot.Y - 2f) / 100f), //0.144f - (float)Game1.random.Next(100) / 10000f,
                        scale = 3f,
                        scaleChange = -0.03f,
                        rotationChange = Game1.random.Next(-5, 6) * (float)Math.PI / 256f
                    });
                }
            }
        }

        var suckedPed = currentlySuckedPedestal();
        if (suckedPed is not null)
        {
            suckedPed.isJittering.Value = true;
            if (suckTimer.Value > 1500 && Game1.IsMasterGame)
            {
                suckTimer.Value = 0;
                suckedPed.hideObject.Value = true;
                Location?.playSound("throw", TileLocation);
                Location?.playSound("breakingGlass", TileLocation);
                TemporaryAnimatedSprite[] spritesToBroadcast = new TemporaryAnimatedSprite[3];
                for (int i = 0; i < 3; i++)
                {
                    spritesToBroadcast[i] = new TemporaryAnimatedSprite("LooseSprites\\Cursors",
                        new Rectangle(372, 1956, 10, 10), suckedPed.TileLocation * 64f - new Vector2(-8, 92f),
                        flipped: false, 0.003f,
                        Color.WhiteSmoke)
                    {
                        alpha = 0.75f,
                        motion = new Vector2(Game1.random.Next(-2, 2), Game1.random.Next(-2, 2)),
                        acceleration = new Vector2(0f, 0f),
                        interval = 99999f,
                        layerDepth =
                            Math.Max(0f,
                                (suckedPed.TileLocation.Y - 3f) /
                                100f), //0.144f - (float)Game1.random.Next(100) / 10000f,
                        scale = 4f,
                        scaleChange = -0.03f,
                        rotationChange = Game1.random.Next(-5, 6) * (float)Math.PI / 256f
                    };
                }
                Game1.Multiplayer.broadcastSprites(Location, spritesToBroadcast);
            }
        } else if (isInRitual.Value && suckTimer.Value > 1250 * Location?.farmers.Count)
        {
            suckTimer.Value = 0;
            stopRitual();
            Game1.playSound("yoba");
        }

        checkForRitualStart();
    }

    public AgromanticPedestal? currentlySuckedPedestal()
    {
        return getSurroundingPedestals().FirstOrDefault(ped => !ped.hideObject.Value && ped.isInRitual.Value);
    }

    public void setPedestalsForRitual()
    {
        int currentVialTier = ObjectPatches.GetEssenceVialTier(heldObject.Value);
        int requiredQuality = currentVialTier switch
        {
            1 => 1,
            2 => 2,
            _ => 1
        };
        
        List<string> cropList = getCropsFromSeason((Season)currentVialTier);
        foreach (var ped in getSurroundingPedestals())
        {
            ped.setRequiredItems(cropList.OrderBy(_ => Guid.NewGuid()).ToList());
            ped.setMinimumQualityRequired(requiredQuality);
            ped.minimumQuality.Value = requiredQuality;
            ped.isReadyForRitual.Value = true;
            ped.locked.Value = false;
            ped.isInRitual.Value = false;
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
            layerDepth: Math.Max(0f, (position.Y - 2f) / 10000f)
        );
        
        // if (__instance.match.Value)
        // {
        //     b.Draw(itemData.Texture, Game1.GlobalToLocal(Game1.viewport, position), itemData.GetSourceRect(1), __instance.successColor.Value, 0f, new Vector2(0f, 16f), 4f, SpriteEffects.None, Math.Max(0f, (position.Y - 1f) / 10000f));
        // }
        
        Agromancy.EssenceVialFx.Parameters["PerlinNoise"].SetValue(Agromancy.PerlinNoise);
        Agromancy.EssenceVialFx.Parameters["Waviness"].SetValue(0f);
        Agromancy.EssenceVialFx.Parameters["FillPercentage"].SetValue(0f);
        Agromancy.EssenceVialFx.Parameters["BottomOfVial"].SetValue(1f - 0.125f);
        Agromancy.EssenceVialFx.Parameters["TopOfVial"].SetValue(0.5f);
        Agromancy.EssenceVialFx.Parameters["Time"].SetValue((float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500f);
        Agromancy.EssenceVialFx.Parameters["PrismaticColour"].SetValue(new Vector4(Utility.GetPrismaticColor().R / 255f, Utility.GetPrismaticColor().G / 255f, Utility.GetPrismaticColor().B / 255f, 1f));
        Agromancy.EssenceVialFx.Parameters["GlassShineColour"].SetValue(new Vector4(219, 211, 206, 255) / 255f);
        Agromancy.EssenceVialFx.Parameters["Flipped"].SetValue(false);

        RenderTarget2D? oldTarget;
        {
            RenderTargetBinding[] wasRenderTargets = Game1.graphics.GraphicsDevice.GetRenderTargets();
            oldTarget = wasRenderTargets.Length > 0 ? wasRenderTargets[0].RenderTarget as RenderTarget2D : null;
        }
        // capture output
        RenderTarget2D renderTarget = new(
            Game1.graphics.GraphicsDevice,
            48,
            48,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents
        );
        Game1.SetRenderTarget(renderTarget);
        SpriteBatch batch = Agromancy.VialSpriteBatch ??= new SpriteBatch(Game1.graphics.GraphicsDevice);

        Texture2D vialTexture;
        Rectangle vialSourceRect;
        
        if (heldObject.Value == null)
        {
            var requiredItemData = ItemRegistry.GetDataOrErrorItem(requiredItems[currentlyShownItemIndex]);
            requiredItemData.LoadTextureIfNeeded();
            vialTexture = requiredItemData.Texture;
            vialSourceRect = requiredItemData.GetSourceRect();
        } else 
        {
            var heldObjectData = ItemRegistry.GetDataOrErrorItem(heldObject.Value.QualifiedItemId);
            heldObjectData.LoadTextureIfNeeded();
            vialTexture = heldObjectData.Texture;
            vialSourceRect = heldObjectData.GetSourceRect();
                
            for (int i = 0; i < 7; i++)
            {
                heldObject.Value.modData.TryAdd($"{Agromancy.UNIQUE_ID}_{i}", "0");
            }
            
            float yield = float.Parse(heldObject.Value.modData[$"{Agromancy.UNIQUE_ID}_0"]) / 255f;
            float qualityEssence = float.Parse(heldObject.Value.modData[$"{Agromancy.UNIQUE_ID}_1"]) / 255f;
            float growth = float.Parse(heldObject.Value.modData[$"{Agromancy.UNIQUE_ID}_2"]) / 255f;
            float giant = float.Parse(heldObject.Value.modData[$"{Agromancy.UNIQUE_ID}_3"]) / 255f;
            float water = float.Parse(heldObject.Value.modData[$"{Agromancy.UNIQUE_ID}_4"]) / 255f;
            float seed = float.Parse(heldObject.Value.modData[$"{Agromancy.UNIQUE_ID}_5"]) / 255f;
            float total = yield + qualityEssence + growth + giant + water + seed;

            float fillPercentage = Math.Clamp(total / (10f * ObjectPatches.GetEssenceVialTier(heldObject.Value) * 6f), 0f, 1f);
            Agromancy.EssenceVialFx.Parameters["Waviness"].SetValue(fillPercentage > 0f ? 0.5f : 0f);
            Agromancy.EssenceVialFx.Parameters["FillPercentage"].SetValue(fillPercentage > 0f ? fillPercentage + 0.5f : 0f);
        }
        
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, effect: Agromancy.EssenceVialFx);
        Game1.graphics.GraphicsDevice.Clear(Color.Transparent);
            
        batch.Draw(
            texture: vialTexture,
            position: Vector2.Zero,
            sourceRectangle: vialSourceRect,
            color: Color.White,
            rotation: 0f,
            origin: Vector2.Zero,
            scale: 3f,
            effects: SpriteEffects.None,
            layerDepth: 0f
        );
        
        batch.End();
        Game1.SetRenderTarget(oldTarget);
        
        if (heldObject.Value != null)
        {
            var heldObjectData = ItemRegistry.GetDataOrErrorItem(heldObject.Value.QualifiedItemId);
            heldObjectData.LoadTextureIfNeeded();
            Vector2 draw_position = position - new Vector2(-8, 92f);
            float yOffset = MathUtility.MultiLerp([0f, -8f, 0f], (float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2000 / 2000);
            draw_position.Y += yOffset;
            if (isInRitual.Value)
            {
                draw_position += new Vector2((float)(Game1.random.NextDouble() - 0.5) * 4f,
                    (float)(Game1.random.NextDouble() - 0.5) * 4f);
            }

            b.Draw(
                texture: renderTarget,
                position: Game1.GlobalToLocal(Game1.viewport, draw_position),
                sourceRectangle: renderTarget.Bounds,
                color: Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 1f,
                effects: SpriteEffects.None,
                layerDepth: Math.Max(0f, (position.Y - 1f) / 10000f)
            );
        }
        else if (requiredItems.Count > 0 && !locked.Value)
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
                texture: renderTarget,
                position: Game1.GlobalToLocal(Game1.viewport, draw_position),
                sourceRectangle: renderTarget.Bounds,
                color: Color.White * 0.3f,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 1f,
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

    private bool areAllPedestalsMatched()
    {
        return getSurroundingPedestals().All(p => p.match.Value);
    }

    public void checkForRitualStart()
    {
        if (match.Value && doWeHaveEnoughPedestals() && !areWePreppedForRitual())
        {
            isReadyForRitual.Value = true;
            setPedestalsForRitual();
        }
        else if (match.Value && doWeHaveEnoughPedestals() && areAllPedestalsMatched() && !isInRitual.Value)
        {
            startRitual();
        }
        else if ((isReadyForRitual.Value || isInRitual.Value) && (!doWeHaveEnoughPedestals() || heldObject.Value == null || !heldObject.Value.IsEssenceVial()))
        {
            isReadyForRitual.Value = false;
            isInRitual.Value = false;
            foreach (var ped in getSurroundingPedestals())
            {
                ped.isJittering.Value = false;
                ped.isInRitual.Value = false;
                ped.isReadyForRitual.Value = false;
                ped.locked.Value = false;
                ped.hideObject.Value = false;
                ped.setRequiredItems([]);
            }
        }
    }

    public void startRitual()
    {
        isInRitual.Value = true;
        isReadyForRitual.Value = true;
        isJittering.Value = true;
        var peds = getSurroundingPedestals();
        for (var i = 0; i < peds.Count; i++)
        {
            var ped = peds[i];
            if (i == 0) ped.isJittering.Value = true;
            ped.isInRitual.Value = true;
        }

        Location?.playSound("warrior", TileLocation);
    }

    public void stopRitual()
    {
        isInRitual.Value = false;
        isReadyForRitual.Value = false;
        isJittering.Value = false;
        if (Game1.IsMasterGame)
        {
            lightningStrike();
            Agromancy.ModHelper.Multiplayer.SendMessage(new RitualFinishInfo(Location?.NameOrUniqueName, TileLocation), "RitualFinish");
        }
        foreach (var ped in getSurroundingPedestals())
        {
            ped.isInRitual.Value = false;
            ped.isReadyForRitual.Value = false;
            ped.hideObject.Value = false;
            ped.isJittering.Value = false;
            ped.setRequiredItems([]);
            ped.heldObject.Value = null;
            ped.UpdateItemMatch();
        }

        var currentVialTier = ObjectPatches.GetEssenceVialTier(heldObject.Value);
        if (currentVialTier < 1) return;
        
        var newVial = ItemRegistry.Create<StardewValley.Object>($"(O){Agromancy.UNIQUE_ID}_T{currentVialTier + 1}EssenceVial");
        if (newVial is not null)
        {
            newVial.modData.Clear();
            foreach (string key in heldObject.Value.modData.Keys)
            {
                newVial.modData[key] = heldObject.Value.modData[key];
            }
            heldObject.Value = newVial;
        }
        UpdateItemMatch();
    }

    public void lightningStrike()
    {
        Game1.flashAlpha = 0.5f;
        Utility.drawLightningBolt((TileLocation + new Vector2(0.5f, -1.35f)) * 64f, Location);
        Game1.playSound("thunder");
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
        NetFields.AddField(suckTimer);
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