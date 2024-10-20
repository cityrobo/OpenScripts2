using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;

namespace OpenScripts2
{
    public class RecoilModificationCore : OpenScripts2_BasePlugin
    {
        public List<float> VerticalRecoilMultipliers = new();

        public List<RecoilMultipliers> RecoilMultipliersList = new();

        private FVRFireArm _firearm;
        private FVRFireArmRecoilProfile _origRecoil;
        private FVRFireArmRecoilProfile _origRecoilStocked;
        private static readonly Dictionary<FVRFireArm, RecoilModificationCore> _existingRecoilModificationCores = new();

        public void Awake()
        {
            if (_firearm == null) _firearm = GetComponent<FVRFireArm>();

            _origRecoil = _firearm.RecoilProfile;
            _origRecoilStocked = _firearm.RecoilProfileStocked;

            _existingRecoilModificationCores.Add(_firearm, this);
        }

        public void OnDestroy()
        {
            _existingRecoilModificationCores.Remove(_firearm);
        }

        public void UpdateRecoilProfile()
        {
            RecoilMultipliers multipliers = new RecoilMultipliers();
            if (RecoilMultipliersList.Count > 0)
            {
                for (int i = 0; i < RecoilMultipliersList.Count; i++)
                {
                    multipliers.VerticalRotPerShot *= RecoilMultipliersList[i].VerticalRotPerShot;
                    multipliers.MaxVerticalRot_Bipodded *= RecoilMultipliersList[i].MaxVerticalRot_Bipodded;
                    multipliers.MaxVerticalRot *= RecoilMultipliersList[i].MaxVerticalRot;
                    multipliers.VerticalRotRecovery *= RecoilMultipliersList[i].VerticalRotRecovery;

                    multipliers.HorizontalRotPerShot *= RecoilMultipliersList[i].HorizontalRotPerShot;
                    multipliers.MaxHorizontalRot_Bipodded *= RecoilMultipliersList[i].MaxHorizontalRot_Bipodded;
                    multipliers.MaxHorizontalRot *= RecoilMultipliersList[i].MaxHorizontalRot;
                    multipliers.HorizontalRotRecovery *= RecoilMultipliersList[i].HorizontalRotRecovery;

                    multipliers.ZLinearPerShot *= RecoilMultipliersList[i].ZLinearPerShot;
                    multipliers.ZLinearMax *= RecoilMultipliersList[i].ZLinearMax;
                    multipliers.ZLinearRecovery *= RecoilMultipliersList[i].ZLinearRecovery;

                    multipliers.XYLinearPerShot *= RecoilMultipliersList[i].XYLinearPerShot;
                    multipliers.XYLinearMax *= RecoilMultipliersList[i].XYLinearMax;
                    multipliers.XYLinearRecovery *= RecoilMultipliersList[i].XYLinearRecovery;
                }
            }

            _firearm.RecoilProfile = CopyAndAdjustRecoilProfile(_origRecoil, multipliers);
            if (_origRecoilStocked != null) _firearm.RecoilProfileStocked = CopyAndAdjustRecoilProfile(_origRecoilStocked, multipliers);
        }

#if !DEBUG
        static RecoilModificationCore()
        {
            On.FistVR.FVRFireArm.Recoil += FVRFireArm_Recoil;
        }

        private static void FVRFireArm_Recoil(On.FistVR.FVRFireArm.orig_Recoil orig, FVRFireArm self, bool twoHandStabilized, bool foregripStabilized, bool shoulderStabilized, FVRFireArmRecoilProfile overrideprofile, float VerticalRecoilMult)
        {
            if (_existingRecoilModificationCores.TryGetValue(self, out RecoilModificationCore recoilCore))
            {
                float totalRecoilMultiplier = 1f;

                if (recoilCore.VerticalRecoilMultipliers.Count > 0)
                {
                    for (int i = 0; i < recoilCore.VerticalRecoilMultipliers.Count; i++)
                    {
                        totalRecoilMultiplier *= recoilCore.VerticalRecoilMultipliers[i];
                    }
                }

                VerticalRecoilMult *= totalRecoilMultiplier;
            }

            orig(self, twoHandStabilized, foregripStabilized, shoulderStabilized, overrideprofile, VerticalRecoilMult);
        }
#endif
    }
}
