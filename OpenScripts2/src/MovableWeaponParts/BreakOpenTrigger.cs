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


		public void Awake()
        {
			_foreStartPos = Hinge.transform.localPosition;
            if (CenterOfMassOverride != null)
            {
				Rigidbody RB = Hinge.GetComponent<Rigidbody>();
				RB.centerOfMass = CenterOfMassOverride.localPosition;
			}

			SetBreakObjectsState(false);
		}

		public void Start()
		{
			Hook();
		}

		public void OnDestroy()
        {
			Unhook();
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

		void UpdateInputAndAnimate(FVRViveHand hand)
		{
			this._latchHeldOpen = false;
			if (hand.IsInStreamlinedMode)
			{
				if (hand.Input.BYButtonPressed)
				{
					this._latchHeldOpen = true;
					this._latchRot = 1f * this.MaxLatchRot;
				}
				else
				{
					this._latchRot = Mathf.MoveTowards(this._latchRot, 0f, Time.deltaTime * this.MaxLatchRot * 3f);
				}
			}
			else
			{
				if (hand.Input.TouchpadPressed && hand.Input.TouchpadAxes.y > 0.1f)
				{
					this._latchHeldOpen = true;
					this._latchRot = hand.Input.TouchpadAxes.y * this.MaxLatchRot;
				}
				else
				{
					this._latchRot = Mathf.MoveTowards(this._latchRot, 0f, Time.deltaTime * this.MaxLatchRot * 3f);
				}
			}

			if (this.HasLatchObject)
			{
				this.Latch.localEulerAngles = new Vector3(0f, this._latchRot, 0f);
			}
		}

		void UpdateBreakFore()
		{
			if (this._isLatched && Mathf.Abs(this._latchRot) > 5f)
			{
				this._isLatched = false;
				SM.PlayGenericSound(BreakOpenAudio, PhysicalObject.transform.position);
				JointLimits limits = this.Hinge.limits;
				limits.max = this.HingeLimit;
				this.Hinge.limits = limits;
				SetBreakObjectsState(true);
			}
			if (!this._isLatched)
			{
				if (!this._latchHeldOpen && this.Hinge.transform.localEulerAngles.x <= 1f && Mathf.Abs(this._latchRot) < LatchLatchingRot)
				{
					this._isLatched = true;
					SM.PlayGenericSound(BreakCloseAudio, PhysicalObject.transform.position);
					JointLimits limits = this.Hinge.limits;
					limits.max = 0f;
					this.Hinge.limits = limits;
					SetBreakObjectsState(false);
					this.Hinge.transform.localPosition = this._foreStartPos;
					_hasEjectedMag = false;
				}
				if (DoesEjectMag && Mathf.Abs(this.Hinge.transform.localEulerAngles.x) >= this.HingeEjectThreshhold && Mathf.Abs(this.Hinge.transform.localEulerAngles.x) <= HingeLimit)
				{
					TryEjectMag();
				}
			}
		}

		void TryEjectMag()
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

		void SetBreakObjectsState(bool active)
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

		void Unhook()
		{
#if !MEATKIT
			switch (PhysicalObject)
			{
				case ClosedBoltWeapon w:
					On.FistVR.ClosedBoltWeapon.DropHammer += ClosedBoltWeapon_DropHammer;
					break;
				case OpenBoltReceiver w:
					On.FistVR.OpenBoltReceiver.ReleaseSeer += OpenBoltReceiver_ReleaseSeer;
					break;
				case Handgun w:
					On.FistVR.Handgun.ReleaseSeer += Handgun_ReleaseSeer;
					break;
				case TubeFedShotgun w:
					On.FistVR.TubeFedShotgun.ReleaseHammer -= TubeFedShotgun_ReleaseHammer;
					break;
				default:
					break;
			}
#endif
		}
		void Hook()
		{
#if !MEATKIT
			switch (PhysicalObject)
            {
				case ClosedBoltWeapon w:
                    On.FistVR.ClosedBoltWeapon.DropHammer += ClosedBoltWeapon_DropHammer;
					break;
				case OpenBoltReceiver w:
                    On.FistVR.OpenBoltReceiver.ReleaseSeer += OpenBoltReceiver_ReleaseSeer;
					break;
				case Handgun w:
                    On.FistVR.Handgun.ReleaseSeer += Handgun_ReleaseSeer;
					break;
				case TubeFedShotgun w:
                    On.FistVR.TubeFedShotgun.ReleaseHammer += TubeFedShotgun_ReleaseHammer;
					break;
                default:
                    break;
            }
#endif
		}
#if !MEATKIT
		private void TubeFedShotgun_ReleaseHammer(On.FistVR.TubeFedShotgun.orig_ReleaseHammer orig, TubeFedShotgun self)
        {
			if (self == PhysicalObject)
			{
				if (!_isLatched || _latchHeldOpen) return;
			}
			orig(self);
		}

        private void Handgun_ReleaseSeer(On.FistVR.Handgun.orig_ReleaseSeer orig, Handgun self)
        {
			if (self == PhysicalObject)
			{
				if (!_isLatched || _latchHeldOpen) return;
			}
			orig(self);
		}

        private void OpenBoltReceiver_ReleaseSeer(On.FistVR.OpenBoltReceiver.orig_ReleaseSeer orig, OpenBoltReceiver self)
        {
			if (self == PhysicalObject)
			{
				if (!_isLatched || _latchHeldOpen) return;
			}
			orig(self);
		}

        private void ClosedBoltWeapon_DropHammer(On.FistVR.ClosedBoltWeapon.orig_DropHammer orig, ClosedBoltWeapon self)
        {
            if (self == PhysicalObject)
            {
				if (!_isLatched || _latchHeldOpen) return;
            }
			orig(self);
        }
#endif
	}
}