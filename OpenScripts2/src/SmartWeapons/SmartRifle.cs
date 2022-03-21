using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace OpenScripts2
{
    public class SmartRifle : OpenScripts2_BasePlugin
	{
        public ClosedBoltWeapon rifle;
		public MeshRenderer Reticle;
		public bool DisableReticleWithoutTarget = true;
		public float EngageRange = 15f;
		[Range(1f,179f)]
		public float EngageAngle = 45f;
		public float PrecisionAngle = 5f;

		public LayerMask LatchingMask;
		public LayerMask BlockingMask;

		public bool LocksUpWithoutTarget = false;
		public int SafetyIndex = 0;

		public bool DoesRandomRotationWithoutTarget = true;
		public float RandomAngleMagnitude = 5f;
		//constants
		private string _nameOfDistanceVariable = "_RedDotDist";

		private bool _isLocked;
		private int _lastSelectorPos;

#if !DEBUG
		public void Start()
        {
			Hook();
        }
		
		public void Hook()
        {
            On.FistVR.ClosedBoltWeapon.UpdateInputAndAnimate += ClosedBoltWeapon_UpdateInputAndAnimate;
        }
        private void ClosedBoltWeapon_UpdateInputAndAnimate(On.FistVR.ClosedBoltWeapon.orig_UpdateInputAndAnimate orig, ClosedBoltWeapon self, FVRViveHand hand)
        {
			if (self == rifle)
			{
				EarlyUpdate();
			}

			orig(self,hand);
		}

        public void EarlyUpdate()
        {
            if (rifle.m_hand != null)
            {
				Vector3 target = FindTarget();

				if (target != new Vector3(0, 0, 0))
                {
                    //Debug.Log(target);

					if (LocksUpWithoutTarget) LockRifle(false);
					//Debug.DrawRay(pistol.MuzzlePos.position, target, Color.green);
					//Popcron.Gizmos.Line(pistol.MuzzlePos.position, target, Color.green);

					rifle.CurrentMuzzle.LookAt(target);
					rifle.MuzzlePos.LookAt(target);
                    if (Reticle != null)
                    {
						Reticle.material.SetFloat(_nameOfDistanceVariable, (target - rifle.CurrentMuzzle.position).magnitude);
						if (DisableReticleWithoutTarget) Reticle.gameObject.SetActive(true);
					}
                }
				else
                {
					if(LocksUpWithoutTarget) LockRifle(true);
					if (DoesRandomRotationWithoutTarget)
					{
						Vector3 randRot = new Vector3();
						randRot.x = UnityEngine.Random.Range(-RandomAngleMagnitude, RandomAngleMagnitude);
						randRot.y = UnityEngine.Random.Range(-RandomAngleMagnitude, RandomAngleMagnitude);

						rifle.CurrentMuzzle.localEulerAngles = randRot;
						rifle.MuzzlePos.localEulerAngles = randRot;
					}
					else
					{
						rifle.CurrentMuzzle.localEulerAngles = new Vector3(0, 0, 0);
						rifle.MuzzlePos.localEulerAngles = new Vector3(0, 0, 0);
					}

					if (DisableReticleWithoutTarget && Reticle != null) Reticle.gameObject.SetActive(false);
				}
            }
        }

		private Vector3 FindTarget()
        {
			float radius = EngageRange * Mathf.Tan(0.5f * EngageAngle * Mathf.Deg2Rad);
			Collider[] array = Physics.OverlapCapsule(rifle.CurrentMuzzle.position, rifle.CurrentMuzzle.position + rifle.transform.forward * this.EngageRange, radius, this.LatchingMask);
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
							Vector3 from = list[j].transform.position - rifle.CurrentMuzzle.position;
							float num2 = Vector3.Angle(from, rifle.transform.forward);

							Sosig s = component.S;
							if (num2 <= PrecisionAngle) sosigLink2 = s.Links[0];
							else sosigLink2 = s.Links[1];


							if (num2 < num &&  !Physics.Linecast(rifle.CurrentMuzzle.position, sosigLink2.transform.position, this.BlockingMask, QueryTriggerInteraction.Ignore))
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
				return sosigLink.transform.position; ;
			}
            else
            {
				return new Vector3(0, 0, 0);
            }
		}
		public void LockRifle(bool lockRifle)
        {
			if (lockRifle && !_isLocked)
            {
				_lastSelectorPos = rifle.m_fireSelectorMode;
				rifle.m_fireSelectorMode = SafetyIndex;

				_isLocked = true;
            }
            else if (lockRifle && _isLocked)
			{
				rifle.m_fireSelectorMode = SafetyIndex;
			}
			else if (!lockRifle && _isLocked)
            {
				rifle.m_fireSelectorMode = _lastSelectorPos;

				_isLocked = false;
			}
        }
#endif
	}
}
