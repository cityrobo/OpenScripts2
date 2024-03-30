using UnityEngine;
using FistVR;
using System.Collections;
using System.Reflection;
using System;

namespace OpenScripts2
{
	public class UniversalAdvancedMagazineGrabTrigger : FVRInteractiveObject
	{
        [Header("Universal Advanced Magazine Grab Trigger Config")]
		public FVRFireArm FireArm;
        public bool IsBeltBoxGrabTrigger;

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

        [Header("Secondary Magazine Release Button")]
        
        // public bool HasDedicatedMagazineReleaseButtonVisuals = false;
        [Tooltip("Optional magazine release button that will only be used when actuating the mag release from the grab trigger as supposed to pressing the mag release on the gun itself.")]
        public Transform SecondaryMagazineRelease;
        public OpenScripts2_BasePlugin.Axis SecondaryMagazineReleaseAxis;
        public OpenScripts2_BasePlugin.TransformType SecondaryMagazineReleaseInterpStyle;
        public float SecondaryMagazineReleaseReleased;
        public float SecondaryMagazineReleasePressed;

        [Header("Misc")]
        public bool AnimateMagReleaseOnMagEnter = true;
        public float MagButtonMagInsertPressDelay = 0.1f;
        public bool AllowExternalInputTypeModification = true;

        private FVRFireArmMagazine _currentMagazine;

        private FVRFireArmMagazine _lastMagazineInFireArm = null;

        public override void FVRUpdate()
        {
            base.FVRUpdate();
            if (AnimateMagReleaseOnMagEnter)
            {
                if (_lastMagazineInFireArm == null && FireArm.Magazine != null)
                {
                    _lastMagazineInFireArm = FireArm.Magazine;
                    StartCoroutine(MoveMagReleaseButton(MagButtonMagInsertPressDelay, true));
                }
                else if (_lastMagazineInFireArm != null && FireArm.Magazine == null)
                {
                    _lastMagazineInFireArm = null;
                }
            }
        }

        public override bool IsInteractable()
		{
			if (!IsSecondarySlotGrab)
			{
                return FireArm.Magazine != null && (FireArm.BeltDD == null || !FireArm.BeltDD.isBeltGrabbed()) && (FireArm.Magazine.IsBeltBox == IsBeltBoxGrabTrigger && (!FireArm.ConnectedToBox || FireArm.Magazine.CanBeTornOut));
            }
			return FireArm.SecondaryMagazineSlots[SecondaryGrabSlot].Magazine != null;
		}

		public override void BeginInteraction(FVRViveHand hand)
		{
            base.BeginInteraction(hand);
            _currentMagazine = IsSecondarySlotGrab ? FireArm.SecondaryMagazineSlots[SecondaryGrabSlot].Magazine : FireArm.Magazine;
            if (OpenScripts2_BepInExPlugin.AdvancedMagGrabSimpleMagRelease.Value || RequiredInput == E_InputType.Vanilla)
            {
                StartCoroutine(MoveMagReleaseButton(MagazineReleaseButtonReleaseDelay));
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

        public void SetRequiredInput(E_InputType newInputType)
        {
            if (AllowExternalInputTypeModification)
            {
                RequiredInput = newInputType;
            }
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);

            switch (RequiredInput)
            {
                case E_InputType.TouchpadUp_BYButton:
                    if (!hand.IsInStreamlinedMode && OpenScripts2_BasePlugin.TouchpadDirDown(hand, Vector2.up) || hand.IsInStreamlinedMode && hand.Input.BYButtonDown)
                    {
                        StartCoroutine(MoveMagReleaseButton(MagazineReleaseButtonReleaseDelay));
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
                    if (!hand.IsInStreamlinedMode && OpenScripts2_BasePlugin.TouchpadDirDown(hand, Vector2.down) || hand.IsInStreamlinedMode && hand.Input.AXButtonDown)
                    {
                        StartCoroutine(MoveMagReleaseButton(MagazineReleaseButtonReleaseDelay));
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
                case E_InputType.MainHandMagazineReleaseButton:
                    // See below!
                    break;
            }
            if (FireArm.Magazine == null)
            {
                EndInteraction(hand);
                hand.ForceSetInteractable(_currentMagazine);
                _currentMagazine.BeginInteraction(hand);
            }
        }

        private IEnumerator MoveMagReleaseButton(float delay, bool mainHand = false)
        {
            if (SecondaryMagazineRelease == null || mainHand)
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
                yield return new WaitForSeconds(delay);
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
            else
            {
                SecondaryMagazineRelease.ModifyLocalTransform(SecondaryMagazineReleaseInterpStyle, SecondaryMagazineReleaseAxis, SecondaryMagazineReleasePressed);
                yield return new WaitForSeconds(delay);
                SecondaryMagazineRelease.ModifyLocalTransform(SecondaryMagazineReleaseInterpStyle, SecondaryMagazineReleaseAxis, SecondaryMagazineReleaseReleased);
            }
        }
    }
}