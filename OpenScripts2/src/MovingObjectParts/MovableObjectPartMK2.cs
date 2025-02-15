using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace OpenScripts2
{
    [Obsolete("Not Finished Yet/untested, use MovableObjectPart instead!")]
    public class MovableObjectPartMK2 : FVRInteractiveObject
    {
        public enum Mode
        {
            Sliding,
            Rotating,
            Folding
        }
        public Mode MovementMode;

        public OpenScripts2_BasePlugin.Axis MovementAxis;

        public Transform Root;
        public Transform ObjectToMove;

        [Tooltip("When used for a stock should be used for the closed angle.\n Limited between -360°/360° for folding.")]
        public float LowerLimit = 0f;
        [Tooltip("When used for a stock should be used for the open angle.\n Limited between -360°/360° for folding.")]
        public float UpperLimit = 0f;
        public bool SnapsToEndStops = true;
        [Tooltip("In meters when in sliding mode.\nIn degrees when in rotating or folding mode.\nThis is important to know because angles are usually much higher lol.\n(0.02m vs 0.02°)")]
        public float LimitWiggleRoom = 0.02f;

        [Header("Optional endstop and handling sounds")]
        public AudioEvent CloseSounds;
        public AudioEvent OpenSounds;
        public AudioEvent BeginInteractionSounds;
        public AudioEvent EndInteractionSounds;

        public enum E_State
        {
            Open,
            Mid,
            Closed
        }

        [HideInInspector]
        public E_State CurrentState;
        private E_State _lastState;

        private float _currentPositionValue;
        private float _lastPositionValue;

        private Vector3 _origPos;

        private Vector3 _lastHandPlane;
        private Vector3 _lastHandPos;
        private Quaternion _lastHandRot;

        private bool _debug = false;

        public bool IsOpen => CurrentState == E_State.Open;
        public bool IsClosed => CurrentState == E_State.Closed;
        public bool IsMid => CurrentState == E_State.Mid;

        public override void Start()
        {
            base.Start();
            _origPos = ObjectToMove.localPosition;
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            base.BeginInteraction(hand);

            SM.PlayGenericSound(BeginInteractionSounds, ObjectToMove.position);
            switch (MovementAxis)
            {
                case OpenScripts2_BasePlugin.Axis.X:
                    _lastHandPlane = Vector3.ProjectOnPlane(hand.transform.up, Root.right);
                    break;
                case OpenScripts2_BasePlugin.Axis.Y:
                    _lastHandPlane = Vector3.ProjectOnPlane(hand.transform.up, Root.forward);
                    break;
                case OpenScripts2_BasePlugin.Axis.Z:
                    _lastHandPlane = Vector3.ProjectOnPlane(hand.transform.forward, -Root.up);
                    break;
            }
            if (MovementMode == Mode.Sliding) _lastHandPos = m_handPos;
            else if (MovementMode == Mode.Rotating) _lastHandRot = m_handRot;
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            switch (MovementMode)
            {
                case Mode.Sliding:
                    TranslationMode();
                    break;
                case Mode.Rotating:
                    //RotationMode(hand);
                    RotationMode();
                    break;
                case Mode.Folding:
                    FoldingMode();
                    break;
            }
            if (MovementMode == Mode.Sliding) _lastHandPos = m_handPos;
            else if (MovementMode == Mode.Rotating) _lastHandRot = m_handRot;

            CheckSound();
        }
        public override void EndInteraction(FVRViveHand hand)
        {
            base.EndInteraction(hand);

            SM.PlayGenericSound(EndInteractionSounds, ObjectToMove.position);
        }

        public void TranslationMode()
        {
            Vector3 transformedHandPos = Root.InverseTransformPoint(m_handPos);
            Vector3 lasttransformedHandPos = Root.InverseTransformPoint(_lastHandPos);

            Vector3 deltaPos = transformedHandPos - lasttransformedHandPos;

            float deltaValue = deltaPos.GetAxisValue(MovementAxis);
            float curValue = ObjectToMove.localPosition.GetAxisValue(MovementAxis);

            _currentPositionValue = Mathf.Clamp(curValue + deltaValue, LowerLimit, UpperLimit);

            if (SnapsToEndStops && Mathf.Abs(_currentPositionValue - LowerLimit) < LimitWiggleRoom)
            {
                _currentPositionValue = LowerLimit;
            }
            else if (SnapsToEndStops && Mathf.Abs(_currentPositionValue - UpperLimit) < LimitWiggleRoom)
            {
                _currentPositionValue = UpperLimit;
            }
            ObjectToMove.ModifyLocalPositionAxisValue(MovementAxis, _currentPositionValue);
        }

        public void RotationMode()
        {
            Quaternion curHandRot = m_handRot;
            Quaternion handRotDelta = curHandRot.Subtract(_lastHandRot);

            Vector3 referenceDirection = Vector3.zero;
            Vector3 normalDirection = Vector3.zero;
            switch (MovementAxis)
            {
                case OpenScripts2_BasePlugin.Axis.X:
                    normalDirection = Root.right;
                    referenceDirection = Root.forward;
                    break;
                case OpenScripts2_BasePlugin.Axis.Y:
                    normalDirection = Root.up;
                    referenceDirection = Root.forward;
                    break;
                case OpenScripts2_BasePlugin.Axis.Z:
                    referenceDirection = Root.up;
                    normalDirection = Root.forward;
                    break;
            }

            handRotDelta.ToAngleAxis(out float rotAngle, out Vector3 rotAxis);
            if (rotAxis.magnitude == 0 || rotAngle == 0) return;

            float axisSpecificAngleMult = Vector3.Dot(normalDirection, rotAxis);

            if (_debug)
            {
                Popcron.Gizmos.Line(ObjectToMove.transform.position, ObjectToMove.transform.position + rotAxis * 0.1f * rotAngle, Color.magenta);
            }

            float deltaAngle = axisSpecificAngleMult * rotAngle;

            _currentPositionValue = Mathf.Clamp(_currentPositionValue + deltaAngle, LowerLimit, UpperLimit);

            if (Mathf.Abs(_currentPositionValue - UpperLimit) <= LimitWiggleRoom)
            {
                _currentPositionValue = UpperLimit;
            }
            else if (Mathf.Abs(_currentPositionValue - LowerLimit) <= LimitWiggleRoom)
            {
                _currentPositionValue = LowerLimit;
            }

            ObjectToMove.ModifyLocalRotationAxisValue(MovementAxis, _currentPositionValue);
        }

        private void FoldingMode()
        {
            Vector3 currentVectorToHand = m_handPos - Root.position;
            //Vector3 lastVectorToHand = _lastHandPos - Root.position;
            Vector3 referenceDirection = Vector3.zero;
            Vector3 normalDirection = Vector3.zero;

            switch (MovementAxis)
            {
                case OpenScripts2_BasePlugin.Axis.X:
                    referenceDirection = Root.forward;
                    normalDirection = Root.right;
                    break;
                case OpenScripts2_BasePlugin.Axis.Y:
                    referenceDirection = Root.forward;
                    normalDirection = Root.up;
                    break;
                case OpenScripts2_BasePlugin.Axis.Z:
                    referenceDirection = Root.up;
                    normalDirection = Root.forward;
                    break;
            }

            _currentPositionValue = Vector3Utils.SignedAngle(referenceDirection, currentVectorToHand, normalDirection);

            float deltaRot = _lastPositionValue - _currentPositionValue;
            if (Mathf.Abs(deltaRot) > 180f)
            {
                if (deltaRot > 0f)
                {
                    _currentPositionValue += 360f;
                }
                else
                {
                    _currentPositionValue -= 360f;
                }
            }
            //float currentAngle = Vector3Utils.SignedAngle(referenceDirection, currentVectorToHand, normalDirection);
            //float lastAngle = Vector3Utils.SignedAngle(referenceDirection, lastVectorToHand, normalDirection);
            //float deltaAngle = currentAngle - lastAngle;

            _currentPositionValue = Mathf.Clamp(_currentPositionValue, LowerLimit, UpperLimit);

            if (Mathf.Abs(_currentPositionValue - LowerLimit) < LimitWiggleRoom)
            {
                _currentPositionValue = LowerLimit;
            }
            if (Mathf.Abs(_currentPositionValue - UpperLimit) < LimitWiggleRoom)
            {
                _currentPositionValue = UpperLimit;
            }
            if (_currentPositionValue >= LowerLimit && _currentPositionValue <= UpperLimit)
            {
                ObjectToMove.ModifyLocalRotationAxisValue(MovementAxis, _currentPositionValue);
            }

            _lastPositionValue = _currentPositionValue;

            if (_debug)
            {
                Popcron.Gizmos.Line(Root.position, m_handPos, Color.magenta);
                Popcron.Gizmos.Line(Root.position, referenceDirection, Color.green);
                Popcron.Gizmos.Line(Root.position, currentVectorToHand, Color.red);
                Popcron.Gizmos.Line(Root.position, Vector3.Cross(referenceDirection, currentVectorToHand), Color.blue);
            }
        }

        private void CheckSound()
        {
            float lerp = Mathf.InverseLerp(LowerLimit, UpperLimit, _currentPositionValue);
            if (Mathf.Approximately(lerp, 1f))
            {
                CurrentState = E_State.Open;
            }
            else if (Mathf.Approximately(lerp, 0f))
            {
                CurrentState = E_State.Closed;
            }
            else
            {
                CurrentState = E_State.Mid;
            }
            if (CurrentState == E_State.Open && _lastState != E_State.Open)
            {
                SM.PlayGenericSound(OpenSounds, ObjectToMove.position);
            }
            if (CurrentState == E_State.Closed && _lastState != E_State.Closed)
            {
                SM.PlayGenericSound(CloseSounds, ObjectToMove.position);
            }
            _lastState = CurrentState;
        }
    }
}
