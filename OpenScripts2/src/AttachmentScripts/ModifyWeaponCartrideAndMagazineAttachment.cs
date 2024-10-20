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

        private FVRFireArm _fireArm = null;

        [Header("Caliber Modification")]
        public bool ChangesCaliber = true;
        [SearchableEnum]
        public FireArmRoundType RoundType;

        private FireArmRoundType _origRoundType;
        private FireArmMagazineType _origMagType;

        [Header("MagazineType Modification")]
        public bool ChangesMagType = true;
        [SearchableEnum]
        public FireArmMagazineType MagType;

        [Header("MagPos Calculation Modification")]
        [Header("Place _firearm and the new mag pos into these fields and use the context menu to calculate the position.")]
        public FVRFireArm TemporaryFirearm;
        public Transform MagMountPos;
        public Transform MagEjectPos;

        private Vector3 _relativeMagPos;
        private Quaternion _relativeMagRot;

        private Vector3 _relativeMagEjectPos;
        private Quaternion _relativeMagEjectRot;

        private Vector3 _origMagPos;
        private Quaternion _origMagRot;

        private Vector3 _origMagEjectPos;
        private Quaternion _origMagEjectRot;

        [Header("Recoil Manipulation")]       
        public FVRFireArmRecoilProfile RecoilProfile;
        public FVRFireArmRecoilProfile RecoilProfileStocked;

        public bool UsesRecoilMultiplierInstead = false;
        public float RecoilMultiplier = 1f;

        private FVRFireArmRecoilProfile _origRecoilProfile;
        private FVRFireArmRecoilProfile _origRecoilProfileStocked;

        [Header("Accuracy Manipulation")]
        public FVRFireArmMechanicalAccuracyClass AccuracyClass;

        private FVRFireArmMechanicalAccuracyClass _origAccuracyClass;

        [Header("Sound Manipulation")]
        public AudioEvent Shots_Main;
        public AudioEvent Shots_Suppressed;
        public AudioEvent Shots_LowPressure;

        private AudioEvent _orig_Shots_Main;
        private AudioEvent _orig_Shots_Suppressed;
        private AudioEvent _orig_Shots_LowPressure;

        [Header("Round Power")]
        public bool ChangesChamberVelocityMultiplier = false;
        public float ChamberVelocityMultiplierOverride = 0f;

        private float _origChamberVelocityMultiplier;

        public void Update()
        {
            if (Attachment.curMount != null && _fireArm == null)
            {
                _fireArm = Attachment.curMount.GetRootMount().MyObject as FVRFireArm;
                if (_fireArm != null)
                {
                    List<FVRFireArmChamber> chambers = _fireArm.GetChambers();

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
                        chambers.ForEach(c => c.RoundType = RoundType);
                    }

                    _origRecoilProfile = _fireArm.RecoilProfile;
                    _origRecoilProfileStocked = _fireArm.RecoilProfileStocked;

                    _origAccuracyClass = _fireArm.AccuracyClass;

                    _orig_Shots_Main = _fireArm.AudioClipSet.Shots_Main;
                    _orig_Shots_Suppressed = _fireArm.AudioClipSet.Shots_Suppressed;
                    _orig_Shots_LowPressure = _fireArm.AudioClipSet.Shots_LowPressure;

                    if (!UsesRecoilMultiplierInstead)
                    {
                        if (RecoilProfile != null) _fireArm.RecoilProfile = RecoilProfile;
                        if (RecoilProfileStocked != null) _fireArm.RecoilProfileStocked = RecoilProfileStocked;
                    }
                    else
                    {
                        _fireArm.RecoilProfile = CopyAndAdjustRecoilProfile(_fireArm.RecoilProfile, RecoilMultiplier);
                        if (_fireArm.RecoilProfileStocked != null) _fireArm.RecoilProfileStocked = CopyAndAdjustRecoilProfile(_fireArm.RecoilProfileStocked, RecoilMultiplier);
                    }

                    if (AccuracyClass != 0) _fireArm.AccuracyClass = AccuracyClass;

                    if (Shots_Main.Clips.Count != 0) _fireArm.AudioClipSet.Shots_Main = Shots_Main;
                    if (Shots_Suppressed.Clips.Count != 0) _fireArm.AudioClipSet.Shots_Suppressed = Shots_Suppressed;
                    if (Shots_LowPressure.Clips.Count != 0) _fireArm.AudioClipSet.Shots_LowPressure = Shots_LowPressure;

                    if (ChangesChamberVelocityMultiplier)
                    {
                        _origChamberVelocityMultiplier = chambers[0].ChamberVelocityMultiplier;

                        chambers.ForEach(c => c.ChamberVelocityMultiplier = ChamberVelocityMultiplierOverride);
                    }
                }
            }
            else if (Attachment.curMount == null && _fireArm != null)
            {
                List<FVRFireArmChamber> chambers = _fireArm.GetChambers();

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

                    chambers.ForEach(c => c.RoundType = _origRoundType);
                }

                _fireArm.RecoilProfile = _origRecoilProfile;
                _fireArm.RecoilProfileStocked = _origRecoilProfileStocked;

                _fireArm.AccuracyClass = _origAccuracyClass;

                _fireArm.AudioClipSet.Shots_Main = _orig_Shots_Main;
                _fireArm.AudioClipSet.Shots_Suppressed = _orig_Shots_Suppressed;
                _fireArm.AudioClipSet.Shots_LowPressure = _orig_Shots_LowPressure;

                if (ChangesChamberVelocityMultiplier)
                {
                    chambers.ForEach(c => c.ChamberVelocityMultiplier = _origChamberVelocityMultiplier);
                }

                _fireArm = null;
            }
        }

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
