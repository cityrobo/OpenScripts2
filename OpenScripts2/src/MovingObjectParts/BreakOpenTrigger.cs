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
        public Axis HingeAxis;

        [Header("LatchGeo (leave at default if no visible latch)")]
        public bool HasLatchObject;
        public Transform Latch;
        public float MaxLatchRot = 45f;
        [Tooltip("If latch is below this angle the fore will latch. Latch rot dependend on how far up you press on touchpad (like break action shotgun)")]
        public float LatchLatchingRot = 5f;

        [Header("Objects that turn off or on dependend on break state")]
        public GameObject[] TurnOffObjectsOnOpen;
        public GameObject[] TurnOnObjectsOnOpen;

        [Header("Magazine Ejection")]
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

        private static readonly Dictionary<FVRPhysicalObject, BreakOpenTrigger> _exisitingBreakOpenTriggers = new();

        public void Awake()
        {
            _exisitingBreakOpenTriggers.Add(PhysicalObject, this);

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
            else _latchHeldOpen = false;
        }

        private void UpdateInputAndAnimate(FVRViveHand hand)
        {
            _latchHeldOpen = false;
            if (hand.IsInStreamlinedMode)
            {
                if (hand.Input.BYButtonPressed)
                {
                    _latchHeldOpen = true;
                    _latchRot = 1f * MaxLatchRot;
                }
                else
                {
                    _latchRot = Mathf.MoveTowards(_latchRot, 0f, Time.deltaTime * MaxLatchRot * 3f);
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
            float currentHingeAngle = HingeAxis switch
            {
                Axis.X => Hinge.transform.localEulerAngles.x,
                Axis.Y => Hinge.transform.localEulerAngles.y,
                Axis.Z => Hinge.transform.localEulerAngles.z,
                _ => 0f,
            };

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
                if (!_latchHeldOpen && currentHingeAngle <= 1f && Mathf.Abs(_latchRot) < LatchLatchingRot)
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
                if (DoesEjectMag && Mathf.Abs(currentHingeAngle) >= HingeEjectThreshhold && Mathf.Abs(currentHingeAngle) <= HingeLimit)
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
                fireArm.m_hand?.Buzz(fireArm.m_hand.Buzzer.Buzz_BeginInteraction);
                fireArm.Magazine.Release(PhysicalRelease);

                fireArm.Magazine.RootRigidbody.velocity = -fireArm.MagazineEjectPos.up * MagEjectSpeed;
                fireArm.Magazine.m_hand?.Buzz(fireArm.m_hand.Buzzer.Buzz_BeginInteraction);
                fireArm.Magazine = null;
            }
        }

        private void SetBreakObjectsState(bool active)
        {
            foreach (var TurnOnObjectOnOpen in TurnOnObjectsOnOpen)
            {
                TurnOnObjectOnOpen?.SetActive(active);
            }
            foreach (var TurnOffObjectOnOpen in TurnOffObjectsOnOpen)
            {
                TurnOffObjectOnOpen?.SetActive(!active);
            }
        }

#if !DEBUG
        static BreakOpenTrigger()
        {
            On.FistVR.ClosedBoltWeapon.DropHammer += ClosedBoltWeapon_DropHammer;
            On.FistVR.OpenBoltReceiver.ReleaseSeer += OpenBoltReceiver_ReleaseSeer;
            On.FistVR.Handgun.ReleaseSeer += Handgun_ReleaseSeer;
            On.FistVR.TubeFedShotgun.ReleaseHammer += TubeFedShotgun_ReleaseHammer;
        }

        private static void TubeFedShotgun_ReleaseHammer(On.FistVR.TubeFedShotgun.orig_ReleaseHammer orig, TubeFedShotgun self)
        {
            if (_exisitingBreakOpenTriggers.TryGetValue(self, out BreakOpenTrigger breakOpenTrigger))
            {
                if (!breakOpenTrigger._isLatched || breakOpenTrigger._latchHeldOpen) return;
            }
            orig(self);
        }

        private static void Handgun_ReleaseSeer(On.FistVR.Handgun.orig_ReleaseSeer orig, Handgun self)
        {
            if (_exisitingBreakOpenTriggers.TryGetValue(self, out BreakOpenTrigger breakOpenTrigger))
            {
                if (!breakOpenTrigger._isLatched || breakOpenTrigger._latchHeldOpen) return;
            }
            orig(self);
        }

        private static void OpenBoltReceiver_ReleaseSeer(On.FistVR.OpenBoltReceiver.orig_ReleaseSeer orig, OpenBoltReceiver self)
        {
            if (_exisitingBreakOpenTriggers.TryGetValue(self, out BreakOpenTrigger breakOpenTrigger))
            {
                if (!breakOpenTrigger._isLatched || breakOpenTrigger._latchHeldOpen) return;
            }
            orig(self);
        }

        private static void ClosedBoltWeapon_DropHammer(On.FistVR.ClosedBoltWeapon.orig_DropHammer orig, ClosedBoltWeapon self)
        {
            if (_exisitingBreakOpenTriggers.TryGetValue(self, out BreakOpenTrigger breakOpenTrigger))
            {
                if (!breakOpenTrigger._isLatched || breakOpenTrigger._latchHeldOpen) return;
            }
            orig(self);
        }
#endif
    }
}