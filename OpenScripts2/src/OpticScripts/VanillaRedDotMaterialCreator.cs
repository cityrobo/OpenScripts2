using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;

namespace OpenScripts2 
{ 
	public class VanillaRedDotMaterialCreator : OpenScripts2_BasePlugin
	{
		[Header("RedDotSight")]
		public RedDotSight RedDotComponent;

		[Header("H3VR/HolograpicSight")]
		public Texture SightTexture;
		[ColorUsage(true, true, 0f, float.MaxValue, 0f, float.MaxValue)]
		public Color SightColor;
		public Vector4 SightOffset = new();
		public float SightSizeCompensation = 1f;
		public float SightBlendIn = 0f;
		public float SightBlendOut = 0f;

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
			// H3VR/HolograpicSight
			if (_h3vrHolograpicSightShader == null) _h3vrHolograpicSightShader = Shader.Find(c_h3vrHolograpicSightShader);
			_h3vrHolograpicSightMaterial = new Material(_h3vrHolograpicSightShader);

			if (SightTexture != null) _h3vrHolograpicSightMaterial.SetTexture(c_h3vrHolograpicSightMainTex, SightTexture);
			_h3vrHolograpicSightMaterial.SetColor(c_h3vrHolograpicSightMainColor, SightColor);
			_h3vrHolograpicSightMaterial.SetVector(c_h3vrHolograpicSightOffset, SightOffset);
			_h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightSizeCompensation, SightSizeCompensation);
			_h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightBlendIn, SightBlendIn);
			_h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightBlendOut, SightBlendOut);
			_h3vrHolograpicSightMaterial.renderQueue = c_h3vrHolograpicSightRenderQueue;

			Renderer reticleRenderer = RedDotComponent.BrightnessRend;
			reticleRenderer.material = _h3vrHolograpicSightMaterial;
		}

		public void OnDestroy()
        {
			Destroy(_h3vrHolograpicSightMaterial);
		}

		[ContextMenu("Copy Temporary Material")]
		public void CopyTemporaryMaterial()
		{
            Renderer reticleRenderer = RedDotComponent.BrightnessRend;
			SightTexture = reticleRenderer.sharedMaterial.GetTexture(c_h3vrHolograpicSightMainTex);
			SightColor = reticleRenderer.sharedMaterial.GetColor(c_h3vrHolograpicSightMainColor);
			SightOffset = reticleRenderer.sharedMaterial.GetVector(c_h3vrHolograpicSightOffset);
			SightSizeCompensation = reticleRenderer.sharedMaterial.GetFloat(c_h3vrHolograpicSightSizeCompensation);
            SightBlendIn = reticleRenderer.sharedMaterial.GetFloat(c_h3vrHolograpicSightBlendIn);
            SightBlendOut = reticleRenderer.sharedMaterial.GetFloat(c_h3vrHolograpicSightBlendOut);
        }
	}
}
