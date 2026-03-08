using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Agromancy.Helpers;
using Agromancy.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace Agromancy.Menus;

public partial class AgrometerMenu : IClickableMenu
{
    private Tool Agrometer;
    private Item? EssenceVial;

    private bool shouldAllowClick = false;

    private Texture2D agrometerFrame;
    private Texture2D agrometerCircles;
    private Texture2D agrometerStatRing;

    private int itemListOffset = 0;

    public Dictionary<int, bool> EssencesBeingDrained = new();
    public bool IsCropBeingDrained = false;

    public int[] drainParticleCooldown = new int[6];
    public int unsuccessfulDrainCooldown = 0;
    public int extractAllCooldown = 0;

    private int timeDraining = 0;

    private bool isCropSuckedDry => GetCurrentlySelectedCropEssences() is { } essences && EssenceCalculator.PercentToPerfectCrop(essences) <= 0;
    private int timeSinceSuckingDry = 0;

    private bool isExtractMode = true;

    public Random rng = new();

    private Texture2D ArrowsTexture => Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/MonochromeArrows");
    private Rectangle UpArrowSourceRect => new(0, 0, 11, 12);
    private Rectangle DownArrowSourceRect => new(11, 0, 11, 12);
    private Rectangle LeftArrowSourceRect => new(22, 0, 12, 12);
    private Rectangle RightArrowSourceRect => new(34, 0, 12, 12);

    public ClickableTextureComponent UpArrow;
    public ClickableTextureComponent DownArrow;
    
    private Texture2D EssenceIconSheet => Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/EssenceIcons");
    private List<ClickableTextureComponent> EssenceIcons = new();

    Dictionary<Item, int> agromancyCrops => GetItemsWithAgromancyData();

