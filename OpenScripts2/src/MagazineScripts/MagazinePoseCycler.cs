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

        private static readonly Dictionary<FVRPhysicalObject, MagazinePoseCycler> _existingMagazinePoseCyclers = new();
        private static readonly Dictionary<string, int> _changedMagazines = new();

#if!DEBUG
        static MagazinePoseCycler()
        {
            Hook();
        }

        public void Awake()
        {
            AlternatePoseOverrides.Add(Instantiate(Magazine.PoseOverride));
            _poseIndex = AlternatePoseOverrides.Count - 1;

            _existingMagazinePoseCyclers.Add(Magazine,this);

            _poseIndex = _changedMagazines.ContainsKey(Magazine.ObjectWrapper.ItemID) ? _changedMagazines[Magazine.ObjectWrapper.ItemID] : 0;
        }

        public void OnDestroy()
        {
            _existingMagazinePoseCyclers.Remove(Magazine);
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

        private void NextPose()
        {
            _poseIndex++;
            if (_poseIndex >= AlternatePoseOverrides.Count) _poseIndex = 0;

            UpdatePose();

            if (_changedMagazines.ContainsKey(Magazine.ObjectWrapper.ItemID))
            {
                _changedMagazines[Magazine.ObjectWrapper.ItemID] = _poseIndex;
            }
            else
            {
                _changedMagazines.Add(Magazine.ObjectWrapper.ItemID, _poseIndex);
            }
        }

        private void UpdatePose()
        {
            Magazine.PoseOverride.localPosition = AlternatePoseOverrides[_poseIndex].localPosition + _positionalOffset;
            Magazine.PoseOverride.localRotation = _rotationalOffset * AlternatePoseOverrides[_poseIndex].localRotation;
        }

        private void CalculateOffset(FVRPhysicalObject physicalObject)
        {
            _positionalOffset = physicalObject.PoseOverride_Touch.localPosition - physicalObject.PoseOverride.localPosition;
            _rotationalOffset = physicalObject.PoseOverride_Touch.localRotation * Quaternion.Inverse(physicalObject.PoseOverride.localRotation);
            _offsetCalculated = true;
        }

        public static void Hook()
        {
            On.FistVR.FVRPhysicalObject.UpdatePosesBasedOnCMode += FVRPhysicalObject_UpdatePosesBasedOnCMode;
        }

        private static void FVRPhysicalObject_UpdatePosesBasedOnCMode(On.FistVR.FVRPhysicalObject.orig_UpdatePosesBasedOnCMode orig, FVRPhysicalObject self, FVRViveHand hand)
        {
            MagazinePoseCycler magazinePoseCycler;
            if (_existingMagazinePoseCyclers.TryGetValue(self, out magazinePoseCycler))
            {
                if (!magazinePoseCycler._offsetCalculated && (hand.CMode == ControlMode.Oculus || hand.CMode == ControlMode.Index) && self.PoseOverride_Touch != null)
                {
                    magazinePoseCycler.CalculateOffset(self);
                }
            }
            orig(self, hand);
        }

#endif
    }
}
