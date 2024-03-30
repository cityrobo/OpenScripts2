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
        public Transform BoltRoundChamberingEndPos;
        public Transform BoltRoundExtractionStartPos;
        public Transform BoltRoundExtractionEndPos;

        public bool UsesDoubleFeedMagazines;
        public bool StartsOnTheRight;

        public float DoubleFeedXPosOffset;
        public float DoubleFeedYRotOffset;

        [Header("In Editor Testing")]
        public Transform ProxyRound;
        public bool TestExtraction = false;
        public int VirtualNumberOfRoundsInMag;

        private static readonly Dictionary<ClosedBoltWeapon, SimplifiedManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyClosedBolts = new();
        private static readonly Dictionary<Handgun, SimplifiedManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyHandGuns = new();
        private static readonly Dictionary<TubeFedShotgun, SimplifiedManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyTubeFedShotguns = new();
        private static readonly Dictionary<OpenBoltReceiver, SimplifiedManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyOpenBoltReceivers = new();
        private static readonly Dictionary<BoltActionRifle, SimplifiedManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyBoltActionRifles = new();

        private float _lastBoltZ;

        public void Awake()
        {
            switch (FireArm)
            {
                case ClosedBoltWeapon w:
                    _existingManipulateFireArmRoundProxyClosedBolts.Add(w, this);

                    // Chambering starts at lock point and ends at forward point, normally
                    if (BoltRoundChamberingStartPos == null) BoltRoundChamberingStartPos = w.Bolt.Point_Bolt_LockPoint;
                    if (BoltRoundChamberingEndPos == null) BoltRoundChamberingEndPos = w.Bolt.Point_Bolt_Forward;

                    // Extraction starts at forward point and ends at lock point, normally
                    if (BoltRoundExtractionStartPos == null) BoltRoundExtractionStartPos = w.Bolt.Point_Bolt_Forward;
                    if (BoltRoundExtractionEndPos == null) BoltRoundExtractionEndPos = w.Bolt.Point_Bolt_LockPoint;

                    _lastBoltZ = w.Bolt.transform.localPosition.z;
                    break;
                case Handgun w:
                    _existingManipulateFireArmRoundProxyHandGuns.Add(w, this);

                    if (BoltRoundChamberingStartPos == null) BoltRoundChamberingStartPos = w.Slide.Point_Slide_LockPoint;
                    if (BoltRoundChamberingEndPos == null) BoltRoundChamberingEndPos = w.Slide.Point_Slide_Forward;

                    if (BoltRoundExtractionStartPos == null) BoltRoundExtractionStartPos = w.Slide.Point_Slide_Forward;
                    if (BoltRoundExtractionEndPos == null) BoltRoundExtractionEndPos = w.Slide.Point_Slide_LockPoint;

                    _lastBoltZ = w.Slide.transform.localPosition.z;
                    break;
                case TubeFedShotgun w:
                    _existingManipulateFireArmRoundProxyTubeFedShotguns.Add(w, this);

                    if (BoltRoundChamberingStartPos == null) BoltRoundChamberingStartPos = w.Bolt.Point_Bolt_LockPoint;
                    if (BoltRoundChamberingEndPos == null) BoltRoundChamberingEndPos = w.Bolt.Point_Bolt_Forward;

                    if (BoltRoundExtractionStartPos == null) BoltRoundExtractionStartPos = w.Bolt.Point_Bolt_Forward;
                    if (BoltRoundExtractionEndPos == null) BoltRoundExtractionEndPos = w.Bolt.Point_Bolt_LockPoint;

                    _lastBoltZ = w.Bolt.transform.localPosition.z;
                    break;
                case OpenBoltReceiver w:
                    _existingManipulateFireArmRoundProxyOpenBoltReceivers.Add(w, this);

                    if (BoltRoundChamberingStartPos == null) BoltRoundChamberingStartPos = w.Bolt.Point_Bolt_LockPoint;
                    if (BoltRoundChamberingEndPos == null) BoltRoundChamberingEndPos = w.Bolt.Point_Bolt_Forward;

                    if (BoltRoundExtractionStartPos == null) BoltRoundExtractionStartPos = w.Bolt.Point_Bolt_Forward;
                    if (BoltRoundExtractionEndPos == null) BoltRoundExtractionEndPos = w.Bolt.Point_Bolt_LockPoint;

                    _lastBoltZ = w.Bolt.transform.localPosition.z;
                    break;
                case BoltActionRifle w:
                    _existingManipulateFireArmRoundProxyBoltActionRifles.Add(w, this);

                    if (BoltRoundChamberingStartPos == null) BoltRoundChamberingStartPos = w.BoltHandle.Point_Rearward;
                    if (BoltRoundChamberingEndPos == null) BoltRoundChamberingEndPos = w.BoltHandle.Point_Forward;

                    if (BoltRoundExtractionStartPos == null) BoltRoundExtractionStartPos = w.BoltHandle.Point_Forward;
                    if (BoltRoundExtractionEndPos == null) BoltRoundExtractionEndPos = w.BoltHandle.Point_Rearward;

                    _lastBoltZ = w.BoltHandle.transform.localPosition.z;
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

        public void Update()
        {
            if (ProxyRound == null)
            {
                switch (FireArm)
                {
                    case ClosedBoltWeapon w:
                        ClosedBoltUpdate(w.Bolt);
                        break;
                    case Handgun w:
                        break;
                    case TubeFedShotgun w:
                        break;
                    case OpenBoltReceiver w:
                        break;
                    case BoltActionRifle w:
                        break;
                }
            }
            else
            {
                UpdateDisplayRoundPositionsEditor();
            }
        }

        #region Closed Bolt custom round operations
        private void ClosedBoltUpdate(ClosedBolt closedBolt)
        {
            Transform parent = closedBolt.transform.parent;
            float extractEndPosZ = parent.InverseTransformPoint(BoltRoundExtractionEndPos.position).z;
            float chamberStartPosZ = parent.InverseTransformPoint(BoltRoundChamberingStartPos.position).z;
            if (_lastBoltZ >= extractEndPosZ && closedBolt.m_boltZ_current < extractEndPosZ)
            {
                BoltEvent_EjectRound(closedBolt);
            }
            if (!closedBolt.Weapon.Chamber.IsFull && _lastBoltZ <= chamberStartPosZ && closedBolt.m_boltZ_current > chamberStartPosZ)
            {
                BoltEvent_ExtractRoundFromMag(closedBolt);
            }
            _lastBoltZ = closedBolt.m_boltZ_current;
        }

        private void BoltEvent_ExtractRoundFromMag(ClosedBolt closedBolt)
        {
            closedBolt.Weapon.BeginChamberingRound();
        }

        private void BoltEvent_EjectRound(ClosedBolt closedBolt)
        {
            closedBolt.Weapon.EjectExtractedRound();
        }
        #endregion

        private void UpdateDisplayRoundPositionsEditor()
        {
            //Transform parent = self.Bolt.transform.parent;
            Transform parent = FireArm switch
            {
                ClosedBoltWeapon w => w.Bolt.transform.parent,
                OpenBoltReceiver w => w.Bolt.transform.parent,
                TubeFedShotgun w => w.Bolt.transform.parent,
                Handgun w => w.Slide.transform.parent,
                BoltActionRifle w => w.BoltHandle.transform.parent,
                _ => null,
            };

            Transform roundPosMag = FireArm switch
            {
                ClosedBoltWeapon w => w.RoundPos_MagazinePos,
                OpenBoltReceiver w => w.RoundPos_MagazinePos,
                TubeFedShotgun w => w.RoundPos_UpperPath_Rearward,
                Handgun w => w.RoundPos_Magazine,
                BoltActionRifle w => w.Extraction_MagazinePos,
                _ => null,
            };

            Transform roundPosEjecting = FireArm switch
            {
                ClosedBoltWeapon w => w.RoundPos_Ejecting,
                OpenBoltReceiver w => w.RoundPos_Ejecting,
                TubeFedShotgun w => w.RoundPos_Ejecting,
                Handgun w => w.RoundPos_Ejecting,
                BoltActionRifle w => w.Extraction_Ejecting,
                _ => null,
            };

            Transform chamber = FireArm switch
            {
                ClosedBoltWeapon w => w.Chamber.transform,
                OpenBoltReceiver w => w.Chamber.transform,
                TubeFedShotgun w => w.Chamber.transform,
                Handgun w => w.Chamber.transform,
                BoltActionRifle w => w.Chamber.transform,
                _ => null,
            };

            float boltZCurrent = FireArm switch
            {
                ClosedBoltWeapon w => w.Bolt.transform.localPosition.z,
                OpenBoltReceiver w => w.Bolt.transform.localPosition.z,
                TubeFedShotgun w => w.Bolt.transform.localPosition.z,
                Handgun w => w.Slide.transform.localPosition.z,
                BoltActionRifle w => w.BoltHandle.transform.localPosition.z,
                _ => 0f,
            };


            float startPosZ;
            float endPosZ;
            if (TestExtraction)
            {
                startPosZ = parent.InverseTransformPoint(BoltRoundExtractionStartPos.position).z;
                endPosZ = parent.InverseTransformPoint(BoltRoundExtractionEndPos.position).z;

                float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(endPosZ, startPosZ, boltZCurrent);

                Vector3 pos = Vector3.Lerp(roundPosEjecting.position, chamber.position, boltLerpBetweenEjectAndFore);
                Quaternion rot = Quaternion.Slerp(roundPosEjecting.rotation, chamber.rotation, boltLerpBetweenEjectAndFore);

                ProxyRound.position = pos;
                ProxyRound.rotation = rot;
            }
            else
            {
                startPosZ = parent.InverseTransformPoint(BoltRoundChamberingStartPos.position).z;
                endPosZ = parent.InverseTransformPoint(BoltRoundChamberingEndPos.position).z;

                float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(startPosZ, endPosZ, boltZCurrent);

                Vector3 MagazinePos_Pos = roundPosMag.localPosition;
                Quaternion MagazinePos_Rot = roundPosMag.localRotation;

                if (UsesDoubleFeedMagazines && (VirtualNumberOfRoundsInMag % 2f == 0 && StartsOnTheRight || VirtualNumberOfRoundsInMag % 2f != 0 && !StartsOnTheRight))
                {
                    MagazinePos_Pos.x += DoubleFeedXPosOffset;
                    MagazinePos_Rot *= Quaternion.Euler(0f, DoubleFeedYRotOffset, 0f);
                }
                else if (UsesDoubleFeedMagazines && !(VirtualNumberOfRoundsInMag % 2f == 0 && StartsOnTheRight || VirtualNumberOfRoundsInMag % 2f != 0 && !StartsOnTheRight))
                {
                    MagazinePos_Pos.x += -DoubleFeedXPosOffset;
                    MagazinePos_Rot *= Quaternion.Euler(0f, -DoubleFeedYRotOffset, 0f);
                }

                Vector3 pos = Vector3.Lerp(roundPosMag.parent.TransformPoint(MagazinePos_Pos), chamber.position, boltLerpBetweenExtractAndFore);
                Quaternion rot = Quaternion.Slerp(roundPosMag.parent.rotation * MagazinePos_Rot, chamber.rotation, boltLerpBetweenExtractAndFore);
                ProxyRound.position = pos;
                ProxyRound.rotation = rot;
            }
        }

        #region Gun Patches
#if !DEBUG
        static SimplifiedManipulateFireArmRoundProxy()
        {
            // Closed Bolt Patch Subscriptions
            On.FistVR.ClosedBoltWeapon.UpdateDisplayRoundPositions += ClosedBoltWeapon_UpdateDisplayRoundPositions;
            On.FistVR.ClosedBolt.BoltEvent_ExtractRoundFromMag += ClosedBolt_BoltEvent_ExtractRoundFromMag;
            On.FistVR.ClosedBolt.BoltEvent_EjectRound += ClosedBolt_BoltEvent_EjectRound;
            // Handgun Patch Subscriptions
            On.FistVR.Handgun.UpdateDisplayRoundPositions += Handgun_UpdateDisplayRoundPositions;

            // TubeFedShotgun Patch Subscriptions
            On.FistVR.TubeFedShotgun.UpdateDisplayRoundPositions += TubeFedShotgun_UpdateDisplayRoundPositions;

            // OpenBoltReceiver Patch Subscriptions
            On.FistVR.OpenBoltReceiver.UpdateDisplayRoundPositions += OpenBoltReceiver_UpdateDisplayRoundPositions;

            // BoltActionRifle Patch Subscriptions
            On.FistVR.BoltActionRifle.UpdateBolt += BoltActionRifle_UpdateBolt;
        }

        #region Closed Bolt Patches
        private static void ClosedBoltWeapon_UpdateDisplayRoundPositions(On.FistVR.ClosedBoltWeapon.orig_UpdateDisplayRoundPositions orig, ClosedBoltWeapon self)
        {
            if (_existingManipulateFireArmRoundProxyClosedBolts.TryGetValue(self, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy))
            {
                Transform parent = self.Bolt.transform.parent;
                float startPosZ;
                float endPosZ;
                if (self.Chamber.IsFull)
                {
                    startPosZ = parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionStartPos.position).z;
                    endPosZ = parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z;

                    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(endPosZ, startPosZ, self.Bolt.m_boltZ_current);

                    Vector3 pos = Vector3.Lerp(self.RoundPos_Ejecting.position, self.Chamber.transform.position, boltLerpBetweenEjectAndFore);
                    Quaternion rot = Quaternion.Slerp(self.RoundPos_Ejecting.rotation, self.Chamber.transform.rotation, boltLerpBetweenEjectAndFore);

                    self.Chamber.ProxyRound.position = pos;
                    self.Chamber.ProxyRound.rotation = rot;
                }
                if (self.m_proxy.IsFull)
                {
                    startPosZ = parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z;
                    endPosZ = parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingEndPos.position).z;

                    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(startPosZ, endPosZ, self.Bolt.m_boltZ_current);
                    
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

        private static void ClosedBolt_BoltEvent_ExtractRoundFromMag(On.FistVR.ClosedBolt.orig_BoltEvent_ExtractRoundFromMag orig, ClosedBolt self)
        {
            if (_existingManipulateFireArmRoundProxyClosedBolts.ContainsKey(self.Weapon))
            {
                // Override it so it ain't doin' nothin'! TAKE THAT!
            }
            else orig(self);
        }

        private static void ClosedBolt_BoltEvent_EjectRound(On.FistVR.ClosedBolt.orig_BoltEvent_EjectRound orig, ClosedBolt self)
        {
            if (_existingManipulateFireArmRoundProxyClosedBolts.ContainsKey(self.Weapon))
            {
                // Just cock hammer, nothin' else! HAHA!
                bool flag = false;
                if (self.IsHeld || self.m_isHandleHeld)
                {
                    flag = true;
                }
                self.Weapon.CockHammer(flag);
            }
            else orig(self);
        }
        #endregion

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
        #endregion
    }
}
