using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using FistVR;
using static UnityEngine.UnityEngineExtensions;

namespace OpenScripts2
{
    public class HandgunSpin : OpenScripts2_BasePlugin
    {
        public Handgun Handgun;
		public Transform PoseSpinHolder;

		public ETouchpadDir TouchpadDir;

		private float _xSpinVel;
		private float _xSpinRot;

		private bool _isSpinning;
        private bool _spinningToggleState;

		private static readonly Dictionary<FVRFireArm, HandgunSpin> _existingHandGunSpins = new();

#if !DEBUG
		static HandgunSpin()
        {
            On.FistVR.FVRFireArm.BeginInteraction += FVRFireArm_BeginInteraction;
            On.FistVR.Handgun.UpdateInteraction += Handgun_UpdateInteraction;
            On.FistVR.FVRFireArm.EndInteraction += FVRFireArm_EndInteraction;
            On.FistVR.FVRFireArm.EndInteractionIntoInventorySlot += FVRFireArm_EndInteractionIntoInventorySlot;

            On.FistVR.FVRFireArm.FVRFixedUpdate += FVRFireArm_FVRFixedUpdate;
        }

        public void Awake()
        {
			_existingHandGunSpins.Add(Handgun, this);
		}

		public void OnDestroy()
        {
			_existingHandGunSpins.Remove(Handgun);
        }

        private static void FVRFireArm_BeginInteraction(On.FistVR.FVRFireArm.orig_BeginInteraction orig, FVRFireArm self, FVRViveHand hand)
        {
            if (_existingHandGunSpins.TryGetValue(self, out HandgunSpin handgunSpin))
            {
                if (OpenScripts2_BepInExPlugin.SpinToggle.Value)
                {
                    handgunSpin._isSpinning = handgunSpin._spinningToggleState;
                }
                else if (!OpenScripts2_BepInExPlugin.SpinToggle.Value && hand.Input.TouchpadPressed && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.up) < 45f)
                {
                    handgunSpin._isSpinning = true;
                    self.StopAllCoroutines();
                    self.StartCoroutine(SpinReleaseDelay(handgunSpin));
                }

                if (handgunSpin._isSpinning)
                {
                    Vector3 spinSpeed = self.transform.InverseTransformDirection(self.RootRigidbody.angularVelocity);

                    handgunSpin._xSpinVel = -spinSpeed.x * 10f;

                    handgunSpin._xSpinRot = Vector3Utils.SignedAngle(self.PoseOverride.forward, hand.Input.Forward, self.transform.right);
                }

                if (OpenScripts2_BepInExPlugin.SpinGrabHelper.Value && handgunSpin._isSpinning)
                {
                    ScaleColliderDown(self);
                }
            }
            orig(self, hand);
        }

        private static void Handgun_UpdateInteraction(On.FistVR.Handgun.orig_UpdateInteraction orig, Handgun self, FVRViveHand hand)
        {
            orig(self, hand);

            if (_existingHandGunSpins.TryGetValue(self, out HandgunSpin handgunSpin))
            {
                handgunSpin._isSpinning = TouchpadDirPressed(hand, handgunSpin.TouchpadDir.GetDir());

                if (!self.IsAltHeld && !hand.IsInStreamlinedMode)
                {
                    if (!OpenScripts2_BepInExPlugin.SpinToggle.Value && TouchpadDirPressed(hand, handgunSpin.TouchpadDir.GetDir()))
                    {
                        handgunSpin._isSpinning = true;
                        self.StopAllCoroutines();
                        self.StartCoroutine(SpinReleaseDelay(handgunSpin));
                    }

                    if (OpenScripts2_BepInExPlugin.SpinToggle.Value && TouchpadDirDown(hand, handgunSpin.TouchpadDir.GetDir()))
                    {
                        handgunSpin._spinningToggleState = !handgunSpin._spinningToggleState;
                    }

                    if (OpenScripts2_BepInExPlugin.SpinToggle.Value)
                    {
                        handgunSpin._isSpinning = handgunSpin._spinningToggleState;
                    }

                    if (handgunSpin._isSpinning) self.UseGrabPointChild = false;
                    else self.UseGrabPointChild = true;
                }
            }
        }

        private static void FVRFireArm_EndInteraction(On.FistVR.FVRFireArm.orig_EndInteraction orig, FVRFireArm self, FVRViveHand hand)
        {
            if (_existingHandGunSpins.TryGetValue(self, out HandgunSpin handgunSpin))
            {
                self.RootRigidbody.AddRelativeTorque(new Vector3(-handgunSpin._xSpinVel / 10f, 0f, 0f), ForceMode.VelocityChange);

                if (OpenScripts2_BepInExPlugin.SpinGrabHelper.Value && handgunSpin._isSpinning)
                {
                    ScaleColliderUp(self);
                }
            }

            orig(self,hand);
		}

