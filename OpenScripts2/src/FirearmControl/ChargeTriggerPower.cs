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
    public class ChargeTriggerPower : OpenScripts2_BasePlugin
    {
        public FVRFireArm FireArm;
        [Tooltip("Charge time in seconds")]
        public float ChargeTime = 1f;
        [Tooltip("If checked, every shot will be charged, even in automatic fire. Else only the first shot will be delayed.")]
        public bool ChargesUpEveryShot = false;
        [Tooltip("If checked, it will not charge on empty and just drop the hammer normally.")]
        public bool StopsOnEmpty = false;

        public AudioEvent ChargingSounds;

        public VisualModifier[] VisualModifiers;

        public float ChargeVibrationFrequency = 1000f;
        [Range(0f, 1f)]
        public float ChargeVibrationAmplitude = 1f;

        public float MinMuzzleVelocityMultiplier = 1f;
        public float MaxMuzzleVelocityMultiplier = 2f;

        public bool ChangesRoundClass = false;
        public FireArmRoundClass FullChargeRoundClass;

        public bool ChangesRoundPrefab = false;
        public GameObject FullChargeRoundPrefab;

        private FVRFireArmRound _fullChargeRound;

        //public AudioEvent ChargingAbortSounds;

        private float _timeCharged = 0f;
        private bool _isCharging = false;
        private bool _isAutomaticFire = false;
#if !DEBUG
        public void Awake()
        {
            switch (FireArm)
            {
                case ClosedBoltWeapon w:
                    HookClosedBolt();
                    break;
                case OpenBoltReceiver w:
                    HookOpenBolt();
                    break;
                case Handgun w:
                    HookHandgun();
                    break;
                case BoltActionRifle w:
                    HookBoltActionRifle();
                    break;
                case TubeFedShotgun w:
                    HookTubeFedShotgun();
                    break;
                case LeverActionFirearm w:
                    HookLeverActionFirearm();
                    break;
                case BreakActionWeapon w:
                    HookBreakActionWeapon();
                    break;
                default:
                    LogWarning($"Firearm type \"{FireArm.GetType()}\" not supported! Tell me and I'll see about adding it!");
                    break;
            }
            if (ChangesRoundPrefab)
            {
                On.FistVR.FVRFireArm.Fire += FVRFireArm_Fire;
                try
                {
                    _fullChargeRound = FullChargeRoundPrefab.GetComponent<FVRFireArmRound>();
                }
                catch (Exception)
                {
                    LogError("No FullChargeRoundPrefab provided but ChangesRoundPrefab set to true!");
                }
            }
        }

        public void OnDestroy()
        {
            switch (FireArm)
            {
                case ClosedBoltWeapon w:
                    UnhookClosedBolt();
                    break;
                case OpenBoltReceiver w:
                    UnhookOpenBolt();
                    break;
                case Handgun w:
                    UnhookHandgun();
                    break;
                case BoltActionRifle w:
                    UnhookBoltActionRifle();
                    break;
                case TubeFedShotgun w:
                    UnhookTubeFedShotgun();
                    break;
                case LeverActionFirearm w:
                    UnhookLeverActionRifle();
                    break;
                case BreakActionWeapon w:
                    UnhookBreakActionWeapon();
                    break;
                default:
                    break;
            }
            if (ChangesRoundPrefab)
            {
                On.FistVR.FVRFireArm.Fire -= FVRFireArm_Fire;
            }
        }
        private void FVRFireArm_Fire(On.FistVR.FVRFireArm.orig_Fire orig, FVRFireArm self, FVRFireArmChamber chamber, Transform muzzle, bool doBuzz, float velMult, float rangeOverride)
        {
            if (self == FireArm && _timeCharged >= ChargeTime && _fullChargeRound != null)
            {
                if (doBuzz && self.m_hand != null)
                {
                    self.m_hand.Buzz(self.m_hand.Buzzer.Buzz_GunShot);
                    if (self.AltGrip != null && self.AltGrip.m_hand != null)
                    {
                        self.AltGrip.m_hand.Buzz(self.m_hand.Buzzer.Buzz_GunShot);
                    }
                }
                GM.CurrentSceneSettings.OnShotFired(self);
                if (self.IsSuppressed()) GM.CurrentPlayerBody.VisibleEvent(0.1f);
                else GM.CurrentPlayerBody.VisibleEvent(2f);
                float chamberVelMult = AM.GetChamberVelMult(chamber.RoundType, Vector3.Distance(chamber.transform.position, muzzle.position));
                float num = self.GetCombinedFixedDrop(self.AccuracyClass) * 0.0166667f;
                Vector2 vector = self.GetCombinedFixedDrift(self.AccuracyClass) * 0.0166667f;
                for (int i = 0; i < _fullChargeRound.NumProjectiles; i++)
                {
                    float d = _fullChargeRound.ProjectileSpread + self.m_internalMechanicalMOA + self.GetCombinedMuzzleDeviceAccuracy();

                    if (_fullChargeRound.BallisticProjectilePrefab != null)
                    {
                        Vector3 b = muzzle.forward * 0.005f;
                        GameObject ballisticProjectilePrefab = Instantiate(_fullChargeRound.BallisticProjectilePrefab, muzzle.position - b, muzzle.rotation);
                        Vector2 vector2 = (UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle) * 0.33333334f * d;
                        ballisticProjectilePrefab.transform.Rotate(new Vector3(vector2.x + vector.y + num, vector2.y + vector.x, 0f));
                        BallisticProjectile component = ballisticProjectilePrefab.GetComponent<BallisticProjectile>();
                        component.Fire(component.MuzzleVelocityBase * chamber.ChamberVelocityMultiplier * velMult * chamberVelMult, ballisticProjectilePrefab.transform.forward, self, true);
                        if (rangeOverride > 0f)
                        {
                            component.ForceSetMaxDist(rangeOverride);
                        }
                    }
                }
            }
            else orig(self, chamber, muzzle, doBuzz, velMult, rangeOverride);
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
            On.FistVR.ClosedBoltWeapon.Fire -= ClosedBoltWeapon_Fire;
        }
        private void HookClosedBolt()
        {
            On.FistVR.ClosedBoltWeapon.DropHammer += ClosedBoltWeapon_DropHammer;
            On.FistVR.ClosedBoltWeapon.FVRUpdate += ClosedBoltWeapon_FVRUpdate;
            On.FistVR.ClosedBoltWeapon.Fire += ClosedBoltWeapon_Fire;
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
            _isCharging = true;
            _timeCharged = 0f;
            FVRPooledAudioSource audioSource = SM.PlayCoreSound(FVRPooledAudioType.Generic, ChargingSounds, self.transform.position);
            SteamVR_Action_Vibration handVibration = self.m_hand.Vibration;
            while (_timeCharged < ChargeTime)
            {
                if (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold) break;
                _timeCharged += Time.deltaTime;
                yield return null;
            }
            _isCharging = false;
            audioSource.Source.Stop();
            ClosedBoltWeapon.FireSelectorModeType modeType = self.FireSelector_Modes[self.m_fireSelectorMode].ModeType;
            if (!ChargesUpEveryShot && modeType != ClosedBoltWeapon.FireSelectorModeType.Single) _isAutomaticFire = true;

            orig(self);

            if (self.FireSelector_Modes[self.m_fireSelectorMode].ModeType == ClosedBoltWeapon.FireSelectorModeType.Single) _timeCharged = 0f;
        }

        private bool ClosedBoltWeapon_Fire(On.FistVR.ClosedBoltWeapon.orig_Fire orig, ClosedBoltWeapon self)
        {
            if (FireArm == self)
            {
                if (ChangesRoundClass && _timeCharged >= ChargeTime)
                {
                    if (self.Chamber != null && self.Chamber.m_round != null && self.Chamber.m_round.RoundClass != FullChargeRoundClass)
                    {
                        self.Chamber.m_round = AM.GetRoundSelfPrefab(self.Chamber.m_round.RoundType, FullChargeRoundClass).GetGameObject().GetComponent<FVRFireArmRound>();
                        self.Chamber.UpdateProxyDisplay();
                    }
                }

                if (!self.Chamber.Fire()) return false;
                self.m_timeSinceFiredShot = 0f;
                float velMult;
                if (self.UsesStickyDetonation) velMult = 1f + Mathf.Lerp(0f, self.StickyMaxMultBonus, self.m_stickyChargeUp);
                else velMult = Mathf.Lerp(MinMuzzleVelocityMultiplier,MaxMuzzleVelocityMultiplier,_timeCharged / ChargeTime);
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
        #endregion

        #region OpenBoltReceiver Hooks and Coroutine
        // OpenBoltReceiver Hooks and Coroutine
        private void UnhookOpenBolt()
        {
            On.FistVR.OpenBoltReceiver.ReleaseSeer -= OpenBoltReceiver_ReleaseSeer;
            On.FistVR.OpenBoltReceiver.FVRUpdate -= OpenBoltReceiver_FVRUpdate;
            On.FistVR.OpenBoltReceiver.Fire -= OpenBoltReceiver_Fire;
        }
        private void HookOpenBolt()
        {
            On.FistVR.OpenBoltReceiver.ReleaseSeer += OpenBoltReceiver_ReleaseSeer;
            On.FistVR.OpenBoltReceiver.FVRUpdate += OpenBoltReceiver_FVRUpdate;
            On.FistVR.OpenBoltReceiver.Fire += OpenBoltReceiver_Fire;
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
            else if (self == FireArm && !_isCharging && _isAutomaticFire) orig(self);
            else if (self == FireArm && !_isCharging && StopsOnEmpty && (FireArm.Magazine == null || (FireArm.Magazine != null && !FireArm.Magazine.HasARound()))) orig(self);
            else if (self != FireArm) orig(self);
        }
        private IEnumerator SeerReleaseOpenBolt(On.FistVR.OpenBoltReceiver.orig_ReleaseSeer orig, OpenBoltReceiver self)
        {
            _isCharging = true;
            _timeCharged = 0f;
            FVRPooledAudioSource audioSource = SM.PlayCoreSound(FVRPooledAudioType.Generic, ChargingSounds, self.transform.position);
            SteamVR_Action_Vibration handVibration = self.m_hand.Vibration;
            while (_timeCharged < ChargeTime)
            {
                if (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold) break;
                _timeCharged += Time.deltaTime;
                handVibration.Execute(0f, Time.fixedDeltaTime, ChargeVibrationFrequency * (1 - (_timeCharged / ChargeTime)), ChargeVibrationAmplitude, self.m_hand.HandSource);
                yield return null;
            }
            _isCharging = false;
            audioSource.Source.Stop();
            OpenBoltReceiver.FireSelectorModeType modeType = self.FireSelector_Modes[self.m_fireSelectorMode].ModeType;
            if (!ChargesUpEveryShot && modeType != OpenBoltReceiver.FireSelectorModeType.Single) _isAutomaticFire = true;

            orig(self);

            if (self.FireSelector_Modes[self.m_fireSelectorMode].ModeType == OpenBoltReceiver.FireSelectorModeType.Single) _timeCharged = 0f;
        }

        private bool OpenBoltReceiver_Fire(On.FistVR.OpenBoltReceiver.orig_Fire orig, OpenBoltReceiver self)
        {
            if (self == FireArm)
            {
                if (ChangesRoundClass && _timeCharged >= ChargeTime)
                {
                    if (self.Chamber != null && self.Chamber.m_round != null && self.Chamber.m_round.RoundClass != FullChargeRoundClass)
                    {
                        self.Chamber.m_round = AM.GetRoundSelfPrefab(self.Chamber.m_round.RoundType, FullChargeRoundClass).GetGameObject().GetComponent<FVRFireArmRound>();
                        self.Chamber.UpdateProxyDisplay();
                    }
                }

                if (!self.Chamber.Fire()) return false;
                self.m_timeSinceFiredShot = 0f;
                self.Fire(self.Chamber, self.GetMuzzle(), true, Mathf.Lerp(MinMuzzleVelocityMultiplier, MaxMuzzleVelocityMultiplier, _timeCharged / ChargeTime), -1f);
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
                            self.Fire(self.Chamber, self.GetMuzzle(), false, Mathf.Lerp(MinMuzzleVelocityMultiplier, MaxMuzzleVelocityMultiplier, _timeCharged / ChargeTime), -1f);
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
        #endregion

        #region Handgun Hooks and Coroutine
        // Handgun Hooks and Coroutine
        private void UnhookHandgun()
        {
            On.FistVR.Handgun.ReleaseSeer -= Handgun_ReleaseSeer;
            On.FistVR.Handgun.FVRUpdate -= Handgun_FVRUpdate;
            On.FistVR.Handgun.Fire -= Handgun_Fire;
        }
        private void HookHandgun()
        {
            On.FistVR.Handgun.ReleaseSeer += Handgun_ReleaseSeer;
            On.FistVR.Handgun.FVRUpdate += Handgun_FVRUpdate;
            On.FistVR.Handgun.Fire += Handgun_Fire;
        }

        private void Handgun_FVRUpdate(On.FistVR.Handgun.orig_FVRUpdate orig, Handgun self)
        {
            orig(self);
            if (FireArm == self)
            {
                if (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold)
                {
                    _isAutomaticFire = false;
                    _timeCharged = 0f;
                }
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
            _isCharging = true;
            _timeCharged = 0f;
            FVRPooledAudioSource audioSource = SM.PlayCoreSound(FVRPooledAudioType.Generic, ChargingSounds, self.transform.position);
            SteamVR_Action_Vibration handVibration = self.m_hand.Vibration;
            while (_timeCharged < ChargeTime)
            {
                if (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold) break;
                _timeCharged += Time.deltaTime;
                handVibration.Execute(0f, Time.fixedDeltaTime, ChargeVibrationFrequency * (1 - (_timeCharged / ChargeTime)), ChargeVibrationAmplitude, self.m_hand.HandSource);
                yield return null;
            }
            _isCharging = false;
            audioSource.Source.Stop();
            Handgun.FireSelectorModeType modeType = self.FireSelectorModes[self.m_fireSelectorMode].ModeType;
            if (!ChargesUpEveryShot && modeType != Handgun.FireSelectorModeType.Single) _isAutomaticFire = true;

            orig(self);

            if (self.FireSelectorModes[self.m_fireSelectorMode].ModeType == Handgun.FireSelectorModeType.Single) _timeCharged = 0f;
        }

        private bool Handgun_Fire(On.FistVR.Handgun.orig_Fire orig, Handgun self)
        {
            if (self == FireArm)
            {
                if (ChangesRoundClass && _timeCharged >= ChargeTime)
                {
                    if (self.Chamber != null && self.Chamber.m_round != null && self.Chamber.m_round.RoundClass != FullChargeRoundClass)
                    {
                        self.Chamber.m_round = AM.GetRoundSelfPrefab(self.Chamber.m_round.RoundType, FullChargeRoundClass).GetGameObject().GetComponent<FVRFireArmRound>();
                        self.Chamber.UpdateProxyDisplay();
                    }
                }

                if (!self.Chamber.Fire()) return false;
                self.m_timeSinceFiredShot = 0f;
                self.Fire(self.Chamber, self.GetMuzzle(), true, Mathf.Lerp(MinMuzzleVelocityMultiplier, MaxMuzzleVelocityMultiplier, _timeCharged / ChargeTime), -1f);
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
        #endregion

        #region BoltActionRifle Hooks and Coroutine
        // BoltActionRifle Hooks and Coroutine
        private void UnhookBoltActionRifle()
        {
            On.FistVR.BoltActionRifle.DropHammer -= BoltActionRifle_DropHammer;
            On.FistVR.BoltActionRifle.Fire -= BoltActionRifle_Fire;
        }

        private void HookBoltActionRifle()
        {
            On.FistVR.BoltActionRifle.DropHammer += BoltActionRifle_DropHammer;
            On.FistVR.BoltActionRifle.Fire += BoltActionRifle_Fire;
        }

        private void BoltActionRifle_DropHammer(On.FistVR.BoltActionRifle.orig_DropHammer orig, BoltActionRifle self)
        {
            if (self == FireArm && !_isCharging && (!StopsOnEmpty || (StopsOnEmpty && self.Chamber.IsFull))) StartCoroutine(DropHammerBoltAction(orig, self));
            else if (self == FireArm && !_isCharging && StopsOnEmpty && !self.Chamber.IsFull) orig(self);
            else if (self != FireArm) orig(self);
        }

        private IEnumerator DropHammerBoltAction(On.FistVR.BoltActionRifle.orig_DropHammer orig, BoltActionRifle self)
        {
            _isCharging = true;
            _timeCharged = 0f;
            FVRPooledAudioSource audioSource = SM.PlayCoreSound(FVRPooledAudioType.Generic, ChargingSounds, self.transform.position);
            SteamVR_Action_Vibration handVibration = self.m_hand.Vibration;
            while (_timeCharged < ChargeTime)
            {
                if (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold) break;
                _timeCharged += Time.deltaTime;
                handVibration.Execute(0f, Time.fixedDeltaTime, ChargeVibrationFrequency * (1 - (_timeCharged / ChargeTime)), ChargeVibrationAmplitude, self.m_hand.HandSource);
                yield return null;
            }
            _isCharging = false;
            audioSource.Source.Stop();

            orig(self);

            _timeCharged = 0f;
        }

        private bool BoltActionRifle_Fire(On.FistVR.BoltActionRifle.orig_Fire orig, BoltActionRifle self)
        {
            if (self == FireArm)
            {
                BoltActionRifle.FireSelectorMode fireSelectorMode = self.FireSelector_Modes[self.m_fireSelectorMode];
                if (ChangesRoundClass && _timeCharged >= ChargeTime)
                {
                    if (self.Chamber != null && self.Chamber.m_round != null && self.Chamber.m_round.RoundClass != FullChargeRoundClass)
                    {
                        self.Chamber.m_round = AM.GetRoundSelfPrefab(self.Chamber.m_round.RoundType, FullChargeRoundClass).GetGameObject().GetComponent<FVRFireArmRound>();
                        self.Chamber.UpdateProxyDisplay();
                    }
                }

                if (!self.Chamber.Fire()) return false;
                self.Fire(self.Chamber, self.GetMuzzle(), true, Mathf.Lerp(MinMuzzleVelocityMultiplier, MaxMuzzleVelocityMultiplier, _timeCharged / ChargeTime), -1f);
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
        #endregion

        #region TubeFedShotgun Hooks and Coroutine
        // TubeFedShotgun Hooks and Coroutine
        private void UnhookTubeFedShotgun()
        {
            On.FistVR.TubeFedShotgun.ReleaseHammer -= TubeFedShotgun_ReleaseHammer;
            On.FistVR.TubeFedShotgun.Fire -= TubeFedShotgun_Fire;
        }

        private void HookTubeFedShotgun()
        {
            On.FistVR.TubeFedShotgun.ReleaseHammer += TubeFedShotgun_ReleaseHammer;
            On.FistVR.TubeFedShotgun.Fire += TubeFedShotgun_Fire;
        }

        private void TubeFedShotgun_ReleaseHammer(On.FistVR.TubeFedShotgun.orig_ReleaseHammer orig, TubeFedShotgun self)
        {
            if (self == FireArm && !_isCharging && (!StopsOnEmpty || (StopsOnEmpty && self.Chamber.IsFull))) StartCoroutine(ReleaseHammerTubeFed(orig, self));
            else if (self == FireArm && !_isCharging && StopsOnEmpty && !self.Chamber.IsFull) orig(self);
            else if (self != FireArm) orig(self);
        }

        private IEnumerator ReleaseHammerTubeFed(On.FistVR.TubeFedShotgun.orig_ReleaseHammer orig, TubeFedShotgun self)
        {
            _isCharging = true;
            _timeCharged = 0f;
            FVRPooledAudioSource audioSource = SM.PlayCoreSound(FVRPooledAudioType.Generic, ChargingSounds, self.transform.position);
            SteamVR_Action_Vibration handVibration = self.m_hand.Vibration;
            while (_timeCharged < ChargeTime)
            {
                if (!self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold) break;
                _timeCharged += Time.deltaTime;
                handVibration.Execute(0f, Time.fixedDeltaTime, ChargeVibrationFrequency * (1 - (_timeCharged / ChargeTime)), ChargeVibrationAmplitude, self.m_hand.HandSource);
                yield return null;
            }
            _isCharging = false;
            audioSource.Source.Stop();

            orig(self);

            _timeCharged = 0f;
        }

        private bool TubeFedShotgun_Fire(On.FistVR.TubeFedShotgun.orig_Fire orig, TubeFedShotgun self)
        {
            if (self == FireArm)
            {
                if (ChangesRoundClass && _timeCharged >= ChargeTime)
                {
                    if (self.Chamber != null && self.Chamber.m_round != null && self.Chamber.m_round.RoundClass != FullChargeRoundClass)
                    {
                        self.Chamber.m_round = AM.GetRoundSelfPrefab(self.Chamber.m_round.RoundType, FullChargeRoundClass).GetGameObject().GetComponent<FVRFireArmRound>();
                        self.Chamber.UpdateProxyDisplay();
                    }
                }

                if (!self.Chamber.Fire()) return false;
                self.Fire(self.Chamber, self.GetMuzzle(), true, Mathf.Lerp(MinMuzzleVelocityMultiplier, MaxMuzzleVelocityMultiplier, _timeCharged / ChargeTime), -1f);
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
            _isCharging = true;
            _timeCharged = 0f;
            FVRPooledAudioSource audioSource = SM.PlayCoreSound(FVRPooledAudioType.Generic, ChargingSounds, self.transform.position);
            SteamVR_Action_Vibration handVibration = self.m_hand.Vibration;
            while (_timeCharged < ChargeTime)
            {
                if (!self.IsHeld || self.m_hand.Input.TriggerUp) break;
                _timeCharged += Time.deltaTime;
                handVibration.Execute(0f, Time.fixedDeltaTime, ChargeVibrationFrequency * (1 - (_timeCharged / ChargeTime)), ChargeVibrationAmplitude, self.m_hand.HandSource);
                yield return null;
            }
            _isCharging = false;
            audioSource.Source.Stop();

            ChargedLeverActionFire(self);

            _timeCharged = 0f;
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
            if (ChangesRoundClass && _timeCharged >= ChargeTime)
            {
                if (self.Chamber != null && self.Chamber.m_round != null && self.Chamber.m_round.RoundClass != FullChargeRoundClass)
                {
                    self.Chamber.m_round = AM.GetRoundSelfPrefab(self.Chamber.m_round.RoundType, FullChargeRoundClass).GetGameObject().GetComponent<FVRFireArmRound>();
                    self.Chamber.UpdateProxyDisplay();
                }
            }
            if (ChangesRoundClass && _timeCharged >= ChargeTime)
            {
                if (self.Chamber2 != null && self.Chamber2.m_round != null && self.Chamber2.m_round.RoundClass != FullChargeRoundClass)
                {
                    self.Chamber2.m_round = AM.GetRoundSelfPrefab(self.Chamber2.m_round.RoundType, FullChargeRoundClass).GetGameObject().GetComponent<FVRFireArmRound>();
                    self.Chamber2.UpdateProxyDisplay();
                }
            }
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
                if (firstChamber) self.Fire(self.Chamber, self.GetMuzzle(), true, Mathf.Lerp(MinMuzzleVelocityMultiplier, MaxMuzzleVelocityMultiplier, _timeCharged / ChargeTime), -1f);
                else self.Fire(self.Chamber2, self.SecondMuzzle, true, Mathf.Lerp(MinMuzzleVelocityMultiplier, MaxMuzzleVelocityMultiplier, _timeCharged / ChargeTime), -1f);
                self.FireMuzzleSmoke();
                bool twoHandStabilized = self.IsTwoHandStabilized();
                bool foregripStabilized = self.AltGrip != null;
                bool shoulderStabilized = self.IsShoulderStabilized();
                self.Recoil(twoHandStabilized, foregripStabilized, shoulderStabilized, null, 1f);
                if (firstChamber) self.PlayAudioGunShot(self.Chamber.GetRound(), GM.CurrentPlayerBody.GetCurrentSoundEnvironment(), 1f);
                else self.PlayAudioGunShot(self.Chamber2.GetRound(), GM.CurrentPlayerBody.GetCurrentSoundEnvironment(), 1f);
            }
        }
        #endregion

        #region BreakOpenShotgun Hooks and Coroutine
        // BreakOpenShotgun Hooks and Coroutine
        private void UnhookBreakActionWeapon()
        {
            On.FistVR.BreakActionWeapon.DropHammer -= BreakActionWeapon_DropHammer;
            On.FistVR.BreakActionWeapon.Fire -= BreakActionWeapon_Fire;
        }
        private void HookBreakActionWeapon()
        {
            On.FistVR.BreakActionWeapon.DropHammer += BreakActionWeapon_DropHammer;
            On.FistVR.BreakActionWeapon.Fire += BreakActionWeapon_Fire;
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

            if (self == FireArm && !_isCharging && (!StopsOnEmpty || (StopsOnEmpty && self.Barrels[i].Chamber.IsFull && !self.Barrels[i].Chamber.IsSpent))) StartCoroutine(DropHammerBreakAction(orig, self));
            else if (self == FireArm && !_isCharging && StopsOnEmpty && (!self.Barrels[i].Chamber.IsFull || self.Barrels[i].Chamber.IsSpent)) orig(self);
            else if (self != FireArm) orig(self);
        }
        private IEnumerator DropHammerBreakAction(On.FistVR.BreakActionWeapon.orig_DropHammer orig, BreakActionWeapon self)
        {
            _isCharging = true;
            _timeCharged = 0f;
            FVRPooledAudioSource audioSource = SM.PlayCoreSound(FVRPooledAudioType.Generic, ChargingSounds, self.transform.position);
            SteamVR_Action_Vibration handVibration = self.m_hand.Vibration;
            while (_timeCharged < ChargeTime)
            {
                if (!self.m_isLatched || !self.IsHeld || self.m_hand.Input.TriggerFloat <= 0.45f) break;
                _timeCharged += Time.deltaTime;
                handVibration.Execute(0f, Time.fixedDeltaTime, ChargeVibrationFrequency * (1 - (_timeCharged / ChargeTime)), ChargeVibrationAmplitude, self.m_hand.HandSource);
                yield return null;
            }
            _isCharging = false;
            audioSource.Source.Stop();

            orig(self);

            _timeCharged = 0f;
        }

        private bool BreakActionWeapon_Fire(On.FistVR.BreakActionWeapon.orig_Fire orig, BreakActionWeapon self, int b, bool FireAllBarrels, int index)
        {
            if (self == FireArm)
            {
                self.m_curBarrel = b;
                if (ChangesRoundClass && _timeCharged >= ChargeTime)
                {
                    if (self.Barrels[b].Chamber != null && self.Barrels[b].Chamber.m_round != null && self.Barrels[b].Chamber.m_round.RoundClass != FullChargeRoundClass)
                    {
                        self.Barrels[b].Chamber.m_round = AM.GetRoundSelfPrefab(self.Barrels[b].Chamber.m_round.RoundType, FullChargeRoundClass).GetGameObject().GetComponent<FVRFireArmRound>();
                        self.Barrels[b].Chamber.UpdateProxyDisplay();
                    }
                }
                if (!self.Barrels[b].Chamber.Fire()) return false;
                self.Fire(self.Barrels[b].Chamber, self.GetMuzzle(), true, Mathf.Lerp(MinMuzzleVelocityMultiplier, MaxMuzzleVelocityMultiplier, _timeCharged / ChargeTime), -1f);
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
        #endregion
#endif
    }
}