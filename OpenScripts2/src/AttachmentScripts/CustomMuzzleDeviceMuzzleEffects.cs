using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace OpenScripts2
{
    public class CustomMuzzleEffectsForMuzzleDevice : MonoBehaviour
    {
        public MuzzleDevice MuzzleDevice;
        public CustomMuzzleEffect[] CustomMuzzleEffects;
        public float MuzzleDeviceEffectSizeMultiplier = 1f;

        private static readonly List<FVRFireArmAttachment> _existingCustomMuzzleEffectsForMuzzleDevices = new();
        public void Awake()
        {
            _existingCustomMuzzleEffectsForMuzzleDevices.Add(MuzzleDevice);
        }

        public void OnDestroy()
        {
            _existingCustomMuzzleEffectsForMuzzleDevices.Remove(MuzzleDevice);
        }

#if !DEBUG
        static CustomMuzzleEffectsForMuzzleDevice()
        {
            On.FistVR.FVRFireArmAttachment.AttachToMount += FVRFireArmAttachment_AttachToMount;
        }

        private static void FVRFireArmAttachment_AttachToMount(On.FistVR.FVRFireArmAttachment.orig_AttachToMount orig, FVRFireArmAttachment self, FVRFireArmAttachmentMount m, bool playSound)
        {
            if (_existingCustomMuzzleEffectsForMuzzleDevices.Contains(self))
            {
                FVRFireArm fireArm = m.GetRootMount().MyObject as FVRFireArm;

                if (fireArm != null && !fireArm.TryGetComponentInChildren<CustomMuzzleEffectsController>(out _))
                {
                    fireArm.gameObject.AddComponent<CustomMuzzleEffectsController>();
                }
            }

            orig(self, m, playSound);
        }
#endif
    }
}