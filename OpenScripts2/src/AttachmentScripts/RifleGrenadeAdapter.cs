using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class RifleGrenadeAdapter : MuzzleDevice
    {
        [Header("Rifle Grenade Adapter Config")]
        public FVRFireArmChamber GrenadeChamber;
        public Transform GrenadeMuzzle;

        public float VelocityMultiplier = 1f;
        public float RangeOverride = -1f;
        public FVRFireArmRecoilProfile OverrideRecoilProfile;
        public FVRFireArmRecoilProfile OverrideRecoilProfileStocked;

        public AudioEvent GrenadeShot;

        [Tooltip("Normally, only caseless rounds will be removed from the chamber when fired. Enabling this will also remove fired cased rounds from the chamber automatically.")]
        public bool DoesClearCasedRounds = false;

        [Header("Safe Rounds Settings (\"Blank Firing rounds\")")]
        [Tooltip("Do you need to use a specific round class to not make the grenade go boom on the muzzle?\nLike Blank firing rounds and such.")]
        public bool UsesSafeRoundClass = false;
        public FireArmRoundClass SafeRoundClass;

        private Vector3 _origMuzzlePos;
        private Quaternion _origMuzzleRot;
        private FVRFireArm _fireArm;
        private FVRFireArmRecoilProfile _origRecoilProfile;
        private FVRFireArmRecoilProfile _origRecoilProfileStocked;
        private bool _recoilProfileSet = false;

        public override void Awake()
        {
            base.Awake();

            _origMuzzlePos = Muzzle.localPosition;
            _origMuzzleRot = Muzzle.localRotation;

            OpenScripts2_BasePlugin.ProjectileFiredEvent += ProjectileFiredEvent;
        }

        private void ProjectileFiredEvent(FVRFireArm fireArm, ref BallisticProjectile projectile)
        {
            if (fireArm == curMount.GetRootMount().MyObject && GrenadeChamber.IsFull)
            {
                Destroy(projectile.gameObject);
            }
        }

        public override void OnDestroy()
        {
            OpenScripts2_BasePlugin.ProjectileFiredEvent -= ProjectileFiredEvent;
            base.OnDestroy();
        }

        public override void FVRUpdate()
        {
            base.FVRUpdate();

            if (GrenadeChamber.IsFull)
            {
                //Muzzle.position = Vector3.down * 3 + transform.TransformPoint(_origMuzzlePos);
                //Muzzle.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

                if (!_recoilProfileSet && _fireArm != null && OverrideRecoilProfile != null)
                {
                    _origRecoilProfile = _fireArm.RecoilProfile;
                    _origRecoilProfileStocked = _fireArm.RecoilProfileStocked;

                    if (OverrideRecoilProfileStocked != null)
                    {
                        _fireArm.RecoilProfile = OverrideRecoilProfile;
                        _fireArm.RecoilProfileStocked = OverrideRecoilProfileStocked;
                    }
                    else
                    {
                        _fireArm.RecoilProfile = OverrideRecoilProfile;
                        _fireArm.RecoilProfileStocked = OverrideRecoilProfile;
                    }

                    _recoilProfileSet = true;
                }

                if (GrenadeChamber.IsSpent && (GrenadeChamber.GetRound().IsCaseless || DoesClearCasedRounds)) GrenadeChamber.Unload();
            }
            else
            {
                if (_recoilProfileSet && _fireArm != null)
                {

                    _fireArm.RecoilProfile = _origRecoilProfile;
                    _fireArm.RecoilProfileStocked = _origRecoilProfileStocked;

                    _recoilProfileSet = false;
                }

                //Muzzle.localPosition = _origMuzzlePos;
                //Muzzle.localRotation = _origMuzzleRot;
            }
        }

        public override void AttachToMount(FVRFireArmAttachmentMount m, bool playSound)
        {
            base.AttachToMount(m, playSound);

            _fireArm = m.GetRootMount().MyObject as FVRFireArm;
        }

        public override void OnShot(FVRFireArm f, FVRTailSoundClass tailClass)
        {
            base.OnShot(f, tailClass);

            FireGrenade();
        }

        private void FireGrenade()
        {
            if (UsesSafeRoundClass && GrenadeChamber.IsFull && !GrenadeChamber.IsSpent && OpenScripts2_BasePlugin.GetCurrentChamber(_fireArm).m_round.RoundClass != SafeRoundClass)
            {
                foreach (var explosionPart in GrenadeChamber.m_round.BallisticProjectilePrefab.GetComponent<BallisticProjectile>().Submunitions.Where(submun => submun.Type == BallisticProjectile.Submunition.SubmunitionType.GameObject).SelectMany(submun => submun.Prefabs))
                {
                    Instantiate(explosionPart);
                }

                GrenadeChamber.Unload();
                return;
            }

            if (!GrenadeChamber.Fire() || _fireArm == null) return;

            //float chamberVelMult = AM.GetChamberVelMult(Chamber.RoundType, Vector3.Distance(Chamber.transform.position, GrenadeMuzzle.position));
            float num = _fireArm.GetCombinedFixedDrop(MechanicalAccuracy) * 0.0166667f;
            Vector2 vector = _fireArm.GetCombinedFixedDrift(MechanicalAccuracy) * 0.0166667f;

            for (int i = 0; i < GrenadeChamber.GetRound().NumProjectiles; i++)
            {
                float d = GrenadeChamber.GetRound().ProjectileSpread + this.m_mechanicalAccuracy;
                if (GrenadeChamber.GetRound().BallisticProjectilePrefab != null)
                {
                    Vector3 b = GrenadeMuzzle.forward * 0.005f;
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(GrenadeChamber.GetRound().BallisticProjectilePrefab, GrenadeMuzzle.position - b, GrenadeMuzzle.rotation);
                    Vector2 vector2 = (UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle) * 0.33333334f * d;
                    gameObject.transform.Rotate(new Vector3(vector2.x + vector.y + num, vector2.y + vector.x, 0f));
                    BallisticProjectile component = gameObject.GetComponent<BallisticProjectile>();
                    component.Fire(component.MuzzleVelocityBase * GrenadeChamber.ChamberVelocityMultiplier * VelocityMultiplier /* * chamberVelMult*/, gameObject.transform.forward, _fireArm, true);
                    if (RangeOverride > 0f)
                    {
                        component.ForceSetMaxDist(RangeOverride);
                    }
                }
            }

            SM.PlayCoreSound(FVRPooledAudioType.GunShot, GrenadeShot, GrenadeMuzzle.position);
        }
    }
}
