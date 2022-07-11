using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
	public class RigidbodyJiggleBones : OpenScripts2_BasePlugin
	{
		public FVRPhysicalObject MainObject;
		public Transform RootBone;
		public Rigidbody ReferenceRigidbody;
		[Tooltip("Script will ignore these bones and their children when creating joints.")]
		public List<Transform> BoneBlackList;

		public bool UsesSprings = true;
		public Axis JointAxis = Axis.Y;

		public float SpringStrength;
		public float SpringDampening;

		public float TwistLimitX;
		public float AngleLimitY;
		public float AngleLimitZ;

		public List<Collider> AddedColliders;
		public List<Rigidbody> AddedRBs;
		public List<Joint> AddedJoints;
		public List<Joint> RootJoints;
		public List<Vector3> RootJointsPos;
		public List<Quaternion> RootJointsRot;

		public bool UseConfigurableJoints = true;
		public bool HasEndBones = true;
		[Tooltip("You will want to use this setting for pretty much anything gravity enabled that is only attached on one end. Having all RBs act on gravity gave weird results in testing.")]
		public bool OnlyLastRigidbodyUsesGravity = true;
		public bool AddBasicColliders = true;
		public float ColliderRadius;



		public Axis ColliderAxis = Axis.Y;
		//public float collider_Height;

		private bool _mainRBWasNull = false;

		private bool IsDebug
        {
			get { return false; }
        }

		public void Start()
		{
			ReferenceRigidbody.gameObject.SetActive(false);
			
			FixParenting();
        }

		public void Update()
        {
			if (!_mainRBWasNull && MainObject.RootRigidbody == null)
			{
				SetJiggleboneRootRB(FindNewRigidbody(MainObject.transform.parent));
				_mainRBWasNull = true;
			}
			else if (_mainRBWasNull && MainObject.RootRigidbody != null) 
			{
				SetJiggleboneRootRB(MainObject.RootRigidbody);
				_mainRBWasNull = false;
			}
        }

		public void configureJoint(ConfigurableJoint joint, Rigidbody parent)
		{
			joint.enablePreprocessing = false;
			joint.projectionMode = JointProjectionMode.PositionAndRotation;

			joint.connectedBody = parent;
            switch (JointAxis)
            {
                case Axis.X:
					joint.axis = new Vector3(1, 0, 0);
					break;
                case Axis.Y:
					joint.axis = new Vector3(0, 1, 0);
					break;
                case Axis.Z:
					joint.axis = new Vector3(0, 0, 1);
					break;
                default:
                    break;
            }

			//joint.autoConfigureConnectedAnchor = false;

			//joint.connectedAnchor = new Vector3(0,0,0);

			//joint.anchor = joint.transform.InverseTransformPoint(parent.transform.position);

			joint.anchor = Vector3.zero;

			joint.xMotion = ConfigurableJointMotion.Locked;
			joint.yMotion = ConfigurableJointMotion.Locked;
			joint.zMotion = ConfigurableJointMotion.Locked;

            if (UsesSprings)
            {
				joint.angularXMotion = ConfigurableJointMotion.Limited;
				joint.angularYMotion = ConfigurableJointMotion.Limited;
				joint.angularZMotion = ConfigurableJointMotion.Limited;
			}
            else
            {
				joint.angularXMotion = ConfigurableJointMotion.Free;
				joint.angularYMotion = ConfigurableJointMotion.Free;
				joint.angularZMotion = ConfigurableJointMotion.Free;
			}

			JointDrive drive = new JointDrive();
			drive.positionSpring = SpringStrength;
			drive.positionDamper = SpringDampening;
			drive.maximumForce = joint.angularYZDrive.maximumForce;

			joint.angularXDrive = drive;
			joint.angularYZDrive = drive;

			SoftJointLimit high_twistlimit = new SoftJointLimit();
			high_twistlimit.limit = TwistLimitX;
			high_twistlimit.bounciness = joint.highAngularXLimit.bounciness;
			high_twistlimit.contactDistance = joint.highAngularXLimit.contactDistance;
			joint.highAngularXLimit = high_twistlimit;

			SoftJointLimit low_twistlimit = new SoftJointLimit();
			low_twistlimit.limit = -TwistLimitX;
			low_twistlimit.bounciness = joint.lowAngularXLimit.bounciness;
			low_twistlimit.contactDistance = joint.lowAngularXLimit.contactDistance;
			joint.lowAngularXLimit = low_twistlimit;

			SoftJointLimit anglelimitY = new SoftJointLimit();
			anglelimitY.limit = AngleLimitY;
			anglelimitY.bounciness = joint.angularYLimit.bounciness;
			anglelimitY.contactDistance = joint.angularYLimit.contactDistance;
			joint.angularYLimit = anglelimitY;


			SoftJointLimit anglelimitZ = new SoftJointLimit();
			anglelimitZ.limit = AngleLimitZ;
			anglelimitZ.bounciness = joint.angularZLimit.bounciness;
			anglelimitZ.contactDistance = joint.angularZLimit.contactDistance;
			joint.angularZLimit = anglelimitZ;
		}


		public void configureJoint(CharacterJoint joint, Rigidbody parent)
		{
			joint.enablePreprocessing = false;

			joint.connectedBody = parent;
			switch (JointAxis)
			{
				case Axis.X:
					joint.axis = new Vector3(1, 0, 0);
					break;
				case Axis.Y:
					joint.axis = new Vector3(0, 1, 0);
					break;
				case Axis.Z:
					joint.axis = new Vector3(0, 0, 1);
					break;
				default:
					break;
			}

			//joint.autoConfigureConnectedAnchor = false;

			//joint.connectedAnchor = new Vector3(0,0,0);

			//joint.anchor = joint.transform.InverseTransformPoint(parent.transform.position);

			joint.anchor = Vector3.zero;

			SoftJointLimitSpring spring = new SoftJointLimitSpring();
			spring.spring = SpringStrength;
			spring.damper = SpringDampening;


			joint.twistLimitSpring = spring;
			joint.swingLimitSpring = spring;

			SoftJointLimit high_twistlimit = new SoftJointLimit();
			high_twistlimit.limit = 0.1f;
			high_twistlimit.bounciness = joint.lowTwistLimit.bounciness;
			high_twistlimit.contactDistance = joint.lowTwistLimit.contactDistance;
			joint.lowTwistLimit = high_twistlimit;

			SoftJointLimit low_twistlimit = new SoftJointLimit();
			low_twistlimit.limit = -0.1f;
			low_twistlimit.bounciness = joint.highTwistLimit.bounciness;
			low_twistlimit.contactDistance = joint.highTwistLimit.contactDistance;
			joint.highTwistLimit = low_twistlimit;

			SoftJointLimit anglelimit = new SoftJointLimit();
			anglelimit.limit = 0.1f;
			anglelimit.bounciness = joint.swing1Limit.bounciness;
			anglelimit.contactDistance = joint.swing1Limit.contactDistance;
			joint.swing1Limit = anglelimit;
			joint.swing2Limit = anglelimit;
		}

		private bool CreateJointsOnChildren(Transform parent, Rigidbody connectedRB)
        {	
			Transform[] immediateChildren = parent.GetComponentsInDirectChildren<Transform>();

			if (immediateChildren.Length == 0)
			{
				return false;
			}

			foreach (var child in immediateChildren)
			{
				if ( BoneBlackList.Contains(child) || child.GetComponent<Collider>() != null) continue;
                if (child.childCount > 0 || !HasEndBones)
                {
					Rigidbody RB = child.gameObject.AddComponent<Rigidbody>();
					CopyRigidBody(RB);
					if (OnlyLastRigidbodyUsesGravity) RB.useGravity = false;
					AddedRBs.Add(RB);
					//RB = StaticExtras.GetCopyOf(RB, referenceRigidbody);
					//Rigidbody RB = StaticExtras.CopyComponent(referenceRigidbody, child.gameObject);


					if (UseConfigurableJoints) 
					{
						ConfigurableJoint joint = child.gameObject.AddComponent<ConfigurableJoint>();
						configureJoint(joint, connectedRB);
						AddedJoints.Add(joint);
					}
					else
                    {
						CharacterJoint joint = child.gameObject.AddComponent<CharacterJoint>();
						configureJoint(joint, connectedRB);
						AddedJoints.Add(joint);
					}
					Transform[] immediateGrandChildren = child.GetComponentsInDirectChildren<Transform>();

                    if (immediateGrandChildren.Length == 1)
                    {
						RB.centerOfMass = immediateGrandChildren[0].localPosition;

					}

					if (AddBasicColliders)
					{
						for (int i = 0; i < immediateGrandChildren.Length; i++)
						{
							GameObject childColliderGO = new GameObject(child.name + "_Collider_" + i);
							childColliderGO.transform.SetParent(child);
							childColliderGO.transform.position = Vector3.Lerp(child.position, immediateGrandChildren[i].position, 0.5f);
							childColliderGO.transform.localRotation = Quaternion.identity;

							CapsuleCollider collider = childColliderGO.gameObject.AddComponent<CapsuleCollider>();
							collider.direction = (int) ColliderAxis;
							collider.radius = ColliderRadius;
							collider.height = Vector3.Distance(child.position, immediateGrandChildren[i].position);
							AddedColliders.Add(collider);
						}
					}
                    if (OnlyLastRigidbodyUsesGravity)
                    {
						bool isLastBone = true;
                        foreach (var grandchild in immediateGrandChildren)
                        {
							if (grandchild.GetComponent<Collider>() == null) isLastBone = false;
                        }
						if (isLastBone && ReferenceRigidbody.useGravity) RB.useGravity = true;
					}
					CreateJointsOnChildren(child, RB);
					//child.SetParent(null);
				}
			}
			return true;
		}

		private Rigidbody FindNewRigidbody(Transform parent)
        {
			if (parent == null)
            {
                this.LogError("Couldn't find new Rigidbody to connect jiggle bones to!");
				return null;
            }

			Rigidbody RB = parent.GetComponent<Rigidbody>();

			if (RB == null) return FindNewRigidbody(parent.parent);
			else return RB;
		}

		private void SetJiggleboneRootRB(Rigidbody RB)
        {
			DebugMessage(RB.gameObject.name);
			DebugMessage(RootJoints.Count.ToString());
			ResetRootJointsTransform();

			foreach (var rootJoint in RootJoints)
            {
				rootJoint.connectedBody = RB;
            }
		}

		private void SetRootJoints()
        {
			RootJoints = new List<Joint>(RootBone.GetComponentsInDirectChildren<Joint>());

            foreach (var rootJoint in RootJoints)
            {
				RootJointsPos.Add(rootJoint.transform.localPosition);
				RootJointsRot.Add(rootJoint.transform.localRotation);

				/*
				Collider collider = rootJoint.GetComponent<Collider>();
				collider.enabled = false;
				*/
            }
		}

		private void ResetRootJointsTransform()
        {
            for (int i = 0; i < RootJoints.Count; i++)
            {
				RootJoints[i].transform.localPosition = RootJointsPos[i];
				RootJoints[i].transform.localRotation = RootJointsRot[i];
			}
        }

		private void FixParenting()
        {
            foreach (var RB in AddedRBs)
            {
				RB.transform.SetParent(RootBone);
            }
        }
		[ContextMenu("Create JiggleBones")]
		public void CreateJiggleBones()
        {
			ClearJiggleBones();

			if (!CreateJointsOnChildren(RootBone, MainObject.GetComponent<Rigidbody>())) Debug.LogError("No Children for JiggleBones found!");

			SetRootJoints();
		}

		[ContextMenu("Clear JiggleBones")]
		public void ClearJiggleBones()
        {
			if (AddedColliders == null) AddedColliders = new List<Collider>();
			foreach (var col in AddedColliders)
			{
				DestroyImmediate(col.gameObject);
			}
			AddedColliders.Clear();
			if (AddedJoints == null) AddedJoints = new List<Joint>();
			foreach (var joint in AddedJoints)
			{
				DestroyImmediate(joint);
			}
			AddedJoints.Clear();
			if (RootJoints == null) RootJoints = new List<Joint>();
			foreach (var joint in RootJoints)
			{
				DestroyImmediate(joint);
			}
			RootJoints.Clear();
			if (AddedRBs == null) AddedRBs = new List<Rigidbody>();
			foreach (var RB in AddedRBs)
			{
				DestroyImmediate(RB);
			}
			AddedRBs.Clear();

			RootJoints.Clear();
			RootJointsPos.Clear();
			RootJointsRot.Clear();

		}

		private void DebugMessage(string message)
        {
			if (!IsDebug) return;
            Debug.Log(message);
        }
		private void CopyRigidBody(Rigidbody RB)
        {
			RB.mass = ReferenceRigidbody.mass;
			RB.drag = ReferenceRigidbody.drag;
			RB.angularDrag = ReferenceRigidbody.angularDrag;
			RB.useGravity = ReferenceRigidbody.useGravity;
			RB.isKinematic = ReferenceRigidbody.isKinematic;
			RB.interpolation = ReferenceRigidbody.interpolation;
			RB.collisionDetectionMode = ReferenceRigidbody.collisionDetectionMode;
			RB.constraints = ReferenceRigidbody.constraints;
        }
	}
}
