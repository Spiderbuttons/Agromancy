using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Agromancy.Menus;

public partial class AgrometerMenu
{
    private class Particle(Texture2D texture, Rectangle sourceRect, Color colour, Vector2 startPosition, Vector2 endPosition, Vector2 scale, float rotation = 0f)
    {
        private Texture2D texture = texture;
        private Rectangle sourceRect = sourceRect;
        private Color colour = colour;
        private Vector2 startPosition = startPosition;
        private Vector2 endPosition = endPosition;
        private Vector2 currentPosition = startPosition;
        private Vector2 scale = scale;
        private float rotation = rotation;

        public void update(GameTime time)
        {
            currentPosition = Vector2.Lerp(currentPosition, endPosition, 0.05f);
            if (Vector2.Distance(currentPosition, endPosition) < 0.1f) currentPosition = endPosition;
        }

        public void draw(SpriteBatch b)
        {
            b.Draw(
                texture: texture,
                position: currentPosition,
                sourceRectangle: sourceRect,
                color: colour * (Vector2.Distance(currentPosition, endPosition) / Vector2.Distance(startPosition, endPosition)),
                rotation: rotation,
                origin: new Vector2(sourceRect.Width / 2f, sourceRect.Height / 2f),
                scale: scale,
                effects: SpriteEffects.None,
                layerDepth: 1f
            );
        }

        public bool isFinished()
        {
            return Vector2.Distance(currentPosition, endPosition) < 0.1f;
        }
    }
    
    private List<Particle> drainParticles = [];
    
    private void drawParticles(SpriteBatch b)
    {
        foreach (var particle in drainParticles)
        {
            particle.draw(b);
        }
    }

    private void updateParticles()
    {
        for (int i = drainParticles.Count - 1; i >= 0; i--)
        {
            drainParticles[i].update(Game1.currentGameTime);
            if (drainParticles[i].isFinished())
            {
                drainParticles.RemoveAt(i);
            }
        }
    }
    
    private void createParticle(Vector2 startPosition, Vector2 endPosition, Color colour, Vector2 scale)
    {
        // Using the same puff of smoke from the WizardHouse cauldron.
        Texture2D particleTexture = Game1.content.Load<Texture2D>($"LooseSprites\\Cursors");
        Rectangle sourceRect = new Rectangle(372, 1956, 10, 10);
        float rotation = (float)rng.NextDouble() * MathHelper.TwoPi;
        drainParticles.Add(new Particle(particleTexture, sourceRect, colour, startPosition, endPosition, scale, rotation));
    }

    private void createParticleFromDraining(int essenceIdx, Vector3 essenceCircle, bool silent = false)
    {
        if (drainParticleCooldown[essenceIdx] > 0) return;
        
        createParticle(
            startPosition: new Vector2(essenceCircle.X, essenceCircle.Y),
            endPosition: GetEssenceVialSlotPosition(),
            colour: GetEssenceColour(essenceIdx),
            scale: new Vector2(1f, 1f) * GetAgrometerScale().X
        );
        if (!silent)
        {
            Game1.playSound("boulderCrack", out var cue);
            cue.Pitch = 0.1f + (float)rng.NextDouble() * 0.35f;
        }

        drainParticleCooldown[essenceIdx] = 85;
    }
}