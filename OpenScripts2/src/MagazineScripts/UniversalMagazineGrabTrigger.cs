using UnityEngine;
using FistVR;
using System;

namespace OpenScripts2
{
    [Obsolete("Use Vanilla UniversalMagGrabTrigger instead!")]
    public class UniversalMagazineGrabTrigger : FVRInteractiveObject
	{
		public FVRFireArm FireArm;
		public bool IsSecondarySlotGrab;
		public int SecondaryGrabSlot;

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
			if (!IsSecondarySlotGrab && FireArm.Magazine != null)
			{
				EndInteraction(hand);
				FVRFireArmMagazine magazine = FireArm.Magazine;
				FireArm.EjectMag(false);
				hand.ForceSetInteractable(magazine);
				magazine.BeginInteraction(hand);
			}
			else if (IsSecondarySlotGrab && FireArm.SecondaryMagazineSlots[SecondaryGrabSlot].Magazine != null)
			{
				EndInteraction(hand);
				FVRFireArmMagazine magazine2 = FireArm.SecondaryMagazineSlots[SecondaryGrabSlot].Magazine;
				FireArm.EjectSecondaryMagFromSlot(SecondaryGrabSlot, false);
				hand.ForceSetInteractable(magazine2);
				magazine2.BeginInteraction(hand);
			}
		}
	}
}