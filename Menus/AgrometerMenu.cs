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

    public int MillisecondsMenuHasBeenOpen;

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

    public Random rng = new();

    private Texture2D ArrowsTexture => Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/MonochromeArrows");
    private Rectangle UpArrowSourceRect => new(0, 0, 11, 12);
    private Rectangle DownArrowSourceRect => new(11, 0, 11, 12);
    private Rectangle LeftArrowSourceRect => new(22, 0, 12, 12);
    private Rectangle RightArrowSourceRect => new(34, 0, 12, 12);

    public ClickableTextureComponent UpArrow;
    public ClickableTextureComponent DownArrow;

    List<Item> agromancyCrops => GetItemsWithAgromancyData();

    public AgrometerMenu(Tool agrometer)
    {
        Agrometer = agrometer;
        EssenceVial = GetEssenceVial();
        MillisecondsMenuHasBeenOpen = Game1.currentGameTime.TotalGameTime.Milliseconds;

        agrometerFrame = Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/AgrometerFrame");
        agrometerCircles = Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/AgrometerCircles");
        agrometerStatRing = Game1.content.Load<Texture2D>($"{Agromancy.UNIQUE_ID}/AgrometerStatRing");

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

        EssenceCenters = GetEssenceCenters();
    }

    public override void receiveGamePadButton(Buttons button)
    {
        base.receiveGamePadButton(button);

        if (button is Buttons.DPadUp or Buttons.LeftThumbstickUp)
        {
            ScrollItem(-1);
        }
        else if (button is Buttons.DPadDown or Buttons.LeftThumbstickDown)
        {
            ScrollItem(1);
        }
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
        shouldUpdateArrows = true;
        EssenceCenters = GetEssenceCenters();
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
        shouldAllowClick = true;
        IsCropBeingDrained = false;
        for (int i = 0; i < EssencesBeingDrained.Count; i++)
        {
            EssencesBeingDrained[i] = false;
        }
    }

    public override void leftClickHeld(int x, int y)
    {
        if (!shouldAllowClick) return;

        base.leftClickHeld(x, y);
        bool foundDrainedEssence = false;
        foreach (var (essenceIdx, essenceCircle) in EssenceCenters)
        {
            if (PointInCircle(new Vector2(x, y), new Vector2(essenceCircle.X, essenceCircle.Y), essenceCircle.Z))
            {
                foundDrainedEssence = true;
                EssencesBeingDrained[essenceIdx] = true;
            }
            else EssencesBeingDrained[essenceIdx] = false;

            if (EssencesBeingDrained[essenceIdx])
            {
                bool didDrain = drainEssence(essenceIdx);
                if (didDrain)
                {
                    createParticleFromDraining(essenceIdx, essenceCircle);
                }
                else
                {
                    cannotDrainEssenceFeedback();
                }
            }
        }

        Vector2 extractAllPosition = GetExtractAllPosition();
        var extractAllRect = new Rectangle((int)(extractAllPosition.X - 32 * GetItemSlotScale(2).X * 0.75f),
            (int)(extractAllPosition.Y - 32 * GetItemSlotScale(2).X * 0.75f), (int)(64 * GetItemSlotScale(2).X * 0.75f),
            (int)(64 * GetItemSlotScale(2).X * 0.75f));
        if (extractAllRect.Contains(x, y))
        {
            foundDrainedEssence = true;
            drainAllEssences();
        }

        IsCropBeingDrained = foundDrainedEssence;
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
                createParticleFromDraining(i, EssenceCenters[i], silent: true);
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

        float currentVialAmount = EssenceVial.modData[$"{Agromancy.UNIQUE_ID}_{essenceIdx}"] is not { } s ? 0 : float.Parse(s);
        float newVialAmount = currentVialAmount + essenceDiff * GetCurrentlySelectedCrop()!.Stack;
        EssenceVial.modData[$"{Agromancy.UNIQUE_ID}_{essenceIdx}"] = newVialAmount.ToString(CultureInfo.CurrentCulture);

        EssenceCalculator.SetEssence(essences, essenceIdx, newEssenceAmount);
        GetCurrentlySelectedCrop()!.ApplyEssences(essences);
        return true;
    }

    private void cannotDrainEssenceFeedback()
    {
        if (unsuccessfulDrainCooldown > 0) return;

        Game1.playSound("cancel");
        unsuccessfulDrainCooldown = 1000;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        // base.receiveLeftClick(x, y, playSound);
        if (!shouldAllowClick) return;

        if (UpArrow.bounds.Contains(x, y) || DownArrow.bounds.Contains(x, y))
        {
            int direction = UpArrow.bounds.Contains(x, y) ? -1 : 1;
            ScrollItem(direction);
        }
    }

    private void ScrollItem(int direction)
    {
        itemListOffset = (itemListOffset + direction + agromancyCrops.Count) % Math.Max(1, agromancyCrops.Count);
    }

    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        base.receiveRightClick(x, y, playSound);
    }

    public override void receiveKeyPress(Keys key)
    {
        base.receiveKeyPress(key);
        // TODO: Gamepad support.
    }

    public override void gamePadButtonHeld(Buttons b)
    {
        base.gamePadButtonHeld(b);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int scrollDirection = direction > 0 ? -1 : 1;
        ScrollItem(scrollDirection);
    }

    public override void performHoverAction(int x, int y)
    {
        foreach (var (essenceIdx, essenceCircle) in EssenceCenters)
        {
            if (PointInCircle(new Vector2(x, y), new Vector2(essenceCircle.X, essenceCircle.Y), essenceCircle.Z))
            {
                targetEssenceScale[essenceIdx] = 1.15f;
            }
            else targetEssenceScale[essenceIdx] = 1f;
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