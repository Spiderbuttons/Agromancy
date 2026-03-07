using System;
using System.Linq;
using Agromancy.Helpers;
using Agromancy.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;

namespace Agromancy.Menus;

public partial class AgrometerMenu
{
    private bool shouldUpdateArrows = false;
    
    private float targetTotalEssencePct = 0f;
    private float currentTotalEssencePct = 0f;
    
    private float[] targetEssencePct = new float[6];
    private float[] currentEssencePct = new float[6];
    
    private float[] targetEssenceScale = new float[6];
    private float[] currentEssenceScale = new float[6];
    
    private int suckedDryNoiseCooldown = 0;
    private bool alreadyCreatedSuckedDryParticles = false;

    private float targetMenuRotation = 0f;
    private float currentMenuRotation = 0f;
    private float rotationAcceleration = 0f;
    
    public override void update(GameTime time)
    {
        for (var index = 0; index < drainParticleCooldown.Length; index++)
        {
            var cooldown = drainParticleCooldown[index];
            cooldown -= time.ElapsedGameTime.Milliseconds;
            drainParticleCooldown[index] = cooldown;
        }

        unsuccessfulDrainCooldown -= time.ElapsedGameTime.Milliseconds;
        extractAllCooldown -= time.ElapsedGameTime.Milliseconds;
        if (IsCropBeingDrained) timeDraining += time.ElapsedGameTime.Milliseconds;
        else timeDraining = 0;
        
        if (isCropSuckedDry) timeSinceSuckingDry += time.ElapsedGameTime.Milliseconds;
        else timeSinceSuckingDry = 0;

        if (isCropSuckedDry && suckedDryNoiseCooldown <= 0 && timeSinceSuckingDry <= 2800f)
        {
            Game1.playSound("boulderCrack", out var cue);
            cue.Pitch = 0.1f + (float)rng.NextDouble() * 0.35f;
            suckedDryNoiseCooldown = 85;
        } else if (isCropSuckedDry)
        {
            suckedDryNoiseCooldown -= time.ElapsedGameTime.Milliseconds;
        }

        if (timeSinceSuckingDry >= 2800f && !alreadyCreatedSuckedDryParticles)
        {
            float radius = 25f;
            for (int i = 0; i < 12; i++)
            {
                double angle = rng.NextDouble() * Math.PI * 2;
                Vector2 startPosition = GetAgrometerCenter();
                Vector2 endPosition = GetAgrometerCenter() + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius * GetAgrometerScale().X;
                createParticle(startPosition, endPosition, Color.Lerp(Color.WhiteSmoke, GetEssenceColour(i % 6), 0.25f), GetAgrometerScale());
                alreadyCreatedSuckedDryParticles = true;
            }
        }

        if (timeSinceSuckingDry >= 4500f)
        {
            poofCurrentItem();
        }
        
        updateMenuRotation();
        updateTotalEssencePercent();
        updateEssencePercents();
        updateArrows();
        updateHoveredEssence();
        updateParticles();
    }

    public void updateMenuRotation()
    {
        currentMenuRotation += rotationAcceleration;
        if (currentMenuRotation < targetMenuRotation)
        {
            // This is supposed to slow the acceleration-changing down if we're close to 0.
            // But tbh I just kinda fucked around with numbers and lerp here and idk if that's actually what it's doing LOL
            float distanceToPositiveAcceleration = Math.Abs(0f - rotationAcceleration);
            rotationAcceleration = MathHelper.Lerp(rotationAcceleration, 15f, MathHelper.Lerp(0.025f, 0.015f, MathHelper.Lerp(0f, 1f, distanceToPositiveAcceleration / 15f)));
        } else
        {
            rotationAcceleration = MathHelper.Lerp(rotationAcceleration, -15f, 0.025f);
        }
        // Damping so we don't bounce back and forth when we overshoot the target endlessly.
        if (Math.Abs(currentMenuRotation - targetMenuRotation) < 30f)
        {
            rotationAcceleration *= 0.9f;
        }
        if (Math.Abs(currentMenuRotation - targetMenuRotation) < 0.75f && Math.Abs(rotationAcceleration) < 0.25f)
        {
            currentMenuRotation = targetMenuRotation;
            rotationAcceleration = 0f;
        }
    }

    private void updateHoveredEssence()
    {
        for (int i = 0; i < 6; i++)
        {
            if (currentEssenceScale[i] < targetEssenceScale[i])
            {
                currentEssenceScale[i] = MathHelper.SmoothStep(currentEssenceScale[i], targetEssenceScale[i], 0.25f);
            } else
            {
                currentEssenceScale[i] = MathHelper.Lerp(currentEssenceScale[i], targetEssenceScale[i], 0.1f);
            }
        }
    }

    private void updateArrows()
    {
        if (!shouldUpdateArrows) return;
        
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
        
        shouldUpdateArrows = false;
    }

    private void updateTotalEssencePercent()
    {
        CropEssences? essences = GetCurrentlySelectedCropEssences();
        
        float pct = essences is not null ? EssenceCalculator.PercentToPerfectCrop(essences) : 0f;
        targetTotalEssencePct = pct;
        if (currentTotalEssencePct < targetTotalEssencePct)
        {
            currentTotalEssencePct = MathHelper.SmoothStep(currentTotalEssencePct, targetTotalEssencePct, 0.15f);
        } else
        {
            currentTotalEssencePct = MathHelper.Lerp(currentTotalEssencePct, targetTotalEssencePct, 0.075f);
        }
    }

    private void updateEssencePercents()
    {
        CropEssences? essences = GetCurrentlySelectedCropEssences();
        
        for (int i = 0; i < targetEssencePct.Length; i++)
        {
            float pct = essences is not null ? EssenceCalculator.GetEssencePercent(essences, i) : 0f;
            targetEssencePct[i] = pct;
            float diffBetweenPercents = Math.Abs(currentEssencePct[i] - targetEssencePct[i]) / 100f;
            if (currentEssencePct[i] < targetEssencePct[i])
            {
                float lerpStrength = MathHelper.Lerp(0.15f, 0.3f, diffBetweenPercents);
                currentEssencePct[i] = MathHelper.SmoothStep(currentEssencePct[i], targetEssencePct[i], lerpStrength);
            } else
            {
                float lerpStrength = MathHelper.Lerp(0.075f, 0.15f, diffBetweenPercents);
                currentEssencePct[i] = MathHelper.Lerp(currentEssencePct[i], targetEssencePct[i], lerpStrength);
            }
        }
    }
}