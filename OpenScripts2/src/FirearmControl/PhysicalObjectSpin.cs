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
    public class PhysicalObjectSpin : OpenScripts2_BasePlugin
    {
        public FVRPhysicalObject PhysicalObject;
		public Transform PoseSpinHolder;

		public ETouchpadDir TouchpadDir;

		private float _xSpinVel;
		private float _xSpinRot;

		private bool _isSpinning;
        private bool _spinningToggleState;

        private static readonly Dictionary<FVRPhysicalObject, PhysicalObjectSpin> _existingHandGunSpins = new();

#if !DEBUG
		static PhysicalObjectSpin()
        {
            On.FistVR.FVRPhysicalObject.BeginInteraction += FVRPhysicalObject_BeginInteraction;
            On.FistVR.FVRPhysicalObject.UpdateInteraction += FVRPhysicalObject_UpdateInteraction;
            On.FistVR.FVRPhysicalObject.EndInteraction += FVRPhysicalObject_EndInteraction;
            On.FistVR.FVRPhysicalObject.EndInteractionIntoInventorySlot += FVRPhysicalObject_EndInteractionIntoInventorySlot;

            On.FistVR.FVRPhysicalObject.FVRFixedUpdate += FVRPhysicalObject_FVRFixedUpdate;
        }

        public void Awake()
        {
			_existingHandGunSpins.Add(PhysicalObject, this);
		}

		public void OnDestroy()
        {
			_existingHandGunSpins.Remove(PhysicalObject);
        }

        private static void FVRPhysicalObject_BeginInteraction(On.FistVR.FVRPhysicalObject.orig_BeginInteraction orig, FVRPhysicalObject self, FVRViveHand hand)
        {
            if (_existingHandGunSpins.TryGetValue(self, out PhysicalObjectSpin physicalObjectSpin))
            {
                if (OpenScripts2_BepInExPlugin.SpinToggle.Value)
                {
                    physicalObjectSpin._isSpinning = physicalObjectSpin._spinningToggleState;
                }
                else if (!OpenScripts2_BepInExPlugin.SpinToggle.Value && hand.Input.TouchpadPressed && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.up) < 45f)
                {
                    physicalObjectSpin._isSpinning = true;
                    self.StopAllCoroutines();
                    self.StartCoroutine(SpinReleaseDelay(physicalObjectSpin));
                }

                if (physicalObjectSpin._isSpinning)
                {
                    Vector3 spinSpeed = self.transform.InverseTransformDirection(self.RootRigidbody.angularVelocity);

                    physicalObjectSpin._xSpinVel = -spinSpeed.x * 10f;

                    physicalObjectSpin._xSpinRot = Vector3Utils.SignedAngle(self.PoseOverride.forward, hand.Input.Forward, self.transform.right);
                }

                if (OpenScripts2_BepInExPlugin.SpinGrabHelper.Value && physicalObjectSpin._isSpinning)
                {
                    ScaleColliderDown(self);
                }
            }
            orig(self, hand);
        }

        private static void FVRPhysicalObject_UpdateInteraction(On.FistVR.FVRPhysicalObject.orig_UpdateInteraction orig, FVRPhysicalObject self, FVRViveHand hand)
        {
            orig(self, hand);

            if (_existingHandGunSpins.TryGetValue(self, out PhysicalObjectSpin physicalObjectSpin))
            {
                physicalObjectSpin._isSpinning = TouchpadDirPressed(hand, physicalObjectSpin.TouchpadDir.GetDir());

                if (!self.IsAltHeld && !hand.IsInStreamlinedMode)
                {
                    if (!OpenScripts2_BepInExPlugin.SpinToggle.Value && TouchpadDirPressed(hand, physicalObjectSpin.TouchpadDir.GetDir()))
                    {
                        physicalObjectSpin._isSpinning = true;
                        self.StopAllCoroutines();
                        self.StartCoroutine(SpinReleaseDelay(physicalObjectSpin));
                    }

                    if (OpenScripts2_BepInExPlugin.SpinToggle.Value && TouchpadDirDown(hand, physicalObjectSpin.TouchpadDir.GetDir()))
                    {
                        physicalObjectSpin._spinningToggleState = !physicalObjectSpin._spinningToggleState;
                    }

                    if (OpenScripts2_BepInExPlugin.SpinToggle.Value)
                    {
                        physicalObjectSpin._isSpinning = physicalObjectSpin._spinningToggleState;
                    }

                    if (physicalObjectSpin._isSpinning) self.UseGrabPointChild = false;
                    else self.UseGrabPointChild = true;
                }
            }
        }

        private static void FVRPhysicalObject_EndInteraction(On.FistVR.FVRPhysicalObject.orig_EndInteraction orig, FVRPhysicalObject self, FVRViveHand hand)
        {
            if (_existingHandGunSpins.TryGetValue(self, out PhysicalObjectSpin physicalObjectSpin))
            {
                self.RootRigidbody.AddRelativeTorque(new Vector3(-physicalObjectSpin._xSpinVel / 10f, 0f, 0f), ForceMode.VelocityChange);

                if (OpenScripts2_BepInExPlugin.SpinGrabHelper.Value && physicalObjectSpin._isSpinning)
                {
                    ScaleColliderUp(self);
                }
            }

            orig(self, hand);
        }

        private static void FVRPhysicalObject_EndInteractionIntoInventorySlot(On.FistVR.FVRPhysicalObject.orig_EndInteractionIntoInventorySlot orig, FVRPhysicalObject self, FVRViveHand hand, FVRQuickBeltSlot slot)
        {
            orig(self, hand, slot);

            if (_existingHandGunSpins.TryGetValue(self, out PhysicalObjectSpin physicalObjectSpin))
            {
                physicalObjectSpin._isSpinning = false;
            }
        }

        private static void FVRPhysicalObject_FVRFixedUpdate(On.FistVR.FVRPhysicalObject.orig_FVRFixedUpdate orig, FVRPhysicalObject self)
        {
            orig(self);

            if (_existingHandGunSpins.TryGetValue(self, out PhysicalObjectSpin physicalObjectSpin))
            {
                physicalObjectSpin.UpdateSpinning();
            }
        }

        private void UpdateSpinning()
        {
            if (!PhysicalObject.IsHeld || PhysicalObject.IsAltHeld || PhysicalObject.AltGrip != null)
            {
                _isSpinning = false;
            }
            if (_isSpinning)
            {
                Vector3 vector = Vector3.zero;
                if (PhysicalObject.m_hand != null)
                {
                    vector = PhysicalObject.m_hand.Input.VelLinearWorld;
                }
                float num = Vector3.Dot(vector.normalized, PhysicalObject.transform.up);
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
                    if (Vector3.Dot(PhysicalObject.transform.up, Vector3.down) >= 0f && Mathf.Sign(_xSpinVel) == 1f)
                    {
                        _xSpinVel += Time.deltaTime * 50f;
                    }
                    if (Vector3.Dot(PhysicalObject.transform.up, Vector3.down) < 0f && Mathf.Sign(_xSpinVel) == -1f)
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

        private static IEnumerator SpinReleaseDelay(PhysicalObjectSpin physicalObjectSpin)
        {
            for (float i = 0; i < OpenScripts2_BepInExPlugin.SpinReleaseDelayTime.Value; i += Time.deltaTime)
            {
                physicalObjectSpin._isSpinning = true;
                yield return null;
            }
        }
#endif
    }
}
