using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class SemiAutoClosedFullAutoOpenBolt : OpenScripts2_BasePlugin
    {
        public OpenBoltReceiver Weapon;
        
        public int SemiAutoFireSelectorPosition;
        public int FullAutoFireSelectorPosition;

        public Transform ClosedBoltSearPoint;
        private float m_boltZ_closedBoltSear;

        private float m_boltZ_last;

        private bool _semiAutoSearActive = false;
        private bool _wasOnFullAutoSear = false;

        private static readonly Dictionary<OpenBoltReceiverBolt, SemiAutoClosedFullAutoOpenBolt> _existingSemiAutoClosedFullAutoOpenBolts = new();

        private enum BoltState
        {
            semiAuto,
            fullAuto,
            safe,
            uncocked
        }

        private bool waitForShot = false;
        private BoltState _boltState;

        public void Start()
        {
            m_boltZ_closedBoltSear = ClosedBoltSearPoint.localPosition.z;

            _existingSemiAutoClosedFullAutoOpenBolts.Add(Weapon.Bolt, this);
        }

        public void OnDestroy()
        {
            _existingSemiAutoClosedFullAutoOpenBolts.Remove(Weapon.Bolt);
        }

        public void Update()
        {
            if (Weapon.m_fireSelectorMode == SemiAutoFireSelectorPosition && !_wasOnFullAutoSear)
            {
                _semiAutoSearActive = true;
            }
            else if (Weapon.m_fireSelectorMode == FullAutoFireSelectorPosition)
            {
                _semiAutoSearActive = false;
            }

            if (Weapon.m_fireSelectorMode == FullAutoFireSelectorPosition && Weapon.IsBoltCatchEngaged()) _wasOnFullAutoSear = true;

            //if (Weapon.m_fireSelectorMode == SemiAutoFireSelectorPosition && _wasOnFullAutoSear == true)
            //{
            //    if (Weapon.IsHeld && Weapon.m_hasTriggeredUpSinceBegin && Weapon.m_hand.Input.TriggerFloat > Weapon.TriggerFiringThreshold)
            //    {
            //        Weapon.ReleaseSeer();
            //        //StartCoroutine(ReengageSear());
            //    }
            //}
        }

        private IEnumerator ReengageSear()
        {
            yield return null;
            yield return null;

            Weapon.EngageSeer();
            _wasOnFullAutoSear = false;
        }
#if !DEBUG
        static SemiAutoClosedFullAutoOpenBolt()
        {
            On.FistVR.OpenBoltReceiverBolt.UpdateBolt += OpenBoltReceiverBolt_UpdateBolt;
        }

        private static void OpenBoltReceiverBolt_UpdateBolt(On.FistVR.OpenBoltReceiverBolt.orig_UpdateBolt orig, OpenBoltReceiverBolt self)
        {
            if (_existingSemiAutoClosedFullAutoOpenBolts.TryGetValue(self, out SemiAutoClosedFullAutoOpenBolt script))
            {
                bool boltOrHandleIsHeld = false;
                if (self.IsHeld || self.m_isChargingHandleHeld)
                {
                    boltOrHandleIsHeld = true;
                }
                float boltZ_current = self.m_boltZ_current;
                if (self.IsHeld)
                {
                    Vector3 vector = self.GetClosestValidPoint(self.Point_Bolt_Forward.position, self.Point_Bolt_Rear.position, self.m_hand.Input.Pos);
                    if (self.UseBoltTransformRootOverride)
                    {
                        vector = self.BoltTransformOverride.InverseTransformPoint(vector);
                    }
                    else
                    {
                        vector = self.Receiver.transform.InverseTransformPoint(vector);
                    }
                    self.m_boltZ_heldTarget = vector.z;
                }
                else if (self.m_isChargingHandleHeld)
                {
                    self.m_boltZ_heldTarget = Mathf.Lerp(self.m_boltZ_forward, self.m_boltZ_rear, self.m_chargingHandleLerp);
                }

                // Sear Selection
                Vector2 boltMovementRange = new Vector2(self.m_boltZ_rear, self.m_boltZ_forward);
                // Use full-auto sear
                if (!script._semiAutoSearActive && self.m_boltZ_current <= self.m_boltZ_lock && self.Receiver.IsBoltCatchEngaged())
                {
                    boltMovementRange = new Vector2(self.m_boltZ_rear, self.m_boltZ_lock);
                }
                // Use semi-auto sear
                else if (script._semiAutoSearActive && self.m_boltZ_current <= script.m_boltZ_closedBoltSear && self.Receiver.IsBoltCatchEngaged() && !script._wasOnFullAutoSear)
                {
                    boltMovementRange = new Vector2(self.m_boltZ_rear, script.m_boltZ_closedBoltSear);
                }

                // Full-auto to semi-auto sear switch
                // Keep on full-auto sear while trigger has not been pressed.
                else if (script._semiAutoSearActive && self.m_boltZ_current <= self.m_boltZ_lock && self.Receiver.IsBoltCatchEngaged() && script._wasOnFullAutoSear)
                {
                    boltMovementRange = new Vector2(self.m_boltZ_rear, self.m_boltZ_lock);
                }
                // Reengage sear when trigger pressed to release from full-auto sear so that bolt catches on semi-auto sear again.
                else if (script._semiAutoSearActive && self.m_boltZ_current <= script.m_boltZ_closedBoltSear && !self.Receiver.IsBoltCatchEngaged() && script._wasOnFullAutoSear)
                {
                    self.Receiver.EngageSeer();
                    script._wasOnFullAutoSear = false;
                    boltMovementRange = new Vector2(self.m_boltZ_rear, script.m_boltZ_closedBoltSear);
                }

                bool boltOnSafetyCatch = false;
                if (self.m_hasSafetyCatch)
                {
                    float currentBoltRotation = self.m_currentBoltRot;
                    float savetyRotationLerpValue = Mathf.InverseLerp(Mathf.Min(self.BoltRot_Standard, self.BoltRot_Safe), Mathf.Max(self.BoltRot_Standard, self.BoltRot_Safe), currentBoltRotation);
                    if (self.IsHeld)
                    {
                        if (self.m_boltZ_current < self.m_boltZ_safetyrotLimit)
                        {
                            Vector3 handToBoltDelta = self.m_hand.Input.Pos - self.transform.position;
                            handToBoltDelta = Vector3.ProjectOnPlane(handToBoltDelta, self.transform.forward).normalized;
                            Vector3 receiverUp = self.Receiver.transform.up;
                            currentBoltRotation = Mathf.Atan2(Vector3.Dot(self.transform.forward, Vector3.Cross(receiverUp, handToBoltDelta)), Vector3.Dot(receiverUp, handToBoltDelta)) * 57.29578f;
                            currentBoltRotation = Mathf.Clamp(currentBoltRotation, Mathf.Min(self.BoltRot_Standard, self.BoltRot_Safe), Mathf.Max(self.BoltRot_Standard, self.BoltRot_Safe));
                        }
                    }
                    else if (!self.m_isChargingHandleHeld)
                    {
                        if (savetyRotationLerpValue <= 0.5f)
                        {
                            currentBoltRotation = Mathf.Min(self.BoltRot_Standard, self.BoltRot_Safe);
                        }
                        else
                        {
                            currentBoltRotation = Mathf.Max(self.BoltRot_Standard, self.BoltRot_Safe);
                        }
                    }
                    if (Mathf.Abs(currentBoltRotation - self.BoltRot_Safe) < self.BoltRot_SlipDistance)
                    {
                        boltMovementRange = new Vector2(self.m_boltZ_rear, self.m_boltZ_safetyCatch);
                        boltOnSafetyCatch = true;
                    }
                    else if (Mathf.Abs(currentBoltRotation - self.BoltRot_Standard) >= self.BoltRot_SlipDistance)
                    {
                        boltMovementRange = new Vector2(self.m_boltZ_rear, self.m_boltZ_safetyrotLimit);
                    }
                    if (Mathf.Abs(currentBoltRotation - self.m_currentBoltRot) > 0.1f)
                    {
                        self.transform.localEulerAngles = new Vector3(0f, 0f, currentBoltRotation);
                    }
                    self.m_currentBoltRot = currentBoltRotation;
                }

                if (boltOrHandleIsHeld)
                {
                    self.m_curBoltSpeed = 0f;
                }
                else if (self.m_curBoltSpeed >= 0f || self.CurPos >= OpenBoltReceiverBolt.BoltPos.Locked)
                {
                    self.m_curBoltSpeed = Mathf.MoveTowards(self.m_curBoltSpeed, self.BoltSpeed_Forward, Time.deltaTime * self.BoltSpringStiffness);
                }

                float newBoltPosition = self.m_boltZ_current;
                float boltTargetWhenHeld = self.m_boltZ_current;
                if (boltOrHandleIsHeld)
                {
                    boltTargetWhenHeld = self.m_boltZ_heldTarget;
                }
                if (boltOrHandleIsHeld)
                {
                    newBoltPosition = Mathf.MoveTowards(self.m_boltZ_current, boltTargetWhenHeld, self.BoltSpeed_Held * Time.deltaTime);
                }
                else
                {
                    newBoltPosition = self.m_boltZ_current + self.m_curBoltSpeed * Time.deltaTime;
                }
                newBoltPosition = Mathf.Clamp(newBoltPosition, boltMovementRange.x, boltMovementRange.y);
                if (Mathf.Abs(newBoltPosition - self.m_boltZ_current) > Mathf.Epsilon)
                {
                    self.m_boltZ_current = newBoltPosition;
                    self.transform.localPosition = new Vector3(self.transform.localPosition.x, self.transform.localPosition.y, self.m_boltZ_current);
                    if (self.SlidingPieces.Length > 0)
                    {
                        float z = self.Point_Bolt_Rear.localPosition.z;
                        for (int i = 0; i < self.SlidingPieces.Length; i++)
                        {
                            Vector3 localPosition = self.SlidingPieces[i].Piece.localPosition;
                            float slidingPieceZ = Mathf.Lerp(self.m_boltZ_current, z, self.SlidingPieces[i].DistancePercent);
                            self.SlidingPieces[i].Piece.localPosition = new Vector3(localPosition.x, localPosition.y, slidingPieceZ);
                        }
                    }
                    if (self.Spring != null)
                    {
                        float springScaleZ = Mathf.InverseLerp(self.m_boltZ_rear, self.m_boltZ_forward, self.m_boltZ_current);
                        self.Spring.localScale = new Vector3(1f, 1f, Mathf.Lerp(self.SpringScales.x, self.SpringScales.y, springScaleZ));
                    }
                }
                else
                {
                    self.m_curBoltSpeed = 0f;
                }

                // Bolt Pos Enum Selection
                OpenBoltReceiverBolt.BoltPos boltPos = self.CurPos;
                if (Mathf.Abs(self.m_boltZ_current - self.m_boltZ_forward) < 0.001f)
                {
                    boltPos = OpenBoltReceiverBolt.BoltPos.Forward;
                }
                else if (Mathf.Abs(self.m_boltZ_current - self.m_boltZ_lock) < 0.001f)
                {
                    boltPos = OpenBoltReceiverBolt.BoltPos.Locked;
                }
                else if (Mathf.Abs(self.m_boltZ_current - self.m_boltZ_rear) < 0.001f)
                {
                    boltPos = OpenBoltReceiverBolt.BoltPos.Rear;
                }
                else if (!script._semiAutoSearActive && self.m_boltZ_current > self.m_boltZ_lock || script._semiAutoSearActive && self.m_boltZ_current > script.m_boltZ_closedBoltSear)
                {
                    boltPos = OpenBoltReceiverBolt.BoltPos.ForwardToMid;
                }
                else
                {
                    boltPos = OpenBoltReceiverBolt.BoltPos.LockedToRear;
                }
                self.CurPos = boltPos;

                // Bolt Events
                if (self.m_hasSafetyCatch && !self.IsHeld && boltOnSafetyCatch && Mathf.Abs(self.m_boltZ_current - self.m_boltZ_safetyCatch) < 0.001f && Mathf.Abs(boltZ_current - self.m_boltZ_safetyCatch) >= 0.001f)
                {
                    self.Receiver.PlayAudioEvent(FirearmAudioEventType.CatchOnSear, 1f);
                }
                if (self.CurPos == OpenBoltReceiverBolt.BoltPos.Rear && self.LastPos != OpenBoltReceiverBolt.BoltPos.Rear)
                {
                    self.BoltEvent_BoltSmackRear();
                }
                if (!script._semiAutoSearActive && self.CurPos == OpenBoltReceiverBolt.BoltPos.Locked && self.LastPos != OpenBoltReceiverBolt.BoltPos.Locked || script._semiAutoSearActive && Mathf.Approximately(self.m_boltZ_current, script.m_boltZ_closedBoltSear) && !Mathf.Approximately(script.m_boltZ_last, self.m_boltZ_current))
                {
                    self.BoltEvent_BoltCaught();
                }
                if (self.CurPos >= OpenBoltReceiverBolt.BoltPos.Locked && self.LastPos < OpenBoltReceiverBolt.BoltPos.Locked)
                {
                    self.BoltEvent_EjectRound();
                }
                if (self.CurPos < OpenBoltReceiverBolt.BoltPos.Locked && self.LastPos > OpenBoltReceiverBolt.BoltPos.ForwardToMid)
                {
                    self.BoltEvent_BeginChambering();
                }
                if (self.CurPos == OpenBoltReceiverBolt.BoltPos.Forward && self.LastPos != OpenBoltReceiverBolt.BoltPos.Forward)
                {
                    self.BoltEvent_ArriveAtFore();
                }
                if (!self.IsBoltLockbackRequiredForChamberAccessibility && self.CurPos != OpenBoltReceiverBolt.BoltPos.Forward)
                {
                    self.Receiver.Chamber.IsAccessible = true;
                }
                else if (self.CurPos == OpenBoltReceiverBolt.BoltPos.LockedToRear || self.CurPos == OpenBoltReceiverBolt.BoltPos.Rear)
                {
                    self.Receiver.Chamber.IsAccessible = true;
                }
                else
                {
                    self.Receiver.Chamber.IsAccessible = false;
                }
                if (self.HasLockLatch)
                {
                    float lockLatchLerp = self.CurPos == OpenBoltReceiverBolt.BoltPos.Forward && !self.IsHeld ? 1f : 0f;

                    if (Mathf.Abs(lockLatchLerp - self.m_lockLatchLerp) > 0.001f)
                    {
                        self.m_lockLatchLerp = lockLatchLerp;
                        self.Receiver.SetAnimatedComponent(self.LockLatch, self.m_lockLatchLerp, self.LockLatch_Interp, self.LockLatch_SafetyAxis);
                    }
                }
                self.LastPos = self.CurPos;
                script.m_boltZ_last = self.m_boltZ_current;
            }
            else orig(self);
        }
#endif
    }
}
