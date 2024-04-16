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
    public class ChargeTriggerPowerRevolverSpecial : OpenScripts2_BasePlugin
    {
        [Header("Charge Trigger Power Config, Revolver Special Edition!")]
        public Revolver Revolver;
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
        public AudioSource FullChargeLoopingAudioSource;

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

        FVRPooledAudioSource _chargingAudioSource;
#if !DEBUG
        public void Awake()
        {
            HookRevolver();
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

            FullChargeLoopingAudioSource?.Stop();
        }

        private void CurrentSceneSettings_ShotFiredEvent(FVRFireArm firearm)
        {
            if (firearm == Revolver && _timeCharged >= ChargeTime)
            {
                foreach (var emitter in FullChargeShotParticleEmitters)
                {
                    emitter.FullChargeShotParticleSystem.Emit(emitter.NumbersOfParticlesToEmit);
                }
            }
        }

        public void OnDestroy()
        {
            UnhookRevolver();

            if (ChangesRoundPrefab)
            {
                On.FistVR.FVRFireArm.Fire -= FVRFireArm_Fire;
            }
        }

        private void FVRFireArm_Fire(On.FistVR.FVRFireArm.orig_Fire orig, FVRFireArm self, FVRFireArmChamber chamber, Transform muzzle, bool doBuzz, float velMult, float rangeOverride)
        {
            if (self == Revolver && _timeCharged >= ChargeTime && _fullChargeRound != null)
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

            int nextChamberIndex = Revolver.CurChamber + Revolver.ChamberOffset + (Revolver.IsCylinderRotClockwise ? 1 : -1);
            if (nextChamberIndex >= Revolver.Cylinder.numChambers)
            {
                nextChamberIndex -= Revolver.Cylinder.numChambers;
            }

            FVRFireArmChamber nextChamber = Revolver.Chambers[nextChamberIndex];

            if (nextChamber.IsFull && !nextChamber.IsSpent)
            {
                if (Revolver.m_isHammerLocked)
                {
                    _timeCharged += Time.deltaTime;

                    _timeCharged = Mathf.Clamp(_timeCharged, 0f, ChargeTime);
                }

                if (Revolver.IsHeld && _timeCharged > 0f)
                {
                    Vibrate(Revolver.m_hand);
                }
            }

            if (_timeCharged >= ChargeTime)
            {
                _chargingAudioSource?.Source.Stop();
                if (FullChargeLoopingAudioSource != null && !FullChargeLoopingAudioSource.isPlaying)
                {
                    FullChargeLoopingAudioSource.time = 0f;
                    FullChargeLoopingAudioSource.Play();
                }
            }
            else FullChargeLoopingAudioSource?.Stop();
        }

        #region Revolver Hooks and Coroutine
        // Revolver Hooks and Coroutine
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
            if (self == Revolver)
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
            if (self == Revolver)
            {
                int nextChamber = self.CurChamber + self.ChamberOffset + (self.IsCylinderRotClockwise ? 1 : -1);
                if (nextChamber >= self.Cylinder.numChambers)
                {
                    nextChamber -= self.Cylinder.numChambers;
                }

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
                if (self.CanFan && self.IsHeld && !self.m_isHammerLocked && self.m_recockingState == FistVR.Revolver.RecockingState.Forward && self.m_hand.OtherHand != null)
                {
                    Vector3 velLinearWorld = self.m_hand.OtherHand.Input.VelLinearWorld;
                    float num = Vector3.Distance(self.m_hand.OtherHand.PalmTransform.position, self.HammerFanDir.position);
                    if (num < 0.15f && Vector3.Angle(velLinearWorld, self.HammerFanDir.forward) < 60f && velLinearWorld.magnitude > 1f)
                    {
                        self.InitiateRecock();
                        self.PlayAudioEvent(FirearmAudioEventType.Prefire, 1f);

                        // New Code
                        if (!StopsOnEmpty || (StopsOnEmpty && self.Chambers[nextChamber].IsFull && !self.Chambers[nextChamber].IsSpent))
                        {
                            _chargingAudioSource = SM.PlayCoreSound(FVRPooledAudioType.Generic, ChargingSounds, Revolver.transform.position);
                            _chargingAudioSource.FollowThisTransform(Revolver.transform);
                        }
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
                        // Single Action
                        if (self.m_isHammerLocked)
                        {
                            DropHammerRevolver(self);
                        }
                        // Double Action
                        else if (!_isCharging && (!StopsOnEmpty || (StopsOnEmpty && self.Chambers[nextChamber].IsFull && !self.Chambers[nextChamber].IsSpent)))
                        {
                            StartCoroutine(DropHammerRevolverDelay(self));
                        }
                        // Next Chamber Empty
                        else if (!_isCharging && StopsOnEmpty && (!self.Chambers[nextChamber].IsFull || self.Chambers[nextChamber].IsSpent))
                        {
                            DropHammerRevolver(self);
                        }
                    }
                    else if ((self.m_curTriggerFloat <= 0.14f || !self.IsDoubleActionTrigger) && !self.m_isHammerLocked && self.CanManuallyCockHammer)
                    {
                        bool flag2 = false;
                        if (self.DoesFiringRecock && self.m_recockingState != FistVR.Revolver.RecockingState.Forward)
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

                                    // New Code
                                    if (!StopsOnEmpty || (StopsOnEmpty && self.Chambers[nextChamber].IsFull && !self.Chambers[nextChamber].IsSpent))
                                    {
                                        _chargingAudioSource = SM.PlayCoreSound(FVRPooledAudioType.Generic, ChargingSounds, Revolver.transform.position);
                                        _chargingAudioSource.FollowThisTransform(Revolver.transform);
                                    }
                                }
                            }
                            else if (self.m_hand.Input.TouchpadDown && Vector2.Angle(self.TouchPadAxes, Vector2.down) < 45f)
                            {
                                self.m_isHammerLocked = true;
                                self.PlayAudioEvent(FirearmAudioEventType.Prefire, 1f);

                                // New Code
                                if (!StopsOnEmpty || (StopsOnEmpty && self.Chambers[nextChamber].IsFull && !self.Chambers[nextChamber].IsSpent))
                                {
                                    _chargingAudioSource = SM.PlayCoreSound(FVRPooledAudioType.Generic, ChargingSounds, Revolver.transform.position);
                                    _chargingAudioSource.FollowThisTransform(Revolver.transform);
                                }
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
            _isCharging = true;
            // _timeCharged = 0f;
            if (_chargingAudioSource == null || _chargingAudioSource != null && !_chargingAudioSource.Source.isPlaying)
            {
                _chargingAudioSource = SM.PlayCoreSound(FVRPooledAudioType.Generic, ChargingSounds, self.transform.position);
                _chargingAudioSource.FollowThisTransform(Revolver.transform);
            }

            while (_timeCharged < ChargeTime)
            {
                if (!self.IsHeld || CheckTrigger(self)) break;
                _timeCharged += Time.deltaTime;
                yield return null;
            }

            DropHammerRevolver(self);
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

            if (self.Chambers[nextChamber].Fire())
            {
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

            // New Code
            _isCharging = false;
            _chargingAudioSource?.Source.Stop();
            _timeCharged = 0f;
        }
        #endregion

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

        private bool CheckTrigger(Revolver revolver) => revolver.m_hand.Input.TriggerFloat < 0.95;
#endif
    }
}