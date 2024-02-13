using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;

namespace OpenScripts2 
{
	public class VanillaScopeMaterialCreator : OpenScripts2_BasePlugin
	{
		[Header("Amplifier Reference")]
		public Amplifier AmplifierComponent;

		[Header("H3VR/HolograpicSight Material Settings")]
		public Texture H3vrHolograpicSightTexture;
		[ColorUsage(true, true, 0f, float.MaxValue, 0f, float.MaxValue)]
		public Color H3vrHolograpicSightColor;
		public Vector4 H3vrHolograpicSightOffset = new();
		public float H3vrHolograpicSightSizeCompensation = 1f;
		public float H3vrHolograpicSightBlendIn = 0f;
		public float H3vrHolograpicSightBlendOut = 0f;

        [Header("Use context menu to enable prefab loaded scope")]
        [Header("and copy scope material properties in play mode.")]
        public GameObject PrefabLoadedScope;

		[HideInInspector]
		public static Shader HolographicSightShader
		{
			get
			{
                _h3vrHolograpicSightShader ??= Shader.Find(c_h3vrHolograpicSightShader);
				return _h3vrHolograpicSightShader;
            }
		}

        [HideInInspector]
        public static Material ScopeMaterial
        {
            get
            {
                _unlitScopeShader ??= Shader.Find(c_unlitScopeShader);
                _unlitScopeShaderMaterial ??= new Material(_unlitScopeShader);
                return _unlitScopeShaderMaterial;
            }
        }

        [HideInInspector]
        public static Material ScopeBlurMaterial
        {
            get
            {
                _hiddenScopeBlurShader ??= Shader.Find(c_hiddenScopeBlurShader);
                _hiddenScopeBlurMaterial ??= new Material(_hiddenScopeBlurShader);
                return _hiddenScopeBlurMaterial;
            }
        }

        private static Shader _h3vrHolograpicSightShader = null;
		private Material _h3vrHolograpicSightMaterial = null;

		// Holographic sight (reticle) shader path
		private const string c_h3vrHolograpicSightShader = "H3VR/HolograpicSight";
        // Shader property names
        private const string c_h3vrHolograpicSightMainTex = "_MainTex";
        private const string c_h3vrHolograpicSightMainColor = "_Color";
		private const string c_h3vrHolograpicSightOffset = "_Offset";
		private const string c_h3vrHolograpicSightSizeCompensation = "_SizeCompensation";
		private const string c_h3vrHolograpicSightBlendIn = "_BlendIn";
		private const string c_h3vrHolograpicSightBlendOut = "_BlendOut";
		private const int c_h3vrHolograpicSightRenderQueue = 3001;

		//[Header("Unlit/ScopeShader")]
		private static Shader _unlitScopeShader = null;
		private static Material _unlitScopeShaderMaterial = null;
        // Scope Shader path
        private const string c_unlitScopeShader = "Unlit/ScopeShader";

		//[Header("Hidden/ScopeBlur")]
		private static Shader _hiddenScopeBlurShader = null;
		private static Material _hiddenScopeBlurMaterial = null;
        // Scope Blur Shader path
        private const string c_hiddenScopeBlurShader = "Hidden/ScopeBlur";

		public void Awake() 
		{
            if (IsInEditor) return;

            ApplyMaterialsToAmplifier();
		}

