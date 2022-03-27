using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class MagazinePoseCycler : OpenScripts2_BasePlugin
    {
        public FVRFireArmMagazine Magazine;

        public List<Transform> AlternatePoseOverrides;

        private int _poseIndex = 0;

        private Vector3 _positionalOffset = new Vector3(0,0,0);
        private Quaternion _rotationalOffset = new Quaternion();

        private bool _offsetCalculated = false;

#if!DEBUG
        public void Awake()
        {
            AlternatePoseOverrides.Add(Transform.Instantiate(Magazine.PoseOverride));
            _poseIndex = AlternatePoseOverrides.Count - 1;

            Hook();
        }

        public void OnDestroy()
        {
            Unhook();
        }

        public void Update()
        {
            FVRViveHand hand = Magazine.m_hand;

            if (hand != null)
            {
                if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f)
                {
                    NextPose();
                }
                UpdatePose();
            }
        }

        void NextPose()
        {
            _poseIndex++;
            if (_poseIndex >= AlternatePoseOverrides.Count) _poseIndex = 0;

            UpdatePose();
        }

        void UpdatePose()
        {
            Magazine.PoseOverride.localPosition = AlternatePoseOverrides[_poseIndex].localPosition + _positionalOffset;
            Magazine.PoseOverride.localRotation = _rotationalOffset * AlternatePoseOverrides[_poseIndex].localRotation;
        }

        void CalculateOffset(FVRFireArmMagazine magazine)
        {
            _positionalOffset = magazine.PoseOverride_Touch.localPosition - magazine.PoseOverride.localPosition;
            _rotationalOffset = magazine.PoseOverride_Touch.localRotation * Quaternion.Inverse(magazine.PoseOverride.localRotation);
            _offsetCalculated = true;
        }

        public void Unhook()
        {
            On.FistVR.FVRPhysicalObject.UpdatePosesBasedOnCMode -= FVRPhysicalObject_UpdatePosesBasedOnCMode;
        }

        public void Hook()
        {
            On.FistVR.FVRPhysicalObject.UpdatePosesBasedOnCMode += FVRPhysicalObject_UpdatePosesBasedOnCMode;
        }

        private void FVRPhysicalObject_UpdatePosesBasedOnCMode(On.FistVR.FVRPhysicalObject.orig_UpdatePosesBasedOnCMode orig, FVRPhysicalObject self, FVRViveHand hand)
        {
            if (self as FVRFireArmMagazine == Magazine)
            {
                if (!_offsetCalculated && (hand.CMode == ControlMode.Oculus || hand.CMode == ControlMode.Index) && (self as FVRFireArmMagazine).PoseOverride_Touch != null)
                {
                    CalculateOffset(self as FVRFireArmMagazine);
                }
            }
            orig(self, hand);
        }

#endif
    }
}
