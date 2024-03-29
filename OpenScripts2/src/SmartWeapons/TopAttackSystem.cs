using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class TopAttackSystem : OpenScripts2_BasePlugin
	{
		public FVRFireArm FireArm;

		public LayerMask LM_OverlapCapsuleTargetMask = LayerMask.GetMask("AI Entity");
		public LayerMask LM_RaycastTargetMask = LayerMask.GetMask("Environment");
        public LayerMask LM_BlockMask = LayerMask.GetMask("Environment");
        public float MaxRange = 2000f;
		public float ObjectTargetingFOV = 1f;

		private Vector3? _targetPoint = null;
		private Rigidbody _targetRB;

		public TopAttackProjectile.EAttackMode AttackMode;

		public float MinRangeTopAttackMode = 150f;
		public float MinRangeFrontalAttackMode = 65f;

		[Header("Mode Text Config")]
		public Text ModeTextField;
		public string TopAttackModeText = "Top";
		public string FrontalAttackModeText = "Frontal";

		[Header("Rangefinder Text Config")]
		public Text RangeTextField;
		public string OutOfRangeText = "INF";

		[Header("Target Text Config")]
		public Text TargetTextField;
		public string NoTargetText = "No Target";
		public string PositionTargetText = "Target Position: {0:F0}X {1:F0}Y {2:F0}Z";
		public string RigidbodyTargetText = "Target Object: {0}";

		private string[] _modeTexts;
		private const string _removeFromName = "(Clone)";


		private float _overlapCapsuleRadius;

		private RaycastHit _raycastHit;
		private Collider[] _targetArray = new Collider[32];

		// Hook Stuff
		private static Dictionary<FVRFireArm, TopAttackSystem> _exisingTopAttackFirearms = new();

#if !(DEBUG || MEATKIT)

		static TopAttackSystem()
		{
            Hook();
        }

		public void Awake()
		{
			_modeTexts = new string[]{ TopAttackModeText, FrontalAttackModeText };
			_overlapCapsuleRadius = Mathf.Tan(ObjectTargetingFOV * Mathf.Deg2Rad) * MaxRange;

			_exisingTopAttackFirearms.Add(FireArm, this);
		}
		public void OnDestroy()
		{
            //Unhook();
            _exisingTopAttackFirearms.Remove(FireArm);
        }

		public void Update()
        {
			int numTargets = Physics.OverlapCapsuleNonAlloc(transform.position, transform.position + MaxRange * transform.forward, _overlapCapsuleRadius, _targetArray, LM_OverlapCapsuleTargetMask, QueryTriggerInteraction.Collide);

			float distance = MaxRange + 100f;

			Collider finalTarget = null;
			Vector3 direction;

			for (int i = 0; i < numTargets; i++)
			{
				direction = _targetArray[i].transform.position - transform.position;

				if (Vector3.Angle(direction, transform.forward) > ObjectTargetingFOV) continue;
				if (direction.magnitude < distance && !Physics.Linecast(transform.position, _targetArray[i].transform.position, LM_BlockMask))
				{
					distance = Vector3.Distance(_targetArray[i].transform.position, transform.position);

					finalTarget = _targetArray[i];
				}
			}

			if (finalTarget != null)
			{
				_targetRB = finalTarget.attachedRigidbody;
			}
			else
			{
				_targetRB = null;
			}

			bool raycastHit = false;

            if (Physics.Raycast(transform.position, transform.forward, out _raycastHit, MaxRange, LM_RaycastTargetMask,QueryTriggerInteraction.Collide))
            {
				raycastHit = true;
                if (AttackMode == TopAttackProjectile.EAttackMode.Top && _raycastHit.distance > MinRangeTopAttackMode)
                {
					_targetPoint = _raycastHit.point;
				}
				else if (AttackMode == TopAttackProjectile.EAttackMode.Direct && _raycastHit.distance > MinRangeFrontalAttackMode)
                {
					_targetPoint = _raycastHit.point;
				}
			}
            if (_targetRB != null)
            {
                string targetName = _targetRB.name.Replace(_removeFromName, "");
                TargetTextField.text = string.Format(RigidbodyTargetText, targetName);
                RangeTextField.text = string.Format("{0:F0}m", distance);
            }
            else if (raycastHit)
            {
                RangeTextField.text = string.Format("{0:F0}m", _raycastHit.distance);
                TargetTextField.text = string.Format(PositionTargetText, _raycastHit.point.x, _raycastHit.point.y, _raycastHit.point.z);
            }
            else
            {
				RangeTextField.text = OutOfRangeText;
				TargetTextField.text = NoTargetText;

				_targetPoint = null;
				_targetRB = null;
			}
        }
		//public void Unhook()
		//{
		//	On.FistVR.FVRFireArm.Fire -= FVRFireArm_Fire; 
		//	On.FistVR.FVRPhysicalObject.UpdateInteraction -= FVRPhysicalObject_UpdateInteraction;
		//}
		public static void Hook()
		{
			On.FistVR.FVRFireArm.Fire += FVRFireArm_Fire;
            On.FistVR.FVRPhysicalObject.UpdateInteraction += FVRPhysicalObject_UpdateInteraction;
		}

        private static void FVRPhysicalObject_UpdateInteraction(On.FistVR.FVRPhysicalObject.orig_UpdateInteraction orig, FVRPhysicalObject self, FVRViveHand hand)
        {
            orig(self, hand);
			if (self is FVRFireArm && _exisingTopAttackFirearms.TryGetValue(self as FVRFireArm, out TopAttackSystem topAttackSystem))
            {
                if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes,Vector2.left)<45f)
                {
                    topAttackSystem.ChangeMode();
                }
            }
        }

        private static void FVRFireArm_Fire(On.FistVR.FVRFireArm.orig_Fire orig, FVRFireArm self, FVRFireArmChamber chamber, Transform muzzle, bool doBuzz, float velMult, float rangeOverride)
        {
            if (_exisingTopAttackFirearms.TryGetValue(self as FVRFireArm, out TopAttackSystem topAttackSystem))
            {
                if (doBuzz && self.m_hand != null)
                {
                    self.m_hand.Buzz(self.m_hand.Buzzer.Buzz_GunShot);
                    if (self.AltGrip != null && self.AltGrip.m_hand != null)
                    {
                        self.AltGrip.m_hand.Buzz(self.m_hand.Buzzer.Buzz_GunShot);
                    }
                }
                GM.CurrentSceneSettings.OnShotFired(self);
                if (self.IsSuppressed())
                {
                    GM.CurrentPlayerBody.VisibleEvent(0.1f);
                }
                else
                {
                    GM.CurrentPlayerBody.VisibleEvent(2f);
                }
                float chamberVelMult = AM.GetChamberVelMult(chamber.RoundType, Vector3.Distance(chamber.transform.position, muzzle.position));
                float num = self.GetCombinedFixedDrop(self.AccuracyClass) * 0.0166667f;
                Vector2 vector = self.GetCombinedFixedDrift(self.AccuracyClass) * 0.0166667f;
                for (int i = 0; i < chamber.GetRound().NumProjectiles; i++)
                {
                    float d = chamber.GetRound().ProjectileSpread + self.m_internalMechanicalMOA + self.GetCombinedMuzzleDeviceAccuracy();
                    if (chamber.GetRound().BallisticProjectilePrefab != null)
                    {
                        Vector3 b = muzzle.forward * 0.005f;
                        GameObject gameObject = Instantiate<GameObject>(chamber.GetRound().BallisticProjectilePrefab, muzzle.position - b, muzzle.rotation);
                        Vector2 vector2 = (UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle) * 0.33333334f * d;
                        gameObject.transform.Rotate(new Vector3(vector2.x + vector.y + num, vector2.y + vector.x, 0f));
                        BallisticProjectile component = gameObject.GetComponent<BallisticProjectile>();
                        component.Fire(component.MuzzleVelocityBase * chamber.ChamberVelocityMultiplier * velMult * chamberVelMult, gameObject.transform.forward, self, true);

                        TopAttackProjectile topAttackProjectile = gameObject.GetComponent<TopAttackProjectile>();
                        if (topAttackProjectile != null && topAttackSystem._targetPoint != new Vector3(float.MaxValue, float.MaxValue, float.MaxValue))
                        {
                            if (topAttackSystem._targetRB == null) topAttackProjectile.TargetPoint = topAttackSystem._targetPoint;
                            else topAttackProjectile.TargetRB = topAttackSystem._targetRB;

                            topAttackProjectile.AttackMode = topAttackSystem.AttackMode;
                        }
                        if (rangeOverride > 0f)
                        {
                            component.ForceSetMaxDist(rangeOverride);
                        }
                    }
                }
            }
            else orig(self, chamber, muzzle, doBuzz, velMult, rangeOverride);
        }

        private void ChangeMode()
        {
			switch (AttackMode)
			{
				case TopAttackProjectile.EAttackMode.Top:
					AttackMode = TopAttackProjectile.EAttackMode.Direct;
					break;
				case TopAttackProjectile.EAttackMode.Direct:
					AttackMode = TopAttackProjectile.EAttackMode.Top;
					break;
				default:
					break;
			}
			if (ModeTextField != null)
			{
				ModeTextField.text = _modeTexts[(int)AttackMode];
			}
		}

#endif
	}
}
