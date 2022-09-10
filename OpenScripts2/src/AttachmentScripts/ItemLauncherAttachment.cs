using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using OpenScripts2;

namespace Cityrobo
{
    public class ItemLauncherAttachment : MuzzleDevice
    {
        [Header("ItemLauncher Config")]
        public ItemLauncherQBSlot ItemHolder;
        public Transform ItemLaunchPoint;

        public float SpeedMultiplier = 1f;
        public FVRFireArmRecoilProfile OverrideRecoilProfile;
        public FVRFireArmRecoilProfile OverrideRecoilProfileStocked;

        public AudioEvent GrenadeShot;

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
        }

        public override void FVRUpdate()
        {
            base.FVRUpdate();

            if (ItemHolder.HeldObject != null)
            {
                Muzzle.position = Vector3.down * 3 + this.transform.TransformPoint(_origMuzzlePos);
                Muzzle.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

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

            }
            else
            {
                if (_recoilProfileSet && _fireArm != null)
                {

                    _fireArm.RecoilProfile = _origRecoilProfile;
                    _fireArm.RecoilProfileStocked = _origRecoilProfileStocked;

                    _recoilProfileSet = false;
                }

                Muzzle.localPosition = _origMuzzlePos;
                Muzzle.localRotation = _origMuzzleRot;
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

            if (ItemHolder.HeldObject != null) FireItem();
        }

        private void FireItem()
        {
            float speed = CalculateLaunchSpeed();

            bool launched = ItemHolder.LaunchHeldObject(speed * SpeedMultiplier, ItemLaunchPoint.position);
            if (launched) SM.PlayCoreSound(FVRPooledAudioType.GunShot, GrenadeShot, ItemHolder.transform.position);
        }

        private float CalculateLaunchSpeed()
        {
            FVRFireArmChamber chamber = OpenScripts2_BasePlugin.GetCurrentChamber(_fireArm);
            if (chamber == null) return 5f;
            GameObject roundPrefab = chamber.GetRound().BallisticProjectilePrefab;
            BallisticProjectile ballisticProjectile = roundPrefab.GetComponent<BallisticProjectile>();

            float kinecticEnergy = 0.5f * ballisticProjectile.Mass * Mathf.Pow(ballisticProjectile.MuzzleVelocityBase, 2);

            float ItemMass = ItemHolder.CurObject.RootRigidbody.mass;

            return Mathf.Sqrt(kinecticEnergy / (0.5f * ItemMass));
        }
    }
}
