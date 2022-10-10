using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace OpenScripts2
{
    public class ModifyWeaponCartrideAndMagazineAttachment : OpenScripts2_BasePlugin
    {
        public FVRFireArmAttachment Attachment;

        [Header("Caliber Modification")]
        public bool ChangesCaliber = true;
        [SearchableEnum]
        public FireArmRoundType RoundType;

        [Header("MagazineType Modification")]
        public bool ChangesMagType = true;
        [SearchableEnum]
        public FireArmMagazineType MagType;

        [Header("MagPos Calculation Modification")]
        [Header("Place Firearm and the new mag pos into these fields and use the context menu to calculate the position.")]
        public FVRFireArm TemporaryFirearm;
        public Transform MagMountPos;
        public Transform MagEjectPos;

        [Header("Recoil Manipulation")]
        public FVRFireArmRecoilProfile RecoilProfile;
        public FVRFireArmRecoilProfile RecoilProfileStocked;

        [Header("Accuracy Manipulation")]
        public FVRFireArmMechanicalAccuracyClass AccuracyClass;

        [Header("Sound Manipulation")]
        public AudioEvent Shots_Main;
        public AudioEvent Shots_Suppressed;
        public AudioEvent Shots_LowPressure;

        private Vector3 _relativeMagPos;
        private Quaternion _relativeMagRot;

        private Vector3 _relativeMagEjectPos;
        private Quaternion _relativeMagEjectRot;

        private Vector3 _origMagPos;
        private Quaternion _origMagRot;

        private Vector3 _origMagEjectPos;
        private Quaternion _origMagEjectRot;

        private FVRFireArm _fireArm = null;

        private FireArmRoundType _origRoundType;
        private FireArmMagazineType _origMagType;

        private FVRFireArmRecoilProfile _origRecoilProfile;
        private FVRFireArmRecoilProfile _origRecoilProfileStocked;

        private FVRFireArmMechanicalAccuracyClass _origAccuracyClass;

        private AudioEvent _orig_Shots_Main;
        private AudioEvent _orig_Shots_Suppressed;
        private AudioEvent _orig_Shots_LowPressure;

        public void Update()
        {
            if (Attachment.curMount != null && _fireArm == null)
            {
                _fireArm = Attachment.curMount.GetRootMount().MyObject as FVRFireArm;

                if (ChangesMagType)
                {
                    _origMagType = _fireArm.MagazineType;
                    if (MagMountPos != null)
                    {
                        CalculateRelativeMagMountPos(_fireArm);
                        _origMagPos = _fireArm.MagazineMountPos.localPosition;
                        _origMagRot = _fireArm.MagazineMountPos.localRotation;

                        _fireArm.MagazineMountPos.localPosition = _relativeMagPos;
                        _fireArm.MagazineMountPos.localRotation = _relativeMagRot;
                    }
                    if (MagEjectPos != null)
                    {
                        CalculateRelativeMagEjectPos(_fireArm);
                        _origMagEjectPos = _fireArm.MagazineEjectPos.localPosition;
                        _origMagEjectRot = _fireArm.MagazineEjectPos.localRotation;

                        _fireArm.MagazineEjectPos.localPosition = _relativeMagEjectPos;
                        _fireArm.MagazineEjectPos.localRotation = _relativeMagEjectRot;
                    }
                    _fireArm.MagazineType = MagType;
                }
                if (ChangesCaliber)
                {
                    _origRoundType = _fireArm.RoundType;

                    _fireArm.RoundType = RoundType;

                    switch (_fireArm)
                    {
                        case ClosedBoltWeapon w:
                            w.Chamber.RoundType = RoundType;
                            break;
                        case OpenBoltReceiver w:
                            w.Chamber.RoundType = RoundType;
                            break;
                        case Handgun w:
                            w.Chamber.RoundType = RoundType;
                            break;
                        case BoltActionRifle w:
                            w.Chamber.RoundType = RoundType;
                            break;
                        case TubeFedShotgun w:
                            w.Chamber.RoundType = RoundType;
                            break;
                        default:
                            LogWarning($"ModifyWeaponCartrideAndMagazineAttachment: FireArm type \"{_fireArm.GetType()}\" not supported!");
                            break;
                    }
                }

                _origRecoilProfile = _fireArm.RecoilProfile;
                _origRecoilProfileStocked = _fireArm.RecoilProfileStocked;

                _origAccuracyClass = _fireArm.AccuracyClass;

                _orig_Shots_Main = _fireArm.AudioClipSet.Shots_Main;
                _orig_Shots_Suppressed = _fireArm.AudioClipSet.Shots_Suppressed;
                _orig_Shots_LowPressure = _fireArm.AudioClipSet.Shots_LowPressure;

                if (RecoilProfile != null) _fireArm.RecoilProfile = RecoilProfile;
                if (RecoilProfileStocked != null) _fireArm.RecoilProfileStocked = RecoilProfileStocked;
                if (AccuracyClass != 0) _fireArm.AccuracyClass = AccuracyClass;

                if (Shots_Main.Clips.Count != 0) _fireArm.AudioClipSet.Shots_Main = Shots_Main;
                if (Shots_Suppressed.Clips.Count != 0) _fireArm.AudioClipSet.Shots_Suppressed = Shots_Suppressed;
                if (Shots_LowPressure.Clips.Count != 0) _fireArm.AudioClipSet.Shots_LowPressure = Shots_LowPressure;
            }
            else if (Attachment.curMount == null && _fireArm != null)
            {
                if (ChangesMagType)
                {
                    _fireArm.MagazineType = _origMagType;
                    if (MagMountPos != null)
                    {
                        _fireArm.MagazineMountPos.localPosition = _origMagPos;
                        _fireArm.MagazineMountPos.localRotation = _origMagRot;

                        _fireArm.MagazineEjectPos.localPosition = _origMagEjectPos;
                        _fireArm.MagazineEjectPos.localRotation = _origMagEjectRot;
                    }
                }
                if (ChangesCaliber)
                {
                    _fireArm.RoundType = _origRoundType;

                    switch (_fireArm)
                    {
                        case ClosedBoltWeapon w:
                            w.Chamber.RoundType = _origRoundType;
                            break;
                        case OpenBoltReceiver w:
                            w.Chamber.RoundType = _origRoundType;
                            break;
                        case Handgun w:
                            w.Chamber.RoundType = _origRoundType;
                            break;
                        case BoltActionRifle w:
                            w.Chamber.RoundType = _origRoundType;
                            break;
                        case TubeFedShotgun w:
                            w.Chamber.RoundType = _origRoundType;
                            break;
                        default:
                            LogWarning($"ModifyWeaponCartrideAndMagazineAttachment: FireArm type \"{_fireArm.GetType()}\" not supported!");
                            break;
                    }
                }

                _fireArm.RecoilProfile = _origRecoilProfile;
                _fireArm.RecoilProfileStocked = _origRecoilProfileStocked;

                _fireArm.AccuracyClass = _origAccuracyClass;

                _fireArm.AudioClipSet.Shots_Main = _orig_Shots_Main;
                _fireArm.AudioClipSet.Shots_Suppressed = _orig_Shots_Suppressed;
                _fireArm.AudioClipSet.Shots_LowPressure = _orig_Shots_LowPressure;

                _fireArm = null;
            }
        }

        /*[ContextMenu("Calculate relative magazine transforms")]
        public void CalculateRelativeMagPos()
        {
            RelativeMagPos = TemporaryFirearm.transform.InverseTransformPoint(MagMountPos.position);
            //RelativeMagRot = Quaternion.Inverse(TemporaryFirearm.transform.rotation) * MagMountPos.rotation;
            RelativeMagRot = TemporaryFirearm.transform.InverseTransformRotation(MagMountPos.rotation);

            RelativeMagEjectPos = TemporaryFirearm.transform.InverseTransformPoint(MagEjectPos.position);
            //RelativeMagEjectRot = Quaternion.Inverse(TemporaryFirearm.transform.rotation) * MagEjectPos.rotation;
            RelativeMagEjectRot = TemporaryFirearm.transform.InverseTransformRotation(MagEjectPos.rotation);
        }*/

        public void CalculateRelativeMagMountPos(FVRFireArm fireArm)
        {
            _relativeMagPos = fireArm.transform.InverseTransformPoint(MagMountPos.position);
            _relativeMagRot = fireArm.transform.InverseTransformRotation(MagMountPos.rotation);
        }

        public void CalculateRelativeMagEjectPos(FVRFireArm fireArm)
        {
            _relativeMagEjectPos = fireArm.transform.InverseTransformPoint(MagEjectPos.position);
            _relativeMagEjectRot = fireArm.transform.InverseTransformRotation(MagEjectPos.rotation);
        }
    }
}
