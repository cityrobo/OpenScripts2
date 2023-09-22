using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class SimplifiedManipulateFireArmRoundProxy : OpenScripts2_BasePlugin
    {
        [Header("ManipulateFireArmRoundProxy Config")]
        public FVRFireArm FireArm;
        public Transform BoltRoundChamberingStartPos;
        public Transform BoltRoundExtractionEndPos;

        public bool UsesDoubleFeedMagazines;
        public bool StartsOnTheRight;

        public float DoubleFeedXPosOffset;
        public float DoubleFeedYRotOffset;

        private static Dictionary<ClosedBoltWeapon, SimplifiedManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyClosedBolts = new();
        private static Dictionary<Handgun, SimplifiedManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyHandGuns = new();
        private static Dictionary<TubeFedShotgun, SimplifiedManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyTubeFedShotguns = new();
        private static Dictionary<OpenBoltReceiver, SimplifiedManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyOpenBoltReceivers = new();
        private static Dictionary<BoltActionRifle, SimplifiedManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyBoltActionRifles = new();

        static SimplifiedManipulateFireArmRoundProxy()
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
            if (_existingManipulateFireArmRoundProxyClosedBolts.TryGetValue(self, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy))
            {
                if (self.Chamber.IsFull)
                {
                    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);

                    Vector3 pos = Vector3.Lerp(self.RoundPos_Ejecting.position, self.Chamber.transform.position, boltLerpBetweenEjectAndFore);
                    Quaternion rot = Quaternion.Slerp(self.RoundPos_Ejecting.rotation, self.Chamber.transform.rotation, boltLerpBetweenEjectAndFore);

                    self.Chamber.ProxyRound.position = pos;
                    self.Chamber.ProxyRound.rotation = rot;
                }
                if (self.m_proxy.IsFull)
                {
                    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);
                    
                    Vector3 MagazinePos_Pos = self.RoundPos_MagazinePos.localPosition;
                    Quaternion MagazinePos_Rot = self.RoundPos_MagazinePos.localRotation;

                    if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && (self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                    {
                        MagazinePos_Pos.x += manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                        MagazinePos_Rot *= Quaternion.Euler(0f, manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                    }
                    else if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && !(self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                    {
                        MagazinePos_Pos.x += -manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                        MagazinePos_Rot *= Quaternion.Euler(0f, -manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                    }

                    Vector3 pos = Vector3.Lerp(self.RoundPos_MagazinePos.parent.TransformPoint(MagazinePos_Pos), self.Chamber.transform.position, boltLerpBetweenExtractAndFore);
                    Quaternion rot = Quaternion.Slerp(self.RoundPos_MagazinePos.parent.rotation * MagazinePos_Rot, self.Chamber.transform.rotation, boltLerpBetweenExtractAndFore);
                    self.m_proxy.ProxyRound.position = pos;
                    self.m_proxy.ProxyRound.rotation = rot;
                }
            }
            else orig(self);
        }

        private static void Handgun_UpdateDisplayRoundPositions(On.FistVR.Handgun.orig_UpdateDisplayRoundPositions orig, Handgun self)
        {
            if (_existingManipulateFireArmRoundProxyHandGuns.TryGetValue(self, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy))
            {
                if (self.Chamber.IsFull)
                {
                    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(self.Slide.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z, self.Slide.m_slideZ_forward, self.Slide.m_slideZ_current);
                    
                    Vector3 pos = Vector3.Lerp(self.RoundPos_Ejecting.position, self.Chamber.transform.position, boltLerpBetweenEjectAndFore);
                    Quaternion rot = Quaternion.Slerp(self.RoundPos_Ejecting.rotation, self.Chamber.transform.rotation, boltLerpBetweenEjectAndFore);

                    self.Chamber.ProxyRound.position = pos;
                    self.Chamber.ProxyRound.rotation = rot;
                }
                if (self.m_proxy.IsFull)
                {
                    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(self.Slide.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z, self.Slide.m_slideZ_forward, self.Slide.m_slideZ_current);

                    Vector3 MagazinePos_Pos = self.RoundPos_Magazine.localPosition;
                    Quaternion MagazinePos_Rot = self.RoundPos_Magazine.localRotation;

                    if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && (self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                    {
                        MagazinePos_Pos.x += manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                        MagazinePos_Rot *= Quaternion.Euler(0f, manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                    }
                    else if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && !(self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                    {
                        MagazinePos_Pos.x += -manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                        MagazinePos_Rot *= Quaternion.Euler(0f, -manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                    }

                    Vector3 pos = Vector3.Lerp(self.RoundPos_Magazine.parent.TransformPoint(MagazinePos_Pos), self.Chamber.transform.position, boltLerpBetweenExtractAndFore);
                    Quaternion rot = Quaternion.Slerp(self.RoundPos_Magazine.parent.rotation * MagazinePos_Rot, self.Chamber.transform.rotation, boltLerpBetweenExtractAndFore);
                    self.m_proxy.ProxyRound.position = pos;
                    self.m_proxy.ProxyRound.rotation = rot;
                }
            }
            else orig(self);
        }

        private static void TubeFedShotgun_UpdateDisplayRoundPositions(On.FistVR.TubeFedShotgun.orig_UpdateDisplayRoundPositions orig, TubeFedShotgun self)
        {
            if (_existingManipulateFireArmRoundProxyTubeFedShotguns.TryGetValue(self, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy))
            {
                if (self.Chamber.IsFull)
                {
                    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);

                    Vector3 pos = Vector3.Lerp(self.RoundPos_Ejecting.position, self.Chamber.transform.position, boltLerpBetweenEjectAndFore);
                    Quaternion rot = Quaternion.Slerp(self.RoundPos_Ejecting.rotation, self.Chamber.transform.rotation, boltLerpBetweenEjectAndFore);

                    self.Chamber.ProxyRound.position = pos;
                    self.Chamber.ProxyRound.rotation = rot;
                }
                if (self.m_proxy.IsFull)
                {
                    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);

                    Vector3 MagazinePos_Pos = self.RoundPos_UpperPath_Forward.localPosition;
                    Quaternion MagazinePos_Rot = self.RoundPos_UpperPath_Forward.localRotation;

                    if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && (self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                    {
                        MagazinePos_Pos.x += manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                        MagazinePos_Rot *= Quaternion.Euler(0f, manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                    }
                    else if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && !(self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                    {
                        MagazinePos_Pos.x += -manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                        MagazinePos_Rot *= Quaternion.Euler(0f, -manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                    }

                    Vector3 pos = Vector3.Lerp(self.RoundPos_UpperPath_Forward.parent.TransformPoint(MagazinePos_Pos), self.Chamber.transform.position, boltLerpBetweenExtractAndFore);
                    Quaternion rot = Quaternion.Slerp(self.RoundPos_UpperPath_Forward.parent.rotation * MagazinePos_Rot, self.Chamber.transform.rotation, boltLerpBetweenExtractAndFore);
                    self.m_proxy.ProxyRound.position = pos;
                    self.m_proxy.ProxyRound.rotation = rot;
                }
            }
            else orig(self);
        }

        private static void OpenBoltReceiver_UpdateDisplayRoundPositions(On.FistVR.OpenBoltReceiver.orig_UpdateDisplayRoundPositions orig, OpenBoltReceiver self)
        {
            if (_existingManipulateFireArmRoundProxyOpenBoltReceivers.TryGetValue(self, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy))
            {
                if (self.Chamber.IsFull)
                {
                    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);

                    Vector3 pos = Vector3.Lerp(self.RoundPos_Ejecting.position, self.Chamber.transform.position, boltLerpBetweenEjectAndFore);
                    Quaternion rot = Quaternion.Slerp(self.RoundPos_Ejecting.rotation, self.Chamber.transform.rotation, boltLerpBetweenEjectAndFore);

                    self.Chamber.ProxyRound.position = pos;
                    self.Chamber.ProxyRound.rotation = rot;
                }
                if (self.m_proxy.IsFull)
                {
                    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);

                    Vector3 MagazinePos_Pos = self.RoundPos_MagazinePos.localPosition;
                    Quaternion MagazinePos_Rot = self.RoundPos_MagazinePos.localRotation;

                    if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && (self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                    {
                        MagazinePos_Pos.x += manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                        MagazinePos_Rot *= Quaternion.Euler(0f, manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                    }
                    else if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && !(self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                    {
                        MagazinePos_Pos.x += -manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                        MagazinePos_Rot *= Quaternion.Euler(0f, -manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                    }

                    Vector3 pos = Vector3.Lerp(self.RoundPos_MagazinePos.parent.TransformPoint(MagazinePos_Pos), self.Chamber.transform.position, boltLerpBetweenExtractAndFore);
                    Quaternion rot = Quaternion.Slerp(self.RoundPos_MagazinePos.parent.rotation * MagazinePos_Rot, self.Chamber.transform.rotation, boltLerpBetweenExtractAndFore);
                    self.m_proxy.ProxyRound.position = pos;
                    self.m_proxy.ProxyRound.rotation = rot;
                }
            }
            else orig(self);
        }

        private static FVRFireArmRound BoltActionRifle_UpdateBolt(On.FistVR.BoltActionRifle.orig_UpdateBolt orig, BoltActionRifle self, BoltActionRifle_Handle.BoltActionHandleState State, float lerp, bool isCatchHeld)
        {
            FVRFireArmRound round = orig(self,State,lerp, isCatchHeld);
            if (_existingManipulateFireArmRoundProxyBoltActionRifles.TryGetValue(self, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy))
            {
                if (self.Chamber.IsFull)
                {
                    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(self.BoltHandle.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z, self.BoltHandle.transform.parent.InverseTransformPoint(self.BoltHandle.Point_Forward.position).z, self.BoltHandle.transform.localPosition.z);

                    Vector3 pos = Vector3.Lerp(self.Extraction_ChamberPos.position, self.Chamber.transform.position, boltLerpBetweenEjectAndFore);
                    Quaternion rot = Quaternion.Slerp(self.Extraction_ChamberPos.rotation, self.Chamber.transform.rotation, boltLerpBetweenEjectAndFore);

                    self.Chamber.ProxyRound.position = pos;
                    self.Chamber.ProxyRound.rotation = rot;
                }
                if (self.m_proxy.IsFull)
                {
                    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(self.BoltHandle.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z, self.BoltHandle.transform.parent.InverseTransformPoint(self.BoltHandle.Point_Forward.position).z, self.BoltHandle.transform.localPosition.z);

                    Vector3 MagazinePos_Pos = self.Extraction_MagazinePos.localPosition;
                    Quaternion MagazinePos_Rot = self.Extraction_MagazinePos.localRotation;

                    if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && (self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                    {
                        MagazinePos_Pos.x += manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                        MagazinePos_Rot *= Quaternion.Euler(0f, manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                    }
                    else if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && !(self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                    {
                        MagazinePos_Pos.x += -manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                        MagazinePos_Rot *= Quaternion.Euler(0f, -manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                    }

                    Vector3 pos = Vector3.Lerp(self.Extraction_MagazinePos.parent.TransformPoint(MagazinePos_Pos), self.Chamber.transform.position, boltLerpBetweenExtractAndFore);
                    Quaternion rot = Quaternion.Slerp(self.Extraction_MagazinePos.parent.rotation * MagazinePos_Rot, self.Chamber.transform.rotation, boltLerpBetweenExtractAndFore);
                    self.m_proxy.ProxyRound.position = pos;
                    self.m_proxy.ProxyRound.rotation = rot;
                }
            }
            return round;
        }
#endif
    }
}
