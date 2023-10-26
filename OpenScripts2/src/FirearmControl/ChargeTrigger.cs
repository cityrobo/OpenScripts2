using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Valve.VR;

namespace OpenScripts2
{
    public class ChargeTrigger : OpenScripts2_BasePlugin
    {
        public FVRFireArm FireArm;
        [Tooltip("Charge time in seconds")]
        public float ChargeTime = 1f;
        [Tooltip("If checked, every shot will be charged, even in automatic fire. Else only the first shot will be delayed.")]
        public bool ChargesUpEveryShot = false;
        [Tooltip("If checked, it will not charge on empty and just drop the hammer normally.")]
        [FormerlySerializedAs("StopsOnEmptyMag")]
        public bool StopsOnEmpty = false;

        public AudioEvent ChargingSounds;
        public AudioEvent ChargingAbortSounds;

        public float ChargeVibrationFrequency = 1000f;
        [Range(0f, 1f)]
        public float ChargeVibrationAmplitude = 1f;

        public VisualModifier[] VisualModifiers;

        private bool _isHooked = false;
        private float _timeCharged = 0f;
        private bool _isCharging = false;
        private bool _isAutomaticFire = false;
#if !DEBUG
        public void Awake()
        {
            if (!_isHooked)
                switch (FireArm)
                {
                    case ClosedBoltWeapon w:
                        HookClosedBolt();
                        _isHooked = true;
                        break;
                    case OpenBoltReceiver w:
                        HookOpenBolt();
                        _isHooked = true;
                        break;
                    case Handgun w:
                        HookHandgun();
                        _isHooked = true;
                        break;
                    case BoltActionRifle w:
                        HookBoltActionRifle();
                        _isHooked = true;
                        break;
                    case TubeFedShotgun w:
                        HookTubeFedShotgun();
                        _isHooked = true;
                        break;
                    case LeverActionFirearm w:
                        HookLeverActionFirearm();
                        _isHooked = true;
                        break;
                    case BreakActionWeapon w:
                        HookBreakActionWeapon();
                        _isHooked = true;
                        break;
                    default:
                        LogWarning($"Firearm type \"{FireArm.GetType()}\" not supported!");
                        break;
                }
        }

        public void OnDestroy()
        {
            if (_isHooked)
                switch (FireArm)
                {
                    case ClosedBoltWeapon w:
                        UnhookClosedBolt();
                        _isHooked = false;
                        break;
                    case OpenBoltReceiver w:
                        UnhookOpenBolt();
                        _isHooked = false;
                        break;
                    case Handgun w:
                        UnhookHandgun();
                        _isHooked = false;
                        break;
                    case BoltActionRifle w:
                        UnhookBoltActionRifle();
                        _isHooked = false;
                        break;
                    case TubeFedShotgun w:
                        UnhookTubeFedShotgun();
                        _isHooked = false;
                        break;
                    case LeverActionFirearm w:
                        UnhookLeverActionRifle();
                        _isHooked = false;
                        break;
                    case BreakActionWeapon w:
                        UnhookBreakActionWeapon();
                        _isHooked = false;
                        break;
                    default:
                        break;
                }
        }
        public void Update()
        {
            foreach (VisualModifier modifier in VisualModifiers)
            {
                modifier.UpdateVisualEffects(_timeCharged / ChargeTime);
            }
        }

        #region ClosedBoltWeapon Hooks and Coroutine
        // ClosedBoltWeapon Hooks and Coroutine
        private void UnhookClosedBolt()
        {
            On.FistVR.ClosedBoltWeapon.DropHammer -= ClosedBoltWeapon_DropHammer;
            On.FistVR.ClosedBoltWeapon.FVRUpdate -= ClosedBoltWeapon_FVRUpdate;
        }
        private void HookClosedBolt()
        {
            On.FistVR.ClosedBoltWeapon.DropHammer += ClosedBoltWeapon_DropHammer;
            On.FistVR.ClosedBoltWeapon.FVRUpdate += ClosedBoltWeapon_FVRUpdate;
        }

        private void ClosedBoltWeapon_FVRUpdate(On.FistVR.ClosedBoltWeapon.orig_FVRUpdate orig, ClosedBoltWeapon self)
        {
            orig(self);
            if (FireArm == self && (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold))
            {
                _isAutomaticFire = false;
                _timeCharged = 0f;
            }
        }

