using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace OpenScripts2
{
    public class SmartMultiBarrelWeapon : OpenScripts2_BasePlugin
    {
        [Header("SmartWeapon Config")]
        public FVRFireArm FireArm;

        [Serializable]
        public class BarrelMuzzleConfiguration
        {
            public Transform[] AdditionalMuzzles;
        }

        public BarrelMuzzleConfiguration[] BarrelMuzzleConfigurations;
		public GameObject ReplacementRoundPrefab;

		public float EngageRange = 15f;
		[Range(1f,179f)]
        [Tooltip("FOV of the Targeting Cone.")]
        public float EngageAngle = 45f;
        [Tooltip("FOV of the Precision Targeting Cone. The Precision Cone only targets Sosig Heads.")]
        public float PrecisionAngle = 5f;

		public LayerMask LatchingMask;
		public LayerMask BlockingMask;

		public bool DoesRandomRotationOfBarrelForCinematicBulletTrails = true;
		public float RandomAngleMagnitude = 5f;

		[Tooltip("Use this if you want the last target to stay locked on for a certain period. Good for shooting around corners!")]
		public float LastTargetTimeout = 1f;

		[Header("Optional")]
        public MeshRenderer ReticleMesh;
        public bool DisableReticleWithoutTarget = true;
        [Tooltip("Object that will be turned on when a target has been locked on.")]
		public GameObject TargetLockedIndicator;
        public AudioEvent TargetLockedSounds;

        [HideInInspector]
		public bool WasManuallyAdded = false;

		[HideInInspector]
		public SmartProjectile.SmartProjectileData ProjectileData;

		[HideInInspector]
		public float BulletVelocityModifier = 1f;
		//constants
		private const string _nameOfDistanceVariable = "_RedDotDist";

		private SosigLink _lastTarget;

		private GameObject _origMuzzlePos;

		private bool _timeoutStarted = false;

		private static readonly Dictionary<FVRFireArm, SmartMultiBarrelWeapon> _existingSmartWeapon = new();

        private FVRFireArmRound _replacementRound;

#if !(DEBUG || MEATKIT)
        static SmartMultiBarrelWeapon()
        {
			On.FistVR.FVRFireArm.Fire += FVRFireArm_Fire;
		}

		public void Awake()
        {
			_existingSmartWeapon.Add(FireArm, this);

			_origMuzzlePos = Instantiate(FireArm.MuzzlePos.gameObject, this.transform);
			_origMuzzlePos.transform.localPosition = FireArm.MuzzlePos.localPosition;
			_origMuzzlePos.transform.localRotation = FireArm.MuzzlePos.localRotation;

			if (ReplacementRoundPrefab != null) _replacementRound = ReplacementRoundPrefab.GetComponent<FVRFireArmRound>();
        }
		public void OnDestroy()
        {
			_existingSmartWeapon.Remove(FireArm);
        }

        private static void FVRFireArm_Fire(On.FistVR.FVRFireArm.orig_Fire orig, FVRFireArm self, FVRFireArmChamber chamber, Transform muzzle, bool doBuzz, float velMult, float rangeOverride)
        {
            if (_existingSmartWeapon.TryGetValue(self, out SmartMultiBarrelWeapon smartMultiBarrelWeapon))
            {
                smartMultiBarrelWeapon.RandomizeMuzzles();

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

				FVRFireArmRound roundToUse = smartMultiBarrelWeapon._replacementRound ?? chamber.GetRound();

                float chamberVelMult = AM.GetChamberVelMult(roundToUse.RoundType, Vector3.Distance(chamber.transform.position, muzzle.position));
                float num = self.GetCombinedFixedDrop(self.AccuracyClass) * 0.0166667f;
                Vector2 vector = self.GetCombinedFixedDrift(self.AccuracyClass) * 0.0166667f;
                for (int i = 0; i < roundToUse.NumProjectiles; i++)
                {
                    float d = roundToUse.ProjectileSpread + self.m_internalMechanicalMOA + self.GetCombinedMuzzleDeviceAccuracy();
                    if (roundToUse.BallisticProjectilePrefab != null)
                    {
                        Vector3 b = muzzle.forward * 0.005f;
                        GameObject gameObject = Instantiate(roundToUse.BallisticProjectilePrefab, muzzle.position - b, muzzle.rotation);
                        Vector2 vector2 = (UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle) * 0.33333334f * d;
                        gameObject.transform.Rotate(new Vector3(vector2.x + vector.y + num, vector2.y + vector.x, 0f));
                        BallisticProjectile component = gameObject.GetComponent<BallisticProjectile>();

                        float baseVelocity = component.MuzzleVelocityBase * chamber.ChamberVelocityMultiplier * velMult * chamberVelMult;
                        component.Fire(baseVelocity * smartMultiBarrelWeapon.BulletVelocityModifier, gameObject.transform.forward, self, true);

                        SmartProjectile smartProjectile = gameObject.GetComponent<SmartProjectile>();
                        if (smartProjectile == null && smartMultiBarrelWeapon.WasManuallyAdded)
                        {
                            smartProjectile = gameObject.AddComponent<SmartProjectile>();
                            smartProjectile.Projectile = component;
                            smartProjectile.ConfigureFromData(smartMultiBarrelWeapon.ProjectileData);
                        }
                        if (smartProjectile != null)
                        {
                            smartProjectile.TargetLink = smartMultiBarrelWeapon._lastTarget;
                        }
                        if (rangeOverride > 0f)
                        {
                            component.ForceSetMaxDist(rangeOverride);
                        }
                        if (smartMultiBarrelWeapon.BulletVelocityModifier != 1f)
                        {
                            component.Mass = (Mathf.Pow(baseVelocity, 2f) * component.Mass) / Mathf.Pow(baseVelocity * smartMultiBarrelWeapon.BulletVelocityModifier, 2f);
                        }
                        if (self is BreakActionWeapon breakAction && smartMultiBarrelWeapon.BarrelMuzzleConfigurations[breakAction.m_curBarrel].AdditionalMuzzles != null)
                        {
                            foreach (var additionalMuzzle in smartMultiBarrelWeapon.BarrelMuzzleConfigurations[breakAction.m_curBarrel].AdditionalMuzzles)
                            {
                                Vector3 b2 = muzzle.forward * 0.005f;
                                GameObject projectileGameObject = Instantiate(roundToUse.BallisticProjectilePrefab, additionalMuzzle.position - b2, additionalMuzzle.rotation);
                                Vector2 firingDirection = (UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle) * 0.33333334f * d;
                                projectileGameObject.transform.Rotate(new Vector3(firingDirection.x + firingDirection.y + num, firingDirection.y + firingDirection.x, 0f));
                                BallisticProjectile projectile = projectileGameObject.GetComponent<BallisticProjectile>();

                                projectile.Fire(baseVelocity * smartMultiBarrelWeapon.BulletVelocityModifier, projectileGameObject.transform.forward, self, true);

                                SmartProjectile smartProjectile2 = projectileGameObject.GetComponent<SmartProjectile>();
                                if (smartProjectile2 == null && smartMultiBarrelWeapon.WasManuallyAdded)
                                {
                                    smartProjectile2 = projectileGameObject.AddComponent<SmartProjectile>();
                                    smartProjectile2.Projectile = projectile;
                                    smartProjectile2.ConfigureFromData(smartMultiBarrelWeapon.ProjectileData);
                                }
                                if (smartProjectile2 != null)
                                {
                                    smartProjectile2.TargetLink = smartMultiBarrelWeapon._lastTarget;
                                }
                                if (rangeOverride > 0f)
                                {
                                    projectile.ForceSetMaxDist(rangeOverride);
                                }
                                if (smartMultiBarrelWeapon.BulletVelocityModifier != 1f)
                                {
                                    projectile.Mass = (Mathf.Pow(baseVelocity, 2f) * projectile.Mass) / Mathf.Pow(baseVelocity * smartMultiBarrelWeapon.BulletVelocityModifier, 2f);
                                }
                            }
                        }
                    }
                }
            }
            else orig(self, chamber, muzzle, doBuzz, velMult, rangeOverride);
        }

        public void Update()
        {
            if (FireArm.IsHeld)
            {
				SosigLink _target = FindTarget();

				if (_target != null)
                {
					//Debug.Log(target);
					if (_timeoutStarted && LastTargetTimeout != 0f)
					{
						StopCoroutine("LastTargetTimeoutCoroutine");
						_timeoutStarted = false;
					}
                    if (_lastTarget != _target) SM.PlayGenericSound(TargetLockedSounds, FireArm.transform.position);

                    _lastTarget = _target;
				}
				else
                {
					if (!_timeoutStarted && LastTargetTimeout != 0f)
					{
						StopCoroutine("LastTargetTimeoutCoroutine");
						StartCoroutine("LastTargetTimeoutCoroutine");
					}
					if (DisableReticleWithoutTarget && ReticleMesh != null) ReticleMesh.gameObject.SetActive(false);

					if (_lastTarget != null && (Vector3.Distance(transform.position, _lastTarget.transform.position) > EngageRange || _lastTarget.S.BodyState == Sosig.SosigBodyState.Dead))
					{
                        StopCoroutine("LastTargetTimeoutCoroutine");
						_timeoutStarted = false;
						_lastTarget = null;
                    }
				}

				if (_lastTarget != null && ReticleMesh != null)
                {
					ReticleMesh.transform.LookAt(_lastTarget.transform.position);
					ReticleMesh.material.SetFloat(_nameOfDistanceVariable, Vector3.Distance(_lastTarget.transform.position, ReticleMesh.transform.position));
					if (DisableReticleWithoutTarget) ReticleMesh.gameObject.SetActive(true);
					TargetLockedIndicator?.gameObject.SetActive(true);
				}
				else if (_lastTarget == null && ReticleMesh != null)
                {
					ReticleMesh.transform.localRotation = Quaternion.identity;

					if (DisableReticleWithoutTarget) ReticleMesh.gameObject.SetActive(false);
                    TargetLockedIndicator?.gameObject.SetActive(false);
                }


			}
			else
			{
                StopCoroutine("LastTargetTimeoutCoroutine");
                _lastTarget = null;

                ReticleMesh.transform.localRotation = Quaternion.identity;
                if (DisableReticleWithoutTarget) ReticleMesh.gameObject.SetActive(false);
                TargetLockedIndicator?.gameObject.SetActive(false);
            }
        }

        private void RandomizeMuzzles()
        {
            if (DoesRandomRotationOfBarrelForCinematicBulletTrails)
            {
                Vector3 randRot = new();
                randRot.x = UnityEngine.Random.Range(-RandomAngleMagnitude, RandomAngleMagnitude);
                randRot.y = UnityEngine.Random.Range(-RandomAngleMagnitude, RandomAngleMagnitude);

                FireArm.CurrentMuzzle.localEulerAngles = randRot;

                foreach (var additionalMuzzle in BarrelMuzzleConfigurations.SelectMany(b => b.AdditionalMuzzles))
                {
                    randRot.x = UnityEngine.Random.Range(-RandomAngleMagnitude, RandomAngleMagnitude);
                    randRot.y = UnityEngine.Random.Range(-RandomAngleMagnitude, RandomAngleMagnitude);

                    additionalMuzzle.localEulerAngles = randRot;
                }
            }
        }

		private IEnumerator LastTargetTimeoutCoroutine()
        {
			_timeoutStarted = true;
			yield return new WaitForSeconds(LastTargetTimeout);
			_lastTarget = null;
			_timeoutStarted = false;
        }

		private SosigLink FindTarget()
        {
			float radius = EngageRange * Mathf.Tan(0.5f * EngageAngle * Mathf.Deg2Rad);
			Collider[] colliderArray = Physics.OverlapCapsule(FireArm.CurrentMuzzle.position, FireArm.CurrentMuzzle.position + _origMuzzlePos.transform.forward * EngageRange, radius, LatchingMask);
			List<Rigidbody> rigidbodyList = new List<Rigidbody>();
			for (int i = 0; i < colliderArray.Length; i++)
			{
				if (colliderArray[i].attachedRigidbody != null && !rigidbodyList.Contains(colliderArray[i].attachedRigidbody))
				{
					rigidbodyList.Add(colliderArray[i].attachedRigidbody);
				}
			}
			SosigLink targetSosigLink = null;
			SosigLink tempSosigLink;
			float minAngle = EngageAngle;
			for (int j = 0; j < rigidbodyList.Count; j++)
			{
				SosigLink sosigLinkComponent = rigidbodyList[j].GetComponent<SosigLink>();

				if (sosigLinkComponent != null && sosigLinkComponent.S.BodyState != Sosig.SosigBodyState.Dead)
				{
					if (true || sosigLinkComponent.S.E.IFFCode == 1)
					{
						Vector3 from = rigidbodyList[j].transform.position - FireArm.CurrentMuzzle.position;
						float angle = Vector3.Angle(from, _origMuzzlePos.transform.forward);

						Sosig s = sosigLinkComponent.S;
						if (angle <= PrecisionAngle) tempSosigLink = s.Links[0];
						else tempSosigLink = s.Links[1];

						if (angle < minAngle && !Physics.Linecast(FireArm.CurrentMuzzle.position, tempSosigLink.transform.position, BlockingMask, QueryTriggerInteraction.Ignore))
						{
							targetSosigLink = tempSosigLink;
							minAngle = angle;
						}
					}
				}

			}
			return targetSosigLink;
		}
#endif
	}
}
