using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace OpenScripts2
{
    public class SmartWeapon : OpenScripts2_BasePlugin
    {
        [Header("SmartWeapon Config")]
        public FVRFireArm FireArm;
		[Tooltip("Use this to add more muzzles, and more bullets comming out the front.")]
        public Transform[] AdditionalMuzzles;
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
        public Transform SensorPosition;

        [HideInInspector]
		public bool WasManuallyAdded = false;

		[HideInInspector]
		public SmartProjectile.SmartProjectileData ProjectileData;

		[HideInInspector]
		public float BulletVelocityModifier = 1f;
		//constants
		private const string _nameOfDistanceVariable = "_RedDotDist";

		private SosigLink _lastTarget;

		//private GameObject _origMuzzlePos;

		private bool _timeoutStarted = false;

		private static readonly Dictionary<FVRFireArm, SmartWeapon> _existingSmartWeapon = new();

        private FVRFireArmRound _replacementRound;

#if !(DEBUG || MEATKIT)
        static SmartWeapon()
        {
			On.FistVR.FVRFireArm.Fire += FVRFireArm_Fire;
		}

		public void Awake()
        {
			_existingSmartWeapon.Add(FireArm, this);

			//_origMuzzlePos = Instantiate(FireArm.MuzzlePos.gameObject, FireArm.MuzzlePos.parent);
			//_origMuzzlePos.transform.localPosition = FireArm.MuzzlePos.localPosition;
			//_origMuzzlePos.transform.localRotation = FireArm.MuzzlePos.localRotation;

            if (ReplacementRoundPrefab != null) _replacementRound = ReplacementRoundPrefab.GetComponent<FVRFireArmRound>();
        }
		public void OnDestroy()
        {
			_existingSmartWeapon.Remove(FireArm);
        }

        public void OnDisable()
        {
            _lastTarget = null;
            if (DisableReticleWithoutTarget && ReticleMesh != null) ReticleMesh.gameObject.SetActive(false);
            TargetLockedIndicator?.SetActive(false);
            //FireArm.MuzzlePos.localRotation = _origMuzzlePos.transform.localRotation;
        }

        private static void FVRFireArm_Fire(On.FistVR.FVRFireArm.orig_Fire orig, FVRFireArm self, FVRFireArmChamber chamber, Transform muzzle, bool doBuzz, float velMult, float rangeOverride)
        {
            if (_existingSmartWeapon.TryGetValue(self, out SmartWeapon smartWeapon))
            {
                //smartWeapon.RandomizeMuzzles();

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

                FVRFireArmRound roundToUse = smartWeapon._replacementRound ?? chamber.GetRound();

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
                        gameObject.transform.Rotate(new Vector3(vector2.x + vector.y + num, vector2.y + vector.x, 0f) + smartWeapon.ReturnRandomizedRotationVector());
                        BallisticProjectile component = gameObject.GetComponent<BallisticProjectile>();

                        float baseVelocity = component.MuzzleVelocityBase * chamber.ChamberVelocityMultiplier * velMult * chamberVelMult;
                        component.Fire(baseVelocity * smartWeapon.BulletVelocityModifier, gameObject.transform.forward, self, true);

                        SmartProjectile smartProjectile = gameObject.GetComponent<SmartProjectile>();
                        if (smartProjectile == null && smartWeapon.WasManuallyAdded)
                        {
                            smartProjectile = gameObject.AddComponent<SmartProjectile>();
                            smartProjectile.Projectile = component;
                            smartProjectile.ConfigureFromData(smartWeapon.ProjectileData);
                        }
                        if (smartProjectile != null)
                        {
                            smartProjectile.TargetLink = smartWeapon._lastTarget;
                        }
                        if (rangeOverride > 0f)
                        {
                            component.ForceSetMaxDist(rangeOverride);
                        }
                        if (smartWeapon.BulletVelocityModifier != 1f)
                        {
                            component.Mass = (Mathf.Pow(baseVelocity, 2f) * component.Mass) / Mathf.Pow(baseVelocity * smartWeapon.BulletVelocityModifier, 2f);
                        }
                        if (smartWeapon.AdditionalMuzzles != null)
                        {
                            foreach (var additionalMuzzle in smartWeapon.AdditionalMuzzles)
                            {
                                Vector3 b2 = muzzle.forward * 0.005f;
                                GameObject projectileGameObject = Instantiate(roundToUse.BallisticProjectilePrefab, additionalMuzzle.position - b2, additionalMuzzle.rotation);
                                Vector2 firingDirection = (UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle) * 0.33333334f * d;
                                projectileGameObject.transform.Rotate(new Vector3(firingDirection.x + firingDirection.y + num, firingDirection.y + firingDirection.x, 0f) + smartWeapon.ReturnRandomizedRotationVector());
                                BallisticProjectile projectile = projectileGameObject.GetComponent<BallisticProjectile>();

                                projectile.Fire(baseVelocity * smartWeapon.BulletVelocityModifier, projectileGameObject.transform.forward, self, true);

                                SmartProjectile smartProjectile2 = projectileGameObject.GetComponent<SmartProjectile>();
                                if (smartProjectile2 == null && smartWeapon.WasManuallyAdded)
                                {
                                    smartProjectile2 = projectileGameObject.AddComponent<SmartProjectile>();
                                    smartProjectile2.Projectile = projectile;
                                    smartProjectile2.ConfigureFromData(smartWeapon.ProjectileData);
                                }
                                if (smartProjectile2 != null)
                                {
                                    smartProjectile2.TargetLink = smartWeapon._lastTarget;
                                }
                                if (rangeOverride > 0f)
                                {
                                    projectile.ForceSetMaxDist(rangeOverride);
                                }
                                if (smartWeapon.BulletVelocityModifier != 1f)
                                {
                                    projectile.Mass = (Mathf.Pow(baseVelocity, 2f) * projectile.Mass) / Mathf.Pow(baseVelocity * smartWeapon.BulletVelocityModifier, 2f);
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

					if (_lastTarget != _target) SM.PlayGenericSound(TargetLockedSounds,FireArm.transform.position);
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
                Vector3 randRot = Vector3.zero;
                randRot.x = UnityEngine.Random.Range(-RandomAngleMagnitude, RandomAngleMagnitude);
                randRot.y = UnityEngine.Random.Range(-RandomAngleMagnitude, RandomAngleMagnitude);

                FireArm.CurrentMuzzle.localEulerAngles = randRot;

                if (AdditionalMuzzles != null)
                {
                    foreach (var additionalMuzzle in AdditionalMuzzles)
                    {
                        randRot.x = UnityEngine.Random.Range(-RandomAngleMagnitude, RandomAngleMagnitude);
                        randRot.y = UnityEngine.Random.Range(-RandomAngleMagnitude, RandomAngleMagnitude);

                        additionalMuzzle.localEulerAngles = randRot;
                    }
                }
            }
        }

        private Vector3 ReturnRandomizedRotationVector()
        {
            Vector3 randRot = Vector3.zero;
            if (DoesRandomRotationOfBarrelForCinematicBulletTrails)
            {
                randRot.x = UnityEngine.Random.Range(-RandomAngleMagnitude, RandomAngleMagnitude);
                randRot.y = UnityEngine.Random.Range(-RandomAngleMagnitude, RandomAngleMagnitude);
            }

            return randRot;
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
            //Collider[] colliderArray = Physics.OverlapCapsule(FireArm.CurrentMuzzle.position, FireArm.CurrentMuzzle.position + _origMuzzlePos.transform.forward * EngageRange, radius, LatchingMask);
            Transform sensorOrigin = SensorPosition ?? FireArm.transform;

            Collider[] colliderArray = Physics.OverlapCapsule(sensorOrigin.position, sensorOrigin.position + sensorOrigin.forward * EngageRange, radius, LatchingMask);
			List<Rigidbody> rigidbodyList = new();
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
                        sensorOrigin = SensorPosition ?? FireArm.CurrentMuzzle;

                        Vector3 from = rigidbodyList[j].transform.position - sensorOrigin.position;
						//float angle = Vector3.Angle(from, _origMuzzlePos.transform.forward);
						float angle = Vector3.Angle(from, sensorOrigin.forward);

						Sosig s = sosigLinkComponent.S;
						if (angle <= PrecisionAngle) tempSosigLink = s.Links[0];
						else tempSosigLink = s.Links[1];

						if (angle < minAngle && !Physics.Linecast(sensorOrigin.position, tempSosigLink.transform.position, BlockingMask, QueryTriggerInteraction.Ignore))
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
