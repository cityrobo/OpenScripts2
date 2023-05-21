using UnityEngine;
using FistVR;
using System.Collections;

namespace OpenScripts2
{
	public class UniversalAdvancedMagazineGrabTrigger : FVRInteractiveObject
	{
		public FVRFireArm FireArm;
		public enum E_InputType 
		{
            Vanilla,
            TouchpadUp_BYButton,
            TouchpadDown_AXButton,
			MainHandMagazineReleaseButton
        }
		public E_InputType RequiredInput;
		public bool IsSecondarySlotGrab;
		public int SecondaryGrabSlot;

        public float MagazineReleaseButtonReleaseDelay = 0.5f;
        private FVRFireArmMagazine _currentMagazine;

		public override bool IsInteractable()
		{
			if (!IsSecondarySlotGrab)
			{
				return FireArm.Magazine != null;
			}
			return FireArm.SecondaryMagazineSlots[SecondaryGrabSlot].Magazine != null;
		}

		public override void BeginInteraction(FVRViveHand hand)
		{
			base.BeginInteraction(hand);
            _currentMagazine = IsSecondarySlotGrab ? FireArm.SecondaryMagazineSlots[SecondaryGrabSlot].Magazine : FireArm.Magazine;
            if (OpenScripts2_BepInExPlugin.AdvancedMagGrabSimpleMagRelease.Value || RequiredInput == E_InputType.Vanilla)
			{
                StartCoroutine(MoveMagReleaseButton());
                if (!IsSecondarySlotGrab && FireArm.Magazine != null)
				{
					EndInteraction(hand);
					FireArm.EjectMag(false);
					hand.ForceSetInteractable(_currentMagazine);
                    _currentMagazine.BeginInteraction(hand);
				}
				else if (IsSecondarySlotGrab && FireArm.SecondaryMagazineSlots[SecondaryGrabSlot].Magazine != null)
				{
					EndInteraction(hand);
					FireArm.EjectSecondaryMagFromSlot(SecondaryGrabSlot, false);
					hand.ForceSetInteractable(_currentMagazine);
                    _currentMagazine.BeginInteraction(hand);
				}
			}
		}

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);

            switch (RequiredInput)
            {
                case E_InputType.TouchpadUp_BYButton:
                    if (!hand.IsInStreamlinedMode && OpenScripts2_BasePlugin.TouchpadDirPressed(hand, Vector2.up) || hand.IsInStreamlinedMode && hand.Input.BYButtonDown)
                    {
                        StartCoroutine(MoveMagReleaseButton());
                        if (!IsSecondarySlotGrab && FireArm.Magazine != null)
                        {
                            EndInteraction(hand);
                            FireArm.EjectMag(false);
                            hand.ForceSetInteractable(_currentMagazine);
                            _currentMagazine.BeginInteraction(hand);
                        }
                        else if (IsSecondarySlotGrab && FireArm.SecondaryMagazineSlots[SecondaryGrabSlot].Magazine != null)
                        {
                            EndInteraction(hand);
                            FireArm.EjectSecondaryMagFromSlot(SecondaryGrabSlot, false);
                            hand.ForceSetInteractable(_currentMagazine);
                            _currentMagazine.BeginInteraction(hand);
                        }
                    }
                    break;
                case E_InputType.TouchpadDown_AXButton:
                    if (!hand.IsInStreamlinedMode && OpenScripts2_BasePlugin.TouchpadDirPressed(hand, Vector2.down) || hand.IsInStreamlinedMode && hand.Input.AXButtonDown)
                    {
                        StartCoroutine(MoveMagReleaseButton());
                        if (!IsSecondarySlotGrab && FireArm.Magazine != null)
                        {
                            EndInteraction(hand);
                            FireArm.EjectMag(false);
                            hand.ForceSetInteractable(_currentMagazine);
                            _currentMagazine.BeginInteraction(hand);
                        }
                        else if (IsSecondarySlotGrab && FireArm.SecondaryMagazineSlots[SecondaryGrabSlot].Magazine != null)
                        {
                            EndInteraction(hand);
                            FireArm.EjectSecondaryMagFromSlot(SecondaryGrabSlot, false);
                            hand.ForceSetInteractable(_currentMagazine);
                            _currentMagazine.BeginInteraction(hand);
                        }
                    }
                    break;
            }
            if (FireArm.Magazine == null)
            {
                EndInteraction(hand);
                hand.ForceSetInteractable(_currentMagazine);
                _currentMagazine.BeginInteraction(hand);
            }
        }

        private IEnumerator MoveMagReleaseButton()
        {
            switch (FireArm)
            {
                case ClosedBoltWeapon w:
                    w.SetAnimatedComponent(w.MagazineReleaseButton, w.MagReleasePressed, w.MagReleaseInterp);
                    break;
                case Handgun w:
                    w.SetAnimatedComponent(w.MagazineReleaseButton, w.MagReleasePressed, w.MagReleaseInterp, w.MagReleaseAxis);
                    break;
            }
            yield return new WaitForSeconds(MagazineReleaseButtonReleaseDelay);
            switch (FireArm)
            {
                case ClosedBoltWeapon w:
                    w.SetAnimatedComponent(w.MagazineReleaseButton, w.MagReleaseUnpressed, w.MagReleaseInterp);
                    break;
                case Handgun w:
                    w.SetAnimatedComponent(w.MagazineReleaseButton, w.MagReleaseUnpressed, w.MagReleaseInterp, w.MagReleaseAxis);
                    break;
            }
        }
    }
}