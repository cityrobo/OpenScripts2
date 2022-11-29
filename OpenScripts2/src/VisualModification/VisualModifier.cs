using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using FistVR;

namespace OpenScripts2
{
    public class VisualModifier : OpenScripts2_BasePlugin
    {
        // Emission weight system
        [Header("Emission modification config")]
        public MeshRenderer MeshRenderer;
        [Tooltip("Index of the material in the MeshRenderer's materials list.")]
        public int MaterialIndex = 0;
        [Tooltip("Anton uses the squared value of the VisualModifier to determine the emission weight. If you wanna replicate that behavior, leave the value as is, but feel free to go crazy if you wanna mix things up.")]
        public float EmissionExponent = 2f;
        public float EmissionStartsAt = 0f;
        public bool AffectsEmissionWeight = true;
        public bool AffectsEmissionScrollSpeed = false;
        [Tooltip("Maximum left/right emission scroll speed.")]
        public float MaxEmissionScrollSpeed_X = 0f;
        [Tooltip("Maximum up/down emission scroll speed.")]
        public float MaxEmissionScrollSpeed_Y = 0f;
        [Tooltip("Enables emission weight AnimationCurve system.")]
        public bool EmissionUsesAdvancedCurve = false;
        [Tooltip("Advanced emission weight control. Values from 0 to 1 only!")]
        public AnimationCurve EmissionCurve = new();
        [Tooltip("Advanced left/right emission scroll speed control. The X-axis is clamped between 0 and 1 and represents the VisualModifier level. The value (Y-Axis) represents the actual scroll speed and is uncapped. The max value set above is ignored.")]
        public AnimationCurve EmissionScrollSpeedCurve_X = new();
        [Tooltip("Advanced up/down emission scroll speed control. The X-axis is clamped between 0 and 1 and represents the VisualModifier level. The value (Y-Axis) represents the actual scroll speed and is uncapped. The max value set above is ignored.")]
        public AnimationCurve EmissionScrollSpeedCurve_Y = new();

        // Detail weight system
        [Header("Detail weight config")]
        public bool AffectsDetailWeight = true;
        [Tooltip("Same as the normal VisualModifierExponent, but for the detail weight.")]
        public float DetailExponent = 2f;
        [Tooltip("Enables emission weight AnimationCurve system.")]
        public float DetailStartsAt = 0f;

        public bool DetailUsesAdvancedCurve = false;
        [Tooltip("Advanced emission weight control. Values from 0 to 1 only!")]
        public AnimationCurve DetailCurve = new();

        // Particle emission rate system
        [Header("Particle effects config")]
        public ParticleSystem ParticleSystem;
        public float MaxEmissionRate;
        [Tooltip("Same as the normal VisualModifierExponent, but for the particle emission rate.")]
        public float ParticleExponent = 2f;
        [Tooltip("VisualModifier level at which particles start appearing.")]
        public float ParticlesStartAt = 0f;
        [Tooltip("Enables particle rate AnimationCurve system.")]
        public bool ParticlesUsesAdvancedCurve = false;
        [Tooltip("Advanced particle rate control. Values from 0 to 1 only! The X-axis is clamped between 0 and 1 and represents the VisualModifier level. The value (Y-axis) acts like a multiplier of the max emission rate, clamped between 0 and 1.")]
        public AnimationCurve ParticlesCurve = new();

        // Sound volume system
        [Header("Sound effects config")]
        public AudioClip SoundEffect;
        [Tooltip("Place a preconfigured AudioSource in here. (Configuring AudioSources at runtime is a pain! This lets you much easier choose the desired settings as well.)")]
        public AudioSource SoundEffectSource;
        [Tooltip("Same as the Emission Exponent, but for the sound volume.")]
        public float SoundExponent = 2f;
        public float MaxVolume = 0.4f;
        [Tooltip("VisualModifier level at which audio starts.")]
        public float SoundStartsAt = 0f;
        [Tooltip("Enables sound volume AnimationCurve system.")]
        public bool SoundUsesAdvancedCurve = false;
        [Tooltip("Advanced sound volume control. Values from 0 to 1 only!. The X-axis is clamped between 0 and 1 and represents the VisualModifier level. The value (Y-axis) is the volume at that VisualModifier level.")]
        public AnimationCurve VolumeCurve = new();

        // Movement System
        public ManipulateTransforms.TransformManipulationDefinition ObjectToMove;
        public float MovementExponent = 1f;
        public float MovementStartsAt = 0f;
        public bool MovementUsesAdvancedCurve = false;
        public AnimationCurve MovementCurve = new();

        // Animation system
        public Animator Animator;
        public string AnimationNodeName;
        public float AnimationExponent = 1f;
        public float AnimationStartsAt = 0f;
        public bool AnimationUsesAdvancedCurve = false;
        public AnimationCurve AnimationCurve = new();

        // Debugging system
        [Header("Debug Messages")]
        public bool ConstantUpdate = false;
        [Range(0f,1f)]
        public float InputValue = 0f;

        // Private variables
        private Material _copyMaterial;

        private bool _soundEnabled = false;

        private float _evaluatedValue;

        // Constants
        private const string c_EmissionWeightPropertyString = "_EmissionWeight";
        private const string c_IncandescenceScrollSpeedPropertyString = "_IncandescenceMapVelocity";
        private const string c_DetailWeightPropertyString = "_DetailWeight";


