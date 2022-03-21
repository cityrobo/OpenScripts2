using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace OpenScripts2
{
    public class MovableWeaponPart : FVRInteractiveObject
    {
        public enum Mode
        {
            Translation,
            Rotation,
            Tilt
        }
		public Mode MovementMode;

		public OpenScripts2_BasePlugin.Axis axis;

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

		private bool _debug = false;

#if !(UNITY_EDITOR || UNITY_5)
		public override void Start()
        {
			base.Start();
			_origPos = ObjectToMove.localPosition;
        }
		public override void BeginInteraction(FVRViveHand hand)
		{
			base.BeginInteraction(hand);
			switch (axis)
			{
				case OpenScripts2_BasePlugin.Axis.X:
					this._lastHandPlane = Vector3.ProjectOnPlane(this.m_hand.transform.up, Root.right);
					break;
				case OpenScripts2_BasePlugin.Axis.Y:
					this._lastHandPlane = Vector3.ProjectOnPlane(this.m_hand.transform.up, Root.forward);
					break;
				case OpenScripts2_BasePlugin.Axis.Z:
					this._lastHandPlane = Vector3.ProjectOnPlane(this.m_hand.transform.forward, -Root.up);
					break;
				default:
					break;
			}
			
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
                case Mode.Tilt:
                    TiltMode(hand);
                    break;
                default:
                    break;
            }
        }

		public void TranslationMode(FVRViveHand hand)
        {
			Vector3 posVector;
			switch (axis)
            {
                case OpenScripts2_BasePlugin.Axis.X:
					posVector = GetClosestValidPoint(new Vector3(LowerLimit,0f,0f), new Vector3(UpperLimit, 0f, 0f), Root.InverseTransformPoint(base.m_handPos));
					_posFloat = posVector.x;
					ObjectToMove.localPosition = new Vector3(_posFloat, _origPos.y, _origPos.z);
					break;
                case OpenScripts2_BasePlugin.Axis.Y:
					posVector = GetClosestValidPoint(new Vector3(0f, LowerLimit, 0f), new Vector3(0f, UpperLimit, 0f), Root.InverseTransformPoint(base.m_handPos));
					_posFloat = posVector.y;
					ObjectToMove.localPosition = new Vector3(_origPos.x, _posFloat, _origPos.z);
					break;
                case OpenScripts2_BasePlugin.Axis.Z:
					posVector = GetClosestValidPoint(new Vector3(0f, 0f, LowerLimit), new Vector3(0f, 0f, UpperLimit), Root.InverseTransformPoint(base.m_handPos));
					_posFloat = posVector.z;
					ObjectToMove.localPosition = new Vector3(_origPos.x, _origPos.y, _posFloat);
					break;
                default:
                    break;
            }

			float lerp = Mathf.InverseLerp(this.LowerLimit, this.UpperLimit, this._posFloat);
			CheckSound(lerp);

		}

		public void RotationMode(FVRViveHand hand)
		{
			Vector3 lhs; 
			Vector3 rhs;
			
			switch (axis)
            {
                case OpenScripts2_BasePlugin.Axis.X:
					lhs = Vector3.ProjectOnPlane(this.m_hand.transform.up, -base.transform.right);
					rhs = Vector3.ProjectOnPlane(this._lastHandPlane, -base.transform.right);
					_posFloat = Mathf.Atan2(Vector3.Dot(-base.transform.right, Vector3.Cross(lhs, rhs)), Vector3.Dot(lhs, rhs)) * 57.29578f;

					this.ObjectToMove.localEulerAngles = new Vector3(this._posFloat, 0f, 0f);
					break;
                case OpenScripts2_BasePlugin.Axis.Y:
					lhs = Vector3.ProjectOnPlane(this.m_hand.transform.forward, base.transform.up);
					rhs = Vector3.ProjectOnPlane(this._lastHandPlane, -base.transform.up);
					_posFloat = Mathf.Atan2(Vector3.Dot(-base.transform.up, Vector3.Cross(lhs, rhs)), Vector3.Dot(lhs, rhs)) * 57.29578f;

					this.ObjectToMove.localEulerAngles = new Vector3(0f, this._posFloat, 0f);
					break;
                case OpenScripts2_BasePlugin.Axis.Z:
					lhs = Vector3.ProjectOnPlane(this.m_hand.transform.up, -base.transform.forward);
					rhs = Vector3.ProjectOnPlane(this._lastHandPlane, -base.transform.forward);
					_posFloat = Mathf.Atan2(Vector3.Dot(-base.transform.forward, Vector3.Cross(lhs, rhs)), Vector3.Dot(lhs, rhs)) * 57.29578f;

					this.ObjectToMove.localEulerAngles = new Vector3(0f, 0f, this._posFloat);
					break;
                default:
					lhs = Vector3.ProjectOnPlane(this.m_hand.transform.up, -base.transform.right);
					break;
            }
			this._lastHandPlane = lhs;


			float lerp = Mathf.InverseLerp(this.LowerLimit, this.UpperLimit, this._posFloat);
			CheckSound(lerp);

		}

		private void TiltMode(FVRViveHand hand)
		{
			Vector3 vector = base.m_handPos - this.Root.position;
			Vector3 lhs = new Vector3();
			
			switch (axis)
			{
				case OpenScripts2_BasePlugin.Axis.X:
					lhs = -this.Root.transform.forward;
					vector = Vector3.ProjectOnPlane(vector, this.Root.right).normalized;
					_posFloat = Mathf.Atan2(Vector3.Dot(this.Root.right, Vector3.Cross(lhs, vector)), Vector3.Dot(lhs, vector)) * 57.29578f;
					break;
				case OpenScripts2_BasePlugin.Axis.Y:
					lhs = -this.Root.transform.forward;
					vector = Vector3.ProjectOnPlane(vector, this.Root.up).normalized;
					_posFloat = Mathf.Atan2(Vector3.Dot(this.Root.up, Vector3.Cross(lhs, vector)), Vector3.Dot(lhs, vector)) * 57.29578f;
					break;
				case OpenScripts2_BasePlugin.Axis.Z:
					lhs = this.Root.transform.up;
					vector = Vector3.ProjectOnPlane(vector, this.Root.forward).normalized;
					_posFloat = Mathf.Atan2(Vector3.Dot(this.Root.forward, Vector3.Cross(lhs, vector)), Vector3.Dot(lhs, vector)) * 57.29578f;
					break;
				default:
					break;
			}

            if (_debug)
            {
				Popcron.Gizmos.Line(this.Root.position, base.m_handPos, Color.magenta);
				Popcron.Gizmos.Line(this.Root.position, lhs, Color.green);
				Popcron.Gizmos.Line(this.Root.position, vector, Color.red);
				Popcron.Gizmos.Line(this.Root.position, Vector3.Cross(lhs, vector), Color.blue);

				
			}

			if (Mathf.Abs(this._posFloat - this.LowerLimit) < 5f)
			{
				this._posFloat = this.LowerLimit;
			}
			if (Mathf.Abs(this._posFloat - this.UpperLimit) < 5f)
			{
				this._posFloat = this.UpperLimit;
			}
			if (this._posFloat >= this.LowerLimit && this._posFloat <= this.UpperLimit)
			{
				switch (axis)
				{
					case OpenScripts2_BasePlugin.Axis.X:
						this.ObjectToMove.localEulerAngles = new Vector3(this._posFloat, 0f, 0f);
						break;
					case OpenScripts2_BasePlugin.Axis.Y:
						this.ObjectToMove.localEulerAngles = new Vector3(0f, this._posFloat, 0f);
						break;
					case OpenScripts2_BasePlugin.Axis.Z:
						this.ObjectToMove.localEulerAngles = new Vector3(0f, 0f, this._posFloat);
						break;
					default:
						break;
				}

				float lerp = Mathf.InverseLerp(this.LowerLimit, this.UpperLimit, this._posFloat);
				CheckSound(lerp);
			}
		}

		private void CheckSound(float lerp)
        {
			if (lerp < LimitWiggleRoom)
			{
				this._state = State.Open;

			}
			else if (lerp > 1f - LimitWiggleRoom)
			{
				this._state = State.Closed;
			}
			else
			{
				this._state = State.Mid;
			}
			if (this._state == State.Open && this._lastState != State.Open)
			{
				SM.PlayGenericSound(OpenSounds, ObjectToMove.position);
			}
			if (this._state == State.Closed && this._lastState != State.Closed)
			{
				SM.PlayGenericSound(CloseSounds, ObjectToMove.position);
			}
			this._lastState = this._state;
		}
#endif
	}
}
