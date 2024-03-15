using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace OpenScripts2
{
    public class AdvancedAmplifier : OpenScripts2_BasePlugin
    {
        [Header("Advanced Amplifier Config")]
        public Amplifier AmplifierComponent;
        public AudioEvent SettingsChangedAudioEvent;

        [Header("Brightness Change Settings")]
        public Transform BrightnessNob;
        public Axis BrightnessNobAxis;
        public TransformType BrightnessNobTransformType;

        [Serializable]
        public class BrightnessSetting
        {
            [ColorUsage(true, true, 0f, float.MaxValue, 0f, float.MaxValue)]
            public Color Color;
            public float KnobTransformValue;
            public string Text;
        }

        public BrightnessSetting[] BrightnessSettings;
        public int SelectedBrightnessSetting;
        public bool EndlessBrightnessCycling;

        [Header("Reticle Change Settings")]
        public Transform ReticleNob;
        public Axis ReticleNobAxis;
        public TransformType ReticleNobTransformType;

        [Serializable]
        public class ReticleSetting
        {
            public Texture2D Reticle;
            public float KnobTransformValue;
            public string Text;
        }

        public ReticleSetting[] ReticleSettings;
        public int SelectedReticleSetting;
        public bool EndlessReticleCycling;

        [Header("Scope Tube Overlay")]
        [Tooltip("A secondary reticle that is used as an overlay to simulate a scope tube. Setup like the primary reticle, just with the \"tube like\" shadow overlay texture instead of a reticle.")]
        public GameObject ScopeTubeOverlayReticle;
        public Texture ScopeTubeOverlayTexture;
        [ColorUsage(true, true, 0f, float.MaxValue, 0f, float.MaxValue)]
        public Color ScopeTubeOverlayColor;
        public Vector4 ScopeTubeOverlayOffset = new();
        public float ScopeTubeOverlaySizeCompensation = 1f;
        public float ScopeTubeOverlayBlendIn = 0f;
        public float ScopeTubeOverlayBlendOut = 0f;

        [Header("Linear Zoom Ring")]
        public LinearZoomRing ZoomRing;

        private Material _scopeTubeOverlayMaterial = null;

        private HolographicSight _scopeTubeOverlayHolographicSightComponent;
        private float _scopeTubeOverlayStartScaleNormalized;

        private HolographicSight _holographicSightComponent;

        private static readonly Dictionary<Amplifier, AdvancedAmplifier> _existingAdvancedAmplifiers = new();
        private static readonly Dictionary<HolographicSight, AdvancedAmplifier> _existingAdvancedAmplifiersHolographicSight = new();

        // Shader Property Constants
        //private const string c_h3vrHolograpicSightShader = "H3VR/HolograpicSight";
        private const string c_h3vrHolograpicSightMainTex = "_MainTex";
        private const string c_h3vrHolograpicSightMainColor = "_Color";
        private const string c_h3vrHolograpicSightOffset = "_Offset";
        private const string c_h3vrHolograpicSightSizeCompensation = "_SizeCompensation";
        private const string c_h3vrHolograpicSightBlendIn = "_BlendIn";
        private const string c_h3vrHolograpicSightBlendOut = "_BlendOut";
        private const int c_h3vrHolograpicSightRenderQueue = 3001;

        public void Awake()
        {
            _existingAdvancedAmplifiers.Add(AmplifierComponent, this);
            if (AmplifierComponent.ScopeCam.Reticule != null)
            {
                _holographicSightComponent = AmplifierComponent.ScopeCam.Reticule.GetComponent<HolographicSight>();
                _existingAdvancedAmplifiersHolographicSight.Add(_holographicSightComponent, this);
            }

            SelectedBrightnessSetting = Mathf.Clamp(SelectedBrightnessSetting, 0, BrightnessSettings.Length - 1);
            SelectedReticleSetting = Mathf.Clamp(SelectedReticleSetting, 0, ReticleSettings.Length - 1);

            if (ScopeTubeOverlayReticle != null)
            {
                _scopeTubeOverlayMaterial = new Material(VanillaScopeMaterialCreator.HolographicSightShader);

                _scopeTubeOverlayMaterial.SetTexture(c_h3vrHolograpicSightMainTex, ScopeTubeOverlayTexture);
                _scopeTubeOverlayMaterial.SetColor(c_h3vrHolograpicSightMainColor, ScopeTubeOverlayColor);
                _scopeTubeOverlayMaterial.SetVector(c_h3vrHolograpicSightOffset, ScopeTubeOverlayOffset);
                _scopeTubeOverlayMaterial.SetFloat(c_h3vrHolograpicSightSizeCompensation, ScopeTubeOverlaySizeCompensation);
                _scopeTubeOverlayMaterial.SetFloat(c_h3vrHolograpicSightBlendIn, ScopeTubeOverlayBlendIn);
                _scopeTubeOverlayMaterial.SetFloat(c_h3vrHolograpicSightBlendOut, ScopeTubeOverlayBlendOut);
                _scopeTubeOverlayMaterial.renderQueue = c_h3vrHolograpicSightRenderQueue;

                Renderer reticleRenderer = ScopeTubeOverlayReticle.GetComponent<Renderer>();
                reticleRenderer.sharedMaterial = _scopeTubeOverlayMaterial;

                _scopeTubeOverlayHolographicSightComponent = ScopeTubeOverlayReticle.GetComponent<HolographicSight>();
            }
        }

        public void Start()
        {
            if (ScopeTubeOverlayReticle != null)
            {
                float magnificationAtStart = ZoomRing == null ? AmplifierComponent.ZoomSettings[AmplifierComponent.m_zoomSettingIndex].Magnification : ZoomRing.GetCurrentlySelectedMagnification();
                _scopeTubeOverlayStartScaleNormalized = _scopeTubeOverlayHolographicSightComponent.Scale * magnificationAtStart;
            }
            AmplifierComponent.UI.UpdateUI(AmplifierComponent);
        }

        public void OnDestroy()
        {
            _existingAdvancedAmplifiers.Remove(AmplifierComponent);
            if (AmplifierComponent.ScopeCam.Reticule != null) _existingAdvancedAmplifiersHolographicSight.Remove(_holographicSightComponent);
        }

        public void Update()
        {
            ScopeTubeOverlayReticle?.SetActive(AmplifierComponent.ScopeCam.MagnificationEnabled);

            if (ZoomRing != null)
            {
                AmplifierComponent.ScopeCam.Magnification = ZoomRing.GetCurrentlySelectedMagnification();
                UpdateScopeTubeOverlay();
                AmplifierComponent.UI.UpdateUI(AmplifierComponent);
            }
        }

        #region Brightness Changing
        public void BrightnessUp(bool cycle)
        {
            SelectedBrightnessSetting++;
            SelectedBrightnessSetting = (cycle || EndlessBrightnessCycling) && SelectedBrightnessSetting >= BrightnessSettings.Length
                ? 0
                : Mathf.Clamp(SelectedBrightnessSetting, 0, BrightnessSettings.Length - 1);
            BrightnessSetting setting = BrightnessSettings[SelectedBrightnessSetting];
            BrightnessNob?.ModifyLocalTransform(BrightnessNobTransformType, BrightnessNobAxis, setting.KnobTransformValue);
        }

        public void BrightnessDown()
        {
            SelectedBrightnessSetting--;
            SelectedBrightnessSetting = EndlessBrightnessCycling && SelectedBrightnessSetting < 0 
                ? BrightnessSettings.Length - 1
                : Mathf.Clamp(SelectedBrightnessSetting, 0, BrightnessSettings.Length - 1);
            BrightnessSetting setting = BrightnessSettings[SelectedBrightnessSetting];
            BrightnessNob?.ModifyLocalTransform(BrightnessNobTransformType, BrightnessNobAxis, setting.KnobTransformValue);
        }
        #endregion

        #region Reticle Changing
        public void ReticleUp(bool cycle)
        {
            SelectedReticleSetting++;
            SelectedReticleSetting = (cycle || EndlessReticleCycling) && SelectedReticleSetting >= ReticleSettings.Length
                ? 0
                : Mathf.Clamp(SelectedReticleSetting, 0, ReticleSettings.Length - 1);
            ReticleSetting setting = ReticleSettings[SelectedReticleSetting];
            ReticleNob?.ModifyLocalTransform(ReticleNobTransformType, ReticleNobAxis, setting.KnobTransformValue);
        }

        public void ReticleDown()
        {
            SelectedReticleSetting--;
            SelectedReticleSetting = EndlessReticleCycling && SelectedReticleSetting < 0
                ? ReticleSettings.Length - 1
                : Mathf.Clamp(SelectedReticleSetting, 0, ReticleSettings.Length - 1);
            ReticleSetting setting = ReticleSettings[SelectedReticleSetting];
            ReticleNob?.ModifyLocalTransform(ReticleNobTransformType, ReticleNobAxis, setting.KnobTransformValue);
        }
        #endregion

        private void UpdateScopeTubeOverlay()
        {
            if (_scopeTubeOverlayHolographicSightComponent != null)
            {
                float currentMagnification = ZoomRing == null ? AmplifierComponent.ZoomSettings[AmplifierComponent.m_zoomSettingIndex].Magnification : ZoomRing.GetCurrentlySelectedMagnification();
                _scopeTubeOverlayHolographicSightComponent.Scale = _scopeTubeOverlayStartScaleNormalized / currentMagnification;
            }
        }

        #region Game Code Patches
#if !DEBUG
        static AdvancedAmplifier()
        {
            On.FistVR.Amplifier.SetCurSettingUp += Amplifier_SetCurSettingUp;
            On.FistVR.Amplifier.SetCurSettingDown += Amplifier_SetCurSettingDown;
            On.HolographicSight.OnWillRenderObject += HolographicSight_OnWillRenderObject;
            On.FistVR.OpticUI.UpdateTextAndArrows_Amplifier_int += OpticUI_UpdateTextAndArrows_Amplifier_int;
        }

        private static void Amplifier_SetCurSettingUp(On.FistVR.Amplifier.orig_SetCurSettingUp orig, Amplifier self, bool cycle)
        {
            orig(self, cycle);

            if (_existingAdvancedAmplifiers.TryGetValue(self, out var advancedAmplifier)) 
            {
                switch (self.OptionTypes[self.CurSelectedOptionIndex])
                {
                    case OpticOptionType.Zero:
                        // Handled by vanilla script
                        break;
                    case OpticOptionType.Magnification:
                        advancedAmplifier.UpdateScopeTubeOverlay();
                        break;
                    case OpticOptionType.ReticleLum:
                        advancedAmplifier.BrightnessUp(cycle);
                        break;
                    case OpticOptionType.ReticleType:
                        advancedAmplifier.ReticleUp(cycle);
                        break;
                    case OpticOptionType.FlipState:
                        // Handled by vanilla script
                        break;
                    case OpticOptionType.ElevationTweak:
                        // Handled by vanilla script
                        break;
                    case OpticOptionType.WindageTweak:
                        // Handled by vanilla script
                        break;
                    case OpticOptionType.AdjustableSightDistance:
                        // No idea
                        break;
                }
                
                SM.PlayGenericSound(advancedAmplifier.SettingsChangedAudioEvent, self.transform.position);
            }
        }

        private static void Amplifier_SetCurSettingDown(On.FistVR.Amplifier.orig_SetCurSettingDown orig, Amplifier self)
        {
            orig(self);

            if (_existingAdvancedAmplifiers.TryGetValue(self, out var advancedAmplifier))
            {
                switch (self.OptionTypes[self.CurSelectedOptionIndex])
                {
                    case OpticOptionType.Zero:
                        // Handled by vanilla script
                        break;
                    case OpticOptionType.Magnification:
                        advancedAmplifier.UpdateScopeTubeOverlay();
                        break;
                    case OpticOptionType.ReticleLum:
                        advancedAmplifier.BrightnessDown();
                        break;
                    case OpticOptionType.ReticleType:
                        advancedAmplifier.ReticleDown();
                        break;
                    case OpticOptionType.FlipState:
                        // Handled by vanilla script
                        break;
                    case OpticOptionType.ElevationTweak:
                        // Handled by vanilla script
                        break;
                    case OpticOptionType.WindageTweak:
                        // Handled by vanilla script
                        break;
                    case OpticOptionType.AdjustableSightDistance:
                        // No idea
                        break;
                }

                SM.PlayGenericSound(advancedAmplifier.SettingsChangedAudioEvent, self.transform.position);
            }
        }

        private static void HolographicSight_OnWillRenderObject(On.HolographicSight.orig_OnWillRenderObject orig, HolographicSight self)
        {
            if (_existingAdvancedAmplifiersHolographicSight.TryGetValue(self, out var advancedAmplifier))
            {
                if (advancedAmplifier.BrightnessSettings.Length > 0)
                {
                    self.m_block.SetColor(c_h3vrHolograpicSightMainColor, advancedAmplifier.BrightnessSettings[advancedAmplifier.SelectedBrightnessSetting].Color);
                }
                if (advancedAmplifier.ReticleSettings.Length > 0)
                {
                    self.m_block.SetTexture(c_h3vrHolograpicSightMainTex, advancedAmplifier.ReticleSettings[advancedAmplifier.SelectedReticleSetting].Reticle);
                }
            }

            orig(self);
        }

        private static void OpticUI_UpdateTextAndArrows_Amplifier_int(On.FistVR.OpticUI.orig_UpdateTextAndArrows_Amplifier_int orig, OpticUI self, Amplifier A, int index)
        {
            orig(self, A, index);
            if (_existingAdvancedAmplifiers.TryGetValue(A, out var advancedAmplifier))
            {
                if (index >= A.OptionTypes.Count)
                {
                    self.SettingNames[index].text = string.Empty;
                    return;
                }

                switch (A.OptionTypes[index])
                {
                    case OpticOptionType.Zero:
                        // Handled by vanilla script
                        break;
                    case OpticOptionType.Magnification:
                        // Handled by vanilla script

                        if (advancedAmplifier.ZoomRing != null)
                        {
                            self.SettingNames[index].text = "Magnification: " + advancedAmplifier.ZoomRing.GetCurrentlySelectedMagnification().ToString("F1") + "x";
                        }
                        break;
                    case OpticOptionType.ReticleLum:
                        string brightnessText = advancedAmplifier.BrightnessSettings[advancedAmplifier.SelectedBrightnessSetting].Text;
                        if (brightnessText == string.Empty) brightnessText = advancedAmplifier.SelectedBrightnessSetting.ToString();
                        self.SettingNames[index].text = $"Brightness:  {brightnessText}";
                        self.Arrow_Left.SetActive(true);
                        self.Arrow_Right.SetActive(true);
                        break;
                    case OpticOptionType.ReticleType:
                        string reticleText = advancedAmplifier.ReticleSettings[advancedAmplifier.SelectedReticleSetting].Text;
                        if (reticleText == string.Empty) reticleText = advancedAmplifier.SelectedBrightnessSetting.ToString();
                        self.SettingNames[index].text = $"Reticle:  {reticleText}";
                        self.Arrow_Left.SetActive(true);
                        self.Arrow_Right.SetActive(true);
                        break;
                    case OpticOptionType.FlipState:
                        // Ugh... Doesn't really need a UI entry?
                        break;
                    case OpticOptionType.ElevationTweak:
                        // Handled by vanilla script
                        break;
                    case OpticOptionType.WindageTweak:
                        // Handled by vanilla script
                        break;
                    case OpticOptionType.AdjustableSightDistance:
                        // No idea
                        break;
                }
            }
        }
#endif
        #endregion
    }
}