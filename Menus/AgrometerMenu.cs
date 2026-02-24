using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Agromancy.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace Agromancy.Menus;

public class AgrometerMenu : IClickableMenu
{
    private Texture2D agrometerBackground;
    private bool menuMovingDown;
    private int menuPositionOffset;

    public AgrometerMenu()
    {
        agrometerBackground = Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/AgrometerRing");
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
        
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);
        
        // b.Draw(
        //     texture: agrometerBackground,
        //     position: GetAgrometerCenter(),
        //     sourceRectangle: new Rectangle(0, 0, agrometerBackground.Width, agrometerBackground.Height),
        //     color: Color.White,
        //     rotation: 0f,
        //     origin: new Vector2(agrometerBackground.Width / 2f, agrometerBackground.Height / 2f),
        //     scale: GetAgrometerScale(),
        //     effects: SpriteEffects.None,
        //     layerDepth: 0.86f
        // );

        Texture2D itemSlotTexture = Game1.uncoloredMenuTexture;
        Rectangle itemSlotSourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10);
        Rectangle itemSlotBgSourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 9);
        b.Draw(
                    texture: itemSlotTexture,
                    position: GetAgrometerCenter(),
                    sourceRectangle: itemSlotBgSourceRect,
                    color: Color.MidnightBlue * 0.5f,
                    rotation: 0f,
                    origin: new Vector2(itemSlotSourceRect.Width / 2f, itemSlotSourceRect.Height / 2f),
                    scale: 1.25f,
                    effects: SpriteEffects.None,
                    layerDepth: 0.5f);
        
        b.Draw(
            texture: itemSlotTexture,
            position: GetAgrometerCenter(),
            sourceRectangle: itemSlotSourceRect,
            color: Color.MidnightBlue * 0.5f,
            rotation: 0f,
            origin: new Vector2(itemSlotSourceRect.Width / 2f, itemSlotSourceRect.Height / 2f),
            scale: 1.25f,
            effects: SpriteEffects.None,
            layerDepth: 0.5f);
        
        drawMouse(b);
    }
    
    public void DrawAgrometerBackground(SpriteBatch b)
    {
        //(°o,88,o° )/\\\\ aaah a spider
        RasterizerState rs = new RasterizerState()
        {
            CullMode = CullMode.None,
        };
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, rs);
        {

            BasicEffect basicEffect = new(Game1.graphics.GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            
            Vector3 PrimitiveNormalize(Vector2 position)
            {
                float x = (position.X / Game1.viewport.Width) * 2f - 1f;
                float y = (position.Y / Game1.viewport.Height) * 2f - 1f;
                return new Vector3(x, y, 0);
            }
            
            // Vector3 top = Vector3.Normalize(new Vector3(0, -1, 0));
            // Vector3 left = Vector3.Normalize(new Vector3(-1, 0, 0));
            // Vector3 right = Vector3.Normalize(new Vector3(1, 0, 0));

            Vector3 center = PrimitiveNormalize(GetAgrometerCenter());
            Vector3 left = PrimitiveNormalize(new Vector2(0, Game1.viewport.Height));
            Vector3 right = PrimitiveNormalize(new Vector2(1000, Game1.viewport.Height / 2f));
            
            float radius = (agrometerBackground.Width * GetAgrometerScale().X) / 2.2f;
            
            List<VertexPositionColor> triangles = [
                // new (center, Color.Red),
                // new (left, Color.Blue),
                // new (right, Color.Green),
            ];
            
            List<Vector3> pointsAroundAgrometer = new();
            for (int i = 0; i < 12; i++)
            {
                Vector2 pointOnCircle = new Vector2(
                    GetAgrometerCenter().X + radius * (float)Math.Cos(MathHelper.ToRadians(i * 30 + (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 10 % 360))),
                    GetAgrometerCenter().Y + radius * (float)Math.Sin(MathHelper.ToRadians(i * 30 + (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 10 % 360)))
                );
                pointsAroundAgrometer.Add(PrimitiveNormalize(pointOnCircle));
            }
            
            foreach (Vector3 point in pointsAroundAgrometer)
            {
                float squareSize = 0.05f;
                Vector3 topLeft = point + new Vector3(-squareSize, -squareSize, 0);
                Vector3 topRight = point + new Vector3(squareSize, -squareSize, 0);
                Vector3 bottomLeft = point + new Vector3(-squareSize, squareSize, 0);
                Vector3 bottomRight = point + new Vector3(squareSize, squareSize, 0);

                triangles.Add(new VertexPositionColor(topLeft, Color.Magenta * 0.75f));
                triangles.Add(new VertexPositionColor(bottomLeft, Color.Magenta * 0.75f));
                triangles.Add(new VertexPositionColor(bottomRight, Color.Magenta * 0.75f));

                triangles.Add(new VertexPositionColor(topLeft, Color.Magenta * 0.75f));
                triangles.Add(new VertexPositionColor(bottomRight, Color.Magenta * 0.75f));
                triangles.Add(new VertexPositionColor(topRight, Color.Magenta * 0.75f));
            }
            
            for (int i = pointsAroundAgrometer.Count - 1; i >= 0; i -= 2)
            {
                Vector3 newCenter = PrimitiveNormalize(GetAgrometerCenter());
                triangles.Add(new VertexPositionColor(center, Color.DarkOliveGreen * 0.9f));
                Vector3 point = pointsAroundAgrometer[i];
                triangles.Add(new VertexPositionColor(point, Color.Lerp(Color.MidnightBlue, Color.Green, 0.15f) * 0.75f));
                Vector3 nextPoint = pointsAroundAgrometer[(i - 2 + pointsAroundAgrometer.Count) % pointsAroundAgrometer.Count];
                triangles.Add(new VertexPositionColor(nextPoint, Color.Lerp(Color.MidnightBlue, Color.Green, 0.915f) * 0.75f));
            }
            
            // Log.Warn(triangles.Count);
            
            // int currentAngle = 0;// + (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 10 % 360);
            // for (int i = 0; i < 6; i++)
            // {
            //     triangles.Add(new VertexPositionColor(center, Color.DarkOliveGreen * 0.9f));
            //     Vector3 point = PrimitiveNormalize(new Vector2(
            //         GetAgrometerCenter().X + radius * (float)Math.Cos(MathHelper.ToRadians(currentAngle)),
            //         GetAgrometerCenter().Y + radius * (float)Math.Sin(MathHelper.ToRadians(currentAngle))
            //     ));
            //     triangles.Add(new VertexPositionColor(point, Color.Lerp(i % 2 == 0 ? Color.MidnightBlue * 0.75f : Color.MidnightBlue, Color.Green, 0.15f) * 0.75f));
            //     currentAngle -= 60;
            //     Vector3 nextPoint = PrimitiveNormalize(new Vector2(
            //         GetAgrometerCenter().X + radius * (float)Math.Cos(MathHelper.ToRadians(currentAngle)),
            //         GetAgrometerCenter().Y + radius * (float)Math.Sin(MathHelper.ToRadians(currentAngle))
            //     ));
            //     triangles.Add(new VertexPositionColor(nextPoint, Color.Lerp(i % 2 == 0 ? Color.MidnightBlue * 0.75f : Color.MidnightBlue, Color.Green, 0.15f) * 0.75f));
            // }
            
            Rectangle itemSlotSourceRect = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10);
            
            float ItemSlotScale = 1.25f;
            Vector2 itemSlot_BL = GetAgrometerCenter() - new Vector2(itemSlotSourceRect.Width / 2f, itemSlotSourceRect.Height / 2f) * ItemSlotScale;
            Vector2 itemSlot_BR = GetAgrometerCenter() + new Vector2(itemSlotSourceRect.Width / 2f, -itemSlotSourceRect.Height / 2f) * ItemSlotScale;
            Vector2 itemSlot_TL = GetAgrometerCenter() + new Vector2(-itemSlotSourceRect.Width / 2f, itemSlotSourceRect.Height / 2f) * ItemSlotScale;
            Vector2 itemSlot_TR = GetAgrometerCenter() + new Vector2(itemSlotSourceRect.Width / 2f, itemSlotSourceRect.Height / 2f) * ItemSlotScale;
            
            // Yield Essence Triangle
            {
                Vector3 pointOne = PrimitiveNormalize(itemSlot_TL);
                Vector3 pointTwo = pointsAroundAgrometer[1];
                Vector3 pointThree = PrimitiveNormalize(itemSlot_BR);
                triangles.Add(new VertexPositionColor(pointOne, Color.Red));
                triangles.Add(new VertexPositionColor(pointTwo, Color.Blue));
                triangles.Add(new VertexPositionColor(pointThree, Color.Red));
            }
            
            // Quality Essence Triangle
            {
                Vector3 pointOne = PrimitiveNormalize(itemSlot_BL);
                Vector3 pointTwo = PrimitiveNormalize(itemSlot_TR);
                Vector3 pointThree = pointsAroundAgrometer[^1];
                triangles.Add(new VertexPositionColor(pointOne, Color.Red));
                triangles.Add(new VertexPositionColor(pointTwo, Color.Red));
                triangles.Add(new VertexPositionColor(pointThree, Color.Blue));
            }
            
            // Growth Essence Triangle
            {
                Vector3 pointOne = PrimitiveNormalize(itemSlot_TR);
                Vector3 pointTwo = pointsAroundAgrometer[0];
                Vector3 pointThree = PrimitiveNormalize(itemSlot_BR);
                triangles.Add(new VertexPositionColor(pointOne, Color.Red));
                triangles.Add(new VertexPositionColor(pointTwo, Color.Blue));
                triangles.Add(new VertexPositionColor(pointThree, Color.Red));
            }
            
            // Giant Essence Triangle
            {
                Vector3 pointOne = PrimitiveNormalize(itemSlot_BL);
                Vector3 pointTwo = pointsAroundAgrometer[5];
                Vector3 pointThree = PrimitiveNormalize(itemSlot_TR);
                triangles.Add(new VertexPositionColor(pointOne, Color.Red));
                triangles.Add(new VertexPositionColor(pointTwo, Color.Blue));
                triangles.Add(new VertexPositionColor(pointThree, Color.Red));
            }
            
            // Water Essence Triangle
            {
                Vector3 pointOne = PrimitiveNormalize(itemSlot_TL);
                Vector3 pointTwo = PrimitiveNormalize(itemSlot_BR);
                Vector3 pointThree = pointsAroundAgrometer[^5];
                triangles.Add(new VertexPositionColor(pointOne, Color.Red));
                triangles.Add(new VertexPositionColor(pointTwo, Color.Red));
                triangles.Add(new VertexPositionColor(pointThree, Color.Blue));
            }
            
            // Seed Essence Triangle
            {
                Vector3 pointOne = PrimitiveNormalize(itemSlot_BL);
                Vector3 pointTwo = pointsAroundAgrometer[6];
                Vector3 pointThree = PrimitiveNormalize(itemSlot_TL);
                triangles.Add(new VertexPositionColor(pointOne, Color.Red));
                triangles.Add(new VertexPositionColor(pointTwo, Color.Blue));
                triangles.Add(new VertexPositionColor(pointThree, Color.Red));
            }
            
            triangles.AddRange(triangles.AsEnumerable().Reverse().Select(vertex => new VertexPositionColor(vertex.Position, Color.Magenta * 0.75f)));
            
            // triangles.Add(new VertexPositionColor(PrimitiveNormalize(itemSlot_TL), Color.MidnightBlue * 0.5f));
            // triangles.Add(new VertexPositionColor(PrimitiveNormalize(itemSlot_BL), Color.Red * 0.5f));
            // triangles.Add(new VertexPositionColor(PrimitiveNormalize(itemSlot_BR), Color.Green * 0.5f));
            // triangles.Add(new VertexPositionColor(PrimitiveNormalize(itemSlot_TL), Color.Yellow * 0.5f));
            // triangles.Add(new VertexPositionColor(PrimitiveNormalize(itemSlot_BR), Color.Orange * 0.5f));
            // triangles.Add(new VertexPositionColor(PrimitiveNormalize(itemSlot_TR), Color.Pink * 0.5f));

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                b.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangles.ToArray(), 0, triangles.Count / 3);
            }
        }
        b.End();
    }

    private Vector2 GetAgrometerCenter()
    {
        float x = Game1.viewport.Width / 2f;
        float y = Game1.viewport.Height / 2f;
        return new Vector2(x, y);
    }

    private Vector2 GetAgrometerScale()
    {
        float scale = (Game1.viewport.Height / 1.25f) / agrometerBackground.Height;
        return new Vector2(scale, scale);
    }

    public override void drawBackground(SpriteBatch b)
    {
        // base.drawBackground(b);
        b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
            Color.Blue * 0.92f);
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