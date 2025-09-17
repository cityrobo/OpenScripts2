using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    [Obsolete("Incomplete")]
    public class AdvancedFVRAlternativeGrip : FVRAlternateGrip
    {
        private static readonly Dictionary<FVRPhysicalObject, AdvancedFVRAlternativeGrip> _existingAdvancedAlternativeGrips = new();
#if !DEBUG

        static AdvancedFVRAlternativeGrip()
        {
            On.FistVR.FVRPhysicalObject.FU += FVRPhysicalObject_FU;
        }

        private static void FVRPhysicalObject_FU(On.FistVR.FVRPhysicalObject.orig_FU orig, FVRPhysicalObject self)
        {
            if (_existingAdvancedAlternativeGrips.TryGetValue(self, out var advancedAlternativeGrip))
            {
                float fixedDeltaTime = Time.fixedDeltaTime;
                if (self.m_timeSinceInQuickbelt < 10f)
                {
                    self.m_timeSinceInQuickbelt += fixedDeltaTime;
                }
                if (self.m_quickbeltSlot != null)
                {
                    self.m_timeSinceInQuickbelt = 0f;
                }
                if (self.CheckDestroyTick > 0f)
                {
                    self.CheckDestroyTick -= fixedDeltaTime;
                }
                else
                {
                    self.CheckDestroyTick = UnityEngine.Random.Range(1f, 1.5f);
                    if (self.Transform.position.y < -1000f)
                    {
                        UnityEngine.Object.Destroy(self.gameObject);
                    }
                }
                if (self.CollisionSound.m_hasCollisionSound && self.CollisionSound.m_colSoundTick > 0f)
                {
                    self.CollisionSound.m_colSoundTick -= fixedDeltaTime;
                }
                if (self.IsHeld || self.QuickbeltSlot != null || self.IsPivotLocked)
                {
                    if (self.RootRigidbody == null)
                    {
                        self.RecoverRigidbody();
                    }
                    if (self.UseGrabPointChild && self.UseGripRotInterp && !self.IsAltHeld)
                    {
                        if (self.Bipod != null && self.Bipod.IsBipodActive)
                        {
                            self.m_pos_interp_tick = 1f;
                        }
                        else if (self.m_pos_interp_tick < 1f)
                        {
                            self.m_pos_interp_tick += fixedDeltaTime * self.PositionInterpSpeed;
                        }
                        else
                        {
                            self.m_pos_interp_tick = 1f;
                        }
                        if (self.Bipod != null && self.Bipod.IsBipodActive)
                        {
                            self.m_rot_interp_tick = 1f;
                        }
                        else if (self.m_rot_interp_tick < 1f)
                        {
                            self.m_rot_interp_tick += fixedDeltaTime * self.RotationInterpSpeed;
                        }
                        else
                        {
                            self.m_rot_interp_tick = 1f;
                        }
                    }
                    Vector3 targetGrabPosition;
                    Quaternion targetGrabRotation;
                    Vector3 currentGrabPosition;
                    Quaternion currentGrabRotation;
                    if (self.IsPivotLocked)
                    {
                        targetGrabPosition = self.m_pivotLockedPos;
                        targetGrabRotation = self.m_pivotLockedRot;
                        currentGrabPosition = self.transform.position;
                        currentGrabRotation = self.transform.rotation;
                    }
                    else
                    {
                        targetGrabPosition = self.GetPosTarget();
                        targetGrabRotation = self.GetRotTarget();
                        currentGrabPosition = self.GetGrabPos();
                        currentGrabRotation = self.GetGrabRot();
                    }
                    Vector3 deltaGrabPosition = targetGrabPosition - currentGrabPosition;
                    Quaternion deltaGrabRotation = targetGrabRotation * Quaternion.Inverse(currentGrabRotation);
                    bool isOnBipod = false;
                    if (self.Bipod != null && self.Bipod.IsBipodActive)
                    {
                        isOnBipod = true;
                    }
                    bool breakActionWeaponUnlatched = false;
                    if (self is BreakActionWeapon && (self as BreakActionWeapon).AltGrip != null && !(self as BreakActionWeapon).IsLatched)
                    {
                        breakActionWeaponUnlatched = true;
                    }
                    if (self.IsPivotLocked)
                    {
                        deltaGrabPosition = targetGrabPosition - currentGrabPosition;
                        deltaGrabRotation = targetGrabRotation * Quaternion.Inverse(currentGrabRotation);
                    }
                    else if (((self.AltGrip != null && self.AltGrip.FunctionalityEnabled && !breakActionWeaponUnlatched) || isOnBipod) && !GM.Options.ControlOptions.UseGunRigMode2)
                    {
                        Vector3 pointingTargetPos;
                        Vector3 pointingCurPos;
                        if (self.AltGrip != null)
                        {
                            pointingTargetPos = self.AltGrip.GetPalmPos(self.m_doesDirectParent);
                            pointingCurPos = self.transform.InverseTransformPoint(self.AltGrip.PoseOverride.position);
                        }
                        else
                        {
                            pointingTargetPos = self.Bipod.GetOffsetSavedWorldPoint();
                            pointingCurPos = self.transform.InverseTransformPoint(self.Bipod.GetBipodRootWorld());
                        }
                        Vector3 pointingTargetPosLocal = self.transform.InverseTransformPoint(pointingTargetPos);
                        Vector3 poseOverridePosLocal = self.transform.InverseTransformPoint(self.PoseOverride.position);
                        float zOffset = Mathf.Max(self.PoseOverride.localPosition.z + 0.05f, pointingTargetPosLocal.z);
                        Vector3 pointingDeltaLocal = new Vector3(pointingTargetPosLocal.x - pointingCurPos.x, pointingTargetPosLocal.y - pointingCurPos.y, zOffset);
                        Vector3 pointingDelta = self.transform.TransformPoint(pointingDeltaLocal);
                        Vector3 upDirection = Vector3.Cross(pointingDelta - self.transform.position, self.m_hand.Input.Right);
                        if (self.DoesFlipUpVector())
                        {
                            upDirection = -upDirection;
                        }
                        if (isOnBipod)
                        {
                            Vector3 upDirectionProjected = Vector3.ProjectOnPlane(upDirection, self.transform.forward);
                            Vector3 upDirectionReference = Vector3.ProjectOnPlane(Vector3.up, self.transform.forward);
                            float upDeltaAngle = Vector3.Angle(upDirectionProjected, upDirectionReference);
                            float clampedUpAngleAsLerpValue = Mathf.Clamp(upDeltaAngle - 20f, 0f, 30f) * 0.1f;
                            upDirection = Vector3.Slerp(upDirectionReference, upDirection, clampedUpAngleAsLerpValue);
                        }
                        targetGrabRotation = Quaternion.LookRotation((pointingDelta - self.transform.position).normalized, upDirection) * self.PoseOverride.localRotation;
                        deltaGrabRotation = targetGrabRotation * Quaternion.Inverse(currentGrabRotation);
                        if (!isOnBipod && GM.Options.ControlOptions.UseVirtualStock && self.HasStockPos() && self.GetStockPos() != null)
                        {
                            Vector3 stockPositionLocal = self.transform.InverseTransformPoint(self.GetStockPos().position);
                            float recoilZ = self.GetRecoilZ();
                            float recoilAdjustedStockHandguardDistance = Mathf.Abs(stockPositionLocal.z - pointingDeltaLocal.z) - recoilZ;
                            Vector3 currentGrabPositionLocal = self.transform.InverseTransformPoint(currentGrabPosition);
                            float recoilAdjustedHandStockDistance = Mathf.Abs(stockPositionLocal.z - currentGrabPositionLocal.z) - recoilZ;
                            Transform head = GM.CurrentPlayerBody.FilteredHead;
                            if (GM.Options.ControlOptions.HandFiltering == ControlOptions.HandFilteringMode.Raw)
                            {
                                head = GM.CurrentPlayerBody.Head;
                            }
                            Vector3 pointingDeltaHeadRelative = head.InverseTransformPoint(pointingDelta);
                            Vector3 targetGrapPositionHeadRelative = head.InverseTransformPoint(targetGrabPosition);
                            Vector3 shoulderMiddlePoint = head.position - head.forward * 0.1f - head.up * 0.05f;
                            Vector3 shoulderMiddlePointHeadRelative = head.InverseTransformPoint(shoulderMiddlePoint);
                            Vector3 targetGrabPositionHeadRelative = head.InverseTransformPoint(targetGrabPosition);
                            //adjust shoulder to the grab position
                            shoulderMiddlePointHeadRelative.x += targetGrabPositionHeadRelative.x;
                            shoulderMiddlePointHeadRelative.y += targetGrabPositionHeadRelative.y + 0.05f;
                            Vector3 shoulderMiddlePointNew = head.TransformPoint(shoulderMiddlePointHeadRelative);
                            Vector3 shoulderPointingDirection = (pointingDelta - shoulderMiddlePointNew).normalized;
                            Vector3 shoulderAdjustedHandPosition = shoulderMiddlePointNew + shoulderPointingDirection * recoilAdjustedHandStockDistance;
                            Vector3 shoulderAdjustedHandPosBodyLocal = GM.CurrentPlayerBody.transform.InverseTransformPoint(shoulderAdjustedHandPosition);
                            self.m_hand.Input.PosUltraFilter = self.m_hand.Input.PUF.Filter<Vector3>(shoulderAdjustedHandPosBodyLocal, -1f);
                            self.m_hand.Input.PosUltraFilter = GM.CurrentPlayerBody.transform.TransformPoint(self.m_hand.Input.PosUltraFilter);
                            Vector3 shoulderAdjustedHandDelta = shoulderAdjustedHandPosition - currentGrabPosition;
                            Quaternion targetRotationLocal = Quaternion.LookRotation((pointingDelta - shoulderMiddlePointNew).normalized, upDirection) * self.PoseOverride.localRotation;
                            self.m_hand.Input.RotUltraFilter = self.m_hand.Input.RUF.Filter<Quaternion>(targetRotationLocal, -1f);
                            Quaternion targetRotationDelta = targetRotationLocal * Quaternion.Inverse(currentGrabRotation);
                            float headToGrabDistance = Vector3.Distance(head.position, targetGrabPosition);
                            headToGrabDistance = Mathf.Clamp(headToGrabDistance - 0.1f, 0f, 1f);
                            float headDistanceDependentLerp = headToGrabDistance * 5f;
                            deltaGrabPosition = Vector3.Lerp(shoulderAdjustedHandDelta, deltaGrabPosition, headDistanceDependentLerp);
                            deltaGrabRotation = Quaternion.Slerp(targetRotationDelta, deltaGrabRotation, headDistanceDependentLerp);
                        }
                    }
                    else if (self.IsHeld && self.AltGrip == null && !self.IsAltHeld && !GM.Options.ControlOptions.UseGunRigMode2 && GM.Options.ControlOptions.UseVirtualStock && self.HasStockPos() && self.GetStockPos() != null)
                    {
                        float stockPosZRelative = Mathf.Abs(self.transform.InverseTransformPoint(self.GetStockPos().position).z);
                        Transform head = GM.CurrentPlayerBody.FilteredHead;
                        if (GM.Options.ControlOptions.HandFiltering == ControlOptions.HandFilteringMode.Raw)
                        {
                            head = GM.CurrentPlayerBody.Head;
                        }
                        Vector3 shoulderMiddlePoint = head.position - head.forward * 0.1f - head.up * 0.05f;
                        Vector3 shoulderMiddlePointHeadRelative = head.InverseTransformPoint(shoulderMiddlePoint);
                        Vector3 shoulderMiddlePointNew = head.TransformPoint(shoulderMiddlePointHeadRelative);
                        Vector3 shoulderToPoseOverrideDirection = (self.PoseOverride.position - shoulderMiddlePointNew).normalized;
                        Vector3 stockAdjustedPoseOverridePos = shoulderMiddlePointNew + shoulderToPoseOverrideDirection * stockPosZRelative;
                        Vector3 poseOverrideDelta = stockAdjustedPoseOverridePos - currentGrabPosition;
                        Vector3 upDirection = self.m_hand.Input.OneEuroPointRotation * Vector3.up;
                        if (self.DoesFlipUpVector())
                        {
                            upDirection = -upDirection;
                        }
                        Quaternion targetRotationLocal = Quaternion.LookRotation((self.PoseOverride.position - shoulderMiddlePointNew).normalized, upDirection) * self.PoseOverride.localRotation;
                        Quaternion targetRotationDelta = targetRotationLocal * Quaternion.Inverse(currentGrabRotation);
                        float headToTargetGrabDistance = Vector3.Distance(head.position, targetGrabPosition);
                        headToTargetGrabDistance = Mathf.Clamp(headToTargetGrabDistance - 0.1f, 0f, 1f);
                        float headDistanceDependentLerp = headToTargetGrabDistance * 5f;
                        float pointingAngleDependentLerp = Vector3.Angle(head.forward, self.m_hand.PointingTransform.forward) / 40f - 0.2f;
                        headDistanceDependentLerp = Mathf.Lerp(headDistanceDependentLerp, 1f, pointingAngleDependentLerp);
                        deltaGrabPosition = Vector3.Lerp(poseOverrideDelta, deltaGrabPosition, headDistanceDependentLerp);
                        deltaGrabRotation = Quaternion.Slerp(targetRotationDelta, deltaGrabRotation, headDistanceDependentLerp);
                    }
                    float angularVelocityMultiplier = 1f;
                    float deltaGrabRotationAxisAngle;
                    Vector3 deltaGrabRotationAxis;
                    deltaGrabRotation.ToAngleAxis(out deltaGrabRotationAxisAngle, out deltaGrabRotationAxis);
                    if (deltaGrabRotationAxisAngle > 180f)
                    {
                        deltaGrabRotationAxisAngle -= 360f;
                    }
                    if (deltaGrabRotationAxisAngle != 0f)
                    {
                        Vector3 fullTargetRotation = fixedDeltaTime * deltaGrabRotationAxisAngle * deltaGrabRotationAxis * self.AttachedRotationMultiplier * self.RotIntensity;
                        self.RootRigidbody.angularVelocity = angularVelocityMultiplier * Vector3.MoveTowards(self.RootRigidbody.angularVelocity, fullTargetRotation, self.AttachedRotationFudge * Time.fixedDeltaTime);
                        if (self.UseSecondStepRotationFiltering)
                        {
                            float secondStageFilter = Mathf.Clamp(self.RootRigidbody.angularVelocity.magnitude * 0.35f, 0f, 1f);
                            secondStageFilter = Mathf.Pow(secondStageFilter, 1.5f);
                            self.RootRigidbody.angularVelocity *= secondStageFilter;
                        }
                        else if ((GM.Options.ControlOptions.LongGunSnipingAssist == ControlOptions.LongGunSnipingAssistMode.AlwaysOn && self is FVRFireArm && (self as FVRFireArm).IsShoulderStabilized()) || (GM.Options.ControlOptions.LongGunSnipingAssist == ControlOptions.LongGunSnipingAssistMode.TriggerHeld && self is FVRFireArm && self.AltGrip != null && self.AltGrip.IsHeld && self.AltGrip.m_hand.Input.TriggerPressed && (self as FVRFireArm).IsShoulderStabilized()))
                        {
                            float num15 = Mathf.Clamp(self.RootRigidbody.angularVelocity.magnitude * 0.35f, 0f, 1f);
                            num15 = Mathf.Pow(num15, 1.35f);
                            num15 = Mathf.Clamp(num15, self.GetMinStabilizationAllowanceFactor(), 1f);
                            self.RootRigidbody.angularVelocity *= num15;
                        }
                        else if (GM.Options.ControlOptions.PistolTwoHandAssist == ControlOptions.PistolTwoHandAssistMode.AlwaysOn && self is FVRFireArm && (self as FVRFireArm).IsTwoHandStabilized())
                        {
                            float num16 = Mathf.Clamp(self.RootRigidbody.angularVelocity.magnitude * 0.35f, 0f, 1f);
                            num16 = Mathf.Pow(num16, 1.1f);
                            num16 = Mathf.Clamp(num16, self.GetMinStabilizationAllowanceFactor(), 1f);
                            self.RootRigidbody.angularVelocity *= num16;
                        }
                    }
                    Vector3 vector32 = deltaGrabPosition * self.AttachedPositionMultiplier * fixedDeltaTime * self.MoveIntensity;
                    self.RootRigidbody.velocity = Vector3.MoveTowards(self.RootRigidbody.velocity, vector32, self.AttachedPositionFudge * fixedDeltaTime);
                }
            }
        }
#endif

        public override void Awake()
        {
            base.Awake();
            _existingAdvancedAlternativeGrips.Add(PrimaryObject, this);
        }

        public override void OnDestroy()
        {
            _existingAdvancedAlternativeGrips.Remove(PrimaryObject);
            base.OnDestroy();
        }
    }
}
