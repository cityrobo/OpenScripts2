using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FistVR;

namespace OpenScripts2
{
    public class ReflexSightNVToggleButton : FVRInteractiveObject
    {
        [Header("ReflexSightNVToggleButton Settings")]
        public ReflexSightController ReflexSightController;

        public List<float> NVReticleIlluminations = new();
        public List<string> DisplayNames_NVReticleIlluminations = new();
        public int StartingNVReticleIlluminationIndex = 0;

        [Header("Optional")]
        public AudioEvent NVButtonSound = new();

        private List<float> _normalReticleIlluminations = new();
        private List<string> _normalDisplayNames_ReticleIlluminations = new();
        private int _normalModeIndex = 0;
        private int _nightVisionModeIndex = 0;

        private bool _isNVModeEnabled = false;

        private static readonly Dictionary<FVRFireArmAttachment, ReflexSightNVToggleButton> _existingReflexSightNVToggleButtons = new();

        private const string NVModeFlag = "NV_MODE_ENABLED";
        private const string VanillaModeIndexFlag = "ReticleIlluminationIndex";
        private const string NVModeIndexFlag = "NV_MODE_INDEX";


        public override void Awake()
        {
            base.Awake();

            _existingReflexSightNVToggleButtons.Add(ReflexSightController.Attachment, this);                                           
            _nightVisionModeIndex = StartingNVReticleIlluminationIndex;
            IsSimpleInteract = true;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            _existingReflexSightNVToggleButtons.Remove(ReflexSightController.Attachment);
        }

        public override void SimpleInteraction(FVRViveHand hand)
        {
            base.SimpleInteraction(hand);

            ToggleNVMode();
        }

        public void ToggleNVMode()
        {
            if (!_isNVModeEnabled)
            {
                _isNVModeEnabled = true;

                _normalModeIndex = ReflexSightController.ReticleIlluminationIndex;
                ReflexSightController.ReticleIlluminationIndex = _nightVisionModeIndex;

                _normalReticleIlluminations = ReflexSightController.ReticleIlluminations;
                ReflexSightController.ReticleIlluminations = NVReticleIlluminations;

                _normalDisplayNames_ReticleIlluminations = ReflexSightController.DisplayNames_ReticleIlluminations;
                ReflexSightController.DisplayNames_ReticleIlluminations = DisplayNames_NVReticleIlluminations;
            }
            else
            {
                _isNVModeEnabled = false;

                _nightVisionModeIndex = ReflexSightController.ReticleIlluminationIndex;
                ReflexSightController.ReticleIlluminationIndex = _normalModeIndex;

                ReflexSightController.ReticleIlluminations = _normalReticleIlluminations;
                ReflexSightController.DisplayNames_ReticleIlluminations = _normalDisplayNames_ReticleIlluminations;
            }
            if (NVButtonSound.Clips.Count == 0) SM.PlayCoreSound(FVRPooledAudioType.GenericClose, ManagerSingleton<SM>.Instance.AudioEvent_AttachmentClick_Minor, transform.position);
            else SM.PlayCoreSound(FVRPooledAudioType.GenericClose, NVButtonSound, transform.position);
            ReflexSightController.Zero();
            ReflexSightController.UI?.RedrawUI();
        }

        private void EnableNVModeAfterUnvault()
        {
            _normalModeIndex = ReflexSightController.ReticleIlluminationIndex;
            ReflexSightController.ReticleIlluminationIndex = _nightVisionModeIndex;

            _normalReticleIlluminations = ReflexSightController.ReticleIlluminations;
            ReflexSightController.ReticleIlluminations = NVReticleIlluminations;

            _normalDisplayNames_ReticleIlluminations = ReflexSightController.DisplayNames_ReticleIlluminations;
            ReflexSightController.DisplayNames_ReticleIlluminations = DisplayNames_NVReticleIlluminations;

            ReflexSightController.Zero();
        }

#if !DEBUG
        // Vaulting Code
        static ReflexSightNVToggleButton()
        {
            On.FistVR.FVRFireArmAttachment.ConfigureFromFlagDic += FVRFireArmAttachment_ConfigureFromFlagDic;
            On.FistVR.FVRFireArmAttachment.GetFlagDic += FVRFireArmAttachment_GetFlagDic;
        }

        private static Dictionary<string, string> FVRFireArmAttachment_GetFlagDic(On.FistVR.FVRFireArmAttachment.orig_GetFlagDic orig, FVRFireArmAttachment self)
        {
            var flagDic = orig(self);

            if (_existingReflexSightNVToggleButtons.TryGetValue(self, out var nvButton))
            {
                flagDic.Add(NVModeFlag, nvButton._isNVModeEnabled.ToString());
                flagDic.Add(NVModeIndexFlag, nvButton._nightVisionModeIndex.ToString());

                // Required due to me overwriting the vanilla ReticleIlluminationIndex with the NV one
                if (flagDic.ContainsKey(VanillaModeIndexFlag)) flagDic[VanillaModeIndexFlag] = nvButton._normalModeIndex.ToString();
            }

            return flagDic;
        }

        private static void FVRFireArmAttachment_ConfigureFromFlagDic(On.FistVR.FVRFireArmAttachment.orig_ConfigureFromFlagDic orig, FVRFireArmAttachment self, Dictionary<string, string> f)
        {
            orig(self, f);

            if (_existingReflexSightNVToggleButtons.TryGetValue(self, out var nvButton))
            {
                nvButton._isNVModeEnabled = f.TryGetValue(NVModeFlag, out var nvModeEnabled) && bool.Parse(nvModeEnabled);
                nvButton._nightVisionModeIndex = f.TryGetValue(NVModeIndexFlag, out var nvModeIndex) ? int.Parse(nvModeIndex) : nvButton.StartingNVReticleIlluminationIndex;

                if (nvButton._isNVModeEnabled)
                {
                    nvButton.EnableNVModeAfterUnvault();
                }
            }
        }
#endif
    }
}