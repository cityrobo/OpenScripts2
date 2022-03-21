using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    public class SmartPistol : OpenScripts2_BasePlugin
	{
        public Handgun handgun;
		public MeshRenderer Reticle;
		public bool DisableReticleWithoutTarget = true;
		public float EngageRange = 15f;
		[Range(1f,179f)]
		public float EngageAngle = 45f;
		public float PrecisionAngle = 5f;

		public LayerMask LatchingMask;
		public LayerMask BlockingMask;

		public bool LocksUpWithoutTarget = true;
		public bool DoesRandomRotationWithoutTarget = true;
		public float RandomAngleMagnitude = 5f;
		//constants
		private string _nameOfDistanceVariable = "_RedDotDist";

#if !DEBUG
		public void Start()
        {
			Hook();
        }
		
		public void Hook()
        {
            On.FistVR.Handgun.UpdateInputAndAnimate += Handgun_UpdateInputAndAnimate;
        }

        private void Handgun_UpdateInputAndAnimate(On.FistVR.Handgun.orig_UpdateInputAndAnimate orig, Handgun self, FVRViveHand hand)
        {
			if (self == handgun)
			{
				EarlyUpdate();
			}

			orig(self,hand);
		}

        public void EarlyUpdate()
        {
            if (handgun.m_hand != null)
            {
				Vector3 target = FindTarget();

				if (target != new Vector3(0, 0, 0))
                {
					if (LocksUpWithoutTarget) handgun.m_isSafetyEngaged = false;

					handgun.CurrentMuzzle.LookAt(target);
					handgun.MuzzlePos.LookAt(target);

                    if (Reticle != null)
                    {
						Reticle.material.SetFloat(_nameOfDistanceVariable, (target - handgun.CurrentMuzzle.position).magnitude);
						if (DisableReticleWithoutTarget) Reticle.gameObject.SetActive(true);
					}
                }
				else
                {
					if(LocksUpWithoutTarget) handgun.m_isSafetyEngaged = true;
					if (DoesRandomRotationWithoutTarget)
					{
						Vector3 randRot = new Vector3();
						randRot.x = UnityEngine.Random.Range(-RandomAngleMagnitude, RandomAngleMagnitude);
						randRot.y = UnityEngine.Random.Range(-RandomAngleMagnitude, RandomAngleMagnitude);

						handgun.CurrentMuzzle.localEulerAngles = randRot;
						handgun.MuzzlePos.localEulerAngles = randRot;
					}
					else
					{
						handgun.CurrentMuzzle.localEulerAngles = new Vector3(0, 0, 0);
						handgun.MuzzlePos.localEulerAngles = new Vector3(0, 0, 0);
					}

					if (DisableReticleWithoutTarget && Reticle != null) Reticle.gameObject.SetActive(false);
				}
            }
        }
		private Vector3 FindTarget()
        {
			float radius = EngageRange * Mathf.Tan(0.5f * EngageAngle * Mathf.Deg2Rad);
			Collider[] array = Physics.OverlapCapsule(handgun.CurrentMuzzle.position, handgun.CurrentMuzzle.position + handgun.transform.forward * this.EngageRange, radius, this.LatchingMask);
			List<Rigidbody> list = new List<Rigidbody>();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].attachedRigidbody != null && !list.Contains(array[i].attachedRigidbody))
				{
					list.Add(array[i].attachedRigidbody);
				}
			}
			bool flag = false;
			SosigLink sosigLink = null;
			SosigLink sosigLink2 = null;
			float num = EngageAngle;
			for (int j = 0; j < list.Count; j++)
			{
				SosigLink component = list[j].GetComponent<SosigLink>();
				if (!(component == null))
				{
					if (component.S.BodyState != Sosig.SosigBodyState.Dead)
					{
						if (true || component.S.E.IFFCode == 1)
						{
							Vector3 from = list[j].transform.position - handgun.CurrentMuzzle.position;
							float num2 = Vector3.Angle(from, handgun.transform.forward);

							Sosig s = component.S;
							if (num2 <= PrecisionAngle) sosigLink2 = s.Links[0];
							else sosigLink2 = s.Links[1];


							if (num2 < num &&  !Physics.Linecast(handgun.CurrentMuzzle.position, sosigLink2.transform.position, this.BlockingMask, QueryTriggerInteraction.Ignore))
							{
								sosigLink = sosigLink2;
								num = num2;
								flag = true;
							}
						}
					}
				}
			}
			if (flag)
			{
				return sosigLink.transform.position;
			}
            else
            {
				return new Vector3(0, 0, 0);
            }
		}
#endif
	}
}
