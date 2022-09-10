using UnityEngine;
using FistVR;

namespace OpenScripts2
{
	public class UniversalMagazineGrabTrigger : FVRInteractiveObject
	{
		public FVRFireArm FireArm;

		public override bool IsInteractable()
		{
			return FireArm.Magazine != null;
		}

		public override void BeginInteraction(FVRViveHand hand)
		{
			base.BeginInteraction(hand);
			if (FireArm.Magazine != null)
			{
				EndInteraction(hand);
				FVRFireArmMagazine magazine = FireArm.Magazine;
				FireArm.EjectMag(false);
				hand.ForceSetInteractable(magazine);
				magazine.BeginInteraction(hand);
			}
		}
		/*
		public override void UpdateInteraction(FVRViveHand hand)
		{
			base.UpdateInteraction(hand);
			if (hand.Input.TouchpadDown && FireArm.Magazine != null)
			{
				EndInteraction(hand);
				FVRFireArmMagazine magazine = FireArm.Magazine;
				FireArm.EjectMag(false);
				hand.ForceSetInteractable(magazine);
				magazine.BeginInteraction(hand);
			}
		}
		*/
	}
}