        private void ClosedBoltWeapon_DropHammer(On.FistVR.ClosedBoltWeapon.orig_DropHammer orig, ClosedBoltWeapon self)
        {
            if (self == FireArm && !_isCharging && !_isAutomaticFire && (!StopsOnEmpty || (StopsOnEmpty && self.Chamber.IsFull))) StartCoroutine(HammerDropClosedBolt(orig, self));
            else if (self == FireArm && !_isCharging && _isAutomaticFire)
            {
                orig(self);
                if (self.FireSelector_Modes[self.m_fireSelectorMode].ModeType == ClosedBoltWeapon.FireSelectorModeType.Burst && self.m_CamBurst <= 0) _timeCharged = 0f;
            }
            else if (self == FireArm && !_isCharging && StopsOnEmpty && !self.Chamber.IsFull) orig(self);
            else if (self != FireArm) orig(self);
        }
        private IEnumerator HammerDropClosedBolt(On.FistVR.ClosedBoltWeapon.orig_DropHammer orig, ClosedBoltWeapon self)
        {
            yield return DropHammer(self);

            ClosedBoltWeapon.FireSelectorModeType modeType = self.FireSelector_Modes[self.m_fireSelectorMode].ModeType;
            if (!ChargesUpEveryShot && modeType != ClosedBoltWeapon.FireSelectorModeType.Single) _isAutomaticFire = true;
            if (_timeCharged >= ChargeTime) orig(self);
            else SM.PlayGenericSound(ChargingAbortSounds, self.transform.position);

            if (self.FireSelector_Modes[self.m_fireSelectorMode].ModeType == ClosedBoltWeapon.FireSelectorModeType.Single) _timeCharged = 0f;
        }
        #endregion

        #region OpenBoltReceiver Hooks and Coroutine
        // OpenBoltReceiver Hooks and Coroutine
        private void UnhookOpenBolt()
        {
            On.FistVR.OpenBoltReceiver.ReleaseSeer -= OpenBoltReceiver_ReleaseSeer;
            On.FistVR.OpenBoltReceiver.FVRUpdate -= OpenBoltReceiver_FVRUpdate;
        }
        private void HookOpenBolt()
        {
            On.FistVR.OpenBoltReceiver.ReleaseSeer += OpenBoltReceiver_ReleaseSeer;
            On.FistVR.OpenBoltReceiver.FVRUpdate += OpenBoltReceiver_FVRUpdate;
        }

        private void OpenBoltReceiver_FVRUpdate(On.FistVR.OpenBoltReceiver.orig_FVRUpdate orig, OpenBoltReceiver self)
        {
            orig(self);
            if (FireArm == self && (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold))
            {
                _isAutomaticFire = false;
                _timeCharged = 0f;
            }
        }

        private void OpenBoltReceiver_ReleaseSeer(On.FistVR.OpenBoltReceiver.orig_ReleaseSeer orig, OpenBoltReceiver self)
        {
            if (self == FireArm && !_isCharging && !_isAutomaticFire && (!StopsOnEmpty || (StopsOnEmpty && FireArm.Magazine != null && FireArm.Magazine.HasARound()))) StartCoroutine(SeerReleaseOpenBolt(orig, self));
            else if (self == FireArm && !_isCharging && _isAutomaticFire ) orig(self);
            else if (self == FireArm && !_isCharging && StopsOnEmpty && (FireArm.Magazine == null || (FireArm.Magazine != null && !FireArm.Magazine.HasARound()))) orig(self);
            else if (self != FireArm) orig(self);
        }
        private IEnumerator SeerReleaseOpenBolt(On.FistVR.OpenBoltReceiver.orig_ReleaseSeer orig, OpenBoltReceiver self)
        {
            yield return DropHammer(self);

            OpenBoltReceiver.FireSelectorModeType modeType = self.FireSelector_Modes[self.m_fireSelectorMode].ModeType;
            if (!ChargesUpEveryShot && modeType != OpenBoltReceiver.FireSelectorModeType.Single) _isAutomaticFire = true;
            if (_timeCharged >= ChargeTime) orig(self);
            else SM.PlayGenericSound(ChargingAbortSounds, self.transform.position);

            if (self.FireSelector_Modes[self.m_fireSelectorMode].ModeType == OpenBoltReceiver.FireSelectorModeType.Single) _timeCharged = 0f;
        }
        #endregion

        #region Handgun Hooks and Coroutine
        // Handgun Hooks and Coroutine
        private void UnhookHandgun()
        {
            On.FistVR.Handgun.ReleaseSeer -= Handgun_ReleaseSeer;
            On.FistVR.Handgun.FVRUpdate -= Handgun_FVRUpdate;
        }
        private void HookHandgun()
        {
            On.FistVR.Handgun.ReleaseSeer += Handgun_ReleaseSeer;
            On.FistVR.Handgun.FVRUpdate += Handgun_FVRUpdate;
        }

        private void Handgun_FVRUpdate(On.FistVR.Handgun.orig_FVRUpdate orig, Handgun self)
        {
            orig(self);
            if (FireArm == self && (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold))
            {
                _isAutomaticFire = false;
                _timeCharged = 0f;
            }
        }

