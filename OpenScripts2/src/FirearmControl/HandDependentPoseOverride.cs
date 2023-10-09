using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    public class HandDependentPoseOverride : OpenScripts2_BasePlugin
    {
        public FVRPhysicalObject PhysicalObject;
        public Transform LeftPoseOverride;
        public Transform LeftPoseOverride_Touch;
        public Transform RightPoseOverride;
        public Transform RightPoseOverride_Touch;

        private bool _hasPoseOverride_Touch = false;

        private static readonly Dictionary<FVRPhysicalObject, HandDependentPoseOverride> _existingHandDependentPoseOverride = new();

#if !DEBUG
        static HandDependentPoseOverride()
        {
            On.FistVR.FVRPhysicalObject.BeginInteraction += FVRPhysicalObject_BeginInteraction;
        }

        public void Awake()
        {
            if (LeftPoseOverride_Touch != null && RightPoseOverride_Touch != null) _hasPoseOverride_Touch = true;

            _existingHandDependentPoseOverride.Add(PhysicalObject, this);
        }

        public void OnDestroy()
        {
            _existingHandDependentPoseOverride.Remove(PhysicalObject);
        }

        private static void FVRPhysicalObject_BeginInteraction(On.FistVR.FVRPhysicalObject.orig_BeginInteraction orig, FVRPhysicalObject self, FVRViveHand hand)
        {
            if (_existingHandDependentPoseOverride.TryGetValue(self, out HandDependentPoseOverride handDependentPoseOverride))
            {
                if (!hand.IsThisTheRightHand)
                {
                    if ((hand.CMode == ControlMode.Oculus || hand.CMode == ControlMode.Index) && handDependentPoseOverride._hasPoseOverride_Touch)
                    {
                        self.PoseOverride = handDependentPoseOverride.LeftPoseOverride_Touch;
                        self.PoseOverride_Touch = handDependentPoseOverride.LeftPoseOverride_Touch;
                        if (self is FVRFireArm f) f.m_storedLocalPoseOverrideRot = f.PoseOverride.localRotation;
                    }
                    else
                    {
                        self.PoseOverride = handDependentPoseOverride.LeftPoseOverride;
                        if (self is FVRFireArm f) f.m_storedLocalPoseOverrideRot = f.PoseOverride.localRotation;
                    }
                }
                else
                {
                    if ((hand.CMode == ControlMode.Oculus || hand.CMode == ControlMode.Index) && handDependentPoseOverride._hasPoseOverride_Touch)
                    {
                        self.PoseOverride = handDependentPoseOverride.RightPoseOverride_Touch;
                        self.PoseOverride_Touch = handDependentPoseOverride.RightPoseOverride_Touch;
                        if (self is FVRFireArm f) f.m_storedLocalPoseOverrideRot = f.PoseOverride.localRotation;
                    }
                    else
                    {
                        self.PoseOverride = handDependentPoseOverride.RightPoseOverride;
                        if (self is FVRFireArm f) f.m_storedLocalPoseOverrideRot = f.PoseOverride.localRotation;
                    }
                }
            }

            orig(self, hand);
        }
#endif
    }
}