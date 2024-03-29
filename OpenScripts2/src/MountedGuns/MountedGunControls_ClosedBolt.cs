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
    public class MountedGunControls_ClosedBolt : OpenScripts2_BasePlugin
	{
        public wwGatlingControlHandle ControlHandle;
        public ClosedBoltWeapon ClosedBoltWeapon;

		private FVRPhysicalObject _mount;
        private bool _hooked = false;
		private FVRViveHand _hand;


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
			if (_hand != ControlHandle.m_hand) _hand = ControlHandle.m_hand;

			if (_hand != null && !_hooked)
            {
				ClosedBoltWeapon.m_hand = _hand;
				ClosedBoltWeapon.m_hasTriggeredUpSinceBegin = true;

                _hooked = true;
            }
            else if (_hand == null && _hooked)
            {
				ClosedBoltWeapon.m_hand = _hand;
				ClosedBoltWeapon.m_hasTriggeredUpSinceBegin = false;

                _hooked = false;
            }

			if (_hand != null) ClosedBoltWeapon.UpdateInputAndAnimate(_hand);
		}

        private void Hook()
        {
#if !DEBUG
			On.FistVR.ClosedBoltWeapon.UpdateInputAndAnimate += ClosedBoltWeapon_UpdateInputAndAnimate;
			On.FistVR.FVRFireArmMagazine.Release += FVRFireArmMagazine_Release;
#endif
        }

        private void Unhook()
        {
#if !DEBUG
			On.FistVR.ClosedBoltWeapon.UpdateInputAndAnimate -= ClosedBoltWeapon_UpdateInputAndAnimate;
			On.FistVR.FVRFireArmMagazine.Release -= FVRFireArmMagazine_Release;
#endif
		}
