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
        [Header("Charge Trigger Power Config")]
        public FVRFireArm FireArm;
        [Tooltip("Charge time in seconds")]
        public float ChargeTime = 1f;
        [Tooltip("If checked, every shot will be charged, even in automatic fire. Else only the first shot will be delayed.")]
        public bool ChargesUpEveryShot = false;
        [Tooltip("If checked, it will not charge on empty and just drop the hammer normally.")]
        public bool StopsOnEmpty = false;
        public float ChargeVibrationFrequency = 1000f;
        [Range(0f, 1f)]
        public float ChargeVibrationAmplitude = 1f;

        public float MinMuzzleVelocityMultiplier = 1f;
        public float MaxMuzzleVelocityMultiplier = 2f;


        [Header("Optional")]
        public AudioEvent ChargingSounds;

        public VisualModifier[] VisualModifiers;

        public bool ChangesRoundClass = false;
        public FireArmRoundClass FullChargeRoundClass;

        public bool ChangesRoundPrefab = false;
        public GameObject FullChargeRoundPrefab;

        [Serializable]
        public class FullChargeShotParticleEmitter
        {
            public ParticleSystem FullChargeShotParticleSystem;
            public int NumbersOfParticlesToEmit;
        }

        public FullChargeShotParticleEmitter[] FullChargeShotParticleEmitters;

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
                case Revolver w:
                    HookRevolver();
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

            if (FullChargeShotParticleEmitters.Length > 0)
            {
                GM.CurrentSceneSettings.ShotFiredEvent += CurrentSceneSettings_ShotFiredEvent;
            }
        }

        private void CurrentSceneSettings_ShotFiredEvent(FVRFireArm firearm)
        {
            if (firearm == FireArm && _timeCharged >= ChargeTime)
            {
                foreach (var emitter in FullChargeShotParticleEmitters)
                {
                    emitter.FullChargeShotParticleSystem.Emit(emitter.NumbersOfParticlesToEmit);
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
                case Revolver w:
                    UnhookRevolver();
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
            else if (FireArm == self && StopsOnEmpty && (!self.Chamber.IsFull || self.Chamber.IsFull && self.Chamber.IsSpent) && (!self.m_proxy.IsFull || self.m_proxy.IsFull && self.m_proxy.IsSpent) && (self.Magazine == null || !self.Magazine.HasARound()))
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
            else if (FireArm == self && StopsOnEmpty && (!self.Chamber.IsFull || self.Chamber.IsFull && self.Chamber.IsSpent) && (!self.m_proxy.IsFull || self.m_proxy.IsFull && self.m_proxy.IsSpent) && (self.Magazine == null || !self.Magazine.HasARound()))
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
            yield return DropHammer(self);

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
            if (FireArm == self && !self.IsHeld || self.m_hand.Input.TriggerFloat < self.TriggerResetThreshold)
            {
                _isAutomaticFire = false;
                _timeCharged = 0f;
            }
            else if (FireArm == self && StopsOnEmpty && (!self.Chamber.IsFull || self.Chamber.IsFull && self.Chamber.IsSpent) && (!self.m_proxy.IsFull || self.m_proxy.IsFull && self.m_proxy.IsSpent) && (self.Magazine == null || !self.Magazine.HasARound()))
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
            if (self == FireArm && !_isCharging && (!StopsOnEmpty || (StopsOnEmpty && self.Chamber.IsFull && !self.Chamber.IsSpent))) StartCoroutine(DropHammerBoltAction(orig, self));
            else if (self == FireArm && !_isCharging && StopsOnEmpty && (!self.Chamber.IsFull || self.Chamber.IsSpent)) orig(self);
            else if (self != FireArm) orig(self);
        }

        private IEnumerator DropHammerBoltAction(On.FistVR.BoltActionRifle.orig_DropHammer orig, BoltActionRifle self)
        {
            yield return DropHammer(self);

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
                    self.m_pool_tail.PlayClipVolumePitchOverride(tailSet, self.transform.position, tailSet.VolumeRange * 1f, self.AudioClipSet.TailPitchMod_Main * tailSet.PitchRange.x, null);
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
            yield return DropHammer(self);

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
            yield return DropHammer(self);

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
            yield return DropHammer(self);

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

        #region Revolver Hooks and Coroutine
        // BreakOpenShotgun Hooks and Coroutine
        private void UnhookRevolver()
        {
            On.FistVR.Revolver.UpdateTriggerHammer -= Revolver_UpdateTriggerHammer;
            On.FistVR.Revolver.Fire -= Revolver_Fire;
        }

        private void HookRevolver()
        {
            On.FistVR.Revolver.UpdateTriggerHammer += Revolver_UpdateTriggerHammer;
            On.FistVR.Revolver.Fire += Revolver_Fire;
        }

        private void Revolver_Fire(On.FistVR.Revolver.orig_Fire orig, Revolver self)
        {
            if (self == FireArm)
            {
                int chamberToFire = self.CurChamber + self.ChamberOffset;
                if (chamberToFire >= self.Cylinder.numChambers)
                {
                    chamberToFire -= self.Cylinder.numChambers;
                }

                if (ChangesRoundClass && _timeCharged >= ChargeTime)
                {
                    if (self.Chambers[chamberToFire] != null && self.Chambers[chamberToFire].m_round != null && self.Chambers[chamberToFire].m_round.RoundClass != FullChargeRoundClass)
                    {
                        self.Chambers[chamberToFire].m_round = AM.GetRoundSelfPrefab(self.Chambers[chamberToFire].m_round.RoundType, FullChargeRoundClass).GetGameObject().GetComponent<FVRFireArmRound>();
                        self.Chambers[chamberToFire].UpdateProxyDisplay();
                    }
                }

                FVRFireArmChamber fvrfireArmChamber = self.Chambers[chamberToFire];
                self.Fire(fvrfireArmChamber, self.GetMuzzle(), true, Mathf.Lerp(MinMuzzleVelocityMultiplier, MaxMuzzleVelocityMultiplier, _timeCharged / ChargeTime), -1f);
                self.FireMuzzleSmoke();
                if (fvrfireArmChamber.GetRound().IsHighPressure)
                {
                    bool flag = self.IsTwoHandStabilized();
                    bool flag2 = self.AltGrip != null;
                    bool flag3 = self.IsShoulderStabilized();
                    self.Recoil(flag, flag2, flag3, null, 1f);
                }
                self.PlayAudioGunShot(fvrfireArmChamber.GetRound(), GM.CurrentPlayerBody.GetCurrentSoundEnvironment(), self.ShotLoudnessMult);
                if (fvrfireArmChamber.GetRound().IsCaseless)
                {
                    fvrfireArmChamber.SetRound(null, false);
                }
            }
            else orig(self);
        }

        private void Revolver_UpdateTriggerHammer(On.FistVR.Revolver.orig_UpdateTriggerHammer orig, Revolver self)
        {
            if (self == FireArm)
            {
                if (self.m_hasTriggeredUpSinceBegin && !self.m_isSpinning && !self.IsAltHeld && self.isCylinderArmLocked)
                {
                    self.m_tarTriggerFloat = self.m_hand.Input.TriggerFloat;
                    self.m_tarRealTriggerFloat = self.m_hand.Input.TriggerFloat;
                }
                else
                {
                    self.m_tarTriggerFloat = 0f;
                    self.m_tarRealTriggerFloat = 0f;
                }
                if (self.m_isHammerLocked)
                {
                    self.m_tarTriggerFloat += 0.8f;
                    self.m_triggerCurrentRot = Mathf.Lerp(self.m_triggerForwardRot, self.m_triggerBackwardRot, self.m_curTriggerFloat);
                }
                else
                {
                    self.m_triggerCurrentRot = Mathf.Lerp(self.m_triggerForwardRot, self.m_triggerBackwardRot, self.m_curTriggerFloat);
                }
                self.m_curTriggerFloat = Mathf.MoveTowards(self.m_curTriggerFloat, self.m_tarTriggerFloat, Time.deltaTime * 14f);
                self.m_curRealTriggerFloat = Mathf.MoveTowards(self.m_curRealTriggerFloat, self.m_tarRealTriggerFloat, Time.deltaTime * 14f);
                if (Mathf.Abs(self.m_triggerCurrentRot - self.lastTriggerRot) > 0.01f)
                {
                    if (self.Trigger != null)
                    {
                        self.Trigger.localEulerAngles = new Vector3(self.m_triggerCurrentRot, 0f, 0f);
                    }
                    for (int i = 0; i < self.TPieces.Count; i++)
                    {
                        self.SetAnimatedComponent(self.TPieces[i].TPiece, Mathf.Lerp(self.TPieces[i].TRange.x, self.TPieces[i].TRange.y, self.m_curTriggerFloat), self.TPieces[i].TInterp, self.TPieces[i].TAxis);
                    }
                }
                self.lastTriggerRot = self.m_triggerCurrentRot;
                if (self.m_shouldRecock)
                {
                    self.m_shouldRecock = false;
                    self.InitiateRecock();
                }
                if (self.CanFan && self.IsHeld && !self.m_isHammerLocked && self.m_recockingState == Revolver.RecockingState.Forward && self.m_hand.OtherHand != null)
                {
                    Vector3 velLinearWorld = self.m_hand.OtherHand.Input.VelLinearWorld;
                    float num = Vector3.Distance(self.m_hand.OtherHand.PalmTransform.position, self.HammerFanDir.position);
                    if (num < 0.15f && Vector3.Angle(velLinearWorld, self.HammerFanDir.forward) < 60f && velLinearWorld.magnitude > 1f)
                    {
                        self.InitiateRecock();
                        self.PlayAudioEvent(FirearmAudioEventType.Prefire, 1f);
                    }
                }
                if (!self.m_hasTriggerCycled || (!self.IsDoubleActionTrigger && !self.DoesFiringRecock))
                {
                    bool flag = false;
                    if (self.m_recockingState != Revolver.RecockingState.Forward)
                    {
                        flag = true;
                    }
                    if (!flag && self.m_curTriggerFloat >= 0.95f && (self.m_isHammerLocked || self.IsDoubleActionTrigger) && !self.m_hand.Input.TouchpadPressed && self.m_isCylinderArmLocked)
                    {
                        int nextChamber = self.CurChamber + self.ChamberOffset + (self.IsCylinderRotClockwise ? 1 : -1);
                        if (nextChamber >= self.Cylinder.numChambers)
                        {
                            nextChamber -= self.Cylinder.numChambers;
                        }

                        if (self == FireArm && !_isCharging && (!StopsOnEmpty || (StopsOnEmpty && self.Chambers[nextChamber].IsFull && !self.Chambers[nextChamber].IsSpent))) StartCoroutine(DropHammerRevolverDelay(self));
                        else if (self == FireArm && !_isCharging && StopsOnEmpty && (!self.Chambers[nextChamber].IsFull || self.Chambers[nextChamber].IsFull)) DropHammerRevolver(self);
                        else if (self != FireArm) DropHammerRevolver(self);
                    }
                    else if ((self.m_curTriggerFloat <= 0.14f || !self.IsDoubleActionTrigger) && !self.m_isHammerLocked && self.CanManuallyCockHammer)
                    {
                        bool flag2 = false;
                        if (self.DoesFiringRecock && self.m_recockingState != Revolver.RecockingState.Forward)
                        {
                            flag2 = true;
                        }
                        if (self.DoesTriggerBlockHammer && self.m_curTriggerFloat > 0.14f)
                        {
                            flag2 = true;
                        }
                        if (!self.IsAltHeld && !flag2)
                        {
                            if (self.m_hand.IsInStreamlinedMode)
                            {
                                if (self.m_hand.Input.AXButtonDown)
                                {
                                    self.m_isHammerLocked = true;
                                    self.PlayAudioEvent(FirearmAudioEventType.Prefire, 1f);
                                }
                            }
                            else if (self.m_hand.Input.TouchpadDown && Vector2.Angle(self.TouchPadAxes, Vector2.down) < 45f)
                            {
                                self.m_isHammerLocked = true;
                                self.PlayAudioEvent(FirearmAudioEventType.Prefire, 1f);
                            }
                        }
                    }
                }
                else if (self.m_hasTriggerCycled && self.m_curRealTriggerFloat <= 0.08f)
                {
                    self.m_hasTriggerCycled = false;
                    self.PlayAudioEvent(FirearmAudioEventType.TriggerReset, 1f);
                }
                if (!self.isChiappaHammer)
                {
                    if (self.m_hasTriggerCycled || !self.IsDoubleActionTrigger)
                    {
                        if (self.m_isHammerLocked)
                        {
                            self.m_hammerCurrentRot = Mathf.Lerp(self.m_hammerCurrentRot, self.m_hammerBackwardRot, Time.deltaTime * 10f);
                        }
                        else
                        {
                            self.m_hammerCurrentRot = Mathf.Lerp(self.m_hammerCurrentRot, self.m_hammerForwardRot, Time.deltaTime * 30f);
                        }
                    }
                    else if (self.m_isHammerLocked)
                    {
                        self.m_hammerCurrentRot = Mathf.Lerp(self.m_hammerCurrentRot, self.m_hammerBackwardRot, Time.deltaTime * 10f);
                    }
                    else
                    {
                        self.m_hammerCurrentRot = Mathf.Lerp(self.m_hammerForwardRot, self.m_hammerBackwardRot, self.m_curTriggerFloat);
                    }
                }
                if (self.isChiappaHammer)
                {
                    bool cockChiappaHammer = false;
                    if (self.m_hand.IsInStreamlinedMode && self.m_hand.Input.AXButtonPressed)
                    {
                        cockChiappaHammer = true;
                    }
                    else if (Vector2.Angle(self.m_hand.Input.TouchpadAxes, Vector2.down) < 45f && self.m_hand.Input.TouchpadPressed)
                    {
                        cockChiappaHammer = true;
                    }
                    if (self.m_curTriggerFloat <= 0.02f && !self.IsAltHeld && cockChiappaHammer)
                    {
                        self.m_hammerCurrentRot = Mathf.Lerp(self.m_hammerCurrentRot, self.m_hammerBackwardRot, Time.deltaTime * 15f);
                    }
                    else
                    {
                        self.m_hammerCurrentRot = Mathf.Lerp(self.m_hammerCurrentRot, self.m_hammerForwardRot, Time.deltaTime * 6f);
                    }
                }
                if (self.Hammer != null)
                {
                    self.Hammer.localEulerAngles = new Vector3(self.m_hammerCurrentRot, 0f, 0f);
                }
            }
            else orig(self);
        }

        private IEnumerator DropHammerRevolverDelay(Revolver self)
        {
            yield return DropHammer(self);

            DropHammerRevolver(self);

            _timeCharged = 0f;
        }

        private void DropHammerRevolver(Revolver self)
        {
            self.m_hasTriggerCycled = true;
            self.m_isHammerLocked = false;
            if (self.IsCylinderRotClockwise)
            {
                self.CurChamber++;
            }
            else
            {
                self.CurChamber--;
            }
            self.m_curChamberLerp = 0f;
            self.m_tarChamberLerp = 0f;
            self.PlayAudioEvent(FirearmAudioEventType.HammerHit, 1f);
            int nextChamber = self.CurChamber + self.ChamberOffset;
            if (nextChamber >= self.Cylinder.numChambers)
            {
                nextChamber -= self.Cylinder.numChambers;
            }
            if (self.Chambers[nextChamber].IsFull && !self.Chambers[nextChamber].IsSpent)
            {
                self.Chambers[nextChamber].Fire();
                self.Fire();
                if (GM.CurrentSceneSettings.IsAmmoInfinite || GM.CurrentPlayerBody.IsInfiniteAmmo)
                {
                    self.Chambers[nextChamber].IsSpent = false;
                    self.Chambers[nextChamber].UpdateProxyDisplay();
                }
                if (self.DoesFiringRecock)
                {
                    self.m_shouldRecock = true;
                }
            }
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