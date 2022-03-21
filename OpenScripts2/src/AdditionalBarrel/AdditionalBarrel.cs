using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class AdditionalBarrel : OpenScripts2_BasePlugin
    {
        public FVRFireArm FireArm;
        public Transform MuzzlePos;

        public bool UsesAdditionalRound;
#if !MEATKIT
        public void Start()
        {
            Hook();
        }

        public void OnDestroy()
        {
            Unhook();
        }

        void Unhook()
        {
            On.FistVR.FVRFireArm.Fire -= FVRFireArm_Fire;
        }

        void Hook()
        {
            On.FistVR.FVRFireArm.Fire += FVRFireArm_Fire;
        }

        private void FVRFireArm_Fire(On.FistVR.FVRFireArm.orig_Fire orig, FVRFireArm self, FVRFireArmChamber chamber, Transform muzzle, bool doBuzz, float velMult, float rangeOverride)
        {
            orig(self, chamber, muzzle, doBuzz, velMult, rangeOverride);
            if (FireArm == self)
            {
                FVRFireArmRound round = chamber.GetRound();
                if (UsesAdditionalRound)
                {
                    if (self.Magazine != null && self.Magazine.HasARound())
                    {
                        GameObject roundWrapper = self.Magazine.RemoveRound(false);
                        round = roundWrapper.GetComponent<FVRFireArmRound>();
                    }
                    else return;
                }
                float chamberVelMult = AM.GetChamberVelMult(round.RoundType, Vector3.Distance(chamber.transform.position, muzzle.position));
                float num = FireArm.GetCombinedFixedDrop(FireArm.AccuracyClass) * 0.0166667f;
                Vector2 vector = FireArm.GetCombinedFixedDrift(FireArm.AccuracyClass) * 0.0166667f;

                for (int i = 0; i < round.NumProjectiles; i++)
                {
                    float d = round.ProjectileSpread + FireArm.m_internalMechanicalMOA + FireArm.GetCombinedMuzzleDeviceAccuracy();
                    if (round.BallisticProjectilePrefab != null)
                    {
                        Vector3 b = muzzle.forward * 0.005f;
                        GameObject gameObject = Instantiate(round.BallisticProjectilePrefab, this.MuzzlePos.position - b, this.MuzzlePos.rotation);
                        Vector2 vector2 = (UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle) * 0.33333334f * d;
                        gameObject.transform.Rotate(new Vector3(vector2.x + vector.y + num, vector2.y + vector.x, 0f));
                        BallisticProjectile component = gameObject.GetComponent<BallisticProjectile>();
                        component.Fire(component.MuzzleVelocityBase * chamber.ChamberVelocityMultiplier * velMult * chamberVelMult, gameObject.transform.forward, FireArm);
                    }
                }
            }
        }
    #endif
    }
}
