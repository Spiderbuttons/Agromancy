using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Agromancy.Helpers;
using Agromancy.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace Agromancy.Menus;

public class AgrometerMenu : IClickableMenu
{
    private class EssenceParticle
    {
        private static Texture2D particleTexture => Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/EssenceParticle");
    }
    
    private Texture2D agrometerFrame;
    private Texture2D agrometerCircles;
    private Texture2D agrometerStatRing;
    
    private bool menuMovingDown;
    private int menuPositionOffset;
    List<Item> agromancyCrops => GetItemsWithAgromancyData();

    public AgrometerMenu()
    {
        agrometerFrame = Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/AgrometerFrame");
        agrometerCircles = Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/AgrometerCircles");
        agrometerStatRing = Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/AgrometerStatRing");
    }

    public override void receiveGamePadButton(Buttons button)
    {
        base.receiveGamePadButton(button);
    }

    public override void populateClickableComponentList()
    {
        base.populateClickableComponentList();
    }

    public override void applyMovementKey(int direction)
    {
        base.applyMovementKey(direction);
    }

    public override bool IsActive()
    {
        return base.IsActive();
    }

    public override bool showWithoutTransparencyIfOptionIsSet()
    {
        return base.showWithoutTransparencyIfOptionIsSet();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
    }

    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        base.receiveRightClick(x, y, playSound);
    }

    public override void receiveKeyPress(Keys key)
    {
        base.receiveKeyPress(key);
    }

    public override void gamePadButtonHeld(Buttons b)
    {
        base.gamePadButtonHeld(b);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
    }

    public override void draw(SpriteBatch b)
    {
        b.End();
        DrawAgrometerBackground(b);

        b.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);

        b.Draw(
            texture: agrometerCircles,
            position: GetAgrometerCenter(),
            sourceRectangle: new Rectangle(0, 0, agrometerCircles.Width, agrometerCircles.Height),
            color: Color.White * 0.5f,
            rotation: 0f,
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
            rotation: 0f,
            origin: new Vector2(agrometerFrame.Width / 2f, agrometerFrame.Height / 2f),
            scale: GetAgrometerScale(),
            effects: SpriteEffects.None,
            layerDepth: 0.86f
        );
        
        drawItemSlots(b);
        drawCurrentCropEssences(b);

        drawMouse(b);
    }
    
    private Vector3 PrimitiveNormalize(Vector2 position)
    {
        float x = (position.X / Game1.viewport.Width) * 2f - 1f;
        float y = (position.Y / Game1.viewport.Height) * 2f - 1f;
        return new Vector3(x, y, 0);
    }

    public void DrawAgrometerBackground(SpriteBatch b)
    {
        //(°o,88,o° )/\\\\ aaah a spider

        Color mainColour = Color.BlueViolet;
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
            Vector2 pointOnCircle = new Vector2(
                GetAgrometerCenter().X + radius * (float)Math.Cos(
                    MathHelper.ToRadians(i * 30 +
                                         (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 25 % 360))),
                GetAgrometerCenter().Y + radius * (float)Math.Sin(
                    MathHelper.ToRadians(i * 30 +
                                         (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 25 % 360)))
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

    private Vector2 GetAgrometerCenter()
    {
        float x = Game1.viewport.Width / 2f;
        float y = Game1.viewport.Height / 2f;
        return new Vector2(x, y);
    }

    private Vector2 GetAgrometerScale()
    {
        float scale = (Game1.viewport.Height / 1.25f) / agrometerFrame.Height;
        return new Vector2(scale, scale);
    }

    private Vector2 GetAgrometerRingScale()
    {
        float scale = (Game1.viewport.Height / 1.55f) / agrometerCircles.Height;
        return new Vector2(scale, scale);
    }

    private void drawCurrentCropEssences(SpriteBatch b)
    {
        int angleStep = 360 / 8;
        Vector2 agrometerScale = GetAgrometerScale();
        float radius = (agrometerFrame.Width * agrometerScale.X) / 3.755f;
        Vector2 center = GetAgrometerCenter();
        List<Vector2> statPositions =
        [
            new(center.X + radius * (float)Math.Cos(MathHelper.ToRadians(0)), center.Y + radius * (float)Math.Sin(MathHelper.ToRadians(0))),
            new(center.X + -(3 * agrometerScale.X) + radius * (float)Math.Cos(MathHelper.ToRadians(angleStep)), center.Y + -(10 * agrometerScale.X) + radius * (float)Math.Sin(MathHelper.ToRadians(angleStep))),
            new(center.X + (3 * agrometerScale.X) + radius * (float)Math.Cos(MathHelper.ToRadians(angleStep * 3)), center.Y + -(10 * agrometerScale.X) + radius * (float)Math.Sin(MathHelper.ToRadians(angleStep * 3))),
            new(center.X + radius * (float)Math.Cos(MathHelper.ToRadians(angleStep * 4)), center.Y + radius * (float)Math.Sin(MathHelper.ToRadians(angleStep * 4))),
            new(center.X + (3 * agrometerScale.X) + radius * (float)Math.Cos(MathHelper.ToRadians(angleStep * 5)), center.Y + (10 * agrometerScale.X) + radius * (float)Math.Sin(MathHelper.ToRadians(angleStep * 5))),
            new(center.X + -(3 * agrometerScale.X) + radius * (float)Math.Cos(MathHelper.ToRadians(angleStep * 7)), center.Y + (10 * agrometerScale.X) + radius * (float)Math.Sin(MathHelper.ToRadians(angleStep * 7)))
        ];
        
        var selectedCrop = agromancyCrops.ElementAtOrDefault(2);
        CropEssences? essences = selectedCrop is not null ? CropManager.GrabEssences(selectedCrop) : null;
        
        for (var index = 0; index < statPositions.Count; index++)
        {
            var pos = statPositions[index];
            if (selectedCrop is null || essences is null) goto drawStatRings;
            byte essenceValue = index switch
            {
                0 => essences.YieldEssence,
                1 => (byte)(essences.QualityEssence.Sum(byt => byt) / essences.QualityEssence.Length),
                2 => essences.GrowthEssence,
                3 => essences.GiantEssence,
                4 => essences.WaterEssence,
                5 => essences.SeedEssence,
                _ => (byte)0
            };
            Color essenceColour = index switch
            {
                0 => Color.Red, // Yield
                1 => Color.Purple, // Quality
                2 => Color.Green, // Growth
                3 => Color.Pink, // Giant
                4 => Color.DeepSkyBlue, // Water
                5 => Color.RosyBrown, // Seed
                _ => Color.White
            };
            drawCircle(pos, 12f * agrometerScale.X, 120, [Color.Transparent, essenceColour * 0.25f, essenceColour], Math.Max(0.25f, essenceValue / 255f));
            
            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);
            float percentOfMax = essenceValue / 255f;
            float stringLength = Game1.tinyFont.MeasureString(percentOfMax.ToString(CultureInfo.InvariantCulture)).X + 2; // The extra 2 is to account for the black outline.
            Vector2 textPosition = pos - new Vector2(stringLength / 2f * GetAgrometerScale().X, 0);
            Utility.drawTinyDigits(
                toDraw: (int)(percentOfMax * 100),
                b: b,
                position: textPosition,
                scale: GetAgrometerScale().X,
                layerDepth: 10f,
                c: Color.White);
            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);

            drawStatRings:
            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
            b.Draw(
                texture: agrometerStatRing,
                position: pos,
                sourceRectangle: new Rectangle(0, 0, agrometerStatRing.Width, agrometerStatRing.Height),
                color: Color.White * 0.75f,
                rotation: 0f,
                origin: new Vector2(agrometerStatRing.Width / 2f, agrometerStatRing.Height / 2f),
                scale: GetAgrometerScale().X * 0.125f * 0.5f,
                effects: SpriteEffects.None,
                layerDepth: 0.87f
            );
            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);
        }
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
                color: Color.PaleVioletRed * 1f * (1f - 0.35f * Math.Abs(2 - i)),
                rotation: 0f,
                origin: new Vector2(itemSlotSourceRect.Width / 2f, itemSlotSourceRect.Height / 2f),
                scale: GetItemSlotScale(i),
                effects: SpriteEffects.None,
                layerDepth: 0.5f);

            if (i < agromancyCrops.Count)
            {
                Item item = agromancyCrops[i];
                ParsedItemData iData = ItemRegistry.GetData(item.QualifiedItemId);
                Texture2D texture = iData.GetTexture();
                b.Draw(
                    texture: texture,
                    position: slotPosition + new Vector2(0, -2),
                    sourceRectangle: iData.DefaultSourceRect,
                    color: Color.White * (1f - 0.45f * Math.Abs(2 - i)),
                    rotation: 0f,
                    origin: new Vector2(iData.DefaultSourceRect.Width / 2f, iData.DefaultSourceRect.Height / 2f),
                    scale: GetItemSlotScale(i) * 4f * 0.6f,
                    effects: SpriteEffects.None,
                    layerDepth: 0.51f);
                
                if (i is 2)
                {
                    float stringLength = Game1.tinyFont.MeasureString(item.Stack.ToString()).X + 2; // The extra 2 is to account for the black outline.
                    // Center the stack beneath the item
                    Vector2 textPosition = GetAgrometerCenter() - new Vector2(stringLength / 2f - itemSlotSourceRect.Width / 4f, -12f) * GetItemSlotScale(i);
                    Utility.drawTinyDigits(
                        toDraw: item.Stack,
                        b: b,
                        position: textPosition,
                        scale: GetItemSlotScale(i).X * 2f,
                        layerDepth: 0.52f,
                        c: Color.White);
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
    
    private void drawTextCenteredAtPoint(SpriteBatch b, string text, Vector2 point, SpriteFont font, Color color, float scale, float layerDepth)
    {
        Vector2 textSize = font.MeasureString(text) * scale;
        Vector2 textPosition = point - textSize / 2f;
        b.DrawString(font, text, textPosition, color, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
    }

    private Vector2 GetItemSlotScale(int index)
    {
        return index switch
        {
            0 or 4 => new Vector2(.75f, .75f),
            1 or 3 => new Vector2(1.25f, 1.25f),
            2 => new Vector2(1.75f, 1.75f),
            _ => new Vector2(1f, 1f)
        };
    }

    private Vector2 GetItemSlotVerticalOffset(int index)
    {
        return index switch
        {
            0 => new Vector2(0, -165),
            1 => new Vector2(0, -100),
            2 => new Vector2(0, 0),
            3 => new Vector2(0, 100),
            4 => new Vector2(0, 165),
            _ => new Vector2(0, 0)
        };
    }

    public override void drawBackground(SpriteBatch b)
    {
        // base.drawBackground(b);
        b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
            Color.Blue * 0.92f);
    }

    private List<Item> GetItemsWithAgromancyData()
    {
        var inventory = Game1.player.Items;
        var items = new List<Item>();
        foreach (var item in inventory)
        {
            if (item is not null && item.modData.ContainsKey(Agromancy.Manifest.UniqueID))
            {
                items.Add(item);
            }
        }
        return items;
    }

    public override void update(GameTime time)
    {
        //
    }

    public override void cleanupBeforeExit()
    {
        base.cleanupBeforeExit();
    }

    public override bool shouldDrawCloseButton()
    {
        return base.shouldDrawCloseButton();
    }

    public override void emergencyShutDown()
    {
        base.emergencyShutDown();
    }

    public override bool readyToClose()
    {
        return base.readyToClose();
    }
}