        private static void FVRFireArm_EndInteractionIntoInventorySlot(On.FistVR.FVRFireArm.orig_EndInteractionIntoInventorySlot orig, FVRFireArm self, FVRViveHand hand, FVRQuickBeltSlot slot)
        {
            orig(self, hand, slot);

            if (_existingHandGunSpins.TryGetValue(self, out HandgunSpin handgunSpin))
            {
                handgunSpin._isSpinning = false;
            }
        }

        private static void FVRFireArm_FVRFixedUpdate(On.FistVR.FVRFireArm.orig_FVRFixedUpdate orig, FVRFireArm self)
        {
			orig(self);

            if (_existingHandGunSpins.TryGetValue(self, out HandgunSpin handgunSpin))
            {
                handgunSpin.UpdateSpinning();
            }
        }

        private void UpdateSpinning()
		{
			if (!Handgun.IsHeld || Handgun.IsAltHeld || Handgun.AltGrip != null)
			{
				_isSpinning = false;
			}
			if (_isSpinning)
			{
				Vector3 vector = Vector3.zero;
				if (Handgun.m_hand != null)
				{
					vector = Handgun.m_hand.Input.VelLinearWorld;
				}
				float num = Vector3.Dot(vector.normalized, Handgun.transform.up);
				num = Mathf.Clamp(num, -vector.magnitude, vector.magnitude);
				if (Mathf.Abs(_xSpinVel) < 90f)
				{
					_xSpinVel += num * Time.deltaTime * 600f;
				}
				else if (Mathf.Sign(num) == Mathf.Sign(_xSpinVel))
				{
					_xSpinVel += num * Time.deltaTime * 600f;
				}
				if (Mathf.Abs(_xSpinVel) < 90f)
				{
					if (Vector3.Dot(Handgun.transform.up, Vector3.down) >= 0f && Mathf.Sign(_xSpinVel) == 1f)
					{
						_xSpinVel += Time.deltaTime * 50f;
					}
					if (Vector3.Dot(Handgun.transform.up, Vector3.down) < 0f && Mathf.Sign(_xSpinVel) == -1f)
					{
						_xSpinVel -= Time.deltaTime * 50f;
					}
				}
				_xSpinVel = Mathf.Clamp(_xSpinVel, -500f, 500f);
				_xSpinRot += _xSpinVel * Time.deltaTime * 5f;
                //PoseSpinHolder.localEulerAngles = new Vector3(xSpinRot, 0f, 0f);
                PoseSpinHolder.ModifyLocalRotationAxisValue(Axis.X, _xSpinRot);
                _xSpinVel = Mathf.Lerp(_xSpinVel, 0f, Time.deltaTime * 0.6f);
			}
			else
			{
				_xSpinRot = 0f;
				_xSpinVel = 0f;
                //PoseSpinHolder.localEulerAngles = new Vector3(xSpinRot, 0f, 0f);
                PoseSpinHolder.ModifyLocalRotationAxisValue(Axis.X, _xSpinRot);
            }
		}

        private static void ScaleColliderUp(FVRPhysicalObject self)
        {
            float colliderScale = OpenScripts2_BepInExPlugin.SpinGrabHelperScale.Value;
            Collider collider = self.GetComponent<Collider>();
            collider.Analyze(out Vector3 _, out Vector3 size, out var type, out var _);

            size *= colliderScale;
            switch (type)
            {
                case EColliderType.Sphere:
                    (collider as SphereCollider).radius = size.x;
                    break;
                case EColliderType.Capsule:
                    (collider as CapsuleCollider).radius = size.x;
                    (collider as CapsuleCollider).height = size.y;
                    break;
                case EColliderType.Box:
                    (collider as BoxCollider).size = size;
                    break;
            }
        }

        private static void ScaleColliderDown(FVRPhysicalObject self)
        {
            float colliderScale = OpenScripts2_BepInExPlugin.SpinGrabHelperScale.Value;
            Collider collider = self.GetComponent<Collider>();
            collider.Analyze(out Vector3 _, out Vector3 size, out var type, out var _);

            size /= colliderScale;
            switch (type)
            {
                case EColliderType.Sphere:
                    (collider as SphereCollider).radius = size.x;
                    break;
                case EColliderType.Capsule:
                    (collider as CapsuleCollider).radius = size.x;
                    (collider as CapsuleCollider).height = size.y;
                    break;
                case EColliderType.Box:
                    (collider as BoxCollider).size = size;
                    break;
            }
        }

        private static IEnumerator SpinReleaseDelay(HandgunSpin handgunSpin)
        {
            for (float i = 0; i < OpenScripts2_BepInExPlugin.SpinReleaseDelayTime.Value; i += Time.deltaTime)
            {
                handgunSpin._isSpinning = true;
                yield return null;
            }
        }
#endif
    }
}
