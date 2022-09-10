using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
	public class HingeForegrip : FVRAlternateGrip
	{
		public Transform ObjectBase;
		public HingeJoint Hinge;

		private Vector3 _localPosStart;
		private Rigidbody _hingeRigidBody;
		private FVRPhysicalObject _physicalObject;

		public override void Awake()
		{
			base.Awake();
			_localPosStart = Hinge.transform.localPosition;
			_hingeRigidBody = Hinge.GetComponent<Rigidbody>();
			_physicalObject = Hinge.connectedBody.GetComponent<FVRPhysicalObject>();
		}

		public override void FVRUpdate()
		{
			base.FVRUpdate();
			if (Vector3.Distance(Hinge.transform.localPosition, _localPosStart) > 0.01f)
			{
				Hinge.transform.localPosition = _localPosStart;
			}
		}

		public override void FVRFixedUpdate()
		{
			base.FVRFixedUpdate();
			if (_physicalObject.IsHeld && _physicalObject.IsAltHeld)
			{
				_hingeRigidBody.mass = 0.001f;
			}
			else
			{
				_hingeRigidBody.mass = 0.1f;
			}
		}

		public override bool IsInteractable()
		{
			return true;
		}

		public override void UpdateInteraction(FVRViveHand hand)
		{
			base.UpdateInteraction(hand);
			Vector3 vector = hand.Input.Pos - Hinge.transform.position;
			Vector3 from = Vector3.ProjectOnPlane(vector, ObjectBase.right);
			if (Vector3.Angle(from, -ObjectBase.up) > 90f)
			{
				from = ObjectBase.forward;
			}
			if (Vector3.Angle(from, ObjectBase.forward) > 90f)
			{
				from = -ObjectBase.up;
			}
			float value = Vector3.Angle(from, ObjectBase.forward);
			JointSpring spring = Hinge.spring;
			spring.spring = 10f;
			spring.damper = 0f;
			spring.targetPosition = Mathf.Clamp(value, 0f, Hinge.limits.max);
			Hinge.spring = spring;
			Hinge.transform.localPosition = _localPosStart;
		}

		public override void EndInteraction(FVRViveHand hand)
		{
			JointSpring spring = Hinge.spring;
			spring.spring = 0.5f;
			spring.damper = 0.05f;
			spring.targetPosition = 45f;
			Hinge.spring = spring;
			base.EndInteraction(hand);
		}
	}
}
