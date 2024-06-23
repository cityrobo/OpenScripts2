using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace OpenScripts2
{
    public class CustomMuzzleEffectsController : MonoBehaviour
    {
        public FVRFireArm FireArm;
        public CustomMuzzleEffect[] CustomMuzzleEffects;
        public float MuzzleEffectSizeMultiplier = 1f;
        [Tooltip("Use a barrel length dependent value instead of a fixed one?")]
        public bool UsesBarrelLengthDependentMuzzleEffectSizeCurve;
        [Tooltip("X-Axis is Barrel length in meters. Y-Axis is MuzzleEffectSizeMultiplier.")]
        public AnimationCurve MuzzleEffectSizeOverBarrelLength;

        private static readonly Dictionary<FVRFireArm, CustomMuzzleEffectsController> _existingCustomMuzzleEffectsControllers = new();
#if !DEBUG
        static CustomMuzzleEffectsController()
        {
            On.FistVR.FVRFireArm.RegenerateMuzzleEffects += FVRFireArm_RegenerateMuzzleEffects;
        }

        private static void FVRFireArm_RegenerateMuzzleEffects(On.FistVR.FVRFireArm.orig_RegenerateMuzzleEffects orig, FVRFireArm self, MuzzleDevice m)
        {
            orig(self, m);

            if (_existingCustomMuzzleEffectsControllers.TryGetValue(self, out CustomMuzzleEffectsController controller))
            {
                bool overrideEffectSizeWithDefault = false;
                CustomMuzzleEffect[] customMuzzleEffects = controller.CustomMuzzleEffects;
                float muzzleDevizeEffectMultiplier = 1f;
                if (m != null)
                {
                    if (!m.ForcesEffectSize) overrideEffectSizeWithDefault = true;
                    if (m.TryGetComponentInChildren(out CustomMuzzleEffectsForMuzzleDevice customMuzzleEffectsForMuzzleDevice))
                    {
                        customMuzzleEffects = customMuzzleEffectsForMuzzleDevice.CustomMuzzleEffects;
                        muzzleDevizeEffectMultiplier = customMuzzleEffectsForMuzzleDevice.MuzzleDeviceEffectSizeMultiplier;
                    }
                }

                for (int j = 0; j < customMuzzleEffects.Length; j++)
                {
                    if (customMuzzleEffects[j].Entry != null)
                    {
                        if ((!customMuzzleEffects[j].EmitWhenGunHasMuzzleDevices && self.MuzzleDevices.Count == 0) || (!customMuzzleEffects[j].EmitWhenGunSuppressed && self.IsSuppressed())) continue;
                        MuzzleEffectConfig muzzleConfig = customMuzzleEffects[j].Entry;
                        MuzzleEffectSize muzzleEffectSize = overrideEffectSizeWithDefault ? muzzleEffectSize = self.DefaultMuzzleEffectSize : customMuzzleEffects[j].Size;
                        int muzzleEffectSizeAsIndex = (int)muzzleEffectSize;
                        GameObject muzzleEffectPrefab = GM.CurrentSceneSettings.IsSceneLowLight ? muzzleConfig.Prefabs_Lowlight[muzzleEffectSizeAsIndex] : muzzleConfig.Prefabs_Highlight[muzzleEffectSizeAsIndex];
                        Transform parent = customMuzzleEffects[j].OverridePoint ?? self.CurrentMuzzle.transform;
                        muzzleEffectPrefab = Instantiate(muzzleEffectPrefab, parent.position, parent.rotation, parent);

                        MuzzlePSystem muzzlePSystem = new()
                        {
                            PSystem = muzzleEffectPrefab.GetComponent<ParticleSystem>(),
                            OverridePoint = customMuzzleEffects[j].OverridePoint,
                            NumParticlesPerShot = GM.CurrentSceneSettings.IsSceneLowLight ? muzzleConfig.NumParticles_Lowlight[muzzleEffectSizeAsIndex] : muzzleConfig.NumParticles_Highlight[muzzleEffectSizeAsIndex]
                        };
                        
                        self.m_muzzleSystems.Add(muzzlePSystem);
                    }
                }

                float muzzleEffectMultiplierFinal = controller.UsesBarrelLengthDependentMuzzleEffectSizeCurve ? controller.MuzzleEffectSizeOverBarrelLength.Evaluate(Vector3.Distance(self.CurrentMuzzle.position, self.FChambers[0].transform.position)) * muzzleDevizeEffectMultiplier : controller.MuzzleEffectSizeMultiplier * muzzleDevizeEffectMultiplier;

                if (muzzleEffectMultiplierFinal != 1f)
                {
                    foreach (ParticleSystem MuzzleEffectSystem in self.m_muzzleSystems.Select(m => m.PSystem).ToArray())
                    {
                        ParticleSystem.MainModule main = MuzzleEffectSystem.main;
                        ParticleSystem.MinMaxCurve size = main.startSize;
                        size.constant *= muzzleEffectMultiplierFinal;
                        size.constantMax *= muzzleEffectMultiplierFinal;
                        size.constantMin *= muzzleEffectMultiplierFinal;

                        if (size.curve != null)
                        {
                            for (int i = 0; i < size.curve.keys.Length; i++)
                            {
                                size.curve.keys[i].value *= muzzleEffectMultiplierFinal;
                            }
                        }
                        if (size.curveMax != null)
                        {
                            for (int i = 0; i < size.curveMax.keys.Length; i++)
                            {
                                size.curveMax.keys[i].value *= muzzleEffectMultiplierFinal;
                            }
                        }
                        if (size.curveMin != null)
                        {
                            for (int i = 0; i < size.curveMin.keys.Length; i++)
                            {
                                size.curveMin.keys[i].value *= muzzleEffectMultiplierFinal;
                            }
                        }

                        main.startSize = size;
                    }
                }
            }
        }
#endif
        public void Awake()
        {
            _existingCustomMuzzleEffectsControllers.Add(FireArm, this);
        }

        public void Start()
        {
            // If the system wasn't hooked into the firearms code fast enough in Awake() due to code execution order, this will make sure that the effects are still applied.
            if (FireArm.m_muzzleSystems.Count < (FireArm.MuzzleEffects.Length + CustomMuzzleEffects.Length))
            {
                int count = FireArm.MuzzleDevices.Count;
                if (count > 0) FireArm.RegenerateMuzzleEffects(FireArm.MuzzleDevices[count - 1]);
                else FireArm.RegenerateMuzzleEffects(null);
            }
        }

        public void OnDestroy()
        {
            _existingCustomMuzzleEffectsControllers.Remove(FireArm);
        }
    }
}