using Agromancy.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace Agromancy.Menus;

public partial class AgrometerMenu
{
    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        // base.receiveLeftClick(x, y, playSound);
        if (!shouldAllowClick) return;

        if (agromancyCrops.Count > 1 && (UpArrow.bounds.Contains(x, y) || DownArrow.bounds.Contains(x, y)))
        {
            int direction = UpArrow.bounds.Contains(x, y) ? -1 : 1;
            ScrollItem(direction);
            Game1.playSound("drumkit6");
        }
        
        Vector2 rotateArrowPosition = GetRotateArrowPosition() - (RotateArrowSourceRect.Size.ToVector2() / 2f) * GetAgrometerScale() * 1.5f;
        Vector2 rotateArrowSize = RotateArrowSourceRect.Size.ToVector2() * GetAgrometerScale() * 1.5f;
        var rotateArrowRect = new Rectangle((int)rotateArrowPosition.X, (int)rotateArrowPosition.Y, (int)rotateArrowSize.X, (int)rotateArrowSize.Y);
        if (rotateArrowRect.Contains(x, y))
        {
            rotateMenu();
        }
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
        if (!shouldAllowClick || isCropSuckedDry) return;

        base.leftClickHeld(x, y);
        bool foundDrainedEssence = false;
        
        for (int i = 0; i < 6; i++)
        {
            Vector2 essenceCenter = GetEssenceCenter(i);
            float radius = GetEssenceContainerRadius() * 0.1f;
            if (IsPointInCircle(new Vector2(x, y), essenceCenter, radius))
            {
                foundDrainedEssence = true;
                EssencesBeingDrained[i] = true;
            }
            else EssencesBeingDrained[i] = false;

            if (EssencesBeingDrained[i])
            {
                bool didDrain = isExtractMode ? drainEssence(i) : infuseEssence(i);
                if (didDrain)
                {
                    createParticleFromDraining(i, essenceCenter, fromVial: !isExtractMode);
                }
                else
                {
                    cannotDrainEssenceFeedback();
                }
            }
        }

        Vector2 extractAllPosition = GetExtractAllPosition() - (ExtractAllButton.Bounds.Size.ToVector2() / 2f) * GetAgrometerScale() * 1.5f;
        Vector2 extractAllSize = ExtractAllButton.Bounds.Size.ToVector2() * GetAgrometerScale() * 1.5f;
        var extractAllRect = new Rectangle((int)extractAllPosition.X, (int)extractAllPosition.Y, (int)extractAllSize.X, (int)extractAllSize.Y);
        if (extractAllRect.Contains(x, y))
        {
            foundDrainedEssence = true;
            if (isExtractMode) drainAllEssences();
            else infuseAllEssences();
        }

        IsCropBeingDrained = foundDrainedEssence;
    }
    
    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        base.receiveRightClick(x, y, playSound);
    }

    public override void receiveKeyPress(Keys key)
    {
        base.receiveKeyPress(key);

        if (key is Keys.RightControl)
        {
            rotateMenu();
        }
        
        // TODO: Gamepad support.
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (agromancyCrops.Count <= 1) return;
        
        int scrollDirection = direction > 0 ? -1 : 1;
        ScrollItem(scrollDirection);
        Game1.playSound("drumkit6");
    }

    public override void performHoverAction(int x, int y)
    {
        for (int i = 0; i < 6; i++)
        {
            Vector2 center = GetEssenceCenter(i);
            float radius = GetEssenceContainerRadius() * 0.1f;
            float distanceFromMouse = Vector2.Distance(new Vector2(x, y), center);
            if (distanceFromMouse < radius)
            {
                targetEssenceScale[i] = 1.15f;
            }
            else targetEssenceScale[i] = 1f;
        }

        Vector2 rotateArrowPosition = GetRotateArrowPosition() - (RotateArrowSourceRect.Size.ToVector2() / 2f) * GetAgrometerScale() * 1.5f;
        Vector2 rotateArrowSize = RotateArrowSourceRect.Size.ToVector2() * GetAgrometerScale() * 1.5f;
        var rotateArrowRect = new Rectangle((int)rotateArrowPosition.X, (int)rotateArrowPosition.Y, (int)rotateArrowSize.X, (int)rotateArrowSize.Y);
        if (rotateArrowRect.Contains(x, y))
        {
            targetRotateArrowScale = 1.15f;
        }
        else targetRotateArrowScale = 1f;
        
        Vector2 essenceVialPosition = GetEssenceVialSlotPosition() - new Vector2(8, 8) * 4f * GetItemSlotScale(2) * 0.6f * 0.75f;
        Vector2 essenceVialSize = new Vector2(16, 16) * 4f * GetItemSlotScale(2) * 0.6f * 0.75f;
        var essenceVialRect = new Rectangle((int)essenceVialPosition.X, (int)essenceVialPosition.Y, (int)essenceVialSize.X, (int)essenceVialSize.Y);
        if (essenceVialRect.Contains(x, y))
        {
            shouldDrawVialTooltip = true;
        }
        else shouldDrawVialTooltip = false;
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
    
    public override void gamePadButtonHeld(Buttons b)
    {
        base.gamePadButtonHeld(b);
    }
    
    public override void applyMovementKey(int direction)
    {
        base.applyMovementKey(direction);
    }
}