#if !DEBUG

		private void ClosedBoltWeapon_UpdateInputAndAnimate(On.FistVR.ClosedBoltWeapon.orig_UpdateInputAndAnimate orig, ClosedBoltWeapon self, FVRViveHand hand)
		{
			if (self == ClosedBoltWeapon)
			{
				self.IsBoltReleaseButtonHeld = false;
				self.IsBoltCatchButtonHeld = false;

				if (hand != null)
				{
					self.m_triggerFloat = this._hand.Input.TriggerFloat;
				}
				else
				{
					self.m_triggerFloat = 0f;
					return;
				}
				if (!self.m_hasTriggerReset && self.m_triggerFloat <= self.TriggerResetThreshold)
				{
					//Debug.Log("Trigger Reset!");
					self.m_hasTriggerReset = true;
					if (self.FireSelector_Modes.Length > 0)
					{
						self.m_CamBurst = self.FireSelector_Modes[self.m_fireSelectorMode].BurstAmount;
					}
					self.PlayAudioEvent(FirearmAudioEventType.TriggerReset, 1f);
				}
				if (this._hand.IsInStreamlinedMode)
				{
					if (this._hand.Input.BYButtonDown)
					{
						if (self.Bolt.IsBoltLocked() && self.HasBoltReleaseButton)
						{
							self.Bolt.ReleaseBolt();
						}
						else
						{
							self.ToggleFireSelector();
						}
						if (self.UsesStickyDetonation)
						{
							self.Detonate();
						}
					}
					if (this._hand.Input.AXButtonDown && self.HasMagReleaseButton && (!self.EjectsMagazineOnEmpty || (self.Bolt.CurPos >= ClosedBolt.BoltPos.Locked && self.Bolt.IsHeld && !self.m_proxy.IsFull)))
					{
						self.ReleaseMag();
					}
					if (self.UsesStickyDetonation)
					{
						if (this._hand.Input.BYButtonDown)
						{
							self.SetAnimatedComponent(self.StickyTrigger, self.StickyRotRange.y, FVRPhysicalObject.InterpStyle.Rotation, FVRPhysicalObject.Axis.X);
						}
						else if (this._hand.Input.BYButtonUp)
						{
							self.SetAnimatedComponent(self.StickyTrigger, self.StickyRotRange.x, FVRPhysicalObject.InterpStyle.Rotation, FVRPhysicalObject.Axis.X);
						}
					}
				}
				else
				{
					Vector2 touchpadAxes = this._hand.Input.TouchpadAxes;
					if (this._hand.Input.TouchpadDown)
					{
						if (self.UsesStickyDetonation)
						{
							self.Detonate();
						}
						if (touchpadAxes.magnitude > 0.2f)
						{
							if (Vector2.Angle(touchpadAxes, Vector2.left) <= 45f)
							{
								self.ToggleFireSelector();
							}
							else if (Vector2.Angle(touchpadAxes, Vector2.up) <= 45f)
							{
								if (self.HasBoltReleaseButton)
								{
									self.Bolt.ReleaseBolt();
								}
							}
							else if (Vector2.Angle(touchpadAxes, Vector2.right) <= 45f && self.HasMagReleaseButton && (!self.EjectsMagazineOnEmpty || (self.Bolt.CurPos >= ClosedBolt.BoltPos.Locked && self.Bolt.IsHeld && !self.m_proxy.IsFull)))
							{
								self.ReleaseMag();
							}
						}
					}
					if (self.UsesStickyDetonation)
					{
						if (this._hand.Input.TouchpadDown)
						{
							self.SetAnimatedComponent(self.StickyTrigger, self.StickyRotRange.y, FVRPhysicalObject.InterpStyle.Rotation, FVRPhysicalObject.Axis.X);
						}
						else if (this._hand.Input.TouchpadUp)
						{
							self.SetAnimatedComponent(self.StickyTrigger, self.StickyRotRange.x, FVRPhysicalObject.InterpStyle.Rotation, FVRPhysicalObject.Axis.X);
						}
					}
					if (this._hand.Input.TouchpadPressed && touchpadAxes.magnitude > 0.2f)
					{
						if (Vector2.Angle(touchpadAxes, Vector2.up) <= 45f)
						{
							if (self.HasBoltReleaseButton)
							{
								self.IsBoltReleaseButtonHeld = true;
							}
						}
						else if (Vector2.Angle(touchpadAxes, Vector2.right) <= 45f && self.HasBoltCatchButton)
						{
							self.IsBoltCatchButtonHeld = true;
						}
					}
				}
				ClosedBoltWeapon.FireSelectorModeType modeType = self.FireSelector_Modes[self.m_fireSelectorMode].ModeType;
				if (modeType != ClosedBoltWeapon.FireSelectorModeType.Safe)
				{
					if (self.UsesStickyDetonation)
					{
						if (self.Bolt.CurPos == ClosedBolt.BoltPos.Forward && self.Chamber.IsFull && !self.Chamber.IsSpent)
						{
							if (this._hand.Input.TriggerPressed && self.m_hasTriggerReset)
							{
								self.m_hasStickTriggerDown = true;
								self.m_stickyChargeUp += Time.deltaTime * 0.25f * self.StickyChargeUpSpeed;
								self.m_stickyChargeUp = Mathf.Clamp(self.m_stickyChargeUp, 0f, 1f);
								if (self.m_stickyChargeUp > 0.05f && !self.m_chargeSound.isPlaying)
								{
									self.m_chargeSound.Play();
								}
							}
							else
							{
								if (self.m_chargeSound.isPlaying)
								{
									self.m_chargeSound.Stop();
								}
								self.m_stickyChargeUp = 0f;
							}
							if (self.m_hasStickTriggerDown && (this._hand.Input.TriggerUp || self.m_stickyChargeUp >= 1f))
							{
								self.m_hasStickTriggerDown = false;
								self.DropHammer();
								self.EndStickyCharge();
							}
						}
						return;
					}
					if (self.m_triggerFloat >= self.TriggerFiringThreshold && self.Bolt.CurPos == ClosedBolt.BoltPos.Forward && (self.m_hasTriggerReset || (modeType == ClosedBoltWeapon.FireSelectorModeType.FullAuto && !self.UsesDualStageFullAuto) || (modeType == ClosedBoltWeapon.FireSelectorModeType.FullAuto && self.UsesDualStageFullAuto && self.m_triggerFloat > self.TriggerDualStageThreshold) || (modeType == ClosedBoltWeapon.FireSelectorModeType.Burst && self.m_CamBurst > 0)))
					{
						//Debug.Log("Hammer Dropped!");
						self.DropHammer();
						self.m_hasTriggerReset = false;
						if (self.m_CamBurst > 0)
						{
							self.m_CamBurst--;
						}
					}
				}
			}
			else orig(self, hand);
		}


		private void FVRFireArmMagazine_Release(On.FistVR.FVRFireArmMagazine.orig_Release orig, FVRFireArmMagazine self, bool PhysicalRelease)
		{
			if (self.FireArm == ClosedBoltWeapon)
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
