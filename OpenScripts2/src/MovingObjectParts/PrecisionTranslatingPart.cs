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
        [Header("Precision Translating Part Config")]

        [Tooltip("One degree means linear movement, two degrees means movement on a plane, three degrees free spacial movement")]
        public EDegreesOfFreedom DegreesOfFreedom;
        public enum EDegreesOfFreedom
        {
            Linear,
            Planar,
            Spacial
        }
        public OpenScripts2_BasePlugin.Axis LimitingAxis;
        public Vector2 XLimits = new(float.NegativeInfinity, float.PositiveInfinity);
        public Vector2 YLimits = new(float.NegativeInfinity, float.PositiveInfinity);
        public Vector2 ZLimits = new(float.NegativeInfinity, float.PositiveInfinity);

        private Vector3 _lastPos;
        private Vector3 _lastHandPos;

        private Vector3 _startPos;
        private Vector3 _lowerLimit;
        private Vector3 _upperLimit;

        public override void Awake()
        {
            base.Awake();
            _lowerLimit = new Vector3(XLimits.x, YLimits.x, ZLimits.x);
            _upperLimit = new Vector3(XLimits.y, YLimits.y, ZLimits.y);

            _startPos = transform.localPosition;
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            base.BeginInteraction(hand);

            _lastPos = transform.position;
            _lastHandPos = hand.Input.FilteredPos;
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);

            if (hand.Input.TriggerFloat > 0f)
            {
                Vector3 adjustedHandPosDelta = (hand.Input.FilteredPos - _lastHandPos) * m_hand.Input.TriggerFloat;
                Vector3 posDelta = (transform.position - _lastPos) * m_hand.Input.TriggerFloat;
                Vector3 newPosRaw = transform.position + adjustedHandPosDelta - posDelta;
                switch (DegreesOfFreedom)
                {
                    case EDegreesOfFreedom.Linear:
                        OneDegreeOfFreedom(newPosRaw);
                        break;
                    case EDegreesOfFreedom.Planar:
                        TwoDegreesOfFreedom(newPosRaw);
                        break;
                    case EDegreesOfFreedom.Spacial:
                        ThreeDegreesOfFreedom(newPosRaw);
                        break;
                }
            }
            else if (OpenScripts2_BasePlugin.TouchpadDirDown(hand, Vector2.up))
            {
                transform.localPosition = _startPos;
            }

            _lastPos = transform.position;
            _lastHandPos = hand.Input.FilteredPos;
        }

        private void OneDegreeOfFreedom(Vector3 newPosRaw)
        {
            Vector3 lowLimit = _lowerLimit.GetCombinedAxisVector(LimitingAxis, transform.localPosition).ApproximateInfiniteComponent(100f);
            Vector3 highLimit = _upperLimit.GetCombinedAxisVector(LimitingAxis, transform.localPosition).ApproximateInfiniteComponent(100f);
            Vector3 newPosProjected = GetClosestValidPoint(lowLimit, highLimit, transform.parent.InverseTransformPoint(newPosRaw));
            transform.localPosition = newPosProjected;
        }
        private void TwoDegreesOfFreedom(Vector3 newPosRaw)
        {
            Vector3 newPosProjected = newPosRaw.ProjectOnPlaneThroughPoint(transform.position, transform.parent.GetLocalDirAxis(LimitingAxis));
            Vector3 newPosClamped = transform.parent.InverseTransformPoint(newPosProjected).Clamp(_lowerLimit, _upperLimit);
            transform.localPosition = newPosClamped;
        }
        private void ThreeDegreesOfFreedom(Vector3 newPosRaw)
        {
            transform.localPosition = transform.parent.InverseTransformPoint(transform.position + newPosRaw).Clamp(_lowerLimit, _upperLimit);
        }
    }
}
