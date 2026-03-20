using System;
using System.Collections.Generic;
using System.Linq;
using Agromancy.Helpers;
using Agromancy.Models;
using Agromancy.Patches;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TokenizableStrings;

namespace Agromancy.Menus;

public partial class AgrometerMenu
{
    private bool shouldDrawVialTooltip = false;
    
    public override void draw(SpriteBatch b)
    {
        b.End();
        drawAgrometerBackground(b);

        b.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);

        b.Draw(
            texture: agrometerCircles,
            position: GetAgrometerCenter(),
            sourceRectangle: new Rectangle(0, 0, agrometerCircles.Width, agrometerCircles.Height),
            color: Color.White * 0.5f,
            rotation: currentMenuRotation / 360f * MathHelper.TwoPi,
            origin: new Vector2(agrometerCircles.Width / 2f, agrometerCircles.Height / 2f),
            scale: GetAgrometerRingScale(),
            effects: SpriteEffects.None,
            layerDepth: 0.86f
        );


        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default,
            RasterizerState.CullNone);

        b.Draw(
            texture: agrometerFrame,
            position: GetAgrometerCenter(),
            sourceRectangle: new Rectangle(0, 0, agrometerFrame.Width, agrometerFrame.Height),
            color: Color.White,
            rotation: currentMenuRotation / 360f * MathHelper.TwoPi,
            origin: new Vector2(agrometerFrame.Width / 2f, agrometerFrame.Height / 2f),
            scale: GetAgrometerScale(),
            effects: SpriteEffects.None,
            layerDepth: 0.86f
        );
        
        drawCurrentCropEssences(b);
        
        drawItemSlots(b);
        drawEssenceVial(b);
        drawExtractAllButton(b);
        
        drawParticles(b);

        drawEssenceIcons(b);

        drawArrows(b);
        
        for (int i = 0; i < 6; i++)
        {
            Vector2 center = GetEssenceCenter(i);
            float radius = GetEssenceContainerRadius() * 0.1f;
            float distanceFromMouse = Vector2.Distance(new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()), center);
            if (distanceFromMouse < radius)
            {
                string[] tooltip = TokenParser.ParseText(GetEssenceTooltip(i)).Split(':');
                drawHoverText(b, tooltip[1], Game1.smallFont, boldTitleText: tooltip[0]);
                break;
            }
        }

        if (shouldDrawVialTooltip && EssenceVial is not null)
        {
            drawHoverText(b, EssenceVial.getDescription(), Game1.smallFont, hoveredItem: EssenceVial, boldTitleText: EssenceVial.DisplayName);
        }

        drawMouse(b);
    }

    private void drawEssenceIcons(SpriteBatch b)
    {
        for (var i = 0; i < EssenceIcons.Count; i++)
        {
            float essenceIconTransparency = MathHelper.SmoothStep(0.5f, 1f, MathHelper.Clamp((currentEssenceScale[i] - 1f) / 0.15f, 0f, 1f));
            var icon = EssenceIcons[i];
            icon.draw(
                b: b,
                c: Color.Lerp(Color.Black, Color.White, essenceIconTransparency + 0.15f) * essenceIconTransparency,
                layerDepth: 0.9f
            );
        }
    }
    
    private void drawExtractAllButton(SpriteBatch b)
    {
        Vector2 slotPosition = GetExtractAllPosition();

        b.Draw(
            texture: ExtractAllButton,
            position: slotPosition,
            sourceRectangle: new Rectangle(0,0, 22, 15),
            color: Color.Lerp(Color.White,new Color(88, 57, 40), 0.75f),
            rotation: currentMenuRotation / 360f * MathHelper.TwoPi,
            origin: new Vector2(ExtractAllButton.Width / 2f, ExtractAllButton.Height / 2f),
            scale: GetAgrometerScale() * 1.5f,
            effects: SpriteEffects.None,
            layerDepth: 0.5f);
    }

    private void drawEssenceVial(SpriteBatch b)
    {
        Texture2D itemSlotTexture = Game1.uncoloredMenuTexture;
        Rectangle itemSlotSourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 16);
        itemSlotSourceRect.Width -= 4;
        itemSlotSourceRect.Height -= 4;
        Vector2 slotPosition = GetEssenceVialSlotPosition();
        
        Color extractColour = Color.DarkRed;
        Color inputColour = Color.SteelBlue;
        
        Color colourOne = isExtractMode ? inputColour : extractColour;
        Color colourTwo = isExtractMode ? extractColour : inputColour;
        float rotationLerp = MathHelper.Lerp(0, 1, Math.Abs(180 - (currentMenuRotation % 360)) / 180f);
        Color currentVialSlotColour = Color.Lerp(isExtractMode ? colourOne : colourTwo, isExtractMode ? colourTwo : colourOne, rotationLerp);

        b.Draw(
            texture: itemSlotTexture,
            position: slotPosition,
            sourceRectangle: itemSlotSourceRect,
            color: Color.Lerp(new Color(88, 57, 40), currentVialSlotColour, 0.5f),
            rotation: currentMenuRotation / 360f * MathHelper.TwoPi,
            origin: new Vector2(itemSlotSourceRect.Width / 2f, itemSlotSourceRect.Height / 2f),
            scale: GetItemSlotScale(2) * 0.75f,
            effects: SpriteEffects.None,
            layerDepth: 0.5f);
        
        ParsedItemData iData = ItemRegistry.GetData(EssenceVial?.QualifiedItemId ?? $"(O){Agromancy.UNIQUE_ID}_T1EssenceVial");
        Texture2D texture = iData.GetTexture();
        Vector2 randomJitter = Vector2.Zero;
        if (IsCropBeingDrained && (EssenceVial is null || !canVialTierInfuseAnyEssence()))
        {
            randomJitter = new Vector2((float)(rng.NextDouble() - 0.5) * 4f,
                (float)(rng.NextDouble() - 0.5) * 4f);
        }
        
        if (EssenceVial is not null)
        {
            for (int i = 0; i < 7; i++)
            {
                EssenceVial.modData.TryAdd($"{Agromancy.UNIQUE_ID}_{i}", "0");
            }
            
            int vialTier = ObjectPatches.GetEssenceVialTier(EssenceVial);
            
            float yield = float.Parse(EssenceVial.modData[$"{Agromancy.UNIQUE_ID}_0"]) / 255f;
            float quality = float.Parse(EssenceVial.modData[$"{Agromancy.UNIQUE_ID}_1"]) / 255f;
            float growth = float.Parse(EssenceVial.modData[$"{Agromancy.UNIQUE_ID}_2"]) / 255f;
            float giant = float.Parse(EssenceVial.modData[$"{Agromancy.UNIQUE_ID}_3"]) / 255f;
            float water = float.Parse(EssenceVial.modData[$"{Agromancy.UNIQUE_ID}_4"]) / 255f;
            float seed = float.Parse(EssenceVial.modData[$"{Agromancy.UNIQUE_ID}_5"]) / 255f;
            float total = yield + quality + growth + giant + water + seed;

            float fillPercentage = Math.Clamp(total / (10f * vialTier * 6f), 0f, 1f);
            
            Rectangle sourceRect = iData.GetSourceRect();
            
            // This first draw is for the rainbow essence inside the vial.
            b.Draw(
                texture: Game1.staminaRect,
                position: slotPosition - new Vector2(0f, slotPosition.Y >= Game1.uiViewport.Height / 2f ? -14 : 14) + randomJitter,
                sourceRectangle: new Rectangle(0, 0, 8, 6),
                color: Utility.GetPrismaticColor() * 0.85f,
                rotation: currentMenuRotation / 360f * MathHelper.TwoPi + MathHelper.ToRadians(180f),
                origin: new Vector2(4f, 0f),
                scale: new Vector2(4f, MathHelper.Lerp(0f, 4f, fillPercentage)) * GetItemSlotScale(2).X * 0.6f * 0.75f,
                effects: SpriteEffects.None,
                layerDepth: 0.505f
            );
            
            b.Draw(
                texture: texture,
                position: slotPosition + new Vector2(0, slotPosition.Y >= Game1.uiViewport.Height / 2f ? -2 : 2) - new Vector2(sourceRect.Width, sourceRect.Height) + randomJitter + new Vector2(16f, 16f),
                sourceRectangle: sourceRect,
                color: Color.White * 1f * (EssenceVial is not null ? 1f : 0.4f),
                rotation: currentMenuRotation / 360f * MathHelper.TwoPi,
                origin: new Vector2(sourceRect.Width / 2f, sourceRect.Height / 2f),
                scale: 4f * GetItemSlotScale(2).X * 0.6f * 0.75f,
                effects: SpriteEffects.None,
                layerDepth: 0.51f);
        }
        else
        {
            b.Draw(
                texture: texture,
                position: slotPosition + new Vector2(0, slotPosition.Y >= Game1.uiViewport.Height / 2f ? -2 : 2) + randomJitter,
                sourceRectangle: iData.DefaultSourceRect,
                color: EssenceVial is not null ? Color.White : Color.Black * 0.4f,
                rotation: currentMenuRotation / 360f * MathHelper.TwoPi,
                origin: new Vector2(iData.DefaultSourceRect.Width / 2f, iData.DefaultSourceRect.Height / 2f),
                scale: GetItemSlotScale(2) * 4f * 0.6f * 0.75f,
                effects: SpriteEffects.None,
                layerDepth: 0.51f);
        }
    }
    
    public void drawAgrometerBackground(SpriteBatch b)
    {
        //(°o,88,o° )/\\\\ aaah a spider

        Color mainColour = GetAgrometerMainColour();
        Color secondaryColour = Color.LawnGreen;
        Color centerColour = Color.Transparent;
        float colourLerp = 0.25f;

        BasicEffect basicEffect = new(Game1.graphics.GraphicsDevice);
        basicEffect.VertexColorEnabled = true;

        Vector3 center = PrimitiveNormalize(GetAgrometerCenter());

        float radius = (agrometerFrame.Width * GetAgrometerScale().X) / 2.2f;

        List<VertexPositionColor> triangles = [];

        List<Vector3> pointsAroundAgrometer = new();
        for (int i = 0; i < 12; i++)
        {
            Vector2 pointOnCircle = new Vector2(GetAgrometerCenter().X + radius * (float)Math.Cos(MathHelper.ToRadians(i * 30 - currentMenuRotation + (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 25 % 360))), GetAgrometerCenter().Y + radius * (float)Math.Sin(MathHelper.ToRadians(i * 30 - currentMenuRotation + (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 25 % 360)))
            );
            pointsAroundAgrometer.Add(PrimitiveNormalize(pointOnCircle));
        }

        for (int i = pointsAroundAgrometer.Count - 1; i >= 0; i -= 2)
        {
            triangles.Add(new VertexPositionColor(center, centerColour * 0.9f));
            Vector3 point = pointsAroundAgrometer[i];
            triangles.Add(
                new VertexPositionColor(point, Color.Lerp(mainColour, secondaryColour, colourLerp) * 0.75f));
            Vector3 nextPoint =
                pointsAroundAgrometer[(i - 2 + pointsAroundAgrometer.Count) % pointsAroundAgrometer.Count];
            triangles.Add(new VertexPositionColor(nextPoint,
                Color.Lerp(mainColour, secondaryColour, colourLerp) * 0.75f));
        }

        foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            b.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangles.ToArray(), 0,
                triangles.Count / 3);
        }
    }
    
    private void drawArrows(SpriteBatch b)
    {
        UpArrow.draw(b, GetAgrometerMainColour() * 3.5f, layerDepth: 0.9f);
        DownArrow.draw(b, GetAgrometerMainColour() * 3.5f, layerDepth: 0.9f);
        
        b.Draw(
            texture: ArrowsTexture,
            position: GetRotateArrowPosition(),
            sourceRectangle: new Rectangle(47, 2, 19, 7),
            color: Color.Lerp(Color.White,new Color(88, 57, 40), 0.75f),
            rotation: currentMenuRotation / 360f * MathHelper.TwoPi,
            origin: new Vector2(19 / 2f, 7 / 2f),
            scale: GetAgrometerScale() * 1.5f * currentRotateArrowScale,
            effects: SpriteEffects.None,
            layerDepth: 0.9f
        );
    }

    private void drawCurrentCropEssences(SpriteBatch b)
    {
        Effect StatsFx = Agromancy.StatsFx;
        
        StatsFx.Parameters["PerlinNoise"].SetValue(Agromancy.PerlinNoise);
        StatsFx.Parameters["Resolution"].SetValue(new Vector2(Game1.viewport.Width, Game1.viewport.Height));
        StatsFx.Parameters["BlobCenter"].SetValue(new Vector2(GetAgrometerCenter().X / Game1.uiViewport.Width, GetAgrometerCenter().Y / Game1.uiViewport.Height));
        StatsFx.Parameters["BlobMinRadius"].SetValue(Game1.viewport.Height * 0.15f);
        StatsFx.Parameters["BlobMaxRadius"].SetValue(Game1.viewport.Height * 0.5f);
        // StatsFx.Parameters["StatPercentage"].SetValue((float)(Game1.getMousePosition().X / (float)Game1.viewport.Width));
        StatsFx.Parameters["StatPercentage"].SetValue(MathHelper.Lerp(-0.1f, 1f, currentTotalEssencePct));
        StatsFx.Parameters["UseNoiseColour"].SetValue(true);
        StatsFx.Parameters["FadeOut"].SetValue(true);
        StatsFx.Parameters["Saturation"].SetValue(1.75f);
        StatsFx.Parameters["Time"].SetValue((float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 100f);
        
        b.End();
        b.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            effect: StatsFx
        );
            
        b.Draw(
            texture: Game1.staminaRect,
            destinationRectangle: b.GraphicsDevice.Viewport.Bounds,
            sourceRectangle: null,
            color: Color.White * 0f,
            rotation: 0f,
            origin: Vector2.Zero,
            effects: SpriteEffects.None,
            layerDepth: 0.9f
        );
        
        b.End();

        Agromancy.LiquidCircleFx.Parameters["Resolution"].SetValue(GetAgrometerScale() * agrometerStatRing.Bounds.Size.ToVector2());
        Agromancy.LiquidCircleFx.Parameters["PerlinNoise"].SetValue(Agromancy.PerlinNoise);

        float radius = GetEssenceContainerRadius() * 0.95f;
        Agromancy.LiquidCircleFx.Parameters["CircleRadius"].SetValue(radius);
        
        for (int i = 0; i < 6; i++)
        {
            Vector2 position = GetEssenceCenter(i);

            float FillPercentage = currentEssencePct[i];
            // The waviness means it won't appear full even at 100%, so this next line just adds a little extra percentage for visuals.
            // More gets added the closer to 100% it is, because if I just add a flat amount, it doesn't look actually empty at 0% like it should.
            // (It won't ever look ABSOLUTELY full though because I don't want to hide my nice liquid shader wobblies...)
            FillPercentage += MathHelper.Lerp(0, 0.1f, FillPercentage);
            float waviness = 200f * FillPercentage;
            
            Agromancy.LiquidCircleFx.Parameters["Time"].SetValue((float)(Game1.currentGameTime.TotalGameTime
                .TotalMilliseconds) / 500f);
            
            Agromancy.LiquidCircleFx.Parameters["yCoordOffset"].SetValue(i / 6f);
            
            Agromancy.LiquidCircleFx.Parameters["FillPercentage"].SetValue(0f);
            Agromancy.LiquidCircleFx.Parameters["Waviness"].SetValue(0f);
            
            b.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                effect: Agromancy.LiquidCircleFx
            );
            
            b.Draw(
                texture: Game1.staminaRect,
                position: position,
                sourceRectangle: null,
                color: Color.Lerp(GetItemSlotColour(), Color.White, 0.65f) * 0.5f,
                rotation: 0f,
                origin: new Vector2(0.5f, 0.5f),
                scale: GetAgrometerScale() * agrometerStatRing.Width * 0.1f * currentEssenceScale[i],
                effects: SpriteEffects.None,
                layerDepth: 0.88f
            );

            b.End();
            b.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                effect: Agromancy.LiquidCircleFx
            );
            
            Agromancy.LiquidCircleFx.Parameters["FillPercentage"].SetValue(1 - FillPercentage);
            Agromancy.LiquidCircleFx.Parameters["Waviness"].SetValue(waviness);
            
            b.Draw(
                texture: Game1.staminaRect,
                position: position,
                sourceRectangle: null,
                color: GetEssenceColour(i),
                rotation: 0f,
                origin: new Vector2(0.5f, 0.5f),
                scale: GetAgrometerScale() * agrometerStatRing.Width * 0.1f * currentEssenceScale[i],
                effects: SpriteEffects.None,
                layerDepth: 0.88f
            );

            b.End();
            b.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp
            );
            
            b.Draw(
                texture: agrometerStatRing,
                position: position,
                sourceRectangle: new Rectangle(0, 0, agrometerStatRing.Width, agrometerStatRing.Height),
                color: Color.White,
                rotation: 0f,
                origin: new Vector2(agrometerStatRing.Width / 2f, agrometerStatRing.Height / 2f),
                scale: GetAgrometerScale() * 0.1f * currentEssenceScale[i],
                effects: SpriteEffects.None,
                layerDepth: 0.89f
            );

            b.End();
        }
        
        b.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp
        );
    }
    
    private void drawItemSlots(SpriteBatch b)
    {
        Vector2 startPosition = GetAgrometerCenter();
        for (int i = 0; i < 5; i++)
        {
            Texture2D itemSlotTexture = Game1.uncoloredMenuTexture;
            Rectangle itemSlotSourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 16);
            itemSlotSourceRect.Width -= 4;
            itemSlotSourceRect.Height -= 4;
            Rectangle itemSlotBgSourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 20);
            Vector2 slotPosition = startPosition + GetItemSlotVerticalOffset(i);
            // b.Draw(
            //     texture: itemSlotTexture,
            //     position: slotPosition,
            //     sourceRectangle: itemSlotBgSourceRect,
            //     color: Color.SteelBlue * 0.25f * (1f - 0.35f * Math.Abs(2 - i)),
            //     rotation: 0f,
            //     origin: new Vector2(itemSlotSourceRect.Width / 2f, itemSlotSourceRect.Height / 2f),
            //     scale: GetItemSlotScale(i) * .9f,
            //     effects: SpriteEffects.None,
            //     layerDepth: 0.5f);

            b.Draw(
                texture: itemSlotTexture,
                position: slotPosition,
                sourceRectangle: itemSlotSourceRect,
                color: GetItemSlotColour() * 1f * (1f - 0.35f * Math.Abs(2 - i)),
                rotation: 0f,
                origin: new Vector2(itemSlotSourceRect.Width / 2f, itemSlotSourceRect.Height / 2f),
                scale: GetItemSlotScale(i),
                effects: SpriteEffects.None,
                layerDepth: 0.5f);

            if (agromancyCrops.Any())
            {
                Item item = agromancyCrops.Keys.ElementAt((i + itemListOffset) % agromancyCrops.Count);
                ParsedItemData iData = ItemRegistry.GetData(item.QualifiedItemId);
                Texture2D texture = iData.GetTexture();
                Vector2 randomJitter = Vector2.Zero;
                if ((IsCropBeingDrained || isCropSuckedDry) && i is 2 && EssenceVial is not null && canVialTierInfuseAnyEssence())
                {
                    randomJitter = new Vector2((float)(rng.NextDouble() - 0.5) * 4f,
                        (float)(rng.NextDouble() - 0.5) * 4f);
                }
                
                float suckingDryLerp = MathHelper.Lerp(0.2f, 0.75f, timeSinceSuckingDry / 3500f);

                if (i is 2)
                {
                    b.End();
                    
                    Agromancy.DissolveFx.Parameters["PerlinNoise"].SetValue(Agromancy.PerlinNoise);
                    Agromancy.DissolveFx.Parameters["UseNoiseColour"].SetValue(false);
                    Agromancy.DissolveFx.Parameters["ClipThreshold"].SetValue(suckingDryLerp);
                    
                    b.Begin(
                        SpriteSortMode.Deferred,
                        BlendState.AlphaBlend,
                        SamplerState.PointClamp,
                        effect: Agromancy.DissolveFx
                    );
                }
                
                b.Draw(
                    texture: texture,
                    position: slotPosition + new Vector2(0, -2) + randomJitter,
                    sourceRectangle: iData.DefaultSourceRect,
                    color: Color.White * (1f - 0.45f * Math.Abs(2 - i)),
                    rotation: 0f,
                    origin: new Vector2(iData.DefaultSourceRect.Width / 2f, iData.DefaultSourceRect.Height / 2f),
                    scale: GetItemSlotScale(i) * 4f * 0.6f * (i is 2 ? 1f - suckingDryLerp + 0.2f : 1f),
                    effects: SpriteEffects.None,
                    layerDepth: 0.51f);

                if (i is 2)
                {
                    float stringLength =
                        Game1.tinyFont.MeasureString(item.Stack.ToString()).X +
                        2; // The extra 2 is to account for the black outline.
                    Vector2 textPosition = GetAgrometerCenter() -
                                           new Vector2(stringLength / 2f - itemSlotSourceRect.Width / 4f, -12f) *
                                           GetItemSlotScale(i);
                    Utility.drawTinyDigits(
                        toDraw: item.Stack,
                        b: b,
                        position: textPosition,
                        scale: GetItemSlotScale(i).X * 2f,
                        layerDepth: 0.52f,
                        c: Color.White);
                    b.End();
                    b.Begin(
                        SpriteSortMode.Deferred,
                        BlendState.AlphaBlend,
                        SamplerState.PointClamp
                    );
                }
                // b.DrawString(
                //     Game1.smallFont,
                //     item.Stack > 1 ? item.Stack.ToString() : "",
                //     slotPosition + new Vector2(16f, 24f) * GetItemSlotScale(i),
                //     Color.White * (1f - 0.45f * Math.Abs(2 - i)),
                //     0f,
                //     new Vector2(0, Game1.smallFont.MeasureString(item.Stack > 1 ? item.Stack.ToString() : "").Y / 2f),
                //     GetItemSlotScale(i),
                //     SpriteEffects.None,
                //     0.52f);
                // item.drawInMenu(b, slotPosition - new Vector2(32f, 32f), GetItemSlotScale(i).X * 0.5f);
            }
        }
    }
    
    public override void drawBackground(SpriteBatch b)
    {
        // base.drawBackground(b);
        // b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
        //     Color.Blue * 0.92f);
    }
    
    private void drawTextCenteredAtPoint(SpriteBatch b, string text, Vector2 point, SpriteFont font, Color color, float scale, float layerDepth)
    {
        Vector2 textSize = font.MeasureString(text) * scale;
        Vector2 textPosition = point - textSize / 2f;
        b.DrawString(font, text, textPosition, color, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
    }
    
    private void drawCircle(Vector2 center, float radius, int resolution, Color[] colours, float lerp)
    {
        BasicEffect basicEffect = new(Game1.graphics.GraphicsDevice);
        basicEffect.VertexColorEnabled = true;
        basicEffect.EmissiveColor = Color.Lerp(colours[1], colours[2], lerp).ToVector3();

        Vector3 centerVec3 = PrimitiveNormalize(center);

        List<VertexPositionColor> triangles = [];

        List<Vector3> pointsAroundCenter = [];
        for (int i = 0; i < 360; i += 360 / resolution)
        {
            Vector2 pointOnCircle = new Vector2(
                center.X + radius * (float)Math.Cos(MathHelper.ToRadians(i)),
                center.Y + radius * (float)Math.Sin(MathHelper.ToRadians(i))
            );
            pointsAroundCenter.Add(PrimitiveNormalize(pointOnCircle));
        }

        for (int i = pointsAroundCenter.Count - 1; i >= 0; i -= 2)
        {
            triangles.Add(new VertexPositionColor(centerVec3, Color.Lerp(colours[0], colours[1], lerp) * 0.9f));
            Vector3 point = pointsAroundCenter[i];
            triangles.Add(new VertexPositionColor(point, Color.Lerp(colours[1], colours[2], lerp) * 0.5f));
            Vector3 nextPoint = pointsAroundCenter[(i - 2 + pointsAroundCenter.Count) % pointsAroundCenter.Count];
            triangles.Add(new VertexPositionColor(nextPoint, Color.Lerp(colours[1], colours[2], lerp) * 0.5f));
        }

        foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            Game1.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangles.ToArray(), 0,
                triangles.Count / 3);
        }
    }
}