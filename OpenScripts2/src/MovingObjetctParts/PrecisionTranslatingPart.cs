using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    public class PrecisionTranslatingPart : FVRInteractiveObject
    {
        [Header("PrecisionTranslatingPart Config")]

        [Tooltip("One degree means linear movement, two degrees means movement on a plane, three degrees ")]
        public EDegreesOfFreedom DegreesOfFreedom;
        public enum EDegreesOfFreedom
        {
            Linear,
            Planar,
            Area
        }
        public OpenScripts2_BasePlugin.Axis LimitingAxis;
        Vector2 XLimits = new Vector2(float.NegativeInfinity, float.PositiveInfinity);
        Vector2 YLimits = new Vector2(float.NegativeInfinity, float.PositiveInfinity);
        Vector2 ZLimits = new Vector2(float.NegativeInfinity, float.PositiveInfinity);
        private Vector3 _lastHandPos;

        private Vector3 _lowerLimit;
        private Vector3 _upperLimit;
        /*
        private const string c_laserAssemblyPositionFlagDicString = "triggerguardLaser_pos";
        private const string c_clampingMechanismPositionFlagDicString = "clamp_pos";
        */

        public override void Awake()
        {
            base.Awake();
            _lowerLimit = new Vector3(XLimits.x, YLimits.x, ZLimits.x);
            _upperLimit = new Vector3(XLimits.y, YLimits.y, ZLimits.y);
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            base.BeginInteraction(hand);

            _lastHandPos = hand.Input.FilteredPos;
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);

            switch (DegreesOfFreedom)
            {
                case EDegreesOfFreedom.Linear:
                    OneDegreeOfFreedom(hand);
                    break;
                case EDegreesOfFreedom.Planar:
                    TwoDegreesOfFreedom(hand);
                    break;
                case EDegreesOfFreedom.Area:
                    ThreeDegreesOfFreedom(hand);
                    break;
            }

            _lastHandPos = hand.Input.FilteredPos;
        }

        private void OneDegreeOfFreedom(FVRViveHand hand)
        {
            if (hand.Input.TriggerFloat > 0f)
            {
                Vector3 adjustedHandPosDelta = (hand.Input.FilteredPos - _lastHandPos) * m_hand.Input.TriggerFloat;
                Vector3 newPosRAW = transform.position + adjustedHandPosDelta;
                Vector3 newPosProjected = GetClosestValidPoint(newPosRAW, Vector3.Scale(_lowerLimit, OpenScripts2_BasePlugin.GetVectorFromAxis(LimitingAxis)), Vector3.Scale(_upperLimit, OpenScripts2_BasePlugin.GetVectorFromAxis(LimitingAxis)));
                transform.position = newPosProjected;
            }
        }
        private void TwoDegreesOfFreedom(FVRViveHand hand)
        {
            if (hand.Input.TriggerFloat > 0f)
            {
                Vector3 adjustedHandPosDelta = (hand.Input.FilteredPos - _lastHandPos) * m_hand.Input.TriggerFloat;

                Vector3 newPosRAW = transform.position + adjustedHandPosDelta;
                Vector3 newPosProjected = newPosRAW.ProjectOnPlaneThroughPoint(transform.position, transform.GetLocalDirAxis(LimitingAxis));
                Vector3 newPosClamped = newPosProjected.Clamp(_lowerLimit, _upperLimit);
                transform.position = newPosClamped;
            }
        }
        private void ThreeDegreesOfFreedom(FVRViveHand hand)
        {
            if (hand.Input.TriggerFloat > 0f)
            {
                Vector3 adjustedHandPosDelta = (hand.Input.FilteredPos - _lastHandPos) * m_hand.Input.TriggerFloat;
                transform.position = (transform.position + adjustedHandPosDelta).Clamp(_lowerLimit, _upperLimit);
            }
        }
        /*
        public override void ConfigureFromFlagDic(Dictionary<string, string> f)
        {
            base.ConfigureFromFlagDic(f);

            if (f.ContainsKey(c_laserAssemblyPositionFlagDicString))
            {
                string flag = f[c_laserAssemblyPositionFlagDicString];

                flag = flag.Replace(" ", "");
                flag = flag.Replace("(", "");
                flag = flag.Replace(")", "");
                string[] coordinatesStrings = flag.Split(',');
                LaserAssembly.localPosition = new Vector3(float.Parse(coordinatesStrings[0]), float.Parse(coordinatesStrings[1]), float.Parse(coordinatesStrings[2]));
            }
            if (f.ContainsKey(c_clampingMechanismPositionFlagDicString))
            {
                string flag = f[c_clampingMechanismPositionFlagDicString];

                flag = flag.Replace(" ", "");
                flag = flag.Replace("(", "");
                flag = flag.Replace(")", "");
                string[] coordinatesStrings = flag.Split(',');
                ClampingMechanism.transform.localPosition = new Vector3(float.Parse(coordinatesStrings[0]), float.Parse(coordinatesStrings[1]), float.Parse(coordinatesStrings[2]));
            }
        }

        public override Dictionary<string, string> GetFlagDic()
        {
            Dictionary<string, string> flagDic = base.GetFlagDic();

            flagDic.Add(c_laserAssemblyPositionFlagDicString, LaserAssembly.localPosition.ToString("F6"));
            flagDic.Add(c_clampingMechanismPositionFlagDicString, ClampingMechanism.transform.localPosition.ToString("F6"));

            return flagDic;
        }
        */
    }
}
