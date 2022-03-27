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

#if !DEBUG

        public void Start()
        {
            if (LeftPoseOverride_Touch != null && RightPoseOverride_Touch != null) _hasPoseOverride_Touch = true;

            Hook();
        }

        public void OnDestroy()
        {
            Unhook();
        }

        void Unhook()
        {
            On.FistVR.FVRPhysicalObject.BeginInteraction -= FVRPhysicalObject_BeginInteraction;
        }
        void Hook()
        {
            On.FistVR.FVRPhysicalObject.BeginInteraction += FVRPhysicalObject_BeginInteraction;
        }

        private void FVRPhysicalObject_BeginInteraction(On.FistVR.FVRPhysicalObject.orig_BeginInteraction orig, FVRPhysicalObject self, FVRViveHand hand)
        {
            if (self == PhysicalObject)
            {
                if (!hand.IsThisTheRightHand)
                {
                    if ((hand.CMode == ControlMode.Oculus || hand.CMode == ControlMode.Index) && _hasPoseOverride_Touch) self.PoseOverride = LeftPoseOverride_Touch;
                    else PhysicalObject.PoseOverride = LeftPoseOverride;
                }
                else
                {
                    if ((hand.CMode == ControlMode.Oculus || hand.CMode == ControlMode.Index) && _hasPoseOverride_Touch) self.PoseOverride = RightPoseOverride_Touch;
                    else PhysicalObject.PoseOverride = RightPoseOverride;
                }
            }
        
            orig(self, hand);
        }
#endif
        //void Update()
        //{
        //    FVRViveHand hand = PhysicalObject.m_hand;
        //    if (hand != null)
        //    {
        //        if (!hand.IsThisTheRightHand)
        //        {
        //            if ((hand.CMode == ControlMode.Oculus || hand.CMode == ControlMode.Index) && _hasPoseOverride_Touch) PhysicalObject.PoseOverride = LeftPoseOverride_Touch;
        //            else PhysicalObject.PoseOverride = LeftPoseOverride;
        //        }
        //        else
        //        {
        //            if ((hand.CMode == ControlMode.Oculus || hand.CMode == ControlMode.Index) && _hasPoseOverride_Touch) PhysicalObject.PoseOverride = RightPoseOverride_Touch;
        //            else PhysicalObject.PoseOverride = RightPoseOverride;
        //        }
        //    }
        //}
    }
}