        public void Awake()
        {
            if (MeshRenderer != null)
            {
                _copyMaterial = MeshRenderer.materials[MaterialIndex];
            }

            if (ParticleSystem != null) ChangeParticleEmissionRate(0f);

            if (SoundEffectSource != null && SoundEffect != null)
            {
                SoundEffectSource.loop = true;
                SoundEffectSource.clip = SoundEffect;
                SoundEffectSource.volume = 0f;
                SoundEffectSource.Stop();
            }

            if (Animator != null)
            {
                Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }
		public void OnDestroy()
        {
            if (_copyMaterial != null) Destroy(_copyMaterial);
        }

        public void Update()
        {
            if (ConstantUpdate) UpdateVisualEffects(InputValue);
        }

        public void UpdateVisualEffects(float Input)
        {
            InputValue = Input;
            // Emission and Detail
            if (MeshRenderer != null)
            {
                if (AffectsEmissionWeight)
                {
                    _evaluatedValue = InputValue > EmissionStartsAt ? !EmissionUsesAdvancedCurve ? Mathf.Pow(InputValue, EmissionExponent) : EmissionCurve.Evaluate(InputValue) : 0f;
                    _copyMaterial.SetFloat(c_EmissionWeightPropertyString, _evaluatedValue);
                }
                if (AffectsEmissionScrollSpeed)
                {
                    Vector4 ScrollSpeed = Vector4.zero;
                    ScrollSpeed.x = InputValue > EmissionStartsAt ? !EmissionUsesAdvancedCurve ? Mathf.Lerp(0f, MaxEmissionScrollSpeed_X, _evaluatedValue) : EmissionScrollSpeedCurve_X.Evaluate(InputValue) : 0f;
                    ScrollSpeed.y = InputValue > EmissionStartsAt ? !EmissionUsesAdvancedCurve ? Mathf.Lerp(0f, MaxEmissionScrollSpeed_Y, _evaluatedValue) : EmissionScrollSpeedCurve_Y.Evaluate(InputValue) : 0f;
                    _copyMaterial.SetVector(c_IncandescenceScrollSpeedPropertyString, ScrollSpeed);
                }
                if (AffectsDetailWeight)
                {
                    _evaluatedValue = InputValue > DetailStartsAt ? !DetailUsesAdvancedCurve ? Mathf.Pow(InputValue, DetailExponent) : DetailCurve.Evaluate(InputValue) : 0f;
                    _copyMaterial.SetFloat(c_DetailWeightPropertyString, _evaluatedValue);
                }
            }

            // Particles
            if (ParticleSystem != null)
            {
                if (!ParticlesUsesAdvancedCurve)
                {
                    _evaluatedValue = InputValue > ParticlesStartAt ? Mathf.Pow(Mathf.InverseLerp(ParticlesStartAt, 1f, InputValue), ParticleExponent) : 0f;
                }
                else
                {
                    _evaluatedValue = ParticlesCurve.Evaluate(InputValue) * MaxEmissionRate;
                }
                ChangeParticleEmissionRate(_evaluatedValue);
            }

            // Sounds
            if (SoundEffect != null)
            {
                _evaluatedValue = !SoundUsesAdvancedCurve
                    ? Mathf.Lerp(0f, MaxVolume, InputValue > SoundStartsAt ? Mathf.Pow(Mathf.InverseLerp(SoundStartsAt, 1f, InputValue), SoundExponent) : 0f)
                    : Mathf.Lerp(0f, MaxVolume, VolumeCurve.Evaluate(InputValue));

                if (_evaluatedValue > 0f)
                {
                    if (!_soundEnabled)
                    {
                        SoundEffectSource.Play();
                        _soundEnabled = true;
                    }
                    SoundEffectSource.volume = _evaluatedValue;
                }
                else if (_soundEnabled)
                {
                    SoundEffectSource.Stop();
                    _soundEnabled = false;
                }
            }

            // Movement
            if (ObjectToMove != null && ObjectToMove.ManipulatedTransform != null)
            {
                _evaluatedValue = !MovementUsesAdvancedCurve ? Mathf.Pow(InputValue > MovementStartsAt ? Mathf.InverseLerp(MovementStartsAt, 1f, InputValue) : 0f, MovementExponent) : MovementCurve.Evaluate(InputValue);
                ObjectToMove.SetLerp(_evaluatedValue);
            }

            // Animation
            if (Animator != null)
            {
                _evaluatedValue = !AnimationUsesAdvancedCurve ? Mathf.Pow(InputValue > AnimationStartsAt ? Mathf.InverseLerp(AnimationStartsAt, 1f, InputValue) : 0f, AnimationExponent) : AnimationCurve.Evaluate(InputValue);
                Animator.Play(AnimationNodeName, -1, _evaluatedValue);
            }
        }

        private void ChangeParticleEmissionRate(float VisualModifier)
        {
            float particleEmissionRate = Mathf.Lerp(0f, MaxEmissionRate, Mathf.Pow(VisualModifier, ParticleExponent));
            ParticleSystem.EmissionModule emission = ParticleSystem.emission;
            ParticleSystem.MinMaxCurve rateOverTime = emission.rateOverTime;
            rateOverTime.mode = ParticleSystemCurveMode.Constant;
            rateOverTime.constantMax = particleEmissionRate;
            rateOverTime.constantMin = particleEmissionRate;
            emission.rateOverTime = rateOverTime;
        }
    }
}
