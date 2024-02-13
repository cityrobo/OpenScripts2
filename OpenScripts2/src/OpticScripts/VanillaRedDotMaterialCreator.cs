using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using UnityEngine.UI;

namespace OpenScripts2 
{
	public class VanillaRedDotMaterialCreator : OpenScripts2_BasePlugin
	{
		[Header("RedDotSight Reference")]
		public RedDotSight RedDotComponent;
        public Renderer AlternativeStandaloneRenderer;

		[Header("H3VR/HolograpicSight Material Settings")]
		public Texture SightTexture;
		[ColorUsage(true, true, 0f, float.MaxValue, 0f, float.MaxValue)]
		public Color SightColor;
		public Vector4 SightOffset = new();
		public float SightSizeCompensation = 1f;
		public float SightBlendIn = 0f;
		public float SightBlendOut = 0f;

        [Header("Use context menu to enable prefab loaded optic")]
        [Header("and copy optic material properties in play mode.")]
        public GameObject PrefabLoadedOptic;

        [HideInInspector]
        public static Shader HolographicSightShader
        {
            get
            {
                if (_h3vrHolograpicSightShader == null) _h3vrHolograpicSightShader = Shader.Find(c_h3vrHolograpicSightShader);
                return _h3vrHolograpicSightShader;
            }
        }

        private static Shader _h3vrHolograpicSightShader = null;
		private Material _h3vrHolograpicSightMaterial = null;
		private const string c_h3vrHolograpicSightShader = "H3VR/HolograpicSight";
		private const string c_h3vrHolograpicSightMainTex = "_MainTex";
		private const string c_h3vrHolograpicSightMainColor = "_Color";
		private const string c_h3vrHolograpicSightOffset = "_Offset";
		private const string c_h3vrHolograpicSightSizeCompensation = "_SizeCompensation";
		private const string c_h3vrHolograpicSightBlendIn = "_BlendIn";
		private const string c_h3vrHolograpicSightBlendOut = "_BlendOut";
		private const int c_h3vrHolograpicSightRenderQueue = 3001;

		public void Awake() 
		{
            if (IsInEditor) return;

            GiveMaterialsToRedDotSight();
		}

        private void GiveMaterialsToRedDotSight()
        {
            // H3VR/HolograpicSight
            _h3vrHolograpicSightMaterial = new Material(HolographicSightShader);

            if (SightTexture != null) _h3vrHolograpicSightMaterial.SetTexture(c_h3vrHolograpicSightMainTex, SightTexture);
            _h3vrHolograpicSightMaterial.SetColor(c_h3vrHolograpicSightMainColor, SightColor);
            _h3vrHolograpicSightMaterial.SetVector(c_h3vrHolograpicSightOffset, SightOffset);
            _h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightSizeCompensation, SightSizeCompensation);
            _h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightBlendIn, SightBlendIn);
            _h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightBlendOut, SightBlendOut);
            _h3vrHolograpicSightMaterial.renderQueue = c_h3vrHolograpicSightRenderQueue;

            Renderer reticleRenderer = RedDotComponent != null ? RedDotComponent.BrightnessRend : AlternativeStandaloneRenderer;
            if (reticleRenderer != null) reticleRenderer.sharedMaterial = _h3vrHolograpicSightMaterial;
            else LogError("Couldn't find ReticleRenderer!");
        }

		public void OnDestroy()
        {
			Destroy(_h3vrHolograpicSightMaterial);
		}

        [ContextMenu("Copy optic material property values in play mode")]
        public void CopyScopeMaterialValues()
        {
            Material redDotMaterial = PrefabLoadedOptic.GetComponentInChildren<RedDotSight>(true).BrightnessRend.sharedMaterial;

            SightColor = redDotMaterial.GetColor(c_h3vrHolograpicSightMainColor);
            SightOffset = redDotMaterial.GetVector(c_h3vrHolograpicSightOffset);
            SightSizeCompensation = redDotMaterial.GetFloat(c_h3vrHolograpicSightSizeCompensation);
            SightBlendIn = redDotMaterial.GetFloat(c_h3vrHolograpicSightBlendIn);
            SightBlendOut = redDotMaterial.GetFloat(c_h3vrHolograpicSightBlendOut);
        }

        [ContextMenu("Enable optic in play mode")]
        public void EnableOptic()
        {
            _h3vrHolograpicSightMaterial = Instantiate(PrefabLoadedOptic.GetComponentInChildren<RedDotSight>(true).BrightnessRend.sharedMaterial);

            if (SightTexture != null) _h3vrHolograpicSightMaterial.SetTexture(c_h3vrHolograpicSightMainTex, SightTexture);
            _h3vrHolograpicSightMaterial.SetColor(c_h3vrHolograpicSightMainColor, SightColor);
            _h3vrHolograpicSightMaterial.SetVector(c_h3vrHolograpicSightOffset, SightOffset);
            _h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightSizeCompensation, SightSizeCompensation);
            _h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightBlendIn, SightBlendIn);
            _h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightBlendOut, SightBlendOut);
            _h3vrHolograpicSightMaterial.renderQueue = c_h3vrHolograpicSightRenderQueue;

            if (RedDotComponent != null) RedDotComponent.BrightnessRend.sharedMaterial = _h3vrHolograpicSightMaterial;
            else if (AlternativeStandaloneRenderer != null) AlternativeStandaloneRenderer.sharedMaterial = _h3vrHolograpicSightMaterial;
            else Debug.LogError("Couldn't find ReticleRenderer!");
        }

        public void GiveShader(Shader holo)
        {
            _h3vrHolograpicSightShader = holo;

            GiveMaterialsToRedDotSight();
        }
    }
}
