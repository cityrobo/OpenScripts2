using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace OpenScripts2
{
    public class AttachableGunControls_OpenBolt : OpenScripts2_BasePlugin
	{
        public FVRInteractiveObject ControlHandle;
        public OpenBoltReceiver OpenBoltWeapon;
		public FVRFireArmAttachment Attachment;


		private FVRViveHand _hand = null;
		private FVRAlternateGrip _alternateGrip = null;

        public void Start()
        {
			Hook();
        }

		public void OnDestroy()
        {
			Unhook();
        }

        public void Update()
        {
            //if (hand != null) Debug.Log(hand);
        }

        private void Hook()
        {
#if !DEBUG
            On.FistVR.OpenBoltReceiver.UpdateControls += OpenBoltReceiver_UpdateControls;
            On.FistVR.FVRFireArmMagazine.Release += FVRFireArmMagazine_Release;
            On.FistVR.AttachableForegrip.BeginInteraction += AttachableForegrip_BeginInteraction;
            On.FistVR.FVRAlternateGrip.EndInteraction += FVRAlternateGrip_EndInteraction;
			On.FistVR.FVRFireArmAttachment.BeginInteraction += FVRFireArmAttachment_BeginInteraction;
			On.FistVR.FVRFireArmAttachment.EndInteraction += FVRFireArmAttachment_EndInteraction;
			On.FistVR.FVRPhysicalObject.EndInteractionIntoInventorySlot += FVRPhysicalObject_EndInteractionIntoInventorySlot;
#endif
		}

        private void Unhook()
        {
#if !DEBUG
            On.FistVR.OpenBoltReceiver.UpdateControls -= OpenBoltReceiver_UpdateControls;
			On.FistVR.FVRFireArmMagazine.Release -= FVRFireArmMagazine_Release;
			On.FistVR.AttachableForegrip.BeginInteraction -= AttachableForegrip_BeginInteraction;
			On.FistVR.FVRAlternateGrip.EndInteraction -= FVRAlternateGrip_EndInteraction;
			On.FistVR.FVRFireArmAttachment.BeginInteraction -= FVRFireArmAttachment_BeginInteraction;
			On.FistVR.FVRFireArmAttachment.EndInteraction -= FVRFireArmAttachment_EndInteraction;
			On.FistVR.FVRPhysicalObject.EndInteractionIntoInventorySlot -= FVRPhysicalObject_EndInteractionIntoInventorySlot;
#endif
		}
#if !DEBUG
        private void OpenBoltReceiver_UpdateControls(On.FistVR.OpenBoltReceiver.orig_UpdateControls orig, OpenBoltReceiver self)
        {
			if (self == OpenBoltWeapon)
			{
				if (_hand != null)
				{
					if (self.HasTriggerButton && self.FireSelector_Modes[self.m_fireSelectorMode].ModeType != OpenBoltReceiver.FireSelectorModeType.Safe)
					{
						self.m_triggerFloat = _hand.Input.TriggerFloat;
					}
					else
					{
						self.m_triggerFloat = 0f;
					}
					bool flag = false;
					if (self.Bolt.HasLastRoundBoltHoldOpen && self.Magazine != null && !self.Magazine.HasARound() && !self.Magazine.IsBeltBox)
					{
						flag = true;
					}
					if (!self.m_hasTriggerCycled)
					{
						if (self.m_triggerFloat >= self.TriggerFiringThreshold)
						{
							self.m_hasTriggerCycled = true;
							if (!flag)
							{
								self.ReleaseSeer();
							}
						}
					}
					else if (self.m_triggerFloat <= self.TriggerResetThreshold && self.m_hasTriggerCycled)
					{
						self.EngageSeer();
						self.m_hasTriggerCycled = false;
						self.PlayAudioEvent(FirearmAudioEventType.TriggerReset, 1f);
					}
					if (!self.IsAltHeld)
					{
						if (_hand.IsInStreamlinedMode)
						{
							if (_hand.Input.BYButtonDown && self.HasFireSelectorButton)
							{
								self.ToggleFireSelector();
							}
							if (_hand.Input.AXButtonDown && self.HasMagReleaseButton)
							{
								self.EjectMag();
							}
						}
						else if (_hand.Input.TouchpadDown && _hand.Input.TouchpadAxes.magnitude > 0.1f)
						{
							if (self.HasFireSelectorButton && Vector2.Angle(_hand.Input.TouchpadAxes, Vector2.left) <= 45f)
							{
								self.ToggleFireSelector();
							}
							else if (self.HasMagReleaseButton && Vector2.Angle(_hand.Input.TouchpadAxes, Vector2.right) <= 45f)
							{
								self.EjectMag();
							}
						}
					}
				}
				else
				{
					self.m_triggerFloat = 0f;
				}
			}
			else orig(self);

		}


		private void FVRFireArmMagazine_Release(On.FistVR.FVRFireArmMagazine.orig_Release orig, FVRFireArmMagazine self, bool PhysicalRelease)
		{
			if (self.FireArm == OpenBoltWeapon)
			{
				self.State = FVRFireArmMagazine.MagazineState.Free;
				self.SetParentage(null);
				if (self.UsesVizInterp)
				{
					self.m_vizLerpStartPos = self.Viz.transform.position;
					self.m_vizLerp = 0f;
					self.m_isVizLerpInward = false;
					self.m_isVizLerping = true;
				}
				if (self.FireArm.MagazineEjectPos != null)
				{
					self.transform.position = self.FireArm.GetMagEjectPos(self.IsBeltBox).position;
				}
				else
				{
					self.transform.position = self.FireArm.GetMagMountPos(self.IsBeltBox).position;
				}
				if (self.UsesVizInterp)
				{
					self.Viz.position = self.m_vizLerpStartPos;
					self.m_vizLerpReferenceTransform = self.FireArm.GetMagMountPos(self.IsBeltBox);
				}
				self.RecoverRigidbody();
				self.RootRigidbody.isKinematic = false;
				//self.RootRigidbody.velocity = mount.RootRigidbody.velocity - self.transform.up * self.EjectionSpeed;
				//self.RootRigidbody.angularVelocity = mount.RootRigidbody.angularVelocity;
				if (self.FireArm.m_hand != null && !PhysicalRelease)
				{
					FVRViveHand otherHand = self.FireArm.m_hand.OtherHand;
					if (otherHand.CurrentInteractable == null && otherHand.Input.IsGrabbing)
					{
						Vector3 position = otherHand.transform.position;
						if (GM.Options.ControlOptions.UseInvertedHandgunMagPose)
						{
							position = otherHand.GetMagPose().position;
						}
						Vector3 to = position - self.FireArm.GetMagMountPos(self.IsBeltBox).position;
						float num = Vector3.Distance(self.transform.position, position);
						if (num < 0.2f && Vector3.Angle(self.transform.up, to) > 90f)
						{
							otherHand.ForceSetInteractable(self);
							self.BeginInteraction(otherHand);
						}
					}
					else if (GM.Options.ControlOptions.MagPalming == ControlOptions.MagPalmingMode.Enabled && self.GetCanPalm() && otherHand.Input.IsGrabbing && otherHand.CurrentInteractable != null && otherHand.CurrentInteractable is FVRFireArmMagazine && (otherHand.CurrentInteractable as FVRFireArmMagazine).GetCanPalm() && (otherHand.CurrentInteractable as FVRFireArmMagazine).GetMagParent() == null && (otherHand.CurrentInteractable as FVRFireArmMagazine).GetMagChild() == null && (otherHand.CurrentInteractable as FVRFireArmMagazine).MagazineType == self.MagazineType && Vector3.Distance(otherHand.CurrentInteractable.transform.position, self.FireArm.GetMagMountPos(self.IsBeltBox).position) < 0.2f)
					{
						self.SetMagParent(otherHand.CurrentInteractable as FVRFireArmMagazine);
					}
				}
				self.FireArm = null;
				self.SetAllCollidersToLayer(false, "Default");
			}
			else orig(self, PhysicalRelease);
		}

		private void AttachableForegrip_BeginInteraction(On.FistVR.AttachableForegrip.orig_BeginInteraction orig, AttachableForegrip self, FVRViveHand hand)
		{
			if (self == ControlHandle)
			{
				FVRFireArm fvrfireArm = self.OverrideFirearm;
				if (fvrfireArm == null)
				{
					fvrfireArm = (self.Attachment.GetRootObject() as FVRFireArm);
				}
				if (fvrfireArm != null && fvrfireArm.Foregrip != null)
				{
					FVRAlternateGrip component = fvrfireArm.Foregrip.GetComponent<FVRAlternateGrip>();
					hand.ForceSetInteractable(component);
					component.BeginInteractionFromAttachedGrip(self, hand);
					this._hand = component.m_hand;
					this._alternateGrip = component;
				}
			}
			else orig(self, hand);
		}

		private void FVRAlternateGrip_EndInteraction(On.FistVR.FVRAlternateGrip.orig_EndInteraction orig, FVRAlternateGrip self, FVRViveHand hand)
		{
			if (this._alternateGrip == self)
			{
				this._hand = null;
				_alternateGrip = null;
			}
			orig(self, hand);
		}

		private void FVRFireArmAttachment_BeginInteraction(On.FistVR.FVRFireArmAttachment.orig_BeginInteraction orig, FVRFireArmAttachment self, FVRViveHand hand)
		{
			if (self == Attachment) this._hand = hand;
			orig(self, hand);
		}
		private void FVRFireArmAttachment_EndInteraction(On.FistVR.FVRFireArmAttachment.orig_EndInteraction orig, FVRFireArmAttachment self, FVRViveHand hand)
		{
			if (self == Attachment) this._hand = null;
			orig(self, hand);
		}
		private void FVRPhysicalObject_EndInteractionIntoInventorySlot(On.FistVR.FVRPhysicalObject.orig_EndInteractionIntoInventorySlot orig, FVRPhysicalObject self, FVRViveHand hand, FVRQuickBeltSlot slot)
		{
			if (self == Attachment) this._hand = null;
			orig(self, hand, slot);
		}
#endif
	}
}
