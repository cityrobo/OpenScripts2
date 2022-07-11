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

        public Vector3 RelativeMagPos;
        public Quaternion RelativeMagRot;

        public Vector3 RelativeMagEjectPos;
        public Quaternion RelativeMagEjectRot;

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
                        _origMagPos = _fireArm.MagazineMountPos.localPosition;
                        _origMagRot = _fireArm.MagazineMountPos.localRotation;

                        _origMagEjectPos = _fireArm.MagazineEjectPos.localPosition;
                        _origMagEjectRot = _fireArm.MagazineEjectPos.localRotation;

                        _fireArm.MagazineMountPos.localPosition = RelativeMagPos;
                        _fireArm.MagazineMountPos.localRotation = RelativeMagRot;

                        _fireArm.MagazineEjectPos.localPosition = RelativeMagEjectPos;
                        _fireArm.MagazineEjectPos.localRotation = RelativeMagEjectRot;
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
                            this.LogWarning("ModifyWeaponCartrideAndMagazineAttachment: FireArm type not supported!");
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

                if (Shots_Main != null) _fireArm.AudioClipSet.Shots_Main = Shots_Main;
                if (Shots_Suppressed != null) _fireArm.AudioClipSet.Shots_Suppressed = Shots_Suppressed;
                if (Shots_LowPressure != null) _fireArm.AudioClipSet.Shots_LowPressure = Shots_LowPressure;
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
                            this.LogWarning("ModifyWeaponCartrideAndMagazineAttachment: FireArm type not supported!");
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

        [ContextMenu("Calculate relative magazine transforms")]
        public void CaluculateRelativeMagPos()
        {
            RelativeMagPos = TemporaryFirearm.transform.InverseTransformPoint(MagMountPos.position);
            RelativeMagRot = Quaternion.Inverse(TemporaryFirearm.transform.rotation) * MagMountPos.rotation;

            RelativeMagEjectPos = TemporaryFirearm.transform.InverseTransformPoint(MagEjectPos.position);
            RelativeMagEjectRot = Quaternion.Inverse(TemporaryFirearm.transform.rotation) * MagEjectPos.rotation;
        }
    }
}
