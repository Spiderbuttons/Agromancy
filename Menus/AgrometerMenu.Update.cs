using System;
using System.Linq;
using Agromancy.Helpers;
using Agromancy.Models;
using Microsoft.Xna.Framework;
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
    
    public override void update(GameTime time)
    {
        MillisecondsMenuHasBeenOpen += time.ElapsedGameTime.Milliseconds;
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
        
        updateTotalEssencePercent();
        updateEssencePercents();
        updateArrows();
        updateHoveredEssence();
        updateParticles();
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
                currentEssencePct[i] = MathHelper.Lerp(currentEssencePct[i], targetEssencePct[i], 0.075f);
            }
        }
    }
}