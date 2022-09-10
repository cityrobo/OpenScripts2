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
		public float LimitWiggleRoom = 0.02f;

		public AudioEvent CloseSounds;
		public AudioEvent OpenSounds;

		private enum State
		{
			Open,
			Mid,
			Closed
		}

		private State _state;
		private State _lastState;

		private float _posFloat;
		private Vector3 _origPos;

		private Vector3 _lastHandPlane;
		private Vector3 _lastHandPos;

		private bool _debug = false;

		public override void Start()
        {
			base.Start();
			_origPos = ObjectToMove.localPosition;
        }
		public override void BeginInteraction(FVRViveHand hand)
		{
			base.BeginInteraction(hand);
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
		}

		public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            switch (MovementMode)
            {
                case Mode.Translation:
					TranslationMode(hand);
                    break;
                case Mode.Rotation:
					RotationMode(hand);
                    break;
                case Mode.Folding:
                    FoldingMode(hand);
                    break;
            }
			_lastHandPos = m_handPos;
        }

		public void TranslationMode(FVRViveHand hand)
        {
			Vector3 transformedHandPos = Root.InverseTransformPoint(m_handPos);
			Vector3 lasttransformedHandPos = Root.InverseTransformPoint(_lastHandPos);

			Vector3 deltaPos = transformedHandPos - lasttransformedHandPos;

			float deltaValue = deltaPos.GetAxisValue(MovementAxis);
			float curValue = ObjectToMove.localPosition.GetAxisValue(MovementAxis);

			float nextValue = Mathf.Clamp(curValue + deltaValue, LowerLimit, UpperLimit);
			/*
			Vector3 posVector;
			switch (MovementAxis)
            {
                case OpenScripts2_BasePlugin.Axis.X:
					posVector = GetClosestValidPoint(new Vector3(LowerLimit,0f,0f), new Vector3(UpperLimit, 0f, 0f), Root.InverseTransformPoint(m_handPos));
					_posFloat = posVector.x;
					ObjectToMove.localPosition = new Vector3(_posFloat, _origPos.y, _origPos.z);
					break;
                case OpenScripts2_BasePlugin.Axis.Y:
					posVector = GetClosestValidPoint(new Vector3(0f, LowerLimit, 0f), new Vector3(0f, UpperLimit, 0f), Root.InverseTransformPoint(m_handPos));
					_posFloat = posVector.y;
					ObjectToMove.localPosition = new Vector3(_origPos.x, _posFloat, _origPos.z);
					break;
                case OpenScripts2_BasePlugin.Axis.Z:
					posVector = GetClosestValidPoint(new Vector3(0f, 0f, LowerLimit), new Vector3(0f, 0f, UpperLimit), Root.InverseTransformPoint(m_handPos));
					_posFloat = posVector.z;
					ObjectToMove.localPosition = new Vector3(_origPos.x, _origPos.y, _posFloat);
					break;
            }
			*/
			ObjectToMove.ModifyLocalPositionAxisValue(MovementAxis, nextValue);

            float lerp = Mathf.InverseLerp(LowerLimit, UpperLimit, _posFloat);
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
					_posFloat = Mathf.Atan2(Vector3.Dot(-transform.right, Vector3.Cross(lhs, rhs)), Vector3.Dot(lhs, rhs)) * 57.29578f;

					ObjectToMove.localEulerAngles = new Vector3(_posFloat, 0f, 0f);
					break;
                case OpenScripts2_BasePlugin.Axis.Y:
					lhs = Vector3.ProjectOnPlane(m_hand.transform.forward, transform.up);
					rhs = Vector3.ProjectOnPlane(_lastHandPlane, -transform.up);
					_posFloat = Mathf.Atan2(Vector3.Dot(-transform.up, Vector3.Cross(lhs, rhs)), Vector3.Dot(lhs, rhs)) * 57.29578f;

					ObjectToMove.localEulerAngles = new Vector3(0f, _posFloat, 0f);
					break;
                case OpenScripts2_BasePlugin.Axis.Z:
					lhs = Vector3.ProjectOnPlane(m_hand.transform.up, -transform.forward);
					rhs = Vector3.ProjectOnPlane(_lastHandPlane, -transform.forward);
					_posFloat = Mathf.Atan2(Vector3.Dot(-transform.forward, Vector3.Cross(lhs, rhs)), Vector3.Dot(lhs, rhs)) * 57.29578f;

					ObjectToMove.localEulerAngles = new Vector3(0f, 0f, _posFloat);
					break;
            }
			_lastHandPlane = lhs;


			float lerp = Mathf.InverseLerp(LowerLimit, UpperLimit, _posFloat);
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
					_posFloat = Mathf.Atan2(Vector3.Dot(Root.right, Vector3.Cross(lhs, vector)), Vector3.Dot(lhs, vector)) * 57.29578f;
					break;
				case OpenScripts2_BasePlugin.Axis.Y:
					lhs = -Root.transform.forward;
					vector = Vector3.ProjectOnPlane(vector, Root.up).normalized;
					_posFloat = Mathf.Atan2(Vector3.Dot(Root.up, Vector3.Cross(lhs, vector)), Vector3.Dot(lhs, vector)) * 57.29578f;
					break;
				case OpenScripts2_BasePlugin.Axis.Z:
					lhs = Root.transform.up;
					vector = Vector3.ProjectOnPlane(vector, Root.forward).normalized;
					_posFloat = Mathf.Atan2(Vector3.Dot(Root.forward, Vector3.Cross(lhs, vector)), Vector3.Dot(lhs, vector)) * 57.29578f;
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

			if (Mathf.Abs(_posFloat - LowerLimit) < 5f)
			{
				_posFloat = LowerLimit;
			}
			if (Mathf.Abs(_posFloat - UpperLimit) < 5f)
			{
				_posFloat = UpperLimit;
			}
			if (_posFloat >= LowerLimit && _posFloat <= UpperLimit)
			{
				switch (MovementAxis)
				{
					case OpenScripts2_BasePlugin.Axis.X:
						ObjectToMove.localEulerAngles = new Vector3(_posFloat, 0f, 0f);
						break;
					case OpenScripts2_BasePlugin.Axis.Y:
						ObjectToMove.localEulerAngles = new Vector3(0f, _posFloat, 0f);
						break;
					case OpenScripts2_BasePlugin.Axis.Z:
						ObjectToMove.localEulerAngles = new Vector3(0f, 0f, _posFloat);
						break;
					default:
						break;
				}

				float lerp = Mathf.InverseLerp(LowerLimit, UpperLimit, _posFloat);
				CheckSound(lerp);
			}
		}

		private void CheckSound(float lerp)
        {
			if (lerp < LimitWiggleRoom)
			{
				_state = State.Open;

			}
			else if (lerp > 1f - LimitWiggleRoom)
			{
				_state = State.Closed;
			}
			else
			{
				_state = State.Mid;
			}
			if (_state == State.Open && _lastState != State.Open)
			{
				SM.PlayGenericSound(OpenSounds, ObjectToMove.position);
			}
			if (_state == State.Closed && _lastState != State.Closed)
			{
				SM.PlayGenericSound(CloseSounds, ObjectToMove.position);
			}
			_lastState = _state;
		}
	}
}
