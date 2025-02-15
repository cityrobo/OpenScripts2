using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace OpenScripts2
{
    public class MovableObjectPart : FVRInteractiveObject
    {
        public enum Mode
        {
            Translation,
            Rotation,
            Folding
        }
        public Mode MovementMode;

        public OpenScripts2_BasePlugin.Axis MovementAxis;

        public Transform Root;
        public Transform ObjectToMove;

        public float LowerLimit = 0f;
        public float UpperLimit = 0f;
        public bool SnapsToEndStops = true;
        public float LimitWiggleRoom = 0.02f;

        public AudioEvent CloseSounds;
        public AudioEvent OpenSounds;
        public AudioEvent BeginInteractionSounds;
        public AudioEvent EndInteractionSounds;

        public bool IsOpen => State == E_State.Open;
        public bool IsClosed => State == E_State.Closed;
        public bool IsMid => State == E_State.Mid;

        public enum E_State
        {
            Open,
            Mid,
            Closed
        }

        public E_State State;
        private E_State _lastState;

        private float _currentPositionValue;

        private Vector3 _origPos;

        private Vector3 _lastHandPlane;
        private Vector3 _lastHandPos;
        private Quaternion _lastHandRot;

        private bool _debug = false;

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
            if (MovementMode == Mode.Translation) _lastHandPos = m_handPos;
            else if (MovementMode == Mode.Rotation) _lastHandRot = m_handRot;
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            switch (MovementMode)
            {
                case Mode.Translation:
                    TranslationMode();
                    break;
                case Mode.Rotation:
                    //RotationMode(hand);
                    RotationModeQuaternion(hand);
                    break;
                case Mode.Folding:
                    FoldingMode(hand);
                    break;
            }
            if (MovementMode == Mode.Translation) _lastHandPos = m_handPos;
            else if (MovementMode == Mode.Rotation) _lastHandRot = m_handRot;
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

            float lerp = Mathf.InverseLerp(LowerLimit, UpperLimit, _currentPositionValue);
            CheckSound(lerp);
        }

        public void RotationMode(FVRViveHand hand)
        {
            Vector3 lhs = Vector3.zero; 
            Vector3 rhs;
            
            switch (MovementAxis)
            {
                case OpenScripts2_BasePlugin.Axis.X:
                    lhs = Vector3.ProjectOnPlane(m_hand.transform.up, -transform.right);
                    rhs = Vector3.ProjectOnPlane(_lastHandPlane, -transform.right);
                    _currentPositionValue = Mathf.Atan2(Vector3.Dot(-transform.right, Vector3.Cross(lhs, rhs)), Vector3.Dot(lhs, rhs)) * 57.29578f;

                    ObjectToMove.localEulerAngles = new Vector3(_currentPositionValue, 0f, 0f);
                    break;
                case OpenScripts2_BasePlugin.Axis.Y:
                    lhs = Vector3.ProjectOnPlane(m_hand.transform.forward, transform.up);
                    rhs = Vector3.ProjectOnPlane(_lastHandPlane, -transform.up);
                    _currentPositionValue = Mathf.Atan2(Vector3.Dot(-transform.up, Vector3.Cross(lhs, rhs)), Vector3.Dot(lhs, rhs)) * 57.29578f;

                    ObjectToMove.localEulerAngles = new Vector3(0f, _currentPositionValue, 0f);
                    break;
                case OpenScripts2_BasePlugin.Axis.Z:
                    lhs = Vector3.ProjectOnPlane(m_hand.transform.up, -transform.forward);
                    rhs = Vector3.ProjectOnPlane(_lastHandPlane, -transform.forward);
                    _currentPositionValue = Mathf.Atan2(Vector3.Dot(-transform.forward, Vector3.Cross(lhs, rhs)), Vector3.Dot(lhs, rhs)) * 57.29578f;

                    ObjectToMove.localEulerAngles = new Vector3(0f, 0f, _currentPositionValue);
                    break;
            }
            _lastHandPlane = lhs;

            float lerp = Mathf.InverseLerp(LowerLimit, UpperLimit, _currentPositionValue);
            CheckSound(lerp);
        }

        public void RotationModeQuaternion(FVRViveHand hand)
        {
            Quaternion curHandRot = m_handRot;
            Quaternion handRotDelta = curHandRot * Quaternion.Inverse(_lastHandRot);

            handRotDelta.ToAngleAxis(out float rotAngle, out Vector3 rotAxis);
            if (rotAxis.magnitude == 0 || rotAngle == 0) return;

            if (rotAngle >= 180f) rotAngle -= 180f;
            else if (rotAngle <= -180f) rotAngle += 180f;
            rotAxis = Root.InverseTransformDirection(rotAxis);

            float rotAxisProjected = rotAxis.GetAxisValue(MovementAxis);

            if (_debug)
            {
                Popcron.Gizmos.Line(ObjectToMove.transform.position, ObjectToMove.transform.position + rotAxis * 0.1f * rotAngle, Color.magenta);
            }
            float deltaAngle = rotAxisProjected * rotAngle;

            if (Mathf.Abs(deltaAngle) > 90f)
            {
                Debug.Log("DeltaAngle: " + deltaAngle);
            }
            /*
            if (_currentPositionValue + deltaAngle > UpperLimit) deltaAngle = UpperLimit - _currentPositionValue;
            else if (_currentPositionValue + deltaAngle < LowerLimit) deltaAngle = LowerLimit + _currentPositionValue;

            
            switch (MovementAxis)
            {
                case Axis.X:
                    ObjectToMove.Rotate(new Vector3(deltaAngle, 0, 0));
                    break;
                case Axis.Y:
                    ObjectToMove.Rotate(new Vector3(0, deltaAngle, 0));
                    break;
                case Axis.Z:
                    ObjectToMove.Rotate(new Vector3(0, 0, deltaAngle));
                    break;
                default:
                    break;
            }
            */
            _currentPositionValue = Mathf.Clamp(_currentPositionValue + deltaAngle, LowerLimit, UpperLimit);

            //if (deltaAngle > 0)
            //{
            //	Quaternion upperLimit = Quaternion.identity;
            //	switch (MovementAxis)
            //	{
            //		case OpenScripts2_BasePlugin.Axis.X:
            //			upperLimit = Quaternion.Euler(UpperLimit, 0f, 0f);
            //			break;
            //		case OpenScripts2_BasePlugin.Axis.Y:
            //			upperLimit = Quaternion.Euler(0f, UpperLimit, 0f);
            //			break;
            //		case OpenScripts2_BasePlugin.Axis.Z:
            //			upperLimit = Quaternion.Euler(0f, 0f, UpperLimit);
            //			break;
            //	}
   //             ObjectToMove.localRotation = Quaternion.RotateTowards(ObjectToMove.localRotation, upperLimit, deltaAngle);
   //         }
            //else
            //{
            //	Quaternion lowerLimit = Quaternion.identity;
            //	switch (MovementAxis)
            //	{
            //		case OpenScripts2_BasePlugin.Axis.X:
            //			lowerLimit = Quaternion.Euler(LowerLimit, 0f, 0f);
            //			break;
            //		case OpenScripts2_BasePlugin.Axis.Y:
            //			lowerLimit = Quaternion.Euler(0f, LowerLimit, 0f);
            //			break;
            //		case OpenScripts2_BasePlugin.Axis.Z:
            //			lowerLimit = Quaternion.Euler(0f, 0f, LowerLimit);
            //			break;
            //	}
   //             ObjectToMove.localRotation = Quaternion.RotateTowards(ObjectToMove.localRotation, lowerLimit, deltaAngle);
   //         }

            //if (_debug)
            //{
            //	Popcron.Gizmos.Line(hand.transform.position, hand.transform.position + hand.transform.forward * 0.1f, Color.blue);
            //	Popcron.Gizmos.Line(hand.transform.position, hand.transform.position + hand.transform.up * 0.1f, Color.green);
            //	Popcron.Gizmos.Line(hand.transform.position, hand.transform.position + hand.transform.right * 0.1f, Color.red);
            //}


            ObjectToMove.ModifyLocalRotationAxisValue(MovementAxis,_currentPositionValue);

            if (Mathf.Abs(_currentPositionValue - UpperLimit) <= LimitWiggleRoom)
            {
                _currentPositionValue = UpperLimit;
                ObjectToMove.ModifyLocalRotationAxisValue(MovementAxis, UpperLimit);
            }
            else if (Mathf.Abs(_currentPositionValue - LowerLimit) <= LimitWiggleRoom)
            {
                _currentPositionValue = LowerLimit;
                ObjectToMove.ModifyLocalRotationAxisValue(MovementAxis, LowerLimit);
            }
            float lerp = Mathf.InverseLerp(LowerLimit, UpperLimit, _currentPositionValue);
            CheckSound(lerp);
        }

        private void FoldingMode(FVRViveHand hand)
        {
            Vector3 vector = m_handPos - Root.position;
            Vector3 lhs = new Vector3();
            
            switch (MovementAxis)
            {
                case OpenScripts2_BasePlugin.Axis.X:
                    lhs = -Root.transform.forward;
                    vector = Vector3.ProjectOnPlane(vector, Root.right).normalized;
                    _currentPositionValue = Mathf.Atan2(Vector3.Dot(Root.right, Vector3.Cross(lhs, vector)), Vector3.Dot(lhs, vector)) * 57.29578f;
                    break;
                case OpenScripts2_BasePlugin.Axis.Y:
                    lhs = -Root.transform.forward;
                    vector = Vector3.ProjectOnPlane(vector, Root.up).normalized;
                    _currentPositionValue = Mathf.Atan2(Vector3.Dot(Root.up, Vector3.Cross(lhs, vector)), Vector3.Dot(lhs, vector)) * 57.29578f;
                    break;
                case OpenScripts2_BasePlugin.Axis.Z:
                    lhs = Root.transform.up;
                    vector = Vector3.ProjectOnPlane(vector, Root.forward).normalized;
                    _currentPositionValue = Mathf.Atan2(Vector3.Dot(Root.forward, Vector3.Cross(lhs, vector)), Vector3.Dot(lhs, vector)) * 57.29578f;
                    break;
                default:
                    break;
            }

            if (_debug)
            {
                Popcron.Gizmos.Line(Root.position, m_handPos, Color.magenta);
                Popcron.Gizmos.Line(Root.position, lhs, Color.green);
                Popcron.Gizmos.Line(Root.position, vector, Color.red);
                Popcron.Gizmos.Line(Root.position, Vector3.Cross(lhs, vector), Color.blue);
            }

            if (Mathf.Abs(_currentPositionValue - LowerLimit) < 5f)
            {
                _currentPositionValue = LowerLimit;
            }
            if (Mathf.Abs(_currentPositionValue - UpperLimit) < 5f)
            {
                _currentPositionValue = UpperLimit;
            }
            if (_currentPositionValue >= LowerLimit && _currentPositionValue <= UpperLimit)
            {
                ObjectToMove.localRotation = OpenScripts2_BasePlugin.GetTargetQuaternionFromAxis(_currentPositionValue, MovementAxis);

                float lerp = Mathf.InverseLerp(LowerLimit, UpperLimit, _currentPositionValue);
                CheckSound(lerp);
            }
        }

        private void CheckSound(float lerp)
        {
            if (lerp < LimitWiggleRoom)
            {
                State = E_State.Open;
            }
            else if (lerp > 1f - LimitWiggleRoom)
            {
                State = E_State.Closed;
            }
            else
            {
                State = E_State.Mid;
            }
            if (State == E_State.Open && _lastState != E_State.Open)
            {
                SM.PlayGenericSound(OpenSounds, ObjectToMove.position);
            }
            if (State == E_State.Closed && _lastState != E_State.Closed)
            {
                SM.PlayGenericSound(CloseSounds, ObjectToMove.position);
            }
            _lastState = State;
        }
    }
}