        private void Handgun_ReleaseSeer(On.FistVR.Handgun.orig_ReleaseSeer orig, Handgun self)
        {
            if (self == FireArm && !_isCharging && !_isAutomaticFire && (!StopsOnEmpty || (StopsOnEmpty && self.Chamber.IsFull))) StartCoroutine(SeerReleaseHandgun(orig, self));
            else if (self == FireArm && !_isCharging && _isAutomaticFire)
            {
                orig(self);
                if (self.FireSelectorModes[self.m_fireSelectorMode].ModeType == Handgun.FireSelectorModeType.Burst && self.m_CamBurst <= 0) _timeCharged = 0f;
            }
            else if (self == FireArm && !_isCharging && StopsOnEmpty && !self.Chamber.IsFull) orig(self);
            else if (self != FireArm) orig(self);
        }
        private IEnumerator SeerReleaseHandgun(On.FistVR.Handgun.orig_ReleaseSeer orig, Handgun self)
        {
            yield return DropHammer(self);

            Handgun.FireSelectorModeType modeType = self.FireSelectorModes[self.m_fireSelectorMode].ModeType;
            if (!ChargesUpEveryShot && modeType != Handgun.FireSelectorModeType.Single) _isAutomaticFire = true;
            if (_timeCharged >= ChargeTime) orig(self);
            else SM.PlayGenericSound(ChargingAbortSounds, self.transform.position);

            if (self.FireSelectorModes[self.m_fireSelectorMode].ModeType == Handgun.FireSelectorModeType.Single) _timeCharged = 0f;
        }
        #endregion

        #region BoltActionRifle Hooks and Coroutine
        // BoltActionRifle Hooks and Coroutine
        private void UnhookBoltActionRifle()
        {
            On.FistVR.BoltActionRifle.DropHammer -= BoltActionRifle_DropHammer;
        }

        private void HookBoltActionRifle()
        {
            On.FistVR.BoltActionRifle.DropHammer += BoltActionRifle_DropHammer;
        }

        private void BoltActionRifle_DropHammer(On.FistVR.BoltActionRifle.orig_DropHammer orig, BoltActionRifle self)
        {
            if (self == FireArm && !_isCharging && (!StopsOnEmpty || (StopsOnEmpty && self.Chamber.IsFull))) StartCoroutine(DropHammerBoltAction(orig, self));
            else if (self == FireArm && !_isCharging && StopsOnEmpty && !self.Chamber.IsFull) orig(self);
            else if (self != FireArm) orig(self);
        }

        private IEnumerator DropHammerBoltAction(On.FistVR.BoltActionRifle.orig_DropHammer orig, BoltActionRifle self)
        {
            yield return DropHammer(self);

            if (_timeCharged >= ChargeTime) orig(self);
            else SM.PlayGenericSound(ChargingAbortSounds, self.transform.position);

            _timeCharged = 0f;
        }
        #endregion

        #region TubeFedShotgun Hooks and Coroutine
        // TubeFedShotgun Hooks and Coroutine
        private void UnhookTubeFedShotgun()
        {
            On.FistVR.TubeFedShotgun.ReleaseHammer -= TubeFedShotgun_ReleaseHammer;
        }

        private void HookTubeFedShotgun()
        {
            On.FistVR.TubeFedShotgun.ReleaseHammer += TubeFedShotgun_ReleaseHammer;
        }

        private void TubeFedShotgun_ReleaseHammer(On.FistVR.TubeFedShotgun.orig_ReleaseHammer orig, TubeFedShotgun self)
        {
            if (self == FireArm && !_isCharging && (!StopsOnEmpty || (StopsOnEmpty && self.Chamber.IsFull))) StartCoroutine(ReleaseHammerTubeFed(orig, self));
            else if (self == FireArm && !_isCharging && StopsOnEmpty && !self.Chamber.IsFull) orig(self);
            else if (self != FireArm) orig(self);
        }

        private IEnumerator ReleaseHammerTubeFed(On.FistVR.TubeFedShotgun.orig_ReleaseHammer orig, TubeFedShotgun self)
        {
            yield return DropHammer(self);

            if (_timeCharged >= ChargeTime) orig(self);
            else SM.PlayGenericSound(ChargingAbortSounds, self.transform.position);

            _timeCharged = 0f;
        }
        #endregion

        #region LeverActionFirearm Hooks and Coroutine
        // LeverActionFirearm Hooks and Coroutine
        private void UnhookLeverActionRifle()
        {
            On.FistVR.LeverActionFirearm.Fire -= LeverActionFirearm_Fire;
        }
        private void HookLeverActionFirearm()
        {
            On.FistVR.LeverActionFirearm.Fire += LeverActionFirearm_Fire;
        }

