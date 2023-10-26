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
    public class ChargeTriggerBreakActionWeapon : OpenScripts2_BasePlugin
    {
        public BreakActionWeapon BreakAction;
        [Tooltip("Charge time in seconds")]
        public float ChargeTime = 1f;
        [Tooltip("If checked, it will not charge on empty and just drop the hammer normally.")]
        public bool StopsOnEmpty = false;

        public AudioEvent ChargingSounds;

        public float ChargeVibrationFrequency = 1000f;
        [Range(0f, 1f)]
        public float ChargeVibrationAmplitude = 1f;

        [Serializable]
        public class BarrelEffect
        {
            public VisualModifier[] VisualModifiers;
        }

        public BarrelEffect[] BarrelEffects;

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

#if !DEBUG
        public void Awake()
        {
            HookBreakActionWeapon();
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
            UnhookBreakActionWeapon();
            if (ChangesRoundPrefab)
            {
                On.FistVR.FVRFireArm.Fire -= FVRFireArm_Fire;
            }
        }
        private void FVRFireArm_Fire(On.FistVR.FVRFireArm.orig_Fire orig, FVRFireArm self, FVRFireArmChamber chamber, Transform muzzle, bool doBuzz, float velMult, float rangeOverride)
        {
            if (self == BreakAction && _timeCharged >= ChargeTime && _fullChargeRound != null)
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
            int i = Array.IndexOf(BreakAction.Barrels, BreakAction.Barrels.FirstOrDefault(b => b.m_isHammerCocked));
            if (i != -1)
            {
                foreach (VisualModifier modifier in BarrelEffects[i].VisualModifiers)
                {
                    modifier.UpdateVisualEffects(_timeCharged / ChargeTime);
                }
            }

            for (int j = 0;  j < BreakAction.Barrels.Length; j++)
            {
                if (j == i) continue;
                foreach (VisualModifier modifier in BarrelEffects[j].VisualModifiers)
                {
                    modifier.UpdateVisualEffects(0f);
                }
            }
        }
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

            if (self == BreakAction && !_isCharging && (!StopsOnEmpty || (StopsOnEmpty && self.Barrels[i].Chamber.IsFull && !self.Barrels[i].Chamber.IsSpent))) StartCoroutine(DropHammerBreakAction(orig, self));
            else if (self == BreakAction && !_isCharging && StopsOnEmpty && (!self.Barrels[i].Chamber.IsFull || self.Barrels[i].Chamber.IsSpent)) orig(self);
            else if (self != BreakAction) orig(self);
        }

        private IEnumerator DropHammerBreakAction(On.FistVR.BreakActionWeapon.orig_DropHammer orig, BreakActionWeapon self)
        {
            yield return DropHammer(self);

            orig(self);

            _timeCharged = 0f;
        }

        private bool BreakActionWeapon_Fire(On.FistVR.BreakActionWeapon.orig_Fire orig, BreakActionWeapon self, int b, bool FireAllBarrels, int index)
        {
            if (self == BreakAction)
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
        #endregion

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