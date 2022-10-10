using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class ChargeTriggerPower : OpenScripts2_BasePlugin
    {
        public FVRFireArm FireArm;
        [Tooltip("Charge time in seconds")]
        public float ChargeTime = 1f;
        [Tooltip("If checked, every shot will be charged, even in automatic fire. Else only the first shot will be delayed.")]
        public bool ChargesUpEveryShot = false;

        public AudioEvent ChargingSounds;
        //public AudioEvent ChargingAbortSounds;

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
                        LogWarning($"Firearm type \"{FireArm.GetType()}\" not supported! Tell me and I'll see about adding it!");
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
            if (FireArm == self && (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold)) _isAutomaticFire = false;
        }

        private void ClosedBoltWeapon_DropHammer(On.FistVR.ClosedBoltWeapon.orig_DropHammer orig, ClosedBoltWeapon self)
        {
            if (!_isCharging && !_isAutomaticFire && self == FireArm) StartCoroutine(HammerDropClosedBolt(orig, self));
            else if (_isAutomaticFire || self != FireArm) orig(self);
        }
        private IEnumerator HammerDropClosedBolt(On.FistVR.ClosedBoltWeapon.orig_DropHammer orig, ClosedBoltWeapon self)
        {
            _isCharging = true;
            _timeCharged = 0f;
            SM.PlayGenericSound(ChargingSounds, self.transform.position);
            while (_timeCharged < ChargeTime)
            {
                if (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold) break;
                _timeCharged += Time.deltaTime;
                yield return null;
            }
            _isCharging = false;
            ClosedBoltWeapon.FireSelectorModeType modeType = self.FireSelector_Modes[self.m_fireSelectorMode].ModeType;
            if (!ChargesUpEveryShot && modeType != ClosedBoltWeapon.FireSelectorModeType.Single) _isAutomaticFire = true;
            if (_timeCharged >= ChargeTime) orig(self);
            else
            {
                //SM.PlayGenericSound(ChargingAbortSounds, self.transform.position);
                On.FistVR.ClosedBoltWeapon.Fire += ClosedBoltWeapon_Fire;
                orig(self);
                On.FistVR.ClosedBoltWeapon.Fire -= ClosedBoltWeapon_Fire;
            }
        }

        private bool ClosedBoltWeapon_Fire(On.FistVR.ClosedBoltWeapon.orig_Fire orig, ClosedBoltWeapon self)
        {
            if (FireArm == self)
            {
                if (!self.Chamber.Fire()) return false;
                self.m_timeSinceFiredShot = 0f;
                float velMult = 1f;
                if (self.UsesStickyDetonation) velMult = 1f + Mathf.Lerp(0f, self.StickyMaxMultBonus, self.m_stickyChargeUp);
                else velMult = _timeCharged / ChargeTime;
                self.Fire(self.Chamber, self.GetMuzzle(), true, velMult, -1f);
                bool twoHandStabilized = self.IsTwoHandStabilized();
                bool foregripStabilized = self.AltGrip != null;
                bool shoulderStabilized = self.IsShoulderStabilized();
                self.Recoil(twoHandStabilized, foregripStabilized, shoulderStabilized, null, 1f);
                bool flag = false;
                ClosedBoltWeapon.FireSelectorMode fireSelectorMode = self.FireSelector_Modes[self.m_fireSelectorMode];
                if (fireSelectorMode.ModeType == ClosedBoltWeapon.FireSelectorModeType.SuperFastBurst)
                {
                    for (int i = 0; i < fireSelectorMode.BurstAmount - 1; i++)
                    {
                        if (self.Magazine.HasARound())
                        {
                            self.Magazine.RemoveRound();
                            self.Fire(self.Chamber, self.GetMuzzle(), false, 1f, -1f);
                            flag = true;
                            self.Recoil(twoHandStabilized, foregripStabilized, shoulderStabilized, null, 1f);
                        }
                    }
                }
                self.FireMuzzleSmoke();
                if (self.UsesDelinker && self.HasBelt) self.DelinkerSystem.Emit(1);
                if (self.HasBelt) self.BeltDD.AddJitter();
                if (flag) self.PlayAudioGunShot(false, self.Chamber.GetRound().TailClass, self.Chamber.GetRound().TailClassSuppressed, GM.CurrentPlayerBody.GetCurrentSoundEnvironment());
                else self.PlayAudioGunShot(self.Chamber.GetRound(), GM.CurrentPlayerBody.GetCurrentSoundEnvironment(), 1f);
                if (self.ReciprocatesOnShot) self.Bolt.ImpartFiringImpulse();
                return true;
            }
            else return orig(self);
        }

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
            if (FireArm == self && (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold)) _isAutomaticFire = false;
        }

        private void OpenBoltReceiver_ReleaseSeer(On.FistVR.OpenBoltReceiver.orig_ReleaseSeer orig, OpenBoltReceiver self)
        {
            if (!_isCharging && !_isAutomaticFire && self == FireArm) StartCoroutine(SeerReleaseOpenBolt(orig, self));
            else if (self != FireArm) orig(self);
        }
        private IEnumerator SeerReleaseOpenBolt(On.FistVR.OpenBoltReceiver.orig_ReleaseSeer orig, OpenBoltReceiver self)
        {
            _isCharging = true;
            _timeCharged = 0f;
            SM.PlayGenericSound(ChargingSounds, self.transform.position);
            while (_timeCharged < ChargeTime)
            {
                if (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold) break;
                _timeCharged += Time.deltaTime;
                yield return null;
            }
            _isCharging = false;
            OpenBoltReceiver.FireSelectorModeType modeType = self.FireSelector_Modes[self.m_fireSelectorMode].ModeType;
            if (!ChargesUpEveryShot && modeType != OpenBoltReceiver.FireSelectorModeType.Single) _isAutomaticFire = true;
            if (_timeCharged >= ChargeTime) orig(self);
            else
            {
                // SM.PlayGenericSound(ChargingAbortSounds, self.transform.position);
                On.FistVR.OpenBoltReceiver.Fire += OpenBoltReceiver_Fire;
                orig(self);
                On.FistVR.OpenBoltReceiver.Fire -= OpenBoltReceiver_Fire;
            }
        }

        private bool OpenBoltReceiver_Fire(On.FistVR.OpenBoltReceiver.orig_Fire orig, OpenBoltReceiver self)
        {
            if (self == FireArm)
            {
                if (!self.Chamber.Fire()) return false;
                self.m_timeSinceFiredShot = 0f;
                self.Fire(self.Chamber, self.GetMuzzle(), true, _timeCharged / ChargeTime, -1f);
                self.FireMuzzleSmoke();
                if (self.UsesDelinker && self.HasBelt) self.DelinkerSystem.Emit(1);
                if (self.HasBelt) self.BeltDD.AddJitter();
                bool twoHandStabilized = self.IsTwoHandStabilized();
                bool foregripStabilized = self.AltGrip != null;
                bool shoulderStabilized = self.IsShoulderStabilized();
                self.Recoil(twoHandStabilized, foregripStabilized, shoulderStabilized, null, 1f);
                bool flag = false;
                OpenBoltReceiver.FireSelectorMode fireSelectorMode = self.FireSelector_Modes[self.m_fireSelectorMode];
                if (fireSelectorMode.ModeType == OpenBoltReceiver.FireSelectorModeType.SuperFastBurst)
                {
                    for (int i = 1; i < self.SuperBurstAmount; i++)
                    {
                        if (self.Magazine.HasARound())
                        {
                            self.Magazine.RemoveRound();
                            self.Fire(self.Chamber, self.GetMuzzle(), false, 1f, -1f);
                            flag = true;
                            self.FireMuzzleSmoke();
                            self.Recoil(twoHandStabilized, foregripStabilized, shoulderStabilized, null, 1f);
                        }
                    }
                }
                if (self.UsesRecoilingSystem)
                {
                    if (flag) self.RecoilingSystem.Recoil(true);
                    else self.RecoilingSystem.Recoil(false);
                }
                if (flag) self.PlayAudioGunShot(false, self.Chamber.GetRound().TailClass, self.Chamber.GetRound().TailClassSuppressed, GM.CurrentPlayerBody.GetCurrentSoundEnvironment());
                else self.PlayAudioGunShot(self.Chamber.GetRound(), GM.CurrentPlayerBody.GetCurrentSoundEnvironment(), 1f);

                return true;
            }
            else return orig(self);
        }

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
            if (FireArm == self && (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold)) _isAutomaticFire = false;
        }

        private void Handgun_ReleaseSeer(On.FistVR.Handgun.orig_ReleaseSeer orig, Handgun self)
        {
            if (!_isCharging && !_isAutomaticFire && self == FireArm) StartCoroutine(SeerReleaseHandgun(orig, self));
            else if (self != FireArm) orig(self);
        }
        private IEnumerator SeerReleaseHandgun(On.FistVR.Handgun.orig_ReleaseSeer orig, Handgun self)
        {
            _isCharging = true;
            _timeCharged = 0f;
            SM.PlayGenericSound(ChargingSounds, self.transform.position);
            while (_timeCharged < ChargeTime)
            {
                if (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold) break;
                _timeCharged += Time.deltaTime;
                yield return null;
            }
            _isCharging = false;
            Handgun.FireSelectorModeType modeType = self.FireSelectorModes[self.m_fireSelectorMode].ModeType;
            if (!ChargesUpEveryShot && modeType != Handgun.FireSelectorModeType.Single) _isAutomaticFire = true;
            if (_timeCharged >= ChargeTime) orig(self);
            else
            {
                //SM.PlayGenericSound(ChargingAbortSounds, self.transform.position);
                On.FistVR.Handgun.Fire += Handgun_Fire;
                orig(self);
                On.FistVR.Handgun.Fire -= Handgun_Fire;
            }
        }

        private bool Handgun_Fire(On.FistVR.Handgun.orig_Fire orig, Handgun self)
        {
            if (self == FireArm)
            {
                if (!self.Chamber.Fire()) return false;
                self.m_timeSinceFiredShot = 0f;
                self.Fire(self.Chamber, self.GetMuzzle(), true, _timeCharged / ChargeTime, -1f);
                self.FireMuzzleSmoke();
                bool twoHandStabilized = self.IsTwoHandStabilized();
                bool foregripStabilized = self.IsForegripStabilized();
                bool shoulderStabilized = self.IsShoulderStabilized();
                float globalLoudnessMultiplier = 1f;
                float verticalRecoilMult = 1f;
                if (self.m_isSlideLockMechanismEngaged)
                {
                    globalLoudnessMultiplier = 0.4f;
                    verticalRecoilMult = 1.5f;
                }
                self.Recoil(twoHandStabilized, foregripStabilized, shoulderStabilized, self.GetRecoilProfile(), verticalRecoilMult);
                self.PlayAudioGunShot(self.Chamber.GetRound(), GM.CurrentPlayerBody.GetCurrentSoundEnvironment(), globalLoudnessMultiplier);
                if (!self.IsSLideLockMechanismEngaged) self.Slide.ImpartFiringImpulse();
                return true;
            }
            else return orig(self);
        }

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
            if (!_isCharging && self == FireArm) StartCoroutine(DropHammerBoltAction(orig, self));
            else if (self != FireArm) orig(self);
        }

        private IEnumerator DropHammerBoltAction(On.FistVR.BoltActionRifle.orig_DropHammer orig, BoltActionRifle self)
        {
            _isCharging = true;
            _timeCharged = 0f;
            SM.PlayGenericSound(ChargingSounds, self.transform.position);
            while (_timeCharged < ChargeTime)
            {
                if (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold) break;
                _timeCharged += Time.deltaTime;
                yield return null;
            }
            _isCharging = false;
            if (_timeCharged >= ChargeTime) orig(self);
            else
            {
                // SM.PlayGenericSound(ChargingAbortSounds, self.transform.position);

                On.FistVR.BoltActionRifle.Fire += BoltActionRifle_Fire;
                orig(self);
                On.FistVR.BoltActionRifle.Fire -= BoltActionRifle_Fire;
            }
        }

        private bool BoltActionRifle_Fire(On.FistVR.BoltActionRifle.orig_Fire orig, BoltActionRifle self)
        {
            if (self == FireArm)
            {
                BoltActionRifle.FireSelectorMode fireSelectorMode = self.FireSelector_Modes[self.m_fireSelectorMode];
                if (!self.Chamber.Fire())
                {
                    return false;
                }
                self.Fire(self.Chamber, self.GetMuzzle(), true, _timeCharged / ChargeTime, -1f);
                self.FireMuzzleSmoke();
                bool twoHandStabilized = self.IsTwoHandStabilized();
                bool foregripStabilized = self.IsForegripStabilized();
                bool shoulderStabilized = self.IsShoulderStabilized();
                self.Recoil(twoHandStabilized, foregripStabilized, shoulderStabilized, null, 1f);
                FVRSoundEnvironment currentSoundEnvironment = GM.CurrentPlayerBody.GetCurrentSoundEnvironment();
                self.PlayAudioGunShot(self.Chamber.GetRound(), currentSoundEnvironment, 1f);
                if (self.PlaysExtraTailOnShot)
                {
                    AudioEvent tailSet = SM.GetTailSet(self.ExtraTail, currentSoundEnvironment);
                    self.m_pool_tail.PlayClipVolumePitchOverride(tailSet, base.transform.position, tailSet.VolumeRange * 1f, self.AudioClipSet.TailPitchMod_Main * tailSet.PitchRange.x, null);
                }
                if (self.HasReciprocatingBarrel)
                {
                    self.RecoilSystem.Recoil(false);
                }
                return true;
            }
            else return orig(self);
        }

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
            if (!_isCharging && self == FireArm) StartCoroutine(ReleaseHammerTubeFed(orig, self));
            else if (self != FireArm) orig(self);
        }

        private IEnumerator ReleaseHammerTubeFed(On.FistVR.TubeFedShotgun.orig_ReleaseHammer orig, TubeFedShotgun self)
        {
            _isCharging = true;
            _timeCharged = 0f;
            SM.PlayGenericSound(ChargingSounds, self.transform.position);
            while (_timeCharged < ChargeTime)
            {
                if (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold) break;
                _timeCharged += Time.deltaTime;
                yield return null;
            }
            _isCharging = false;
            if (_timeCharged >= ChargeTime) orig(self);
            else
            {
                //SM.PlayGenericSound(ChargingAbortSounds, self.transform.position);

                On.FistVR.TubeFedShotgun.Fire += TubeFedShotgun_Fire;
                orig(self);
                On.FistVR.TubeFedShotgun.Fire -= TubeFedShotgun_Fire;
            }
        }

        private bool TubeFedShotgun_Fire(On.FistVR.TubeFedShotgun.orig_Fire orig, TubeFedShotgun self)
        {
            if (self == FireArm)
            {
                if (!self.Chamber.Fire()) return false;
                self.Fire(self.Chamber, self.GetMuzzle(), true, _timeCharged / ChargeTime, -1f);
                self.FireMuzzleSmoke();
                bool twoHandStabilized = self.IsTwoHandStabilized();
                bool foregripStabilized = self.IsForegripStabilized();
                bool shoulderStabilized = self.IsShoulderStabilized();
                self.Recoil(twoHandStabilized, foregripStabilized, shoulderStabilized, null, 1f);
                self.PlayAudioGunShot(self.Chamber.GetRound(), GM.CurrentPlayerBody.GetCurrentSoundEnvironment(), 1f);
                if (self.Mode == TubeFedShotgun.ShotgunMode.Automatic && self.Chamber.GetRound().IsHighPressure)
                {
                    self.Bolt.ImpartFiringImpulse();
                }
                return true;
            }
            else return orig(self);
        }

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
            if (!_isCharging && self == FireArm) StartCoroutine(FireLeverAction(orig, self));
            else if (self != FireArm) orig(self);
        }

        private IEnumerator FireLeverAction(On.FistVR.LeverActionFirearm.orig_Fire orig, LeverActionFirearm self)
        {
            _isCharging = true;
            _timeCharged = 0f;
            SM.PlayGenericSound(ChargingSounds, self.transform.position);
            while (_timeCharged < ChargeTime)
            {
                if (!self.IsHeld || self.m_hand.Input.TriggerUp) break;
                _timeCharged += Time.deltaTime;
                yield return null;
            }
            _isCharging = false;
            if (_timeCharged >= ChargeTime) orig(self);
            else
            {
                // SM.PlayGenericSound(ChargingAbortSounds, self.transform.position);

                ChargedLeverActionFire(self);
            }
        }

        private void ChargedLeverActionFire(LeverActionFirearm self)
        {
            if (self.m_isHammerCocked)
            {
                self.m_isHammerCocked = false;
            }
            else if (self.m_isHammerCocked2)
            {
                self.m_isHammerCocked2 = false;
            }
            self.PlayAudioEvent(FirearmAudioEventType.HammerHit, 1f);
            bool hasFired = false;
            bool firstChamber = true;
            if (self.Chamber.Fire())
            {
                hasFired = true;
                firstChamber = true;
                self.m_isSecondaryMuzzlePos = false;
            }
            else if (self.UsesSecondChamber && self.Chamber2.Fire())
            {
                hasFired = true;
                firstChamber = false;
                self.m_isSecondaryMuzzlePos = true;
            }
            if (hasFired)
            {
                if (firstChamber) self.Fire(self.Chamber, self.GetMuzzle(), true, _timeCharged / ChargeTime, -1f);
                else self.Fire(self.Chamber2, self.SecondMuzzle, true, _timeCharged / ChargeTime, -1f);
                self.FireMuzzleSmoke();
                bool twoHandStabilized = self.IsTwoHandStabilized();
                bool foregripStabilized = self.AltGrip != null;
                bool shoulderStabilized = self.IsShoulderStabilized();
                self.Recoil(twoHandStabilized, foregripStabilized, shoulderStabilized, null, 1f);
                if (firstChamber) self.PlayAudioGunShot(self.Chamber.GetRound(), GM.CurrentPlayerBody.GetCurrentSoundEnvironment(), 1f);
                else self.PlayAudioGunShot(self.Chamber2.GetRound(), GM.CurrentPlayerBody.GetCurrentSoundEnvironment(), 1f);
            }
        }

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
            if (!_isCharging && self == FireArm) StartCoroutine(DropHammerBreakAction(orig, self));
            else if (self != FireArm) orig(self);
        }
        private IEnumerator DropHammerBreakAction(On.FistVR.BreakActionWeapon.orig_DropHammer orig, BreakActionWeapon self)
        {
            _isCharging = true;
            _timeCharged = 0f;
            SM.PlayGenericSound(ChargingSounds, self.transform.position);
            while (_timeCharged < ChargeTime)
            {
                if (!self.m_isLatched || !self.IsHeld || self.m_hand.Input.TriggerFloat <= 0.45f) break;
                _timeCharged += Time.deltaTime;
                yield return null;
            }
            _isCharging = false;
            if (_timeCharged >= ChargeTime) orig(self);
            else
            {
                // SM.PlayGenericSound(ChargingAbortSounds, self.transform.position);

                On.FistVR.BreakActionWeapon.Fire += BreakActionWeapon_Fire;
                orig(self);
                On.FistVR.BreakActionWeapon.Fire -= BreakActionWeapon_Fire;
            }
        }

        private bool BreakActionWeapon_Fire(On.FistVR.BreakActionWeapon.orig_Fire orig, BreakActionWeapon self, int b, bool FireAllBarrels, int index)
        {
            if (self == FireArm)
            {
                self.m_curBarrel = b;
                if (!self.Barrels[b].Chamber.Fire()) return false;
                self.Fire(self.Barrels[b].Chamber, self.GetMuzzle(), true, _timeCharged / ChargeTime, -1f);
                self.FireMuzzleSmoke(self.Barrels[b].MuzzleIndexBarrelFire);
                self.FireMuzzleSmoke(self.Barrels[b].MuzzleIndexBarrelSmoke);
                self.AddGas(self.Barrels[b].GasOutIndexBarrel);
                self.AddGas(self.Barrels[b].GasOutIndexBreach);
                bool twoHandStabilized = self.IsTwoHandStabilized();
                bool foregripStabilized = self.IsForegripStabilized();
                bool shoulderStabilized = self.IsShoulderStabilized();
                self.Recoil(twoHandStabilized, foregripStabilized, shoulderStabilized, null, 1f);
                if (!self.OnlyOneShotSound || !self.firedOneShot)
                {
                    self.firedOneShot = true;
                    self.PlayAudioGunShot(self.Barrels[b].Chamber.GetRound(), GM.CurrentPlayerBody.GetCurrentSoundEnvironment(), 1f);
                }
                if (GM.CurrentSceneSettings.IsAmmoInfinite || GM.CurrentPlayerBody.IsInfiniteAmmo)
                {
                    self.Barrels[b].Chamber.IsSpent = false;
                    self.Barrels[b].Chamber.UpdateProxyDisplay();
                }
                return true;
            }
            else return orig(self, b, FireAllBarrels, index);
        }
#endif
    }
}