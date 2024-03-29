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
    public class MountedGunControls_OpenBolt : OpenScripts2_BasePlugin
	{
        public wwGatlingControlHandle ControlHandle;
        public OpenBoltReceiver OpenBoltWeapon;


		private FVRPhysicalObject _mount;
        private bool _hooked = false;
		private FVRViveHand _hand = null;


		public void Start()
        {
			Hook();

			_mount = ControlHandle.BaseFrame.gameObject.GetComponent<FVRPhysicalObject>();
        }

		public void OnDestroy()
        {
			Unhook();
        }
        public void Update()
        {
			if(_hand != ControlHandle.m_hand) _hand = ControlHandle.m_hand;

			if (_hand != null && !_hooked)
            {
				OpenBoltWeapon.m_hand = _hand;
				OpenBoltWeapon.m_hasTriggeredUpSinceBegin = true;

                _hooked = true;
            }
            else if (_hand == null && _hooked)
            {
				OpenBoltWeapon.m_hand = _hand;
				OpenBoltWeapon.m_hasTriggeredUpSinceBegin = false;

                _hooked = false;
            }
        }

        private void Hook()
        {
#if !DEBUG
            On.FistVR.OpenBoltReceiver.UpdateControls += OpenBoltReceiver_UpdateControls;
            On.FistVR.FVRFireArmMagazine.Release += FVRFireArmMagazine_Release;
#endif
        }

        private void Unhook()
        {
#if !DEBUG
            On.FistVR.OpenBoltReceiver.UpdateControls -= OpenBoltReceiver_UpdateControls;
			On.FistVR.FVRFireArmMagazine.Release -= FVRFireArmMagazine_Release;
#endif
		}
#if !DEBUG
        private void OpenBoltReceiver_UpdateControls(On.FistVR.OpenBoltReceiver.orig_UpdateControls orig, OpenBoltReceiver self)
        {
			if (self == OpenBoltWeapon)
			{
				if (ControlHandle.IsHeld)
				{
					if (self.HasTriggerButton && self.m_hasTriggeredUpSinceBegin && !self.IsAltHeld && self.FireSelector_Modes[self.m_fireSelectorMode].ModeType != OpenBoltReceiver.FireSelectorModeType.Safe)
					{
						self.m_triggerFloat = ControlHandle.m_hand.Input.TriggerFloat;
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
						if (ControlHandle.m_hand.IsInStreamlinedMode)
						{
							if (ControlHandle.m_hand.Input.BYButtonDown && self.HasFireSelectorButton)
							{
								self.ToggleFireSelector();
							}
							if (ControlHandle.m_hand.Input.AXButtonDown && self.HasMagReleaseButton)
							{
								self.EjectMag();
							}
						}
						else if (ControlHandle.m_hand.Input.TouchpadDown && ControlHandle.m_hand.Input.TouchpadAxes.magnitude > 0.1f)
						{
							if (self.HasFireSelectorButton && Vector2.Angle(ControlHandle.m_hand.Input.TouchpadAxes, Vector2.left) <= 45f)
							{
								self.ToggleFireSelector();
							}
							else if (self.HasMagReleaseButton && Vector2.Angle(ControlHandle.m_hand.Input.TouchpadAxes, Vector2.down) <= 45f)
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
#endif
	}
}
