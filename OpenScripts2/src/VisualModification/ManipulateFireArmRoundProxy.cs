using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class ManipulateFireArmRoundProxy : OpenScripts2_BasePlugin
    {
        [Header("ManipulateFireArmRoundProxy Config")]
        public FVRFireArm FireArm;
        public Transform BoltRoundChamberingStartPos;
        public Transform BoltRoundExtractionEndPos;

        public bool DoubleFeed;
        public bool StartsAtEvenRoundCount;

        [Header("Chambering Animation")]
        public AnimationCurve ChamberingPosX;
        public AnimationCurve ChamberingPosY;
        public AnimationCurve ChamberingPosZ;

        public AnimationCurve ChamberingRotX;
        public AnimationCurve ChamberingRotY;
        public AnimationCurve ChamberingRotZ;

        [Header("Extraction Animation")]
        public AnimationCurve ExtractionPosX;
        public AnimationCurve ExtractionPosY;
        public AnimationCurve ExtractionPosZ;

        public AnimationCurve ExtractionRotX;
        public AnimationCurve ExtractionRotY;
        public AnimationCurve ExtractionRotZ;

        private static Dictionary<ClosedBoltWeapon, ManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyClosedBolts = new();
        private static Dictionary<Handgun, ManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyHandGuns = new();
        private static Dictionary<TubeFedShotgun, ManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyTubeFedShotguns = new();
        private static Dictionary<OpenBoltReceiver, ManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyOpenBoltReceivers = new();
        private static Dictionary<BoltActionRifle, ManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyBoltActionRifles = new();

        static ManipulateFireArmRoundProxy()
        {
#if !DEBUG
            On.FistVR.ClosedBoltWeapon.UpdateDisplayRoundPositions += ClosedBoltWeapon_UpdateDisplayRoundPositions;
            On.FistVR.Handgun.UpdateDisplayRoundPositions += Handgun_UpdateDisplayRoundPositions;
            On.FistVR.TubeFedShotgun.UpdateDisplayRoundPositions += TubeFedShotgun_UpdateDisplayRoundPositions;
            On.FistVR.OpenBoltReceiver.UpdateDisplayRoundPositions += OpenBoltReceiver_UpdateDisplayRoundPositions;
            On.FistVR.BoltActionRifle.UpdateBolt += BoltActionRifle_UpdateBolt;
#endif
        }
        public void Awake()
        {
            switch (FireArm)
            {
                case ClosedBoltWeapon w:
                    _existingManipulateFireArmRoundProxyClosedBolts.Add(w, this);
                    break;
                case Handgun w:
                    _existingManipulateFireArmRoundProxyHandGuns.Add(w, this);
                    break;
                case TubeFedShotgun w:
                    _existingManipulateFireArmRoundProxyTubeFedShotguns.Add(w, this);
                    break;
                case OpenBoltReceiver w:
                    _existingManipulateFireArmRoundProxyOpenBoltReceivers.Add(w, this);
                    break;
                case BoltActionRifle w:
                    _existingManipulateFireArmRoundProxyBoltActionRifles.Add(w, this);
                    break;
            }
        }

        public void OnDestroy()
        {
            switch (FireArm)
            {
                case ClosedBoltWeapon w:
                    _existingManipulateFireArmRoundProxyClosedBolts.Remove(w);
                    break;
                case Handgun w:
                    _existingManipulateFireArmRoundProxyHandGuns.Remove(w);
                    break;
                case TubeFedShotgun w:
                    _existingManipulateFireArmRoundProxyTubeFedShotguns.Remove(w);
                    break;
                case OpenBoltReceiver w:
                    _existingManipulateFireArmRoundProxyOpenBoltReceivers.Remove(w);
                    break;
                case BoltActionRifle w:
                    _existingManipulateFireArmRoundProxyBoltActionRifles.Remove(w);
                    break;
            }
        }
#if !DEBUG
        private static void ClosedBoltWeapon_UpdateDisplayRoundPositions(On.FistVR.ClosedBoltWeapon.orig_UpdateDisplayRoundPositions orig, ClosedBoltWeapon self)
        {
            ManipulateFireArmRoundProxy manipulateFireArmRoundProxy;
            if (_existingManipulateFireArmRoundProxyClosedBolts.TryGetValue(self, out manipulateFireArmRoundProxy))
            {
                if (self.Chamber.IsFull)
                {
                    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);
                    float xPosVal = manipulateFireArmRoundProxy.ExtractionPosX.Evaluate(boltLerpBetweenEjectAndFore);
                    float yPosVal = manipulateFireArmRoundProxy.ExtractionPosY.Evaluate(boltLerpBetweenEjectAndFore);
                    float zPosVal = manipulateFireArmRoundProxy.ExtractionPosZ.Evaluate(boltLerpBetweenEjectAndFore);

                    float xRotVal = manipulateFireArmRoundProxy.ExtractionRotX.Evaluate(boltLerpBetweenEjectAndFore);
                    float yRotVal = manipulateFireArmRoundProxy.ExtractionRotY.Evaluate(boltLerpBetweenEjectAndFore);
                    float zRotVal = manipulateFireArmRoundProxy.ExtractionRotZ.Evaluate(boltLerpBetweenEjectAndFore);

                    self.Chamber.ProxyRound.position = self.Bolt.transform.parent.TransformPoint(new Vector3(xPosVal, yPosVal, zPosVal));
                    self.Chamber.ProxyRound.rotation = self.Bolt.transform.parent.rotation * Quaternion.Euler(xRotVal, yRotVal, zRotVal);
                }
                if (self.m_proxy.IsFull)
                {
                    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);
                    float xPosVal = manipulateFireArmRoundProxy.ChamberingPosX.Evaluate(boltLerpBetweenExtractAndFore);
                    float yPosVal = manipulateFireArmRoundProxy.ChamberingPosY.Evaluate(boltLerpBetweenExtractAndFore);
                    float zPosVal = manipulateFireArmRoundProxy.ChamberingPosZ.Evaluate(boltLerpBetweenExtractAndFore);

                    float xRotVal = manipulateFireArmRoundProxy.ChamberingRotX.Evaluate(boltLerpBetweenExtractAndFore);
                    float yRotVal = manipulateFireArmRoundProxy.ChamberingRotY.Evaluate(boltLerpBetweenExtractAndFore);
                    float zRotVal = manipulateFireArmRoundProxy.ChamberingRotZ.Evaluate(boltLerpBetweenExtractAndFore);

                    if (manipulateFireArmRoundProxy.DoubleFeed && self.Magazine != null && !(self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsAtEvenRoundCount || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsAtEvenRoundCount))
                    {
                        xPosVal = -xPosVal;
                        yRotVal = -yRotVal;
                    }

                    self.m_proxy.ProxyRound.position = self.Bolt.transform.parent.TransformPoint(new Vector3(xPosVal, yPosVal, zPosVal));
                    self.m_proxy.ProxyRound.rotation = self.Bolt.transform.parent.rotation * Quaternion.Euler(xRotVal, yRotVal, zRotVal);
                }
            }
            else orig(self);
        }

        private static void Handgun_UpdateDisplayRoundPositions(On.FistVR.Handgun.orig_UpdateDisplayRoundPositions orig, Handgun self)
        {
            ManipulateFireArmRoundProxy manipulateFireArmRoundProxy;
            if (_existingManipulateFireArmRoundProxyHandGuns.TryGetValue(self, out manipulateFireArmRoundProxy))
            {
                if (self.Chamber.IsFull)
                {
                    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(self.Slide.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z, self.Slide.m_slideZ_forward, self.Slide.m_slideZ_current);
                    float xPosVal = manipulateFireArmRoundProxy.ExtractionPosX.Evaluate(boltLerpBetweenEjectAndFore);
                    float yPosVal = manipulateFireArmRoundProxy.ExtractionPosY.Evaluate(boltLerpBetweenEjectAndFore);
                    float zPosVal = manipulateFireArmRoundProxy.ExtractionPosZ.Evaluate(boltLerpBetweenEjectAndFore);

                    float xRotVal = manipulateFireArmRoundProxy.ExtractionRotX.Evaluate(boltLerpBetweenEjectAndFore);
                    float yRotVal = manipulateFireArmRoundProxy.ExtractionRotY.Evaluate(boltLerpBetweenEjectAndFore);
                    float zRotVal = manipulateFireArmRoundProxy.ExtractionRotZ.Evaluate(boltLerpBetweenEjectAndFore);

                    self.Chamber.ProxyRound.position = self.Slide.transform.parent.TransformPoint(new Vector3(xPosVal, yPosVal, zPosVal));
                    self.Chamber.ProxyRound.rotation = self.Slide.transform.parent.rotation * Quaternion.Euler(xRotVal, yRotVal, zRotVal);
                }
                if (self.m_proxy.IsFull)
                {
                    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(self.Slide.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z, self.Slide.m_slideZ_forward, self.Slide.m_slideZ_current);
                    float xPosVal = manipulateFireArmRoundProxy.ChamberingPosX.Evaluate(boltLerpBetweenExtractAndFore);
                    float yPosVal = manipulateFireArmRoundProxy.ChamberingPosY.Evaluate(boltLerpBetweenExtractAndFore);
                    float zPosVal = manipulateFireArmRoundProxy.ChamberingPosZ.Evaluate(boltLerpBetweenExtractAndFore);

                    float xRotVal = manipulateFireArmRoundProxy.ChamberingRotX.Evaluate(boltLerpBetweenExtractAndFore);
                    float yRotVal = manipulateFireArmRoundProxy.ChamberingRotY.Evaluate(boltLerpBetweenExtractAndFore);
                    float zRotVal = manipulateFireArmRoundProxy.ChamberingRotZ.Evaluate(boltLerpBetweenExtractAndFore);

                    if (manipulateFireArmRoundProxy.DoubleFeed && self.Magazine != null && !(self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsAtEvenRoundCount || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsAtEvenRoundCount))
                    {
                        xPosVal = -xPosVal;
                        yRotVal = -yRotVal;
                    }

                    self.m_proxy.ProxyRound.position = self.Slide.transform.parent.TransformPoint(new Vector3(xPosVal, yPosVal, zPosVal));
                    self.m_proxy.ProxyRound.rotation = self.Slide.transform.parent.rotation * Quaternion.Euler(xRotVal, yRotVal, zRotVal);
                }
            }
            else orig(self);
        }

        private static void TubeFedShotgun_UpdateDisplayRoundPositions(On.FistVR.TubeFedShotgun.orig_UpdateDisplayRoundPositions orig, TubeFedShotgun self)
        {
            ManipulateFireArmRoundProxy manipulateFireArmRoundProxy;
            if (_existingManipulateFireArmRoundProxyTubeFedShotguns.TryGetValue(self, out manipulateFireArmRoundProxy))
            {
                if (self.Chamber.IsFull)
                {
                    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);
                    float xPosVal = manipulateFireArmRoundProxy.ExtractionPosX.Evaluate(boltLerpBetweenEjectAndFore);
                    float yPosVal = manipulateFireArmRoundProxy.ExtractionPosY.Evaluate(boltLerpBetweenEjectAndFore);
                    float zPosVal = manipulateFireArmRoundProxy.ExtractionPosZ.Evaluate(boltLerpBetweenEjectAndFore);

                    float xRotVal = manipulateFireArmRoundProxy.ExtractionRotX.Evaluate(boltLerpBetweenEjectAndFore);
                    float yRotVal = manipulateFireArmRoundProxy.ExtractionRotY.Evaluate(boltLerpBetweenEjectAndFore);
                    float zRotVal = manipulateFireArmRoundProxy.ExtractionRotZ.Evaluate(boltLerpBetweenEjectAndFore);

                    self.Chamber.ProxyRound.position = self.Bolt.transform.parent.TransformPoint(new Vector3(xPosVal, yPosVal, zPosVal));
                    self.Chamber.ProxyRound.rotation = self.Bolt.transform.parent.rotation * Quaternion.Euler(xRotVal, yRotVal, zRotVal);
                }
                if (self.m_proxy.IsFull)
                {
                    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);
                    float xPosVal = manipulateFireArmRoundProxy.ChamberingPosX.Evaluate(boltLerpBetweenExtractAndFore);
                    float yPosVal = manipulateFireArmRoundProxy.ChamberingPosY.Evaluate(boltLerpBetweenExtractAndFore);
                    float zPosVal = manipulateFireArmRoundProxy.ChamberingPosZ.Evaluate(boltLerpBetweenExtractAndFore);

                    float xRotVal = manipulateFireArmRoundProxy.ChamberingRotX.Evaluate(boltLerpBetweenExtractAndFore);
                    float yRotVal = manipulateFireArmRoundProxy.ChamberingRotY.Evaluate(boltLerpBetweenExtractAndFore);
                    float zRotVal = manipulateFireArmRoundProxy.ChamberingRotZ.Evaluate(boltLerpBetweenExtractAndFore);

                    if (manipulateFireArmRoundProxy.DoubleFeed && self.Magazine != null && !(self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsAtEvenRoundCount || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsAtEvenRoundCount))
                    {
                        xPosVal = -xPosVal;
                        yRotVal = -yRotVal;
                    }

                    self.m_proxy.ProxyRound.position = self.Bolt.transform.parent.TransformPoint(new Vector3(xPosVal, yPosVal, zPosVal));
                    self.m_proxy.ProxyRound.rotation = self.Bolt.transform.parent.rotation * Quaternion.Euler(xRotVal, yRotVal, zRotVal);
                }
            }
            else orig(self);
        }

        private static void OpenBoltReceiver_UpdateDisplayRoundPositions(On.FistVR.OpenBoltReceiver.orig_UpdateDisplayRoundPositions orig, OpenBoltReceiver self)
        {
            ManipulateFireArmRoundProxy manipulateFireArmRoundProxy;
            if (_existingManipulateFireArmRoundProxyOpenBoltReceivers.TryGetValue(self, out manipulateFireArmRoundProxy))
            {
                if (self.Chamber.IsFull)
                {
                    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);
                    float xPosVal = manipulateFireArmRoundProxy.ExtractionPosX.Evaluate(boltLerpBetweenEjectAndFore);
                    float yPosVal = manipulateFireArmRoundProxy.ExtractionPosY.Evaluate(boltLerpBetweenEjectAndFore);
                    float zPosVal = manipulateFireArmRoundProxy.ExtractionPosZ.Evaluate(boltLerpBetweenEjectAndFore);

                    float xRotVal = manipulateFireArmRoundProxy.ExtractionRotX.Evaluate(boltLerpBetweenEjectAndFore);
                    float yRotVal = manipulateFireArmRoundProxy.ExtractionRotY.Evaluate(boltLerpBetweenEjectAndFore);
                    float zRotVal = manipulateFireArmRoundProxy.ExtractionRotZ.Evaluate(boltLerpBetweenEjectAndFore);

                    self.Chamber.ProxyRound.position = self.Bolt.transform.parent.TransformPoint(new Vector3(xPosVal, yPosVal, zPosVal));
                    self.Chamber.ProxyRound.rotation = self.Bolt.transform.parent.rotation * Quaternion.Euler(xRotVal, yRotVal, zRotVal);
                }
                if (self.m_proxy.IsFull)
                {
                    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);
                    float xPosVal = manipulateFireArmRoundProxy.ChamberingPosX.Evaluate(boltLerpBetweenExtractAndFore);
                    float yPosVal = manipulateFireArmRoundProxy.ChamberingPosY.Evaluate(boltLerpBetweenExtractAndFore);
                    float zPosVal = manipulateFireArmRoundProxy.ChamberingPosZ.Evaluate(boltLerpBetweenExtractAndFore);

                    float xRotVal = manipulateFireArmRoundProxy.ChamberingRotX.Evaluate(boltLerpBetweenExtractAndFore);
                    float yRotVal = manipulateFireArmRoundProxy.ChamberingRotY.Evaluate(boltLerpBetweenExtractAndFore);
                    float zRotVal = manipulateFireArmRoundProxy.ChamberingRotZ.Evaluate(boltLerpBetweenExtractAndFore);

                    if (manipulateFireArmRoundProxy.DoubleFeed && self.Magazine != null && !(self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsAtEvenRoundCount || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsAtEvenRoundCount))
                    {
                        xPosVal = -xPosVal;
                        yRotVal = -yRotVal;
                    }

                    self.m_proxy.ProxyRound.position = self.Bolt.transform.parent.TransformPoint(new Vector3(xPosVal, yPosVal, zPosVal));
                    self.m_proxy.ProxyRound.rotation = self.Bolt.transform.parent.rotation * Quaternion.Euler(xRotVal, yRotVal, zRotVal);
                }
            }
            else orig(self);
        }

        private static FVRFireArmRound BoltActionRifle_UpdateBolt(On.FistVR.BoltActionRifle.orig_UpdateBolt orig, BoltActionRifle self, BoltActionRifle_Handle.BoltActionHandleState State, float lerp, bool isCatchHeld)
        {
            FVRFireArmRound round = orig(self,State,lerp, isCatchHeld);
            ManipulateFireArmRoundProxy manipulateFireArmRoundProxy;
            if (_existingManipulateFireArmRoundProxyBoltActionRifles.TryGetValue(self, out manipulateFireArmRoundProxy))
            {
                if (self.Chamber.IsFull)
                {
                    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(self.BoltHandle.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z, self.BoltHandle.transform.parent.InverseTransformPoint(self.BoltHandle.Point_Forward.position).z, self.BoltHandle.transform.localPosition.z);
                    float xPosVal = manipulateFireArmRoundProxy.ExtractionPosX.Evaluate(boltLerpBetweenEjectAndFore);
                    float yPosVal = manipulateFireArmRoundProxy.ExtractionPosY.Evaluate(boltLerpBetweenEjectAndFore);
                    float zPosVal = manipulateFireArmRoundProxy.ExtractionPosZ.Evaluate(boltLerpBetweenEjectAndFore);

                    float xRotVal = manipulateFireArmRoundProxy.ExtractionRotX.Evaluate(boltLerpBetweenEjectAndFore);
                    float yRotVal = manipulateFireArmRoundProxy.ExtractionRotY.Evaluate(boltLerpBetweenEjectAndFore);
                    float zRotVal = manipulateFireArmRoundProxy.ExtractionRotZ.Evaluate(boltLerpBetweenEjectAndFore);

                    self.Chamber.ProxyRound.position = self.BoltHandle.transform.parent.TransformPoint(new Vector3(xPosVal, yPosVal, zPosVal));
                    self.Chamber.ProxyRound.rotation = self.BoltHandle.transform.parent.rotation * Quaternion.Euler(xRotVal, yRotVal, zRotVal);
                }
                if (self.m_proxy.IsFull)
                {
                    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(self.BoltHandle.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z, self.BoltHandle.transform.parent.InverseTransformPoint(self.BoltHandle.Point_Forward.position).z, self.BoltHandle.transform.localPosition.z);
                    float xPosVal = manipulateFireArmRoundProxy.ChamberingPosX.Evaluate(boltLerpBetweenExtractAndFore);
                    float yPosVal = manipulateFireArmRoundProxy.ChamberingPosY.Evaluate(boltLerpBetweenExtractAndFore);
                    float zPosVal = manipulateFireArmRoundProxy.ChamberingPosZ.Evaluate(boltLerpBetweenExtractAndFore);

                    float xRotVal = manipulateFireArmRoundProxy.ChamberingRotX.Evaluate(boltLerpBetweenExtractAndFore);
                    float yRotVal = manipulateFireArmRoundProxy.ChamberingRotY.Evaluate(boltLerpBetweenExtractAndFore);
                    float zRotVal = manipulateFireArmRoundProxy.ChamberingRotZ.Evaluate(boltLerpBetweenExtractAndFore);

                    if (manipulateFireArmRoundProxy.DoubleFeed && self.Magazine != null && !(self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsAtEvenRoundCount || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsAtEvenRoundCount))
                    {
                        xPosVal = -xPosVal;
                        yRotVal = -yRotVal;
                    }

                    self.m_proxy.ProxyRound.position = self.BoltHandle.transform.parent.TransformPoint(new Vector3(xPosVal, yPosVal, zPosVal));
                    self.m_proxy.ProxyRound.rotation = self.BoltHandle.transform.parent.rotation * Quaternion.Euler(xRotVal, yRotVal, zRotVal);
                }
            }
            return round;
        }
#endif
    }
}