        private void ApplyMaterialsToAmplifier()
        {
            // H3VR/HolograpicSight
            _h3vrHolograpicSightMaterial = new Material(HolographicSightShader);

            if (H3vrHolograpicSightTexture != null) _h3vrHolograpicSightMaterial.SetTexture(c_h3vrHolograpicSightMainTex, H3vrHolograpicSightTexture);
            _h3vrHolograpicSightMaterial.SetColor(c_h3vrHolograpicSightMainColor, H3vrHolograpicSightColor);
            _h3vrHolograpicSightMaterial.SetVector(c_h3vrHolograpicSightOffset, H3vrHolograpicSightOffset);
            _h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightSizeCompensation, H3vrHolograpicSightSizeCompensation);
            _h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightBlendIn, H3vrHolograpicSightBlendIn);
            _h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightBlendOut, H3vrHolograpicSightBlendOut);
            _h3vrHolograpicSightMaterial.renderQueue = c_h3vrHolograpicSightRenderQueue;

            ScopeCam scopeCam = AmplifierComponent.ScopeCam;
            scopeCam.GetComponent<MeshRenderer>().sharedMaterial = ScopeMaterial;
            // Hidden/Blur
            scopeCam.PostMaterial = ScopeBlurMaterial;
            // Unlit/ScopeShader
            scopeCam.Reticule.GetComponent<MeshRenderer>().sharedMaterial = _h3vrHolograpicSightMaterial;
        }

		public void OnDestroy()
        {
			Destroy(_h3vrHolograpicSightMaterial);
		}

        [ContextMenu("Copy scope material property values in play mode")]
        public void CopyScopeMaterialValues()
        {
            Material scopeMaterial = PrefabLoadedScope.GetComponentInChildren<Amplifier>(true).ScopeCam.Reticule.GetComponent<MeshRenderer>().sharedMaterial;

            H3vrHolograpicSightColor = scopeMaterial.GetColor(c_h3vrHolograpicSightMainColor);
            H3vrHolograpicSightOffset = scopeMaterial.GetVector(c_h3vrHolograpicSightOffset);
            H3vrHolograpicSightSizeCompensation = scopeMaterial.GetFloat(c_h3vrHolograpicSightSizeCompensation);
            H3vrHolograpicSightBlendIn = scopeMaterial.GetFloat(c_h3vrHolograpicSightBlendIn);
            H3vrHolograpicSightBlendOut = scopeMaterial.GetFloat(c_h3vrHolograpicSightBlendOut);
        }

        [ContextMenu("Enable scope if blacked out in play mode")]
		public void EnableScope()
		{
            Amplifier prefabLoadedAmplifier = PrefabLoadedScope.GetComponentInChildren<Amplifier>(true);

            prefabLoadedAmplifier.ScopeCam.MagnificationEnabled = true;
            _h3vrHolograpicSightMaterial = prefabLoadedAmplifier.ScopeCam.Reticule.GetComponent<MeshRenderer>().material;

            if (H3vrHolograpicSightTexture != null) _h3vrHolograpicSightMaterial.SetTexture(c_h3vrHolograpicSightMainTex, H3vrHolograpicSightTexture);
            _h3vrHolograpicSightMaterial.SetColor(c_h3vrHolograpicSightMainColor, H3vrHolograpicSightColor);
            _h3vrHolograpicSightMaterial.SetVector(c_h3vrHolograpicSightOffset, H3vrHolograpicSightOffset);
            _h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightSizeCompensation, H3vrHolograpicSightSizeCompensation);
            _h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightBlendIn, H3vrHolograpicSightBlendIn);
            _h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightBlendOut, H3vrHolograpicSightBlendOut);
            _h3vrHolograpicSightMaterial.renderQueue = c_h3vrHolograpicSightRenderQueue;

            _hiddenScopeBlurMaterial = prefabLoadedAmplifier.ScopeCam.PostMaterial;
            _unlitScopeShaderMaterial = prefabLoadedAmplifier.ScopeCam.GetComponent<MeshRenderer>().sharedMaterial;

            ScopeCam scopeCam = AmplifierComponent.ScopeCam;
            scopeCam.GetComponent<MeshRenderer>().sharedMaterial = _unlitScopeShaderMaterial;
            scopeCam.PostMaterial = _hiddenScopeBlurMaterial;
            scopeCam.Reticule.GetComponent<MeshRenderer>().sharedMaterial = _h3vrHolograpicSightMaterial;

			scopeCam.MagnificationEnabled = true;

            ModifiedScopeCamera modifiedScopeCamera = scopeCam.transform.root.GetComponentInChildren<ModifiedScopeCamera>(true);
			if (modifiedScopeCamera != null)
			{
				GameObject reticleCopy = Instantiate(scopeCam.Reticule);
				reticleCopy.transform.position = scopeCam.ScopeCamera.transform.position + scopeCam.transform.forward * modifiedScopeCamera.ReticleDistanceOverride;
                reticleCopy.transform.SetParent(scopeCam.Reticule.transform.parent);
                scopeCam.Reticule.SetActive(false);
            }

            AdvancedAmplifier advancedAmplifier = scopeCam.transform.root.GetComponentInChildren<AdvancedAmplifier>(true);
			if (advancedAmplifier != null && advancedAmplifier.ScopeTubeOverlayReticle != null)
			{
                Material _scopeTubeOverlayMaterial = scopeCam.Reticule.GetComponent<MeshRenderer>().material;

                _scopeTubeOverlayMaterial.SetTexture(c_h3vrHolograpicSightMainTex, advancedAmplifier.ScopeTubeOverlayTexture);
                _scopeTubeOverlayMaterial.SetColor(c_h3vrHolograpicSightMainColor, advancedAmplifier.ScopeTubeOverlayColor);
                _scopeTubeOverlayMaterial.SetVector(c_h3vrHolograpicSightOffset, advancedAmplifier.ScopeTubeOverlayOffset);
                _scopeTubeOverlayMaterial.SetFloat(c_h3vrHolograpicSightSizeCompensation, advancedAmplifier.ScopeTubeOverlaySizeCompensation);
                _scopeTubeOverlayMaterial.SetFloat(c_h3vrHolograpicSightBlendIn, advancedAmplifier.ScopeTubeOverlayBlendIn);
                _scopeTubeOverlayMaterial.SetFloat(c_h3vrHolograpicSightBlendOut, advancedAmplifier.ScopeTubeOverlayBlendOut);
                _scopeTubeOverlayMaterial.renderQueue = c_h3vrHolograpicSightRenderQueue;

                Renderer reticleRenderer = advancedAmplifier.ScopeTubeOverlayReticle.GetComponent<Renderer>();
                reticleRenderer.sharedMaterial = _scopeTubeOverlayMaterial;
            }
        }

        public void GiveShaders(Shader holo, Shader scope, Shader blur)
        {
            _h3vrHolograpicSightShader = holo;
            _unlitScopeShader = scope;
            _hiddenScopeBlurShader = blur;

            ApplyMaterialsToAmplifier();
            ScopeCam scopeCam = AmplifierComponent.ScopeCam;

            if (scopeCam != null)
            {
                ModifiedScopeCamera modifiedScopeCamera = scopeCam.transform.root.GetComponentInChildren<ModifiedScopeCamera>(true);
                if (modifiedScopeCamera != null)
                {
                    GameObject reticleCopy = Instantiate(scopeCam.Reticule);
                    reticleCopy.transform.position = scopeCam.ScopeCamera.transform.position + scopeCam.transform.forward * modifiedScopeCamera.ReticleDistanceOverride;
                    reticleCopy.transform.SetParent(scopeCam.Reticule.transform.parent);
                    scopeCam.Reticule.SetActive(false);
                }

                AdvancedAmplifier advancedAmplifier = scopeCam.transform.root.GetComponentInChildren<AdvancedAmplifier>(true);
                if (advancedAmplifier != null && advancedAmplifier.ScopeTubeOverlayReticle != null)
                {
                    Material _scopeTubeOverlayMaterial = scopeCam.Reticule.GetComponent<MeshRenderer>().material;

                    _scopeTubeOverlayMaterial.SetTexture(c_h3vrHolograpicSightMainTex, advancedAmplifier.ScopeTubeOverlayTexture);
                    _scopeTubeOverlayMaterial.SetColor(c_h3vrHolograpicSightMainColor, advancedAmplifier.ScopeTubeOverlayColor);
                    _scopeTubeOverlayMaterial.SetVector(c_h3vrHolograpicSightOffset, advancedAmplifier.ScopeTubeOverlayOffset);
                    _scopeTubeOverlayMaterial.SetFloat(c_h3vrHolograpicSightSizeCompensation, advancedAmplifier.ScopeTubeOverlaySizeCompensation);
                    _scopeTubeOverlayMaterial.SetFloat(c_h3vrHolograpicSightBlendIn, advancedAmplifier.ScopeTubeOverlayBlendIn);
                    _scopeTubeOverlayMaterial.SetFloat(c_h3vrHolograpicSightBlendOut, advancedAmplifier.ScopeTubeOverlayBlendOut);
                    _scopeTubeOverlayMaterial.renderQueue = c_h3vrHolograpicSightRenderQueue;

                    Renderer reticleRenderer = advancedAmplifier.ScopeTubeOverlayReticle.GetComponent<Renderer>();
                    reticleRenderer.sharedMaterial = _scopeTubeOverlayMaterial;
                }

                // Activate scope outside play mode:
                scopeCam.MagnificationEnabled = true;
            }
        }
	}
}