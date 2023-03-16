using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class MovingFireArmMuzzleDeviceInterface : MuzzleDeviceInterface
    {
        [Header("Moving FireArm MuzzleDevice Interface Config")]

        [Tooltip("One degree means linear movement, two degrees means movement on a plane, three degrees free spacial movement.")]
        public EDegreesOfFreedom DegreesOfFreedom;
        public enum EDegreesOfFreedom
        {
            Linear,
            Planar,
            Spacial
        }
        public OpenScripts2_BasePlugin.Axis LimitingAxis;
        public Vector2 XLimits = new Vector2(float.NegativeInfinity, float.PositiveInfinity);
        public Vector2 YLimits = new Vector2(float.NegativeInfinity, float.PositiveInfinity);
        public Vector2 ZLimits = new Vector2(float.NegativeInfinity, float.PositiveInfinity);

        public Transform SecondaryPiece;

        private Vector3 _lastPos;
        private Vector3 _lastHandPos;

        private Vector3 _startPos;
        private Vector3 _lowerLimit;
        private Vector3 _upperLimit;

        public const string POSITION_FLAGDIC_KEY = "MovingFireArmMuzzleDeviceInterface Position";
        public const string SECONDARY_POSITION_FLAGDIC_KEY = "MovingFireArmMuzzleDeviceInterface Secondary Position";

        public override void Awake()
        {
            base.Awake();
            _lowerLimit = new Vector3(XLimits.x, YLimits.x, ZLimits.x);
            _upperLimit = new Vector3(XLimits.y, YLimits.y, ZLimits.y);

            _startPos = Attachment.ObjectWrapper.GetGameObject().GetComponent<FVRFireArmAttachment>().AttachmentInterface.transform.localPosition;
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
                OpenScripts2_BepInExPlugin.Log(this, posDelta.ToString());

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
            else if (OpenScripts2_BasePlugin.TouchpadDirPressed(hand, Vector2.up))
            {
                transform.localPosition = _startPos;
            }

            _lastPos = transform.position;
            _lastHandPos = hand.Input.FilteredPos;
        }

        private void OneDegreeOfFreedom(Vector3 newPosRaw)
        {
            Vector3 newPosProjected = GetClosestValidPoint(_lowerLimit.GetCombinedAxisVector(LimitingAxis, transform.localPosition), _upperLimit.GetCombinedAxisVector(LimitingAxis, transform.localPosition), transform.parent.InverseTransformPoint(newPosRaw));
            transform.localPosition = newPosProjected;
        }
        private void TwoDegreesOfFreedom(Vector3 newPosRaw)
        {
            Vector3 newPosProjected = newPosRaw.ProjectOnPlaneThroughPoint(transform.position, transform.GetLocalDirAxis(LimitingAxis));
            Vector3 newPosClamped = transform.parent.InverseTransformPoint(newPosProjected).Clamp(_lowerLimit, _upperLimit);
            transform.localPosition = newPosClamped;
        }
        private void ThreeDegreesOfFreedom(Vector3 newPosRaw)
        {
            transform.localPosition = transform.parent.InverseTransformPoint(transform.position + newPosRaw).Clamp(_lowerLimit, _upperLimit);
        }

        [ContextMenu("Copy existing Interface's values")]
        public void CopyAttachment()
        {
            MuzzleDeviceInterface[] attachments = GetComponents<MuzzleDeviceInterface>();

            MuzzleDeviceInterface toCopy = attachments.Single(c => c != this);

            toCopy.Attachment.AttachmentInterface = this;

            this.CopyComponent(toCopy);
        }
    }
}