    public AgrometerMenu(Tool agrometer)
    {
        Agrometer = agrometer;
        EssenceVial = GetEssenceVial();

        agrometerFrame = Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/AgrometerFrame");
        agrometerCircles = Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/AgrometerCircles");
        agrometerStatRing = Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/AgrometerStatRing");

        for (int i = 0; i < 6; i++)
        {
            Rectangle sourceRect = GetEssenceIconSourceRect(i);
            float scale = GetAgrometerScale().X * 0.75f;
            Vector2 position = GetEssenceCenter(i) - new Vector2((sourceRect.Width * scale) / 2f, (sourceRect.Height * scale) / 2f);
            Rectangle destRect = new Rectangle((int)position.X, (int)position.Y, (int)(sourceRect.Width * scale), (int)(sourceRect.Height * scale));
            ClickableTextureComponent icon = new ClickableTextureComponent(
                name: $"EssenceIcon_{i}",
                bounds: destRect,
                label: null,
                hoverText: null,
                texture: EssenceIconSheet,
                sourceRect: sourceRect,
                scale: GetAgrometerScale().X * 0.75f
            );
            EssenceIcons.Add(icon);
        }

        Rectangle upArrowLocation = new Rectangle(
            x: (int)(GetAgrometerCenter().X - 2 - (UpArrowSourceRect.Width) * GetAgrometerScale().X),
            y: (int)(GetAgrometerCenter().Y - (agrometerFrame.Height / 3f) * GetAgrometerScale().Y -
                     (UpArrowSourceRect.Height) * GetAgrometerScale().Y),
            width: (int)(UpArrowSourceRect.Width * GetAgrometerScale().X * 2f),
            height: (int)(UpArrowSourceRect.Height * GetAgrometerScale().Y * 2f)
        );

        Rectangle downArrowLocation = new Rectangle(
            (int)(GetAgrometerCenter().X - 2 - (DownArrowSourceRect.Width) * GetAgrometerScale().X),
            (int)(GetAgrometerCenter().Y + (agrometerFrame.Height / 3f) * GetAgrometerScale().Y -
                  (DownArrowSourceRect.Height) * GetAgrometerScale().Y),
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

    public override void populateClickableComponentList()
    {
        base.populateClickableComponentList();
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
        shouldUpdateArrows = true;
    }

    private void drainAllEssences()
    {
        bool didDrainAny = false;
        for (int i = 0; i < EssencesBeingDrained.Count; i++)
        {
            EssencesBeingDrained[i] = true;
            var didDrain = drainEssence(i);
            if (didDrain)
            {
                didDrainAny = true;
                createParticleFromDraining(i, GetEssenceCenter(i), silent: true, fromVial: !isExtractMode);
            }
        }

        if (!didDrainAny) cannotDrainEssenceFeedback();
        else if (extractAllCooldown <= 0)
        {
            Game1.playSound("boulderCrack", out var cue);
            cue.Pitch = 0.1f + (float)rng.NextDouble() * 0.35f;
            extractAllCooldown = 85;
        }
    }

    private bool drainEssence(int essenceIdx)
    {
        if (EssenceVial is null) return false;

        CropEssences? essences = GetCurrentlySelectedCropEssences();
        if (essences is null || EssenceCalculator.GetEssence(essences, essenceIdx) <= 0)
        {
            return false;
        }

        if (drainParticleCooldown[essenceIdx] > 0) return true;

        int currentEssence = EssenceCalculator.GetEssence(essences, essenceIdx);
        int amountToDrain = (int)MathHelper.Lerp(1, 25, MathHelper.Clamp(timeDraining / 5000f, 0f, 1f));
        
        int newEssenceAmount = currentEssence - amountToDrain;
        newEssenceAmount = Math.Max(0, newEssenceAmount);
        int essenceDiff = currentEssence - newEssenceAmount;

        float currentVialAmount = GetEssenceInVial(essenceIdx);
        float newVialAmount = currentVialAmount + essenceDiff * GetCurrentlySelectedCrop()!.Stack;
        EssenceVial.modData[$"{Agromancy.UNIQUE_ID}_{essenceIdx}"] = newVialAmount.ToString(CultureInfo.CurrentCulture);

        EssenceCalculator.SetEssence(essences, essenceIdx, newEssenceAmount);
        GetCurrentlySelectedCrop()!.ApplyEssences(essences);
        return true;
    }

    private bool infuseEssence(int essenceIdx)
    {
        if (EssenceVial is null) return false;

        CropEssences? essences = GetCurrentlySelectedCropEssences();
        if (essences is null || EssenceCalculator.GetEssence(essences, essenceIdx) >= 255)
        {
            return false;
        }

        if (drainParticleCooldown[essenceIdx] > 0) return true;
        
        float currentEssence = GetEssenceInVial(essenceIdx);
        int amountToInfuse = (int)MathHelper.Lerp(1, 25, MathHelper.Clamp(timeDraining / 5000f, 0f, 1f)) * GetCurrentlySelectedCrop()!.Stack;
        
        float newVialAmount = currentEssence - amountToInfuse;
        newVialAmount = Math.Max(0, newVialAmount);
        float essenceDiff = currentEssence - newVialAmount;
        
        if (essenceDiff / GetCurrentlySelectedCrop()!.Stack <= 0) return false;
        
        int newEssenceAmount = EssenceCalculator.GetEssence(essences, essenceIdx) + (int)(essenceDiff / GetCurrentlySelectedCrop()!.Stack);
        newEssenceAmount = Math.Min(255, newEssenceAmount);
        
        EssenceVial.modData[$"{Agromancy.UNIQUE_ID}_{essenceIdx}"] = newVialAmount.ToString(CultureInfo.CurrentCulture);
        EssenceCalculator.SetEssence(essences, essenceIdx, newEssenceAmount);
        GetCurrentlySelectedCrop()!.ApplyEssences(essences);
        return true;
    }
    
    private void infuseAllEssences()
    {
        bool didInfuseAny = false;
        for (int i = 0; i < EssencesBeingDrained.Count; i++)
        {
            EssencesBeingDrained[i] = true;
            var didInfuse = infuseEssence(i);
            if (didInfuse)
            {
                didInfuseAny = true;
                createParticleFromDraining(i, GetEssenceCenter(i), silent: true, fromVial: !isExtractMode);
            }
        }

        if (!didInfuseAny) cannotDrainEssenceFeedback();
        else if (extractAllCooldown <= 0)
        {
            Game1.playSound("cavedrip", out var cue);
            cue.Pitch = 0.5f + (float)rng.NextDouble() * 0.35f;
            extractAllCooldown = 85;
        }
    }

    private void cannotDrainEssenceFeedback()
    {
        if (unsuccessfulDrainCooldown > 0) return;

        Game1.playSound("cancel");
        unsuccessfulDrainCooldown = 1000;
    }

    private void ScrollItem(int direction)
    {
        itemListOffset = (itemListOffset + direction + agromancyCrops.Count) % Math.Max(1, agromancyCrops.Count);
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
        return base.readyToClose() && !IsCropBeingDrained && !isCropSuckedDry;
    }
}