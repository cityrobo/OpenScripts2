using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;

namespace OpenScripts2 
{ 
	public class VanillaScopeMaterialCreator : OpenScripts2_BasePlugin
	{
		[Header("Amplifier")]
		public Amplifier AmplifierComponent;

		[Header("H3VR/HolograpicSight")]
		public Texture H3vrHolograpicSightTexture;
		[ColorUsage(true, true, 0f, float.MaxValue, 0f, float.MaxValue)]
		public Color H3vrHolograpicSightColor;
		public Vector4 H3vrHolograpicSightOffset = new Vector4();
		public float H3vrHolograpicSightSizeCompensation = 1f;
		public float H3vrHolograpicSightBlendIn = 0f;
		public float H3vrHolograpicSightBlendOut = 0f;

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

		//[Header("Unlit/ScopeShader")]
		//public Texture UnlitScopeShaderTexture;

		private static Shader _unlitScopeShader = null;
		private static Material _unlitScopeShaderMaterial = null;
		private const string c_unlitScopeShader = "Unlit/ScopeShader";
		// private const string c_unlitScopeShaderMainTex = "_MainTex";

		//[Header("Hidden/ScopeBlur")]
		//public Texture HiddenScopeBlurTexture;

		private static Shader _hiddenScopeBlurShader = null;
		private static Material _hiddenScopeBlurMaterial = null;
		private const string c_hiddenScopeBlurShader = "Hidden/ScopeBlur";
		// private const string c_hiddenScopeBlurMainTex = "_MainTex";

		public void Awake() 
		{
			// H3VR/HolograpicSight
			if (_h3vrHolograpicSightShader == null) _h3vrHolograpicSightShader = Shader.Find(c_h3vrHolograpicSightShader);
			_h3vrHolograpicSightMaterial = new Material(_h3vrHolograpicSightShader);

			if (H3vrHolograpicSightTexture != null) _h3vrHolograpicSightMaterial.SetTexture(c_h3vrHolograpicSightMainTex, H3vrHolograpicSightTexture);
			_h3vrHolograpicSightMaterial.SetColor(c_h3vrHolograpicSightMainColor, H3vrHolograpicSightColor);
			_h3vrHolograpicSightMaterial.SetVector(c_h3vrHolograpicSightOffset, H3vrHolograpicSightOffset);
			_h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightSizeCompensation, H3vrHolograpicSightSizeCompensation);
			_h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightBlendIn, H3vrHolograpicSightBlendIn);
			_h3vrHolograpicSightMaterial.SetFloat(c_h3vrHolograpicSightBlendOut, H3vrHolograpicSightBlendOut);
			_h3vrHolograpicSightMaterial.renderQueue = c_h3vrHolograpicSightRenderQueue;

			// Unlit/ScopeShader
			if (_unlitScopeShaderMaterial == null)
			{
				if (_unlitScopeShader == null) _unlitScopeShader = Shader.Find(c_unlitScopeShader);
				_unlitScopeShaderMaterial = new Material(_unlitScopeShader);

				// if (UnlitScopeShaderTexture != null) _unlitScopeShaderMaterial.SetTexture(c_unlitScopeShaderMainTex, UnlitScopeShaderTexture);
			}

			// Hidden/ScopeBlur
			if (_hiddenScopeBlurMaterial == null)
			{
				if (_hiddenScopeBlurShader == null) _hiddenScopeBlurShader = Shader.Find(c_hiddenScopeBlurShader);
				_hiddenScopeBlurMaterial = new Material(_hiddenScopeBlurShader);

				// if (HiddenScopeBlurTexture != null) _hiddenScopeBlurMaterial.SetTexture(c_hiddenScopeBlurMainTex, HiddenScopeBlurTexture);
			}

			ScopeCam scopeCam = AmplifierComponent.ScopeCam;
			scopeCam.GetComponent<MeshRenderer>().sharedMaterial = _unlitScopeShaderMaterial;
			scopeCam.PostMaterial = _hiddenScopeBlurMaterial;
			scopeCam.Reticule.GetComponent<MeshRenderer>().sharedMaterial = _h3vrHolograpicSightMaterial;
		}

		public void OnDestroy()
        {
			Destroy(_h3vrHolograpicSightMaterial);
			// Destroy(_hiddenScopeBlurMaterial);
			// Destroy(_unlitScopeShaderMaterial);
		}
	}
}
