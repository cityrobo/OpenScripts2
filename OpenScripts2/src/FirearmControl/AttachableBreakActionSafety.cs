using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
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

        private static Dictionary<AttachableBreakActions, AttachableBreakActionSafety> _existingGL_Safeties = new Dictionary<AttachableBreakActions, AttachableBreakActionSafety>();

        static AttachableBreakActionSafety()
        {
#if !DEBUG
            On.FistVR.AttachableBreakActions.Fire += AttachableBreakActions_Fire;
            On.FistVR.AttachableBreakActions.ProcessInput += AttachableBreakActions_ProcessInput;
#endif
        }
#if !DEBUG
        private static void AttachableBreakActions_ProcessInput(On.FistVR.AttachableBreakActions.orig_ProcessInput orig, AttachableBreakActions self, FVRViveHand hand, bool fromInterface, FVRInteractiveObject o)
        {
            AttachableBreakActionSafety safety;
            if (_existingGL_Safeties.TryGetValue(self, out safety) && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.up) < 45f)
            {
                safety._safetyEngaged = !safety._safetyEngaged;
                safety.GL.PlayAudioEvent(FirearmAudioEventType.Safety);
            }

            orig(self, hand, fromInterface, o);
        }
        private static void AttachableBreakActions_Fire(On.FistVR.AttachableBreakActions.orig_Fire orig, AttachableBreakActions self, bool firedFromInterface)
        {
            AttachableBreakActionSafety safety;
            if (_existingGL_Safeties.TryGetValue(self, out safety) && safety._safetyEngaged) return;
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
            base.OnDestroy();
            _existingGL_Safeties.Remove(GL);
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
            if (_safetyEngaged)
            {
                GL.Attachment.SetAnimatedComponent(Safety, SafetyOn, FVRPhysicalObject.InterpStyle.Rotation, FVRPhysicalObject.Axis.X);
            }
            else
            {
                GL.Attachment.SetAnimatedComponent(Safety, SafetyOff, FVRPhysicalObject.InterpStyle.Rotation, FVRPhysicalObject.Axis.X);
            }
        }
    }
}