        private void LeverActionFirearm_Fire(On.FistVR.LeverActionFirearm.orig_Fire orig, LeverActionFirearm self)
        {
            if (self == FireArm && !_isCharging && (!StopsOnEmpty || (StopsOnEmpty && self.Chamber.IsFull))) StartCoroutine(FireLeverAction(orig, self));
            else if (self == FireArm && !_isCharging && StopsOnEmpty && !self.Chamber.IsFull) orig(self);
            else if (self != FireArm) orig(self);
        }

        private IEnumerator FireLeverAction(On.FistVR.LeverActionFirearm.orig_Fire orig, LeverActionFirearm self)
        {
            yield return DropHammer(self);

            if (_timeCharged >= ChargeTime) orig(self);
            else SM.PlayGenericSound(ChargingAbortSounds, self.transform.position);

            _timeCharged = 0f;
        }
        #endregion

        # region BreakOpenShotgun Hooks and Coroutine
        // BreakOpenShotgun Hooks and Coroutine
        private void UnhookBreakActionWeapon()
        {
            On.FistVR.BreakActionWeapon.DropHammer -= BreakActionWeapon_DropHammer;
        }

        private void HookBreakActionWeapon()
        {
            On.FistVR.BreakActionWeapon.DropHammer += BreakActionWeapon_DropHammer;
        }

        private void BreakActionWeapon_DropHammer(On.FistVR.BreakActionWeapon.orig_DropHammer orig, BreakActionWeapon self)
        {
            int i;
            for (i = 0; i < self.Barrels.Length; i++)
            {
                if (self.Barrels[i].m_isHammerCocked)
                {
                    break;
                }
            }

            if (self == FireArm &&!_isCharging && (!StopsOnEmpty || (StopsOnEmpty && self.Barrels[i].Chamber.IsFull && !self.Barrels[i].Chamber.IsSpent))) StartCoroutine(DropHammerBreakAction(orig, self));
            else if (self == FireArm && !_isCharging && StopsOnEmpty && (!self.Barrels[i].Chamber.IsFull || self.Barrels[i].Chamber.IsSpent)) orig(self);
            else if (self != FireArm) orig(self);
        }

        private IEnumerator DropHammerBreakAction(On.FistVR.BreakActionWeapon.orig_DropHammer orig, BreakActionWeapon self)
        {
            yield return DropHammer(self);

            if (_timeCharged >= ChargeTime) orig(self);
            else SM.PlayGenericSound(ChargingAbortSounds, self.transform.position);

            _timeCharged = 0f;
        }
        #endregion

        private IEnumerator DropHammer(FVRFireArm fireArm)
        {
            _isCharging = true;
            _timeCharged = 0f;
            FVRPooledAudioSource audioSource = SM.PlayCoreSound(FVRPooledAudioType.Generic, ChargingSounds, fireArm.transform.position);
            while (_timeCharged < ChargeTime)
            {
                if (!fireArm.IsHeld || CheckTrigger(fireArm)) break;
                _timeCharged += Time.deltaTime;
                Vibrate(fireArm.m_hand);
                yield return null;
            }
            _isCharging = false;
            audioSource?.Source.Stop();
        }

        private void Vibrate(FVRViveHand hand)
        {
            SteamVR_Action_Vibration handVibration = hand.Vibration;
            if (hand.CMode == ControlMode.Index)
            {
                handVibration.Execute(0f, Time.fixedDeltaTime, ChargeVibrationFrequency * (1 - (_timeCharged / ChargeTime)), ChargeVibrationAmplitude, hand.HandSource);
            }
            else
            {
                handVibration.Execute(0f, Time.fixedDeltaTime, ChargeVibrationFrequency, ChargeVibrationAmplitude * (_timeCharged / ChargeTime), hand.HandSource);
            }
        }

        private bool CheckTrigger(FVRFireArm fireArm) => fireArm switch
        {
            ClosedBoltWeapon w => fireArm.m_hand.Input.TriggerFloat < w.TriggerResetThreshold,
            OpenBoltReceiver w => fireArm.m_hand.Input.TriggerFloat < w.TriggerResetThreshold,
            Handgun w => fireArm.m_hand.Input.TriggerFloat < w.TriggerResetThreshold,
            TubeFedShotgun w => fireArm.m_hand.Input.TriggerFloat < w.TriggerResetThreshold,
            BoltActionRifle w => fireArm.m_hand.Input.TriggerFloat < w.TriggerResetThreshold,
            BreakActionWeapon w => fireArm.m_hand.Input.TriggerFloat < 0.45f,
            LeverActionFirearm w => fireArm.m_hand.Input.TriggerUp,
            _ => false,
        };
#endif
    }
}
