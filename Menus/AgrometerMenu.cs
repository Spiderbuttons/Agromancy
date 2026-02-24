using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Agromancy.Helpers;
using Agromancy.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using Object = StardewValley.Object;

namespace Agromancy.Menus;

public class AgrometerMenu : IClickableMenu
{
    private Texture2D agrometerFrame;
    private Texture2D agrometerRings;
    private bool menuMovingDown;
    private int menuPositionOffset;
    List<Item> agromancyCrops => GetItemsWithAgromancyData();

    public AgrometerMenu()
    {
        agrometerFrame = Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/AgrometerFrame");
        agrometerRings = Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/AgrometerRings");
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

        b.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp,
            DepthStencilState.Default,
            RasterizerState.CullNone);

        b.Draw(
            texture: agrometerRings,
            position: GetAgrometerCenter(),
            sourceRectangle: new Rectangle(0, 0, agrometerRings.Width, agrometerRings.Height),
            color: Color.White * 0.5f,
            rotation: 0f,
            origin: new Vector2(agrometerRings.Width / 2f, agrometerRings.Height / 2f),
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

        Color mainColour = Color.MidnightBlue;
        Color secondaryColour = Color.Green;
        Color centerColour = Color.DarkOliveGreen;

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
                new VertexPositionColor(point, Color.Lerp(mainColour, secondaryColour, 0.15f) * 0.75f));
            Vector3 nextPoint =
                pointsAroundAgrometer[(i - 2 + pointsAroundAgrometer.Count) % pointsAroundAgrometer.Count];
            triangles.Add(new VertexPositionColor(nextPoint,
                Color.Lerp(mainColour, secondaryColour, 0.15f) * 0.75f));
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
        float scale = (Game1.viewport.Height / 1.55f) / agrometerRings.Height;
        return new Vector2(scale, scale);
    }

    private void drawCurrentCropEssences(SpriteBatch b)
    {
        var selectedCrop = agromancyCrops.ElementAtOrDefault(2);
        if (selectedCrop is null) return;
        
        CropEssences? essences = CropManager.GrabEssences(selectedCrop);
        if (essences is null) return;
        
        // The circles to draw the essences in are arranged in a circle around the center of the agrometer
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

        for (var index = 0; index < statPositions.Count; index++)
        {
            var pos = statPositions[index];
            byte essenceValue = index switch
            {
                0 => essences.YieldEssence,
                1 => (byte)(essences.QualityEssence.Sum(b => b) / essences.QualityEssence.Length),
                2 => essences.GrowthEssence,
                3 => essences.GiantEssence,
                4 => (byte)(255 - essences.WaterEssence),
                5 => essences.SeedEssence,
                _ => (byte)0
            };
            Color essenceColour = index switch
            {
                0 => Color.Gold,
                1 => Color.GreenYellow,
                2 => Color.Orange,
                3 => Color.Purple,
                4 => Color.Cyan,
                5 => Color.Magenta,
                _ => Color.White
            };
            drawCircle(pos, 12f * agrometerScale.X, 120, [essenceColour, essenceColour * 0.5f, Color.Transparent], essenceValue / 255f);
        }
    }

    private void drawCircle(Vector2 center, float radius, int resolution, Color[] colours, float lerp)
    {
        BasicEffect basicEffect = new(Game1.graphics.GraphicsDevice);
        basicEffect.VertexColorEnabled = true;

        Vector3 centerVec3 = PrimitiveNormalize(center);

        List<VertexPositionColor> triangles = [];

        List<Vector3> pointsAroundCenter = [];
        for (int i = 0; i < 360; i += 360 / resolution)
        {
            Vector2 pointOnCircle = new Vector2(
                center.X + radius * (0.1f + lerp) * (float)Math.Cos(MathHelper.ToRadians(i)),
                center.Y + radius * (0.1f + lerp) * (float)Math.Sin(MathHelper.ToRadians(i))
            );
            pointsAroundCenter.Add(PrimitiveNormalize(pointOnCircle));
        }

        for (int i = pointsAroundCenter.Count - 1; i >= 0; i -= 2)
        {
            triangles.Add(new VertexPositionColor(centerVec3, colours[0] * 0.9f));
            Vector3 point = pointsAroundCenter[i];
            triangles.Add(new VertexPositionColor(point, Color.Lerp(colours[2], colours[1], lerp) * 0.75f));
            Vector3 nextPoint = pointsAroundCenter[(i - 2 + pointsAroundCenter.Count) % pointsAroundCenter.Count];
            triangles.Add(new VertexPositionColor(nextPoint, Color.Lerp(colours[2], colours[1], lerp) * 0.75f));
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
        // the 2 index slot is in the center. the others are above and below it, with decreasing distance as they get further from the center
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
                color: Color.SteelBlue * 0.55f * (1f - 0.35f * Math.Abs(2 - i)),
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
                // Draw the stack size in the bottom right corner of the item
                if (i is 2)
                {
                    Utility.drawTinyDigits(
                        toDraw: item.Stack,
                        b: b,
                        position: slotPosition + new Vector2(24f, 20f),
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