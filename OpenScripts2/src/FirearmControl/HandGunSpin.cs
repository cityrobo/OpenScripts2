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
    public class HandgunSpin : OpenScripts2_BasePlugin
    {
        public Handgun Handgun;
		public Transform PoseSpinHolder;

		private float xSpinVel;
		private float xSpinRot;

		private bool m_isSpinning;

		private static Dictionary<FVRFireArm, HandgunSpin> _existingHandGunSpins = new();

#if !(DEBUG || MEATKIT)
		static HandgunSpin()
        {
			Hook();
		}

		public void Awake()
        {
			_existingHandGunSpins.Add(Handgun, this);
		}
		public void OnDestroy()
        {
			_existingHandGunSpins.Remove(Handgun);
        }


		private static void Hook()
        {
            On.FistVR.Handgun.UpdateInputAndAnimate += Handgun_UpdateInputAndAnimate;
            On.FistVR.FVRFireArm.FVRFixedUpdate += FVRFireArm_FVRFixedUpdate;
            On.FistVR.FVRFireArm.EndInteraction += FVRFireArm_EndInteraction;
            On.FistVR.FVRFireArm.EndInteractionIntoInventorySlot += FVRFireArm_EndInteractionIntoInventorySlot;
        }

        private static void FVRFireArm_EndInteractionIntoInventorySlot(On.FistVR.FVRFireArm.orig_EndInteractionIntoInventorySlot orig, FVRFireArm self, FVRViveHand hand, FVRQuickBeltSlot slot)
        {
			HandgunSpin handgunSpin;
			if (_existingHandGunSpins.TryGetValue(self, out handgunSpin))
			{
				handgunSpin.m_isSpinning = false;
			}
			orig(self, hand, slot);
		}

        private static void FVRFireArm_EndInteraction(On.FistVR.FVRFireArm.orig_EndInteraction orig, FVRFireArm self, FVRViveHand hand)
        {
			HandgunSpin handgunSpin;
			if (_existingHandGunSpins.TryGetValue(self, out handgunSpin))
			{
				handgunSpin.m_isSpinning = false;
			}
			orig(self,hand);
		}

        private static void FVRFireArm_FVRFixedUpdate(On.FistVR.FVRFireArm.orig_FVRFixedUpdate orig, FVRFireArm self)
        {
			orig(self);

			HandgunSpin handgunSpin;
			if (_existingHandGunSpins.TryGetValue(self, out handgunSpin))
			{
				handgunSpin.UpdateSpinning();
			}
		}

        private static void Handgun_UpdateInputAndAnimate(On.FistVR.Handgun.orig_UpdateInputAndAnimate orig, Handgun self, FVRViveHand hand)
        {
			orig(self,hand);

			HandgunSpin handgunSpin;
			if (_existingHandGunSpins.TryGetValue(self, out handgunSpin))
			{
				if (TouchpadDirDown(hand, Vector2.right)) handgunSpin.m_isSpinning = true;
				else handgunSpin.m_isSpinning = false;
			}
        }

        private void UpdateSpinning()
		{
			if (!Handgun.IsHeld || Handgun.IsAltHeld || Handgun.AltGrip != null)
			{
				m_isSpinning = false;
			}
			if (m_isSpinning)
			{
				Vector3 vector = Vector3.zero;
				if (Handgun.m_hand != null)
				{
					vector = Handgun.m_hand.Input.VelLinearWorld;
				}
				float num = Vector3.Dot(vector.normalized, Handgun.transform.up);
				num = Mathf.Clamp(num, -vector.magnitude, vector.magnitude);
				if (Mathf.Abs(xSpinVel) < 90f)
				{
					xSpinVel += num * Time.deltaTime * 600f;
				}
				else if (Mathf.Sign(num) == Mathf.Sign(xSpinVel))
				{
					xSpinVel += num * Time.deltaTime * 600f;
				}
				if (Mathf.Abs(xSpinVel) < 90f)
				{
					if (Vector3.Dot(Handgun.transform.up, Vector3.down) >= 0f && Mathf.Sign(xSpinVel) == 1f)
					{
						xSpinVel += Time.deltaTime * 50f;
					}
					if (Vector3.Dot(Handgun.transform.up, Vector3.down) < 0f && Mathf.Sign(xSpinVel) == -1f)
					{
						xSpinVel -= Time.deltaTime * 50f;
					}
				}
				xSpinVel = Mathf.Clamp(xSpinVel, -500f, 500f);
				xSpinRot += xSpinVel * Time.deltaTime * 5f;
				PoseSpinHolder.localEulerAngles = new Vector3(this.xSpinRot, 0f, 0f);
				xSpinVel = Mathf.Lerp(xSpinVel, 0f, Time.deltaTime * 0.6f);
			}
			else
			{
				xSpinRot = 0f;
				xSpinVel = 0f;
			}
		}
#endif
	}
}
