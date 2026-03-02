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
    public int MillisecondsMenuHasBeenOpen;
    
    private Texture2D agrometerFrame;
    private Texture2D agrometerCircles;
    private Texture2D agrometerStatRing;

    private int itemListOffset = 0;
    
    private float targetEssencePct = 0f;
    private float currentEssencePct = 0f;
    
    private Texture2D ArrowsTexture => Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/MonochromeArrows");
    private Rectangle UpArrowSourceRect => new(0, 0, 11, 12);
    private Rectangle DownArrowSourceRect => new(11, 0, 11, 12);
    private Rectangle LeftArrowSourceRect => new(22, 0, 12, 12);
    private Rectangle RightArrowSourceRect => new(34, 0, 12, 12);
    
    public ClickableTextureComponent UpArrow;
    public ClickableTextureComponent DownArrow;
    
    List<Item> agromancyCrops => GetItemsWithAgromancyData();

    public AgrometerMenu()
    {
        MillisecondsMenuHasBeenOpen = Game1.currentGameTime.TotalGameTime.Milliseconds;
        
        agrometerFrame = Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/AgrometerFrame");
        agrometerCircles = Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/AgrometerCircles");
        agrometerStatRing = Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/AgrometerStatRing");
        
        Rectangle upArrowLocation = new Rectangle(
            x: (int)(GetAgrometerCenter().X - 2 - (UpArrowSourceRect.Width) * GetAgrometerScale().X),
            y: (int)(GetAgrometerCenter().Y - (agrometerFrame.Height / 3f) * GetAgrometerScale().Y - (UpArrowSourceRect.Height) * GetAgrometerScale().Y),
            width: (int)(UpArrowSourceRect.Width * GetAgrometerScale().X * 2f),
            height: (int)(UpArrowSourceRect.Height * GetAgrometerScale().Y * 2f)
        );
        
        Rectangle downArrowLocation = new Rectangle(
            (int)(GetAgrometerCenter().X - 2 - (DownArrowSourceRect.Width) * GetAgrometerScale().X),
            (int)(GetAgrometerCenter().Y + (agrometerFrame.Height / 3f) * GetAgrometerScale().Y - (DownArrowSourceRect.Height) * GetAgrometerScale().Y),
            (int)(DownArrowSourceRect.Width * GetAgrometerScale().X * 2f),
            (int)(DownArrowSourceRect.Height * GetAgrometerScale().Y * 2f)
        );
        
        UpArrow = new ClickableTextureComponent(
            name: "UpArrow",
            bounds: upArrowLocation,
            label: null,
            hoverText: "Previous Crop",
            texture: ArrowsTexture,
            sourceRect: UpArrowSourceRect,
            scale: GetAgrometerScale().X * 2f);
        DownArrow = new ClickableTextureComponent(
            name: "DownArrow",
            bounds: downArrowLocation,
            label: null,
            hoverText: "Next Crop",
            texture: ArrowsTexture,
            sourceRect: DownArrowSourceRect,
            scale: GetAgrometerScale().X * 2f);
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
        // base.receiveLeftClick(x, y, playSound);
        
        Rectangle upArrowLocation = new Rectangle(
            x: (int)(GetAgrometerCenter().X - 2 - (UpArrowSourceRect.Width) * GetAgrometerScale().X),
            y: (int)(GetAgrometerCenter().Y - (agrometerFrame.Height / 3f) * GetAgrometerScale().Y - (UpArrowSourceRect.Height) * GetAgrometerScale().Y),
            width: (int)(UpArrowSourceRect.Width * GetAgrometerScale().X * 2f),
            height: (int)(UpArrowSourceRect.Height * GetAgrometerScale().Y * 2f)
        );
        
        Rectangle downArrowLocation = new Rectangle(
            (int)(GetAgrometerCenter().X - 2 - (DownArrowSourceRect.Width) * GetAgrometerScale().X),
            (int)(GetAgrometerCenter().Y + (agrometerFrame.Height / 3f) * GetAgrometerScale().Y - (DownArrowSourceRect.Height) * GetAgrometerScale().Y),
            (int)(DownArrowSourceRect.Width * GetAgrometerScale().X * 2f),
            (int)(DownArrowSourceRect.Height * GetAgrometerScale().Y * 2f)
        );
        
        if (upArrowLocation.Contains(x, y))
        {
            Log.Warn("Clicked up");
            itemListOffset = (itemListOffset - 1 + agromancyCrops.Count) % Math.Max(1, agromancyCrops.Count);
        }
        else if (downArrowLocation.Contains(x, y))
        {
            Log.Warn("clicked down");
            itemListOffset = (itemListOffset + 1) % Math.Max(1, agromancyCrops.Count);
        }
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
        
        drawCurrentCropEssences(b);
        
        drawItemSlots(b);
        drawArrows(b);

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

    private Color GetAgrometerMainColour()
    {
        return Color.BlueViolet;
    }

    private Color GetItemSlotColour()
    {
        return Color.PaleVioletRed;
    }

    public Vector2 GetAgrometerCenter()
    {
        float x = Game1.viewport.Width / 2f;
        float y = Game1.viewport.Height / 2f;
        return new Vector2(x, y);
    }

    public Vector2 GetAgrometerScale()
    {
        float scale = (Game1.viewport.Height / 1.25f) / agrometerFrame.Height;
        return new Vector2(scale, scale);
    }

    private Vector2 GetAgrometerRingScale()
    {
        float scale = (Game1.viewport.Height / 1.55f) / agrometerCircles.Height;
        return new Vector2(scale, scale);
    }

    private void drawArrows(SpriteBatch b)
    {
        UpArrow.draw(b);
        DownArrow.draw(b);
    }

    private void drawCurrentCropEssences(SpriteBatch b)
    {
        CropEssences? essences = agromancyCrops.ElementAtOrDefault((2 + itemListOffset) % agromancyCrops.Count) is not null
            ? CropManager.GrabEssences(agromancyCrops.ElementAtOrDefault((2 + itemListOffset) % agromancyCrops.Count)!)
            : null;
        
        Effect StatsFx = Agromancy.StatsFx;
        
        StatsFx.Parameters["PerlinNoise"].SetValue(Agromancy.PerlinNoise);
        StatsFx.Parameters["Resolution"].SetValue(new Vector2(Game1.viewport.Width, Game1.viewport.Height));
        StatsFx.Parameters["BlobCenter"].SetValue(new Vector2(GetAgrometerCenter().X / Game1.viewport.Width, GetAgrometerCenter().Y / Game1.viewport.Height));
        StatsFx.Parameters["BlobMinRadius"].SetValue(Game1.viewport.Height * 0.15f);
        StatsFx.Parameters["BlobMaxRadius"].SetValue(Game1.viewport.Height * 0.5f);
        // StatsFx.Parameters["StatPercentage"].SetValue((float)(Game1.getMousePosition().X / (float)Game1.viewport.Width));
        StatsFx.Parameters["StatPercentage"].SetValue(currentEssencePct);
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
        Agromancy.LiquidCircleFx.Parameters["CircleRadius"].SetValue(agrometerStatRing.Width / 2f * GetAgrometerScale().X * 0.95f);

        
        
        // This loop'll place these stat rings nicely on a circle around the center. Those extra radians are just me hardcoding some tweaked positioning to not cover my pretty leaves and vines. (:
        int essenceIndex = 0;
        for (int i = -1; i < 8; i++)
        {
            if (i is >= 2 and <= 4) continue;
            Vector2 position = GetAgrometerCenter() + new Vector2(
                (float)Math.Cos(MathHelper.ToRadians(i * 30) - MathHelper.ToRadians(7.5f * (i < 2 ? -1 : 1))) * (agrometerFrame.Width / 2.35f) * GetAgrometerScale().X,
                (float)Math.Sin(MathHelper.ToRadians(i * 30) - MathHelper.ToRadians(7.5f * (i < 2 ? -1 : 1))) * (agrometerFrame.Height / 2.35f) * GetAgrometerScale().Y
            );
            
            float FillPercentage = EssenceCalculator.GetEssencePercent(essences, essenceIndex) + 0.05f;
            float waviness = 200f * (float)Math.Pow(FillPercentage, 1.5f) + 0.625f;
            
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
                scale: GetAgrometerScale() * agrometerStatRing.Width * 0.1f,
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
                color: Color.FromNonPremultiplied(
                    (int)(127 + 127 * Math.Cos(MathHelper.ToRadians(i * 30))),
                    (int)(127 + 127 * Math.Cos(MathHelper.ToRadians(i * 30 + 120))),
                    (int)(127 + 127 * Math.Cos(MathHelper.ToRadians(i * 30 + 240))),
                    255) * 0.85f,
                rotation: 0f,
                origin: new Vector2(0.5f, 0.5f),
                scale: GetAgrometerScale() * agrometerStatRing.Width * 0.1f,
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
                scale: GetAgrometerScale() * 0.1f,
                effects: SpriteEffects.None,
                layerDepth: 0.89f
            );

            b.End();

            essenceIndex++;
        }
        
        b.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp
        );
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
                color: GetItemSlotColour() * 1f * (1f - 0.35f * Math.Abs(2 - i)),
                rotation: 0f,
                origin: new Vector2(itemSlotSourceRect.Width / 2f, itemSlotSourceRect.Height / 2f),
                scale: GetItemSlotScale(i),
                effects: SpriteEffects.None,
                layerDepth: 0.5f);

            if (agromancyCrops.Any())
            {
                Item item = agromancyCrops[(i + itemListOffset) % agromancyCrops.Count];
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
        Vector2 baseScale = index switch
        {
            0 or 4 => new Vector2(.5f, .5f),
            1 or 3 => new Vector2(1f, 1f),
            2 => new Vector2(1.5f, 1.5f),
            _ => new Vector2(1f, 1f)
        };
        float targetHeight = Game1.viewport.Height / 12f;
        float scaleFactor = targetHeight / (Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 16).Height);
        return baseScale * scaleFactor;
    }

    private Vector2 GetItemSlotVerticalOffset(int index)
    {
        Vector2 baseOffset = index switch
        {
            0 => new Vector2(0, -275),
            1 => new Vector2(0, -85),
            2 => new Vector2(0, 0),
            3 => new Vector2(0, 85),
            4 => new Vector2(0, 275),
            _ => new Vector2(0, 0)
        };
        return baseOffset * GetItemSlotScale(index);
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
        MillisecondsMenuHasBeenOpen += time.ElapsedGameTime.Milliseconds;
        var selectedCrop = agromancyCrops.ElementAtOrDefault((2 + itemListOffset) % agromancyCrops.Count);
        CropEssences? essences = selectedCrop is not null ? CropManager.GrabEssences(selectedCrop) : null;
        
        float pct = essences is not null ? EssenceCalculator.PercentToPerfectCrop(essences) : 0f;
        targetEssencePct = pct;
        if (currentEssencePct < targetEssencePct)
        {
            currentEssencePct = MathHelper.SmoothStep(currentEssencePct, targetEssencePct, 0.15f);
        } else
        {
            currentEssencePct = MathHelper.Lerp(currentEssencePct, targetEssencePct, 0.075f);
        }
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