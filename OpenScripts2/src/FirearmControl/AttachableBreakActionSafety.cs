using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class AttachableBreakActionSafety : FVRInteractiveObject
    {
        public AttachableFirearmPhysicalObject PhysicalObject;
        public AttachableFirearmInterface Interface;
        public AttachableBreakActions GL;
        public Transform Safety;
        public float SafetyOff;
        public float SafetyOn;

        private bool _safetyEngaged = true;

        private static readonly Dictionary<AttachableBreakActions, AttachableBreakActionSafety> _existingGL_Safeties = new();

#if !DEBUG
        static AttachableBreakActionSafety()
        {
            On.FistVR.AttachableBreakActions.Fire += AttachableBreakActions_Fire;
            On.FistVR.AttachableBreakActions.ProcessInput += AttachableBreakActions_ProcessInput;
        }

        private static void AttachableBreakActions_ProcessInput(On.FistVR.AttachableBreakActions.orig_ProcessInput orig, AttachableBreakActions self, FVRViveHand hand, bool fromInterface, FVRInteractiveObject o)
        {
            if (_existingGL_Safeties.TryGetValue(self, out AttachableBreakActionSafety safety) && OpenScripts2_BasePlugin.TouchpadDirDown(hand, Vector2.up))
            {
                safety._safetyEngaged = !safety._safetyEngaged;
                safety.GL.PlayAudioEvent(FirearmAudioEventType.Safety);
            }

            orig(self, hand, fromInterface, o);
        }

        private static void AttachableBreakActions_Fire(On.FistVR.AttachableBreakActions.orig_Fire orig, AttachableBreakActions self, bool firedFromInterface)
        {
            if (_existingGL_Safeties.TryGetValue(self, out AttachableBreakActionSafety safety) && safety._safetyEngaged) return;
            else orig(self, firedFromInterface);
        }
#endif

        public override void Awake()
        {
            base.Awake();

            IsSimpleInteract = true;
            _existingGL_Safeties.Add(GL, this);
        }

        public override void OnDestroy()
        {
            _existingGL_Safeties.Remove(GL);

            base.OnDestroy();
        }

        public override void SimpleInteraction(FVRViveHand hand)
        {
            base.SimpleInteraction(hand);

            _safetyEngaged = !_safetyEngaged;
            GL.PlayAudioEvent(FirearmAudioEventType.Safety);
        }

        public override void FVRUpdate()
        {
            base.FVRUpdate();

            float target = _safetyEngaged ? SafetyOn : SafetyOff;
            GL.Attachment.SetAnimatedComponent(Safety, target, FVRPhysicalObject.InterpStyle.Rotation, FVRPhysicalObject.Axis.X);
        }
    }
}
