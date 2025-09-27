using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenScripts2
{
    public class ShotgunChokeInterface : MuzzleDeviceInterface
    {
        [Header("Shotgun Choke Config")]
        public Vector2 ChokingVector = new();

        private FVRFireArm _fireArm;

        static private readonly Dictionary<FVRFireArm, ShotgunChokeInterface> _existingChokes = new();

        public override void OnAttach()
        {
            base.OnAttach();
            _fireArm = Attachment.curMount.Parent as FVRFireArm;
            if (_fireArm != null)
            {
                _existingChokes.Add(_fireArm, this);
            }
        }

        public override void OnDetach()
        {
            if (_fireArm != null) 
            {
                _existingChokes.Remove(_fireArm);
                _fireArm = null;
            }
            base.OnDetach();
        }

#if !DEBUG
        static ShotgunChokeInterface()
        {
            On.FistVR.FVRFireArm.Fire += FVRFireArm_Fire;
        }

        private static void FVRFireArm_Fire(On.FistVR.FVRFireArm.orig_Fire orig, FVRFireArm self, FVRFireArmChamber chamber, Transform muzzle, bool doBuzz, float velMult, float rangeOverride)
        {
            if (_existingChokes.TryGetValue(self, out var shotgunChoke))
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
                        GameObject gameObject = Instantiate(chamber.GetRound().BallisticProjectilePrefab, muzzle.position - b, muzzle.rotation);
                        Vector2 vector2 = (UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle + UnityEngine.Random.insideUnitCircle) * 0.33333334f * d;
                        vector2.x *= shotgunChoke.ChokingVector.x;
                        vector2.y *= shotgunChoke.ChokingVector.y;
                        gameObject.transform.Rotate(new Vector3(vector2.x + vector.y + num, vector2.y + vector.x, 0f));
                        BallisticProjectile component = gameObject.GetComponent<BallisticProjectile>();
                        component.Fire(component.MuzzleVelocityBase * chamber.ChamberVelocityMultiplier * velMult * chamberVelMult, gameObject.transform.forward, self, true);
                        if (rangeOverride > 0f)
                        {
                            component.ForceSetMaxDist(rangeOverride);
                        }
                    }
                }
            }
            else 
            {
                orig(self, chamber, muzzle, doBuzz, velMult, rangeOverride);
            }
        }
#endif
    }
}