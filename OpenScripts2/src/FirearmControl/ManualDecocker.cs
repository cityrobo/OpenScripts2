using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace OpenScripts2
{
    public class ManualDecocker : OpenScripts2_BasePlugin
    {
        [Header("Manual Handgun Decocker")]
        public Handgun Handgun;
        public enum EInputType
        {
            TouchpadUp,
            TouchpadDown, 
            TouchpadLeft,
            TouchpadRight
        }
        public EInputType InputType;
        [Tooltip("Use this if you wanna use the safety as it is configured on the handgun itself.")]
        public bool UsesHandgunSafetyObjectInstead = false;
        [Header("Standalone decocker object.")]
        public Transform Decocker;
        public FVRPhysicalObject.Axis DecockerAxis;
        public FVRPhysicalObject.InterpStyle DecockerInterpStyle;
        public float DecockerReleased;
        public float DecockerPressed;

        private bool _wasDecocked = false;

        public void Update()
        {
            FVRViveHand hand = Handgun.m_hand;

            if (hand != null && CorrectInput(hand))
            {
                if (!_wasDecocked)
                {
                    Handgun.DeCockHammer(true, true);
                    if (!UsesHandgunSafetyObjectInstead) Handgun.SetAnimatedComponent(Decocker, DecockerPressed, DecockerInterpStyle, DecockerAxis);
                    else Handgun.SetAnimatedComponent(Handgun.Safety, Handgun.SafetyOn, Handgun.Safety_Interp, Handgun.SafetyAxis);
                    _wasDecocked = true;
                }
            }
            else if (_wasDecocked)
            {
                if (!UsesHandgunSafetyObjectInstead) Handgun.SetAnimatedComponent(Decocker, DecockerReleased, DecockerInterpStyle, DecockerAxis);
                else Handgun.SetAnimatedComponent(Handgun.Safety, Handgun.SafetyOff, Handgun.Safety_Interp, Handgun.SafetyAxis);
                _wasDecocked = false;
            }
        }

        private bool CorrectInput(FVRViveHand hand) => InputType switch
        {
            EInputType.TouchpadUp => TouchpadDirPressed(hand, Vector2.up),
            EInputType.TouchpadDown => TouchpadDirPressed(hand, Vector2.down),
            EInputType.TouchpadLeft => TouchpadDirPressed(hand, Vector2.left),
            EInputType.TouchpadRight => TouchpadDirPressed(hand, Vector2.right),
            _ => false,
        };
    }
}