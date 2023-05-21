using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
	public class BreakOpenTrigger : OpenScripts2_BasePlugin
	{
		public FVRPhysicalObject PhysicalObject;

		[Header("Break Parts")]
		public HingeJoint Hinge;
		public float HingeLimit = 45f;
		public float HingeEjectThreshhold = 30f;
		public Transform CenterOfMassOverride;

		[Header("LatchGeo (leave at default if no visible latch)")]
		public float MaxLatchRot = 45f;
		[Tooltip("If latch is below this angle the fore will latch. Latch rot dependend on how far up you press on touchpad (like break action shotgun)")]
		public float LatchLatchingRot = 5f;
		public bool HasLatchObject;
		public Transform Latch;

		[Header("Objects that turn off or on dependend on break state")]
		public GameObject[] TurnOffObjectsOnOpen;
		public GameObject[] TurnOnObjectsOnOpen;
		public bool DoesEjectMag = false;
		public float MagEjectSpeed = 5f;

		[Header("Audio")]
		public AudioEvent BreakOpenAudio;
		public AudioEvent BreakCloseAudio;

		private float _latchRot;
		private Vector3 _foreStartPos;

		private bool _isLatched = true;
		private bool _latchHeldOpen;
		private bool _hasEjectedMag = false;

		private static Dictionary<FVRPhysicalObject, BreakOpenTrigger> _exisitingBreakOpenTriggers = new();

		static BreakOpenTrigger()
        {
			Hook();
        }

		public void Awake()
        {
			_exisitingBreakOpenTriggers.Add(PhysicalObject,this);

			_foreStartPos = Hinge.transform.localPosition;
            if (CenterOfMassOverride != null)
            {
				Rigidbody RB = Hinge.GetComponent<Rigidbody>();
				RB.centerOfMass = CenterOfMassOverride.localPosition;
			}

			SetBreakObjectsState(false);
		}

		public void OnDestroy()
        {
			_exisitingBreakOpenTriggers.Remove(PhysicalObject);
        }

		public void FixedUpdate()
		{
			UpdateBreakFore();
		}

		public void Update()
        {
			FVRViveHand hand = PhysicalObject.m_hand;
			if (hand != null) UpdateInputAndAnimate(hand);
			else this._latchHeldOpen = false;
		}

		private void UpdateInputAndAnimate(FVRViveHand hand)
		{
			_latchHeldOpen = false;
			if (hand.IsInStreamlinedMode)
			{
				if (hand.Input.BYButtonPressed)
				{
					_latchHeldOpen = true;
					_latchRot = 1f * this.MaxLatchRot;
				}
				else
				{
					_latchRot = Mathf.MoveTowards(this._latchRot, 0f, Time.deltaTime * this.MaxLatchRot * 3f);
				}
			}
			else
			{
				if (hand.Input.TouchpadPressed && hand.Input.TouchpadAxes.y > 0.1f)
				{
					_latchHeldOpen = true;
					_latchRot = hand.Input.TouchpadAxes.y * MaxLatchRot;
				}
				else
				{
					_latchRot = Mathf.MoveTowards(_latchRot, 0f, Time.deltaTime * MaxLatchRot * 3f);
				}
			}

			if (HasLatchObject)
			{
				Latch.localEulerAngles = new Vector3(0f, _latchRot, 0f);
			}
		}

		private void UpdateBreakFore()
		{
			if (_isLatched && Mathf.Abs(_latchRot) > 5f)
			{
				_isLatched = false;
				SM.PlayGenericSound(BreakOpenAudio, PhysicalObject.transform.position);
				JointLimits limits = Hinge.limits;
				limits.max = HingeLimit;
				Hinge.limits = limits;
				SetBreakObjectsState(true);
			}
			if (!_isLatched)
			{
				if (!_latchHeldOpen && Hinge.transform.localEulerAngles.x <= 1f && Mathf.Abs(_latchRot) < LatchLatchingRot)
				{
					_isLatched = true;
					SM.PlayGenericSound(BreakCloseAudio, PhysicalObject.transform.position);
					JointLimits limits = Hinge.limits;
					limits.max = 0f;
					Hinge.limits = limits;
					SetBreakObjectsState(false);
					Hinge.transform.localPosition = _foreStartPos;
					_hasEjectedMag = false;
				}
				if (DoesEjectMag && Mathf.Abs(Hinge.transform.localEulerAngles.x) >= HingeEjectThreshhold && Mathf.Abs(Hinge.transform.localEulerAngles.x) <= HingeLimit)
				{
					TryEjectMag();
				}
			}
		}

		private void TryEjectMag()
		{
			if (!_hasEjectedMag)
			{
				EjectMag();
				_hasEjectedMag = true;
			}
		}

		public void EjectMag(bool PhysicalRelease = false)
		{
			FVRFireArm fireArm = PhysicalObject as FVRFireArm;

			if (fireArm.Magazine != null)
			{
				if (fireArm.Magazine.UsesOverrideInOut)
				{
					fireArm.PlayAudioEventHandling(fireArm.Magazine.ProfileOverride.MagazineOut);
				}
				else
				{
					fireArm.PlayAudioEvent(FirearmAudioEventType.MagazineOut, 1f);
				}
				fireArm.m_lastEjectedMag = fireArm.Magazine;
				fireArm.m_ejectDelay = 0.4f;
				if (fireArm.m_hand != null)
				{
					fireArm.m_hand.Buzz(fireArm.m_hand.Buzzer.Buzz_BeginInteraction);
				}
				fireArm.Magazine.Release(PhysicalRelease);

				fireArm.Magazine.RootRigidbody.velocity = -fireArm.MagazineEjectPos.up * MagEjectSpeed;
				if (fireArm.Magazine.m_hand != null)
				{
					fireArm.Magazine.m_hand.Buzz(fireArm.m_hand.Buzzer.Buzz_BeginInteraction);
				}
				fireArm.Magazine = null;
			}
		}

		private void SetBreakObjectsState(bool active)
		{
            foreach (var TurnOnObjectOnOpen in TurnOnObjectsOnOpen)
            {
				if (TurnOnObjectOnOpen != null) TurnOnObjectOnOpen.SetActive(active);
			}
			foreach (var TurnOffObjectOnOpen in TurnOffObjectsOnOpen)
			{
				if (TurnOffObjectOnOpen != null) TurnOffObjectOnOpen.SetActive(!active);
			}
		}



		private static void Hook()
        {
#if !DEBUG
			On.FistVR.ClosedBoltWeapon.DropHammer += ClosedBoltWeapon_DropHammer;
            On.FistVR.OpenBoltReceiver.ReleaseSeer += OpenBoltReceiver_ReleaseSeer;
			On.FistVR.Handgun.ReleaseSeer += Handgun_ReleaseSeer;
			On.FistVR.TubeFedShotgun.ReleaseHammer += TubeFedShotgun_ReleaseHammer;
#endif
		}

#if !DEBUG
		private static void TubeFedShotgun_ReleaseHammer(On.FistVR.TubeFedShotgun.orig_ReleaseHammer orig, TubeFedShotgun self)
        {
			BreakOpenTrigger breakOpenTrigger;
			if (_exisitingBreakOpenTriggers.TryGetValue(self, out breakOpenTrigger))
			{
				if (!breakOpenTrigger._isLatched || breakOpenTrigger._latchHeldOpen) return;
			}
			orig(self);
		}

        private static void Handgun_ReleaseSeer(On.FistVR.Handgun.orig_ReleaseSeer orig, Handgun self)
        {
			BreakOpenTrigger breakOpenTrigger;
			if (_exisitingBreakOpenTriggers.TryGetValue(self, out breakOpenTrigger))
			{
				if (!breakOpenTrigger._isLatched || breakOpenTrigger._latchHeldOpen) return;
			}
			orig(self);
		}

        private static void OpenBoltReceiver_ReleaseSeer(On.FistVR.OpenBoltReceiver.orig_ReleaseSeer orig, OpenBoltReceiver self)
        {
			BreakOpenTrigger breakOpenTrigger;
			if (_exisitingBreakOpenTriggers.TryGetValue(self, out breakOpenTrigger))
			{
				if (!breakOpenTrigger._isLatched || breakOpenTrigger._latchHeldOpen) return;
			}
			orig(self);
		}

        private static void ClosedBoltWeapon_DropHammer(On.FistVR.ClosedBoltWeapon.orig_DropHammer orig, ClosedBoltWeapon self)
        {
			BreakOpenTrigger breakOpenTrigger;
			if (_exisitingBreakOpenTriggers.TryGetValue(self, out breakOpenTrigger))
			{
				if (!breakOpenTrigger._isLatched || breakOpenTrigger._latchHeldOpen) return;
            }
			orig(self);
        }
#endif
	}
}