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
        [Header("SimplifiedManipulateFireArmRoundProxy Config")]
        public FVRFireArm FireArm;
        [Tooltip("Bolt/Slide position at which chambering starts.\nIf not set this is the Bolt/Slide-Lock position.")]
        public Transform BoltRoundChamberingStartPos;
        [Tooltip("Bolt/Slide position at which chambering ends.\nIf not set this is the Chamber position.")]
        public Transform BoltRoundChamberingEndPos;
        [Tooltip("Bolt/Slide position at which extraction starts.\nIf not set this is the Chamber position.")]
        public Transform BoltRoundExtractionStartPos;
        [Tooltip("Bolt/Slide position at which extraction ends.\nIf not set this is the Bolt/Slide-Lock position.")]
        public Transform BoltRoundExtractionEndPos;

        [Tooltip("Is magazine a double feed magazine?")]
        public bool UsesDoubleFeedMagazines;
        [Tooltip("Does round in magazine start on the right?")]
        public bool StartsOnTheRight;

        [Tooltip("Round X-Axis offset inside the magazine")]
        public float DoubleFeedXPosOffset;
        [Tooltip("Round Y-Axis rotation inside the magazine")]
        public float DoubleFeedYRotOffset;

        [Header("In Editor Testing")]
        [Tooltip("Putting a testing round into this field activates in editor testing.")]
        public Transform ProxyRound;
        [Tooltip("Check this box to test the extraction cycle.")]
        public bool TestExtraction = false;
        [Tooltip("Set the virtual amount of rounds in the magazine for double feed testing.")]
        public int VirtualNumberOfRoundsInMag;

        [HideInInspector]
        public bool RoundWasExtracted = false;

        private static readonly Dictionary<ClosedBoltWeapon, SimplifiedManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyClosedBolts = new();
        private static readonly Dictionary<Handgun, SimplifiedManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyHandguns = new();
        private static readonly Dictionary<TubeFedShotgun, SimplifiedManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyTubeFedShotguns = new();
        private static readonly Dictionary<OpenBoltReceiver, SimplifiedManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyOpenBoltReceivers = new();
        private static readonly Dictionary<BoltActionRifle, SimplifiedManipulateFireArmRoundProxy> _existingManipulateFireArmRoundProxyBoltActionRifles = new();

        private float _lastBoltZ;

        private static bool CheckAllDictionariesForFirearm(FVRFireArm fireArm, out SimplifiedManipulateFireArmRoundProxy simplifiedManipulateFireArmRoundProxy)
        {
            switch (fireArm)
            {
                case ClosedBoltWeapon closedBoltWeapon when _existingManipulateFireArmRoundProxyClosedBolts.TryGetValue(closedBoltWeapon, out simplifiedManipulateFireArmRoundProxy):
                    return true;
                case Handgun handgun when _existingManipulateFireArmRoundProxyHandguns.TryGetValue(handgun, out simplifiedManipulateFireArmRoundProxy):
                    return true;
                case TubeFedShotgun tubeFedShotgun when _existingManipulateFireArmRoundProxyTubeFedShotguns.TryGetValue(tubeFedShotgun, out simplifiedManipulateFireArmRoundProxy):
                    return true;
                case OpenBoltReceiver openBoltReceiver when _existingManipulateFireArmRoundProxyOpenBoltReceivers.TryGetValue(openBoltReceiver, out simplifiedManipulateFireArmRoundProxy):
                    return true;
                case BoltActionRifle boltActionRifle when _existingManipulateFireArmRoundProxyBoltActionRifles.TryGetValue(boltActionRifle, out simplifiedManipulateFireArmRoundProxy):
                    return true;
                default:
                    simplifiedManipulateFireArmRoundProxy = null;
                    return false;
            }
        }

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
                    _existingManipulateFireArmRoundProxyHandguns.Add(w, this);

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
                    _existingManipulateFireArmRoundProxyHandguns.Remove(w);
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
                        HandgunUpdate(w.Slide);
                        break;
                    case TubeFedShotgun w:
                        TubeFedUpdate(w.Bolt);
                        break;
                    case OpenBoltReceiver w:
                        OpenBoltUpdate(w.Bolt);
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
                ClosedBolt_BoltEvent_EjectRound(closedBolt);
            }
            if (!closedBolt.Weapon.Chamber.IsFull && _lastBoltZ <= chamberStartPosZ && closedBolt.m_boltZ_current > chamberStartPosZ)
            {
                ClosedBolt_BoltEvent_ExtractRoundFromMag(closedBolt);
            }
            _lastBoltZ = closedBolt.m_boltZ_current;
        }

        private void ClosedBolt_BoltEvent_ExtractRoundFromMag(ClosedBolt closedBolt)
        {
            ClosedBoltWeapon_BeginChamberingRound(closedBolt.Weapon);
        }

        private void ClosedBolt_BoltEvent_EjectRound(ClosedBolt closedBolt)
        {
            closedBolt.Weapon.EjectExtractedRound();
        }

        public void ClosedBoltWeapon_BeginChamberingRound(ClosedBoltWeapon closedBolt)
        {
            bool removedRound = false;
            GameObject roundPrefab = null;
            if (closedBolt.HasBelt)
            {
                if (!closedBolt.m_proxy.IsFull && closedBolt.BeltDD.HasARound())
                {
                    removedRound = true;
                    roundPrefab = RemoveRound(closedBolt.BeltDD);
                }
            }
            else if (!closedBolt.m_proxy.IsFull && closedBolt.Magazine != null && !closedBolt.Magazine.IsBeltBox && closedBolt.Magazine.HasARound())
            {
                removedRound = true;
                roundPrefab = RemoveRound(closedBolt.Magazine);
            }
            if (!removedRound)
            {
                return;
            }
            if (removedRound)
            {
                closedBolt.m_proxy.SetFromPrefabReference(roundPrefab);
                UpdateDisplayRoundPositions(closedBolt);
            }
        }
        #endregion

        #region Handgun custom round operations
        private void HandgunUpdate(HandgunSlide slide)
        {
            Transform parent = slide.transform.parent;
            float extractEndPosZ = parent.InverseTransformPoint(BoltRoundExtractionEndPos.position).z;
            float chamberingStartPosZ = parent.InverseTransformPoint(BoltRoundChamberingStartPos.position).z;
            if (_lastBoltZ >= extractEndPosZ && slide.m_slideZ_current < extractEndPosZ)
            {
                Handgun_BoltEvent_EjectRound(slide);
            }
            if (!slide.Handgun.Chamber.IsFull && _lastBoltZ <= chamberingStartPosZ && slide.m_slideZ_current > chamberingStartPosZ)
            {
                Handgun_BoltEvent_ExtractRoundFromMag(slide);
            }
            _lastBoltZ = slide.m_slideZ_current;
        }

        private void Handgun_BoltEvent_ExtractRoundFromMag(HandgunSlide slide)
        {
            Handgun_BeginChamberingRound(slide.Handgun);
        }

        private void Handgun_BoltEvent_EjectRound(HandgunSlide slide)
        {
            slide.Handgun.EjectExtractedRound();
        }

        public void Handgun_BeginChamberingRound(Handgun slide)
        {
            bool removedRound = false;
            GameObject roundPrefab = null;
            if (slide.HasBelt)
            {
                if (!slide.m_proxy.IsFull && slide.BeltDD.HasARound())
                {
                    removedRound = true;
                    roundPrefab = RemoveRound(slide.BeltDD);
                }
            }
            else if (!slide.m_proxy.IsFull && slide.Magazine != null && !slide.Magazine.IsBeltBox && slide.Magazine.HasARound())
            {
                removedRound = true;
                roundPrefab = RemoveRound(slide.Magazine);
            }
            if (!removedRound)
            {
                return;
            }
            if (removedRound)
            {
                slide.m_proxy.SetFromPrefabReference(roundPrefab);
                UpdateDisplayRoundPositions(slide);
            }
        }
        #endregion

        #region Tube Fed custom round operations
        private void TubeFedUpdate(TubeFedShotgunBolt tubeFedBolt)
        {
            Transform parent = tubeFedBolt.transform.parent;
            float extractEndPosZ = parent.InverseTransformPoint(BoltRoundExtractionEndPos.position).z;
            float chamberStartPosZ = parent.InverseTransformPoint(BoltRoundChamberingStartPos.position).z;
            if (_lastBoltZ >= extractEndPosZ && tubeFedBolt.m_boltZ_current < extractEndPosZ)
            {
                TubeFed_BoltEvent_EjectRound(tubeFedBolt);
            }
            if (!tubeFedBolt.Shotgun.Chamber.IsFull && _lastBoltZ <= chamberStartPosZ && tubeFedBolt.m_boltZ_current > chamberStartPosZ)
            {
                TubeFed_BoltEvent_ExtractRoundFromMag(tubeFedBolt);
            }
            _lastBoltZ = tubeFedBolt.m_boltZ_current;
        }

        private void TubeFed_BoltEvent_ExtractRoundFromMag(TubeFedShotgunBolt tubeFedBolt)
        {
            TubeFedWeapon_BeginChamberingRound(tubeFedBolt.Shotgun);
        }

        private void TubeFed_BoltEvent_EjectRound(TubeFedShotgunBolt tubeFedBolt)
        {
            tubeFedBolt.Shotgun.EjectExtractedRound();
            tubeFedBolt.Shotgun.TransferShellToUpperTrack();
        }

        public void TubeFedWeapon_BeginChamberingRound(TubeFedShotgun tubeFed)
        {
            bool removedRound = false;
            GameObject roundPrefab = null;
            if (tubeFed.HasBelt)
            {
                if (!tubeFed.m_proxy.IsFull && tubeFed.BeltDD.HasARound())
                {
                    removedRound = true;
                    roundPrefab = RemoveRound(tubeFed.BeltDD);
                }
            }
            else if (!tubeFed.m_proxy.IsFull && tubeFed.Magazine != null && !tubeFed.Magazine.IsBeltBox && tubeFed.Magazine.HasARound())
            {
                removedRound = true;
                roundPrefab = RemoveRound(tubeFed.Magazine);
            }
            if (!removedRound)
            {
                return;
            }
            if (removedRound)
            {
                tubeFed.m_proxy.SetFromPrefabReference(roundPrefab);
                tubeFed.m_isExtractedRoundOnLowerPath = true;
                UpdateDisplayRoundPositions(tubeFed);
            }
        }
        #endregion

        #region Open Bolt custom round operations
        private void OpenBoltUpdate(OpenBoltReceiverBolt openBolt)
        {
            Transform parent = openBolt.transform.parent;
            float extractEndPosZ = parent.InverseTransformPoint(BoltRoundExtractionEndPos.position).z;
            float chamberStartPosZ = parent.InverseTransformPoint(BoltRoundChamberingStartPos.position).z;
            if (_lastBoltZ >= extractEndPosZ && openBolt.m_boltZ_current < extractEndPosZ)
            {
                OpenBolt_BoltEvent_EjectRound(openBolt);
            }
            if (!openBolt.Receiver.Chamber.IsFull && _lastBoltZ <= chamberStartPosZ && openBolt.m_boltZ_current > chamberStartPosZ)
            {
                OpenBolt_BoltEvent_ExtractRoundFromMag(openBolt);
            }
            _lastBoltZ = openBolt.m_boltZ_current;
        }

        private void OpenBolt_BoltEvent_ExtractRoundFromMag(OpenBoltReceiverBolt openBolt)
        {
            OpenBoltReceiver_BeginChamberingRound(openBolt.Receiver);
        }

        private void OpenBolt_BoltEvent_EjectRound(OpenBoltReceiverBolt openBolt)
        {
            openBolt.Receiver.EjectExtractedRound();
            openBolt.Receiver.PlayAudioEvent(FirearmAudioEventType.MagazineEjectRound, 1f);
        }

        public void OpenBoltReceiver_BeginChamberingRound(OpenBoltReceiver openBolt)
        {
            OpenBoltReceiver.FireSelectorModeType modeType = openBolt.FireSelector_Modes[openBolt.m_fireSelectorMode].ModeType;
            OpenBoltReceiver.FireSelectorMode fireSelectorMode = openBolt.FireSelector_Modes[openBolt.m_fireSelectorMode];
            if (openBolt.m_CamBurst > 0)
            {
                openBolt.m_CamBurst--;
            }
            if (modeType == OpenBoltReceiver.FireSelectorModeType.Single || modeType == OpenBoltReceiver.FireSelectorModeType.SuperFastBurst || (modeType == OpenBoltReceiver.FireSelectorModeType.Burst && openBolt.m_CamBurst <= 0))
            {
                openBolt.EngageSeer();
            }
            bool extractedRound = false;
            GameObject roundPrefab = null;
            if (openBolt.HasBelt)
            {
                if (!openBolt.m_proxy.IsFull && openBolt.BeltDD.HasARound())
                {
                    if (openBolt.AudioClipSet.BeltSettlingLimit > 0)
                    {
                        openBolt.PlayAudioEvent(FirearmAudioEventType.BeltSettle, 1f);
                    }
                    extractedRound = true;
                    roundPrefab = RemoveRound(openBolt.BeltDD);
                }
            }
            else if (!openBolt.m_proxy.IsFull && openBolt.Magazine != null && !openBolt.Magazine.IsBeltBox && openBolt.Magazine.HasARound())
            {
                extractedRound = true;
                roundPrefab = RemoveRound(openBolt.Magazine);
            }
            if (!extractedRound)
            {
                return;
            }
            if (extractedRound)
            {
                openBolt.m_proxy.SetFromPrefabReference(roundPrefab);
                UpdateDisplayRoundPositions(openBolt);
            }
            if (openBolt.Bolt.HasLastRoundBoltHoldOpen && openBolt.Magazine != null && !openBolt.Magazine.HasARound() && openBolt.Magazine.DoesFollowerStopBolt && !openBolt.Magazine.IsBeltBox)
            {
                openBolt.EngageSeer();
            }
        }
        #endregion

        #region Bolt Action custom round operations
        private FVRFireArmRound BoltActionUpdate(BoltActionRifle_Handle handle)
        {
            Transform parent = handle.transform.parent;
            float extractEndPosZ = parent.InverseTransformPoint(BoltRoundExtractionEndPos.position).z;
            float chamberStartPosZ = parent.InverseTransformPoint(BoltRoundChamberingStartPos.position).z;

            FVRFireArmRound ejectedRound = null;
            if (_lastBoltZ >= extractEndPosZ && handle.transform.localPosition.z < extractEndPosZ)
            {
                ejectedRound = handle.Rifle.Chamber.EjectRound(handle.Rifle.EjectionPos.position, handle.Rifle.transform.right * handle.Rifle.RightwardEjectionForce + handle.Rifle.transform.up * handle.Rifle.UpwardEjectionForce, handle.Rifle.transform.up * handle.Rifle.YSpinEjectionTorque, handle.Rifle.EjectionPos.position, handle.Rifle.EjectionPos.rotation, false);
            }
            if (!handle.Rifle.Chamber.IsFull && _lastBoltZ <= chamberStartPosZ && handle.transform.localPosition.z > chamberStartPosZ)
            {
                BoltAction_BoltEvent_ExtractRoundFromMag(handle);
            }
            _lastBoltZ = handle.transform.localPosition.z;

            return ejectedRound;
        }

        private void BoltAction_BoltEvent_ExtractRoundFromMag(BoltActionRifle_Handle handle)
        {
            BoltAction_BeginChamberingRound(handle.Rifle);
        }

        //private void BoltAction_BoltEvent_EjectRound(BoltActionRifle_Handle handle)
        //{
        //    //handle.Rifle.EjectExtractedRound();
        //}

        public void BoltAction_BeginChamberingRound(BoltActionRifle rifle)
        {
            if (!rifle.m_proxy.IsFull && rifle.Magazine.HasARound() && !rifle.Chamber.IsFull)
            {
                GameObject gameObject = RemoveRound(rifle.Magazine);
                rifle.m_proxy.SetFromPrefabReference(gameObject);
                UpdateDisplayRoundPositions(rifle);
            }
            if (rifle.EjectsMagazineOnEmpty && !rifle.Magazine.HasARound())
            {
                rifle.EjectMag(false);
            }
        }
        #endregion

        #region General custom round operations
        public GameObject RemoveRound(FVRFireArmMagazine mag)
        {
            mag.m_timeSinceRoundInserted = 0f;
            GameObject gameObject = mag.LoadedRounds[mag.m_numRounds - 1].LR_ObjectWrapper.GetGameObject();
            if ((!mag.IsInfinite || !GM.CurrentSceneSettings.AllowsInfiniteAmmoMags) && !GM.CurrentSceneSettings.IsAmmoInfinite && !GM.CurrentPlayerBody.IsInfiniteAmmo)
            {
                if (GM.CurrentPlayerBody.IsAmmoDrain)
                {
                    mag.m_numRounds = 0;
                }
                else
                {
                    if (mag.m_numRounds > 0)
                    {
                        mag.LoadedRounds[mag.m_numRounds - 1] = null;
                        mag.m_numRounds--;
                        
                        if (mag.DisplayBullets.Length > 0)
                        {
                            mag.DisplayBullets[0].SetActive(false);
                        }
                    }
                    //mag.UpdateBulletDisplay();
                }
            }
            RoundWasExtracted = true;
            return gameObject;
        }

        public GameObject RemoveRound(FVRFirearmBeltDisplayData belt)
        {
            GameObject gameObject = belt.BeltRounds[0].LR_ObjectWrapper.GetGameObject();
            if (!GM.CurrentPlayerBody.IsInfiniteAmmo)
            {
                if (belt.m_roundsOnBelt > 0)
                {
                    belt.BeltRounds.RemoveAt(0);
                    belt.m_roundsOnBelt--;
                }
            }
            //belt.PullPushBelt(belt.Firearm.Magazine, belt.BeltCapacity);
            if (belt.m_roundsOnBelt <= 0)
            {
                belt.Firearm.HasBelt = false;
            }
            RoundWasExtracted = true;
            return gameObject;
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
            On.FistVR.ClosedBolt.BoltEvent_ArriveAtFore += ClosedBolt_BoltEvent_ArriveAtFore;

            // Handgun Patch Subscriptions
            On.FistVR.Handgun.UpdateDisplayRoundPositions += Handgun_UpdateDisplayRoundPositions;
            On.FistVR.HandgunSlide.SlideEvent_ExtractRoundFromMag += HandgunSlide_SlideEvent_ExtractRoundFromMag;
            On.FistVR.HandgunSlide.SlideEvent_EjectRound += HandgunSlide_SlideEvent_EjectRound;
            On.FistVR.HandgunSlide.SlideEvent_ArriveAtFore += HandgunSlide_SlideEvent_ArriveAtFore;

            // TubeFedShotgun Patch Subscriptions
            On.FistVR.TubeFedShotgun.UpdateDisplayRoundPositions += TubeFedShotgun_UpdateDisplayRoundPositions;
            On.FistVR.TubeFedShotgunBolt.BoltEvent_ExtractRoundFromMag += TubeFedShotgunBolt_BoltEvent_ExtractRoundFromMag;
            On.FistVR.TubeFedShotgunBolt.BoltEvent_EjectRound += TubeFedShotgunBolt_BoltEvent_EjectRound;
            On.FistVR.TubeFedShotgunBolt.BoltEvent_ArriveAtFore += TubeFedShotgunBolt_BoltEvent_ArriveAtFore;

            // OpenBoltReceiver Patch Subscriptions
            On.FistVR.OpenBoltReceiver.UpdateDisplayRoundPositions += OpenBoltReceiver_UpdateDisplayRoundPositions;
            On.FistVR.OpenBoltReceiverBolt.BoltEvent_BeginChambering += OpenBoltReceiverBolt_BoltEvent_BeginChambering;
            On.FistVR.OpenBoltReceiverBolt.BoltEvent_EjectRound += OpenBoltReceiverBolt_BoltEvent_EjectRound;
            On.FistVR.OpenBoltReceiverBolt.BoltEvent_ArriveAtFore += OpenBoltReceiverBolt_BoltEvent_ArriveAtFore;

            // BoltActionRifle Patch Subscriptions
            On.FistVR.BoltActionRifle.UpdateBolt += BoltActionRifle_UpdateBolt;

            // FVRFireArm Patch Subscriptions
            On.FistVR.FVRFireArm.EjectMag += FVRFireArm_EjectMag;

            //On.FistVR.FVRFireArmMagazine.UpdateBulletDisplay += FVRFireArmMagazine_UpdateBulletDisplay;
        }

        #region General Gun Patches
        private static void FVRFireArmMagazine_UpdateBulletDisplay(On.FistVR.FVRFireArmMagazine.orig_UpdateBulletDisplay orig, FVRFireArmMagazine self)
        {
            orig(self);

            if (self.FireArm != null && CheckAllDictionariesForFirearm(self.FireArm, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy) && manipulateFireArmRoundProxy.RoundWasExtracted)
            {
                Debug.Log("UpdateBulletDisplay after extracting round.");
            }
            else
            {
                Debug.Log("Normal UpdateBulletDisplay");
            }
        }

        private static void FVRFireArm_EjectMag(On.FistVR.FVRFireArm.orig_EjectMag orig, FVRFireArm self, bool PhysicalRelease)
        {
            if (self.Magazine != null && CheckAllDictionariesForFirearm(self, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy) && manipulateFireArmRoundProxy.RoundWasExtracted)
            {
                self.Magazine.UpdateBulletDisplay();
            }

            orig(self, PhysicalRelease);
        }
        #endregion

        #region Closed Bolt Patches
        private static void ClosedBoltWeapon_UpdateDisplayRoundPositions(On.FistVR.ClosedBoltWeapon.orig_UpdateDisplayRoundPositions orig, ClosedBoltWeapon self)
        {
            if (_existingManipulateFireArmRoundProxyClosedBolts.TryGetValue(self, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy))
            {
                //Transform parent = self.Bolt.transform.parent;
                //float startPosZ;
                //float endPosZ;
                //if (self.Chamber.IsFull)
                //{
                //    startPosZ = parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionStartPos.position).z;
                //    endPosZ = parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z;

                //    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(endPosZ, startPosZ, self.Bolt.m_boltZ_current);

                //    Vector3 pos = Vector3.Lerp(self.RoundPos_Ejecting.position, self.Chamber.transform.position, boltLerpBetweenEjectAndFore);
                //    Quaternion rot = Quaternion.Slerp(self.RoundPos_Ejecting.rotation, self.Chamber.transform.rotation, boltLerpBetweenEjectAndFore);

                //    self.Chamber.ProxyRound.position = pos;
                //    self.Chamber.ProxyRound.rotation = rot;
                //}
                //if (self.m_proxy.IsFull)
                //{
                //    startPosZ = parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z;
                //    endPosZ = parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingEndPos.position).z;

                //    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(startPosZ, endPosZ, self.Bolt.m_boltZ_current);

                //    Vector3 MagazinePos_Pos = self.RoundPos_MagazinePos.localPosition;
                //    Quaternion MagazinePos_Rot = self.RoundPos_MagazinePos.localRotation;

                //    if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && (self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                //    {
                //        MagazinePos_Pos.x += manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                //        MagazinePos_Rot *= Quaternion.Euler(0f, manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                //    }
                //    else if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && !(self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                //    {
                //        MagazinePos_Pos.x += -manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                //        MagazinePos_Rot *= Quaternion.Euler(0f, -manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                //    }

                //    Vector3 pos = Vector3.Lerp(self.RoundPos_MagazinePos.parent.TransformPoint(MagazinePos_Pos), self.Chamber.transform.position, boltLerpBetweenExtractAndFore);
                //    Quaternion rot = Quaternion.Slerp(self.RoundPos_MagazinePos.parent.rotation * MagazinePos_Rot, self.Chamber.transform.rotation, boltLerpBetweenExtractAndFore);
                //    self.m_proxy.ProxyRound.position = pos;
                //    self.m_proxy.ProxyRound.rotation = rot;
                //}
                manipulateFireArmRoundProxy.UpdateDisplayRoundPositions(self);
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

        private static void ClosedBolt_BoltEvent_ArriveAtFore(On.FistVR.ClosedBolt.orig_BoltEvent_ArriveAtFore orig, ClosedBolt self)
        {
            orig(self);

            // Make magazine or belt update its visual display after we extracted a round from it
            if (_existingManipulateFireArmRoundProxyClosedBolts.TryGetValue(self.Weapon, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy) && manipulateFireArmRoundProxy.RoundWasExtracted)
            {
                self.Weapon.Magazine?.UpdateBulletDisplay();
                self.Weapon.BeltDD?.PullPushBelt(self.Weapon.BeltDD.Firearm.Magazine, self.Weapon.BeltDD.BeltCapacity);
            }
        }
        #endregion

        #region Handgun Patches
        private static void Handgun_UpdateDisplayRoundPositions(On.FistVR.Handgun.orig_UpdateDisplayRoundPositions orig, Handgun self)
        {
            if (_existingManipulateFireArmRoundProxyHandguns.TryGetValue(self, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy))
            {
                //Transform parent = self.Slide.transform.parent;
                //float startPosZ;
                //float endPosZ;

                //if (self.Chamber.IsFull)
                //{
                //    startPosZ = parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionStartPos.position).z;
                //    endPosZ = parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z;

                //    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(endPosZ, startPosZ, self.Slide.m_slideZ_current);

                //    Vector3 pos = Vector3.Lerp(self.RoundPos_Ejecting.position, self.Chamber.transform.position, boltLerpBetweenEjectAndFore);
                //    Quaternion rot = Quaternion.Slerp(self.RoundPos_Ejecting.rotation, self.Chamber.transform.rotation, boltLerpBetweenEjectAndFore);

                //    self.Chamber.ProxyRound.position = pos;
                //    self.Chamber.ProxyRound.rotation = rot;
                //}
                //if (self.m_proxy.IsFull)
                //{


                //    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(self.Slide.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z, self.Slide.m_slideZ_forward, self.Slide.m_slideZ_current);

                //    Vector3 MagazinePos_Pos = self.RoundPos_Magazine.localPosition;
                //    Quaternion MagazinePos_Rot = self.RoundPos_Magazine.localRotation;

                //    if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && (self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                //    {
                //        MagazinePos_Pos.x += manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                //        MagazinePos_Rot *= Quaternion.Euler(0f, manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                //    }
                //    else if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && !(self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                //    {
                //        MagazinePos_Pos.x += -manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                //        MagazinePos_Rot *= Quaternion.Euler(0f, -manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                //    }

                //    Vector3 pos = Vector3.Lerp(self.RoundPos_Magazine.parent.TransformPoint(MagazinePos_Pos), self.Chamber.transform.position, boltLerpBetweenExtractAndFore);
                //    Quaternion rot = Quaternion.Slerp(self.RoundPos_Magazine.parent.rotation * MagazinePos_Rot, self.Chamber.transform.rotation, boltLerpBetweenExtractAndFore);
                //    self.m_proxy.ProxyRound.position = pos;
                //    self.m_proxy.ProxyRound.rotation = rot;
                //}

                manipulateFireArmRoundProxy.UpdateDisplayRoundPositions(self);

                if (self.Slide.CurPos == HandgunSlide.SlidePos.Forward)
                {
                    self.Chamber.IsAccessible = false;
                }
                else
                {
                    self.Chamber.IsAccessible = true;
                }
            }
            else orig(self);
        }

        private static void HandgunSlide_SlideEvent_ExtractRoundFromMag(On.FistVR.HandgunSlide.orig_SlideEvent_ExtractRoundFromMag orig, HandgunSlide self)
        {
            if (_existingManipulateFireArmRoundProxyHandguns.ContainsKey(self.Handgun))
            {
                // Override it so it ain't doin' nothin'! TAKE THAT!
            }
            else orig(self);
        }
        private static void HandgunSlide_SlideEvent_EjectRound(On.FistVR.HandgunSlide.orig_SlideEvent_EjectRound orig, HandgunSlide self)
        {
            if (_existingManipulateFireArmRoundProxyHandguns.ContainsKey(self.Handgun))
            {
                // Just cock hammer, nothin' else! HAHA!
                if (self.Handgun.TriggerType != Handgun.TriggerStyle.DAO)
                {
                    self.Handgun.CockHammer(false);
                }
            }
            else orig(self);
        }

        private static void HandgunSlide_SlideEvent_ArriveAtFore(On.FistVR.HandgunSlide.orig_SlideEvent_ArriveAtFore orig, HandgunSlide self)
        {
            orig(self);

            // Make magazine or belt update its visual display after we extracted a round from it
            if (_existingManipulateFireArmRoundProxyHandguns.TryGetValue(self.Handgun, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy) && manipulateFireArmRoundProxy.RoundWasExtracted)
            {
                self.Handgun.Magazine?.UpdateBulletDisplay();
                self.Handgun.BeltDD?.PullPushBelt(self.Handgun.BeltDD.Firearm.Magazine, self.Handgun.BeltDD.BeltCapacity);
            }
        }
        #endregion

        #region Tube Fed Patches
        private static void TubeFedShotgun_UpdateDisplayRoundPositions(On.FistVR.TubeFedShotgun.orig_UpdateDisplayRoundPositions orig, TubeFedShotgun self)
        {
            if (_existingManipulateFireArmRoundProxyTubeFedShotguns.TryGetValue(self, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy))
            {
                //if (self.Chamber.IsFull)
                //{
                //    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);

                //    Vector3 pos = Vector3.Lerp(self.RoundPos_Ejecting.position, self.Chamber.transform.position, boltLerpBetweenEjectAndFore);
                //    Quaternion rot = Quaternion.Slerp(self.RoundPos_Ejecting.rotation, self.Chamber.transform.rotation, boltLerpBetweenEjectAndFore);

                //    self.Chamber.ProxyRound.position = pos;
                //    self.Chamber.ProxyRound.rotation = rot;
                //}
                //if (self.m_proxy.IsFull)
                //{
                //    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);

                //    Vector3 MagazinePos_Pos = self.RoundPos_UpperPath_Forward.localPosition;
                //    Quaternion MagazinePos_Rot = self.RoundPos_UpperPath_Forward.localRotation;

                //    if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && (self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                //    {
                //        MagazinePos_Pos.x += manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                //        MagazinePos_Rot *= Quaternion.Euler(0f, manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                //    }
                //    else if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && !(self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                //    {
                //        MagazinePos_Pos.x += -manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                //        MagazinePos_Rot *= Quaternion.Euler(0f, -manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                //    }

                //    Vector3 pos = Vector3.Lerp(self.RoundPos_UpperPath_Forward.parent.TransformPoint(MagazinePos_Pos), self.Chamber.transform.position, boltLerpBetweenExtractAndFore);
                //    Quaternion rot = Quaternion.Slerp(self.RoundPos_UpperPath_Forward.parent.rotation * MagazinePos_Rot, self.Chamber.transform.rotation, boltLerpBetweenExtractAndFore);
                //    self.m_proxy.ProxyRound.position = pos;
                //    self.m_proxy.ProxyRound.rotation = rot;
                //}

                manipulateFireArmRoundProxy.UpdateDisplayRoundPositions(self);
            }
            else orig(self);
        }

        private static void TubeFedShotgunBolt_BoltEvent_ExtractRoundFromMag(On.FistVR.TubeFedShotgunBolt.orig_BoltEvent_ExtractRoundFromMag orig, TubeFedShotgunBolt self)
        {
            if (_existingManipulateFireArmRoundProxyTubeFedShotguns.ContainsKey(self.Shotgun))
            {
                // Override it so it ain't doin' nothin'! TAKE THAT!
            }
            else orig(self);
        }

        private static void TubeFedShotgunBolt_BoltEvent_EjectRound(On.FistVR.TubeFedShotgunBolt.orig_BoltEvent_EjectRound orig, TubeFedShotgunBolt self)
        {
            if (_existingManipulateFireArmRoundProxyTubeFedShotguns.ContainsKey(self.Shotgun))
            {
                // Just cock hammer, nothin' else! HAHA!
                self.Shotgun.CockHammer();
            }
            else orig(self);
        }

        private static void TubeFedShotgunBolt_BoltEvent_ArriveAtFore(On.FistVR.TubeFedShotgunBolt.orig_BoltEvent_ArriveAtFore orig, TubeFedShotgunBolt self)
        {
            orig(self);

            // Make magazine or belt update its visual display after we extracted a round from it
            if (_existingManipulateFireArmRoundProxyTubeFedShotguns.TryGetValue(self.Shotgun, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy) && manipulateFireArmRoundProxy.RoundWasExtracted)
            {
                self.Shotgun.Magazine?.UpdateBulletDisplay();
                self.Shotgun.BeltDD?.PullPushBelt(self.Shotgun.BeltDD.Firearm.Magazine, self.Shotgun.BeltDD.BeltCapacity);
            }
        }
        #endregion

        #region Open Bolt Patches
        private static void OpenBoltReceiver_UpdateDisplayRoundPositions(On.FistVR.OpenBoltReceiver.orig_UpdateDisplayRoundPositions orig, OpenBoltReceiver self)
        {
            if (_existingManipulateFireArmRoundProxyOpenBoltReceivers.TryGetValue(self, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy))
            {
                //if (self.Chamber.IsFull)
                //{
                //    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);

                //    Vector3 pos = Vector3.Lerp(self.RoundPos_Ejecting.position, self.Chamber.transform.position, boltLerpBetweenEjectAndFore);
                //    Quaternion rot = Quaternion.Slerp(self.RoundPos_Ejecting.rotation, self.Chamber.transform.rotation, boltLerpBetweenEjectAndFore);

                //    self.Chamber.ProxyRound.position = pos;
                //    self.Chamber.ProxyRound.rotation = rot;
                //}
                //if (self.m_proxy.IsFull)
                //{
                //    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(self.Bolt.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z, self.Bolt.m_boltZ_forward, self.Bolt.m_boltZ_current);

                //    Vector3 MagazinePos_Pos = self.RoundPos_MagazinePos.localPosition;
                //    Quaternion MagazinePos_Rot = self.RoundPos_MagazinePos.localRotation;

                //    if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && (self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                //    {
                //        MagazinePos_Pos.x += manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                //        MagazinePos_Rot *= Quaternion.Euler(0f, manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                //    }
                //    else if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && !(self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                //    {
                //        MagazinePos_Pos.x += -manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                //        MagazinePos_Rot *= Quaternion.Euler(0f, -manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                //    }

                //    Vector3 pos = Vector3.Lerp(self.RoundPos_MagazinePos.parent.TransformPoint(MagazinePos_Pos), self.Chamber.transform.position, boltLerpBetweenExtractAndFore);
                //    Quaternion rot = Quaternion.Slerp(self.RoundPos_MagazinePos.parent.rotation * MagazinePos_Rot, self.Chamber.transform.rotation, boltLerpBetweenExtractAndFore);
                //    self.m_proxy.ProxyRound.position = pos;
                //    self.m_proxy.ProxyRound.rotation = rot;
                //}

                //if (self.DoesForwardBoltDisableReloadWell)
                //{
                //    if (self.Bolt.CurPos >= OpenBoltReceiverBolt.BoltPos.Locked)
                //    {
                //        if (!self.ReloadTriggerWell.activeSelf)
                //        {
                //            self.ReloadTriggerWell.SetActive(true);
                //        }
                //    }
                //    else if (self.ReloadTriggerWell.activeSelf)
                //    {
                //        self.ReloadTriggerWell.SetActive(false);
                //    }
                //}

                manipulateFireArmRoundProxy.UpdateDisplayRoundPositions(self);
            }
            else orig(self);
        }

        private static void OpenBoltReceiverBolt_BoltEvent_BeginChambering(On.FistVR.OpenBoltReceiverBolt.orig_BoltEvent_BeginChambering orig, OpenBoltReceiverBolt self)
        {
            if (_existingManipulateFireArmRoundProxyOpenBoltReceivers.ContainsKey(self.Receiver))
            {
                // Override it so it ain't doin' nothin'! TAKE THAT!
            }
            else orig(self);
        }

        private static void OpenBoltReceiverBolt_BoltEvent_EjectRound(On.FistVR.OpenBoltReceiverBolt.orig_BoltEvent_EjectRound orig, OpenBoltReceiverBolt self)
        {
            if (_existingManipulateFireArmRoundProxyOpenBoltReceivers.ContainsKey(self.Receiver))
            {
                // This one isn't cocking any hammers sooooo. DELETE!
            }
            else orig(self);
        }

        private static void OpenBoltReceiverBolt_BoltEvent_ArriveAtFore(On.FistVR.OpenBoltReceiverBolt.orig_BoltEvent_ArriveAtFore orig, OpenBoltReceiverBolt self)
        {
            orig(self);

            // Make magazine or belt update its visual display after we extracted a round from it
            if (_existingManipulateFireArmRoundProxyOpenBoltReceivers.TryGetValue(self.Receiver, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy) && manipulateFireArmRoundProxy.RoundWasExtracted)
            {
                self.Receiver.Magazine?.UpdateBulletDisplay();
                self.Receiver.BeltDD?.PullPushBelt(self.Receiver.BeltDD.Firearm.Magazine, self.Receiver.BeltDD.BeltCapacity);
            }
        }

        #endregion

        #region Bolt Action Patches
        private static FVRFireArmRound BoltActionRifle_UpdateBolt(On.FistVR.BoltActionRifle.orig_UpdateBolt orig, BoltActionRifle self, BoltActionRifle_Handle.BoltActionHandleState State, float lerp, bool isCatchHeld)
        {
            if (_existingManipulateFireArmRoundProxyBoltActionRifles.TryGetValue(self, out SimplifiedManipulateFireArmRoundProxy manipulateFireArmRoundProxy))
            {
                FVRFireArmRound fvrfireArmRound = null;
                self.CurBoltHandleState = State;
                self.BoltLerp = lerp;
                if (self.UsesClips && self.ClipTrigger != null)
                {
                    if (self.CurBoltHandleState == BoltActionRifle_Handle.BoltActionHandleState.Rear)
                    {
                        if (!self.ClipTrigger.activeSelf)
                        {
                            self.ClipTrigger.SetActive(true);
                        }
                    }
                    else if (self.ClipTrigger.activeSelf)
                    {
                        self.ClipTrigger.SetActive(false);
                    }
                }
                if (self.CurBoltHandleState == BoltActionRifle_Handle.BoltActionHandleState.Rear && self.LastBoltHandleState != BoltActionRifle_Handle.BoltActionHandleState.Rear)
                {
                    if (self.CockType == BoltActionRifle.HammerCockType.OnBack)
                    {
                        self.CockHammer();
                    }
                    if (self.Chamber.IsFull)
                    {
                        //self.PlayAudioEvent(FirearmAudioEventType.HandleBack, 1f);
                        //FVRFireArmRound fvrfireArmRound2 = self.Chamber.EjectRound(self.EjectionPos.position, self.transform.right * self.RightwardEjectionForce + self.transform.up * self.UpwardEjectionForce, self.transform.up * self.YSpinEjectionTorque, self.EjectionPos.position, self.EjectionPos.rotation, false);
                        //if (isCatchHeld && fvrfireArmRound2 != null && !fvrfireArmRound2.IsSpent)
                        //{
                        //    fvrfireArmRound = fvrfireArmRound2;
                        //}
                    }
                    else
                    {
                        self.PlayAudioEvent(FirearmAudioEventType.HandleBackEmpty, 1f);
                    }
                    self.BoltMovingForward = true;
                }
                else if (self.CurBoltHandleState == BoltActionRifle_Handle.BoltActionHandleState.Forward && self.LastBoltHandleState != BoltActionRifle_Handle.BoltActionHandleState.Forward)
                {
                    if (self.CockType == BoltActionRifle.HammerCockType.OnForward)
                    {
                        self.CockHammer();
                    }
                    if (self.m_proxy.IsFull && !self.Chamber.IsFull)
                    {
                        self.Chamber.SetRound(self.m_proxy.Round, false);
                        self.m_proxy.ClearProxy();
                        self.PlayAudioEvent(FirearmAudioEventType.HandleForward, 1f);
                    }
                    else
                    {
                        self.PlayAudioEvent(FirearmAudioEventType.HandleForwardEmpty, 1f);
                    }
                    self.BoltMovingForward = false;

                    // Make magazine or belt update its visual display after we extracted a round from it
                    if (manipulateFireArmRoundProxy.RoundWasExtracted)
                    {
                        self.Magazine?.UpdateBulletDisplay();
                        self.BeltDD?.PullPushBelt(self.BeltDD.Firearm.Magazine, self.BeltDD.BeltCapacity);
                    }
                }
                else if (self.CurBoltHandleState == BoltActionRifle_Handle.BoltActionHandleState.Mid && self.LastBoltHandleState == BoltActionRifle_Handle.BoltActionHandleState.Rear && self.Magazine != null)
                {
                    //if (!self.m_proxy.IsFull && self.Magazine.HasARound() && !self.Chamber.IsFull)
                    //{
                    //    GameObject gameObject = self.Magazine.RemoveRound(false);
                    //    self.m_proxy.SetFromPrefabReference(gameObject);
                    //}
                    //if (self.EjectsMagazineOnEmpty && !self.Magazine.HasARound())
                    //{
                    //    self.EjectMag(false);
                    //}
                }
                if (self.CurBoltHandleState != BoltActionRifle_Handle.BoltActionHandleState.Forward && !self.m_proxy.IsFull && !self.Chamber.IsFull)
                {
                    self.Chamber.IsAccessible = true;
                }
                else
                {
                    self.Chamber.IsAccessible = false;
                }

                FVRFireArmRound ejectedRound = manipulateFireArmRoundProxy.BoltActionUpdate(self.BoltHandle);
                if (isCatchHeld && ejectedRound != null && !ejectedRound.IsSpent)
                {
                    fvrfireArmRound = ejectedRound;
                }

                //if (self.Chamber.IsFull)
                //{
                //    float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(self.BoltHandle.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundExtractionEndPos.position).z, self.BoltHandle.transform.parent.InverseTransformPoint(self.BoltHandle.Point_Forward.position).z, self.BoltHandle.transform.localPosition.z);

                //    Vector3 pos = Vector3.Lerp(self.Extraction_ChamberPos.position, self.Chamber.transform.position, boltLerpBetweenEjectAndFore);
                //    Quaternion rot = Quaternion.Slerp(self.Extraction_ChamberPos.rotation, self.Chamber.transform.rotation, boltLerpBetweenEjectAndFore);

                //    self.Chamber.ProxyRound.position = pos;
                //    self.Chamber.ProxyRound.rotation = rot;
                //}
                //if (self.m_proxy.IsFull)
                //{
                //    float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(self.BoltHandle.transform.parent.InverseTransformPoint(manipulateFireArmRoundProxy.BoltRoundChamberingStartPos.position).z, self.BoltHandle.transform.parent.InverseTransformPoint(self.BoltHandle.Point_Forward.position).z, self.BoltHandle.transform.localPosition.z);

                //    Vector3 MagazinePos_Pos = self.Extraction_MagazinePos.localPosition;
                //    Quaternion MagazinePos_Rot = self.Extraction_MagazinePos.localRotation;

                //    if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && (self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                //    {
                //        MagazinePos_Pos.x += manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                //        MagazinePos_Rot *= Quaternion.Euler(0f, manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                //    }
                //    else if (manipulateFireArmRoundProxy.UsesDoubleFeedMagazines && self.Magazine != null && !(self.Magazine.m_numRounds % 2f == 0 && manipulateFireArmRoundProxy.StartsOnTheRight || self.Magazine.m_numRounds % 2f != 0 && !manipulateFireArmRoundProxy.StartsOnTheRight))
                //    {
                //        MagazinePos_Pos.x += -manipulateFireArmRoundProxy.DoubleFeedXPosOffset;
                //        MagazinePos_Rot *= Quaternion.Euler(0f, -manipulateFireArmRoundProxy.DoubleFeedYRotOffset, 0f);
                //    }

                //    Vector3 pos = Vector3.Lerp(self.Extraction_MagazinePos.parent.TransformPoint(MagazinePos_Pos), self.Chamber.transform.position, boltLerpBetweenExtractAndFore);
                //    Quaternion rot = Quaternion.Slerp(self.Extraction_MagazinePos.parent.rotation * MagazinePos_Rot, self.Chamber.transform.rotation, boltLerpBetweenExtractAndFore);
                //    self.m_proxy.ProxyRound.position = pos;
                //    self.m_proxy.ProxyRound.rotation = rot;
                //}
                manipulateFireArmRoundProxy.UpdateDisplayRoundPositions(self);
                self.LastBoltHandleState = self.CurBoltHandleState;
                return fvrfireArmRound;
            }
            else return orig(self, State, lerp, isCatchHeld);
        }

        #endregion
#endif
        #endregion

        private class ProxyInfo
        {
            public FVRFireArmChamber Chamber;
            public FVRFirearmMovingProxyRound Proxy;
            public Transform Parent;
            public float CurrentBoltZPosition;
            public Transform RoundPosMagExit;
            public Transform RoundPosEjecting;
        }

        public void UpdateDisplayRoundPositions(FVRFireArm fireArm)
        {
            FVRFireArmChamber chamber = fireArm switch
            {
                ClosedBoltWeapon w => w.Chamber,
                Handgun w => w.Chamber,
                TubeFedShotgun w => w.Chamber,
                OpenBoltReceiver w => w.Chamber,
                BoltActionRifle w => w.Chamber,
                _ => null,
            };

            FVRFirearmMovingProxyRound proxy = fireArm switch
            {
                ClosedBoltWeapon w => w.m_proxy,
                Handgun w => w.m_proxy,
                TubeFedShotgun w => w.m_proxy,
                OpenBoltReceiver w => w.m_proxy,
                BoltActionRifle w => w.m_proxy,
                _ => null,
            };

            Transform parent = fireArm switch
            {
                ClosedBoltWeapon w => w.Bolt.transform.parent,
                Handgun w => w.Slide.transform.parent,
                TubeFedShotgun w => w.Bolt.transform.parent,
                OpenBoltReceiver w => w.Bolt.transform.parent,
                BoltActionRifle w => w.BoltHandle.transform.parent,
                _ => null,
            };

            float boltZ_current = fireArm switch
            {
                ClosedBoltWeapon w => w.Bolt.m_boltZ_current,
                Handgun w => w.Slide.m_slideZ_current,
                TubeFedShotgun w => w.Bolt.m_boltZ_current,
                OpenBoltReceiver w => w.Bolt.m_boltZ_current,
                BoltActionRifle w => w.BoltHandle.transform.localPosition.z,
                _ => 0f,
            };

            Transform roundMagExit = fireArm switch
            {
                ClosedBoltWeapon w => w.RoundPos_MagazinePos,
                Handgun w => w.RoundPos_Magazine,
                TubeFedShotgun w => w.RoundPos_UpperPath_Forward,
                OpenBoltReceiver w => w.RoundPos_MagazinePos,
                BoltActionRifle w => w.Extraction_MagazinePos,
                _ => null,
            };

            Transform roundEjecting = fireArm switch
            {
                ClosedBoltWeapon w => w.RoundPos_Ejecting,
                Handgun w => w.RoundPos_Ejecting,
                TubeFedShotgun w => w.RoundPos_Ejecting,
                OpenBoltReceiver w => w.RoundPos_Ejecting,
                BoltActionRifle w => w.Extraction_Ejecting,
                _ => null,
            };

            ProxyInfo proxyInfo = new()
            {
                Chamber = chamber,
                Proxy = proxy,
                Parent = parent,
                CurrentBoltZPosition = boltZ_current,
                RoundPosMagExit = roundMagExit,
                RoundPosEjecting = roundEjecting,
            };

            if (parent != null)
            {
                if (chamber != null && chamber.IsFull)
                {
                    ModifyProxyPositionExtraction(proxyInfo);
                }
                if (proxy != null && proxy.IsFull)
                {
                    ModifyProxyPositionChambering(proxyInfo);
                }
            }
        }

        private void ModifyProxyPositionExtraction(ProxyInfo proxyInfo)
        {
            float startPosZ = proxyInfo.Parent.InverseTransformPoint(BoltRoundExtractionStartPos.position).z;
            float endPosZ = proxyInfo.Parent.InverseTransformPoint(BoltRoundExtractionEndPos.position).z;

            float boltLerpBetweenEjectAndFore = Mathf.InverseLerp(endPosZ, startPosZ, proxyInfo.CurrentBoltZPosition);

            Vector3 pos = Vector3.Lerp(proxyInfo.RoundPosEjecting.position, proxyInfo.Chamber.transform.position, boltLerpBetweenEjectAndFore);
            Quaternion rot = Quaternion.Slerp(proxyInfo.RoundPosEjecting.rotation, proxyInfo.Chamber.transform.rotation, boltLerpBetweenEjectAndFore);

            proxyInfo.Chamber.ProxyRound.position = pos;
            proxyInfo.Chamber.ProxyRound.rotation = rot;
        }

        private void ModifyProxyPositionChambering(ProxyInfo proxyInfo)
        {
            float startPosZ = proxyInfo.Parent.InverseTransformPoint(BoltRoundChamberingStartPos.position).z;
            float endPosZ = proxyInfo.Parent.InverseTransformPoint(BoltRoundChamberingEndPos.position).z;

            float boltLerpBetweenExtractAndFore = Mathf.InverseLerp(startPosZ, endPosZ, proxyInfo.CurrentBoltZPosition);

            Vector3 MagazinePos_Pos = proxyInfo.RoundPosMagExit.localPosition;
            Quaternion MagazinePos_Rot = proxyInfo.RoundPosMagExit.localRotation;

            if (UsesDoubleFeedMagazines && FireArm.Magazine != null && (FireArm.Magazine.m_numRounds % 2f == 0 && StartsOnTheRight || FireArm.Magazine.m_numRounds % 2f != 0 && !StartsOnTheRight))
            {
                MagazinePos_Pos.x += DoubleFeedXPosOffset;
                MagazinePos_Rot *= Quaternion.Euler(0f, DoubleFeedYRotOffset, 0f);
            }
            else if (UsesDoubleFeedMagazines && FireArm.Magazine != null && !(FireArm.Magazine.m_numRounds % 2f == 0 && StartsOnTheRight || FireArm.Magazine.m_numRounds % 2f != 0 && !StartsOnTheRight))
            {
                MagazinePos_Pos.x += -DoubleFeedXPosOffset;
                MagazinePos_Rot *= Quaternion.Euler(0f, -DoubleFeedYRotOffset, 0f);
            }

            Vector3 pos = Vector3.Lerp(proxyInfo.RoundPosMagExit.parent.TransformPoint(MagazinePos_Pos), proxyInfo.Chamber.transform.position, boltLerpBetweenExtractAndFore);
            Quaternion rot = Quaternion.Slerp(proxyInfo.RoundPosMagExit.parent.rotation * MagazinePos_Rot, proxyInfo.Chamber.transform.rotation, boltLerpBetweenExtractAndFore);
            proxyInfo.Proxy.ProxyRound.position = pos;
            proxyInfo.Proxy.ProxyRound.rotation = rot;
        }
    }
}
