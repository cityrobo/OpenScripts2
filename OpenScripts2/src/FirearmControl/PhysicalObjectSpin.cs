using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using FistVR;

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

		private static readonly Dictionary<FVRPhysicalObject, PhysicalObjectSpin> _existingHandGunSpins = new();

#if !DEBUG
		static PhysicalObjectSpin()
        {
            On.FistVR.FVRPhysicalObject.FVRFixedUpdate += FVRPhysicalObject_FVRFixedUpdate;
            On.FistVR.FVRPhysicalObject.UpdateInteraction += FVRPhysicalObject_UpdateInteraction;
            On.FistVR.FVRPhysicalObject.EndInteraction += FVRPhysicalObject_EndInteraction;
            On.FistVR.FVRPhysicalObject.EndInteractionIntoInventorySlot += FVRPhysicalObject_EndInteractionIntoInventorySlot;
        }

        public void Awake()
        {
			_existingHandGunSpins.Add(PhysicalObject, this);
		}

		public void OnDestroy()
        {
			_existingHandGunSpins.Remove(PhysicalObject);
        }

        private static void FVRPhysicalObject_EndInteractionIntoInventorySlot(On.FistVR.FVRPhysicalObject.orig_EndInteractionIntoInventorySlot orig, FVRPhysicalObject self, FVRViveHand hand, FVRQuickBeltSlot slot)
        {
            if (_existingHandGunSpins.TryGetValue(self, out PhysicalObjectSpin handgunSpin))
            {
                handgunSpin._isSpinning = false;
            }

            orig(self, hand, slot);
		}

        private static void FVRPhysicalObject_UpdateInteraction(On.FistVR.FVRPhysicalObject.orig_UpdateInteraction orig, FVRPhysicalObject self, FVRViveHand hand)
        {
            orig(self, hand);

            if (_existingHandGunSpins.TryGetValue(self, out PhysicalObjectSpin handgunSpin))
            {
                handgunSpin._isSpinning = TouchpadDirPressed(hand, handgunSpin.TouchpadDir.GetDir());
            }
        }

        private static void FVRPhysicalObject_EndInteraction(On.FistVR.FVRPhysicalObject.orig_EndInteraction orig, FVRPhysicalObject self, FVRViveHand hand)
        {
            if (_existingHandGunSpins.TryGetValue(self, out PhysicalObjectSpin handgunSpin))
            {
                handgunSpin._isSpinning = false;
            }

            orig(self,hand);
		}

        private static void FVRPhysicalObject_FVRFixedUpdate(On.FistVR.FVRPhysicalObject.orig_FVRFixedUpdate orig, FVRPhysicalObject self)
        {
			orig(self);

            if (_existingHandGunSpins.TryGetValue(self, out PhysicalObjectSpin handgunSpin))
            {
                handgunSpin.UpdateSpinning();
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
                //PoseSpinHolder.localEulerAngles = new Vector3(_xSpinRot, 0f, 0f);
                PoseSpinHolder.ModifyLocalRotationAxisValue(Axis.X, _xSpinRot);
                _xSpinVel = Mathf.Lerp(_xSpinVel, 0f, Time.deltaTime * 0.6f);
			}
			else
			{
				_xSpinRot = 0f;
				_xSpinVel = 0f;
                //PoseSpinHolder.localEulerAngles = new Vector3(_xSpinRot, 0f, 0f);
                PoseSpinHolder.ModifyLocalRotationAxisValue(Axis.X,_xSpinRot);
            }
		}
#endif
	}
}
