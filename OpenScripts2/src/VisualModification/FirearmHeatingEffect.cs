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
    public class FirearmHeatingEffect : OpenScripts2_BasePlugin
    {
        // Public Variables
        [Header("Firearm Heating Effect")]
        [Tooltip("Use this if you plan to use the script on a firearm.")]
        public FVRFireArm FireArm;
        [Tooltip("Use this if you plan to use the script on an attachment.")]
        public FVRFireArmAttachment Attachment;

        // Heat System
        [Header("Heat system config")]
        public MeshRenderer MeshRenderer;
        [Tooltip("Index of the material in the MeshRenderer's materials list.")]
        public int MaterialIndex = 0;
        [Header("Heating Effect Config")]
        [Tooltip("Heat added to the effect per shot fired. Heat caps out at 1.")]
        public float HeatPerShot = 0.01f;
        [Tooltip("Heat removed per second.")]
        public float CooldownRate = 0.01f;

        // Emission weight system
        [Header("Emission weight config")]
        [Tooltip("Anton uses the squared value of the heat to determine the emission weight. If you wanna replicate that behavior, leave the value as is, but feel free to go crazy if you wanna mix things up.")]
        public float HeatExponent = 2f;
        [Tooltip("Enables emission weight AnimationCurve system.")]
        public bool EmissionUsesAdvancedCurve = false;
        [Tooltip("Advanced emission weight control. Values from 0 to 1 only!")]
        public AnimationCurve EmissionCurve;

        // Detail weight system
        [Header("Detail weight config")]
        [Tooltip("Same as the normal HeatExponent, but for the detail weight.")]
        public float DetailExponent = 2f;
        [Tooltip("Enables emission weight AnimationCurve system.")]
        public bool DetailUsesAdvancedCurve = false;
        [Tooltip("Advanced emission weight control. Values from 0 to 1 only!")]
        public AnimationCurve DetailCurve;

        // Particle emission rate system
        [Header("Particle effects config")]
        public ParticleSystem ParticleSystem;
        public float MaxEmissionRate;
        [Tooltip("Same as the normal HeatExponent, but for the particle emission rate.")]
        public float ParticleHeatExponent = 2f;
        [Tooltip("Heat level at which particles start appearing.")]
        public float ParticleHeatThreshold = 0f;
        [Tooltip("If checked, the particle emission rate starts at 0 when hitting the threshold and hits the max when heat is 1, else it starts emitting using the threshold heat value as a reference, aka the heat level it gets enabled at.")]
        public bool ParticleEmissionRateStartsAtZero = false;
        [Tooltip("Enables particle rate AnimationCurve system.")]
        public bool ParticlesUsesAdvancedCurve = false;
        [Tooltip("Advanced particle rate control. Values from 0 to 1 only! The Y axis acts like percentages of the max MOA multiplier.")]
        public AnimationCurve ParticlesCurve;

        // Sound volume system
        [Header("Sound effects config")]
        public AudioClip SoundEffect;
        [Tooltip("Place a preconfigured AudioSource in here. (Configuring AudioSources at runtime is a pain! This lets you much easier choose the desired settings as well.)")]
        public AudioSource SoundEffectSource;
        [Tooltip("Same as the normal HeatExponent, but for the sound volume.")]
        public float SoundHeatExponent = 2f;
        public float MaxVolume = 0.4f;
        [Tooltip("Heat level at which audio starts.")]
        public float SoundHeatThreshold = 0f;
        [Tooltip("If checked, the sound volume starts at 0 when hitting the threshold and hits the max when heat is 1, else the volume using the threshold heat value as a reference, aka the heat level it gets enabled at.")]
        public bool SoundVolumeStartsAtZero = false;
        [Tooltip("Enables sound volume AnimationCurve system.")]
        public bool SoundUsesAdvancedCurve = false;
        [Tooltip("Advanced sound volume control. Values from 0 to 1 only!. The Y axis acts like percentages of the max volume.")]
        public AnimationCurve VolumeCurve;

        // Accuracy MOA multiplier system
        [Header("Accuracy Settings")]
        public bool DoesHeatAffectAccuracy = false;
        public float MaximumMOAMultiplier = 15f;
        [Tooltip("Same as the normal HeatExponent, but for the MOA multiplier.")]
        public float AccuracyHeatExponent = 2f;
        [Tooltip("Enables MOA multiplier AnimationCurve system.")]
        public bool AccuracyUsesAdvancedCurve = false;
        [Tooltip("Advanced MOA multiplier control. Values from 0 to 1 only!. The Y axis acts like percentages of the max MOA multiplier.")]
        public AnimationCurve AccuracyCurve;

        // Debugging system
        [Header("Debug Messages")]
        public bool DebugEnabled = false;

        // Private variables
        private Material _copyMaterial;
        private float _heat = 0f;

        private bool _isAttached = false;

        private bool _soundEnabled = false;

        private float _origInternalMechanicalMOA = 0f;
        // Constants
        private const string c_EmissionWeightPropertyString = "_EmissionWeight";
        private const string c_DetailWeightPropertyString = "_DetailWeight";
#if !(DEBUG)

        public void Awake()
        {
			Hook();
            if (FireArm != null) _origInternalMechanicalMOA = FireArm.m_internalMechanicalMOA;
            else if (Attachment != null && Attachment is MuzzleDevice muzzleDevice) _origInternalMechanicalMOA = muzzleDevice.m_mechanicalAccuracy;

            if (MeshRenderer != null)
            {
                Log(MeshRenderer.sharedMaterials[MaterialIndex]);
                _copyMaterial = MeshRenderer.materials[MaterialIndex];
                Log(_copyMaterial);
            }

            if (ParticleSystem != null) ChangeParticleEmissionRate(0f);

            if (SoundEffect != null) 
            {
                SoundEffectSource.loop = true;
                SoundEffectSource.clip = SoundEffect;
                SoundEffectSource.volume = 0f;
                SoundEffectSource.Stop();
            }
        }
		public void OnDestroy()
        {
			Unhook();

            if (_copyMaterial != null) Destroy(_copyMaterial);
        }

        public void Update()
        {
            if (Attachment != null)
            {
                if (Attachment.curMount != null && !_isAttached)
                {
                    FireArm = Attachment.curMount.GetRootMount().MyObject as FVRFireArm;
                    if (!(Attachment is MuzzleDevice)) _origInternalMechanicalMOA = FireArm.m_internalMechanicalMOA;
                    _isAttached = true;
                }
                else if (Attachment.curMount == null && _isAttached)
                {
                    if (!(Attachment is MuzzleDevice)) FireArm.m_internalMechanicalMOA = _origInternalMechanicalMOA;
                    FireArm = null;
                    _isAttached = false;
                }
            }

            if (_heat > 0f) _heat -= Time.deltaTime * CooldownRate;
            _heat = Mathf.Clamp(_heat, 0f, 1f);
            if (MeshRenderer != null)
            {
                if (!EmissionUsesAdvancedCurve) _copyMaterial.SetFloat(c_EmissionWeightPropertyString, Mathf.Pow(_heat, HeatExponent));
                else _copyMaterial.SetFloat(c_EmissionWeightPropertyString, EmissionCurve.Evaluate(_heat));
                Log(MeshRenderer.materials[MaterialIndex].GetFloat(c_EmissionWeightPropertyString));
                if (!DetailUsesAdvancedCurve) _copyMaterial.SetFloat(c_DetailWeightPropertyString, Mathf.Pow(_heat, DetailExponent));
                else _copyMaterial.SetFloat(c_DetailWeightPropertyString, DetailCurve.Evaluate(_heat));
            }


            if (ParticleSystem != null)
            {
                if (!ParticlesUsesAdvancedCurve)
                {
                    if (_heat > ParticleHeatThreshold)
                    {
                        float inverseLerp;
                        if (ParticleEmissionRateStartsAtZero) inverseLerp = Mathf.InverseLerp(ParticleHeatThreshold, 1f, _heat);
                        else inverseLerp = _heat;
                        ChangeParticleEmissionRate(inverseLerp);
                    }
                    else
                    {
                        ChangeParticleEmissionRate(0f);
                    }
                }
                else ChangeParticleEmissionRate(ParticlesCurve.Evaluate(_heat) * MaxEmissionRate);
            }

            if (SoundEffect != null)
            {
                if (!SoundUsesAdvancedCurve)
                {
                    if (_heat > SoundHeatThreshold)
                    {
                        if (!_soundEnabled)
                        {
                            SoundEffectSource.Play();
                            _soundEnabled = true;
                        }
                        float inverseLerp;
                        if (SoundVolumeStartsAtZero) inverseLerp = Mathf.InverseLerp(SoundHeatThreshold, 1f, _heat);
                        else inverseLerp = _heat;
                        SoundEffectSource.volume = Mathf.Lerp(0f, MaxVolume, Mathf.Pow(inverseLerp, SoundHeatExponent));
                    }
                    else if (_soundEnabled)
                    {
                        SoundEffectSource.Stop();
                        _soundEnabled = false;
                    }
                }
                else
                {
                    float volumeEvaluation = VolumeCurve.Evaluate(_heat);
                    if (volumeEvaluation > 0f)
                    {
                        if (!_soundEnabled)
                        {
                            SoundEffectSource.Play();
                            _soundEnabled = true;
                        }
                        SoundEffectSource.volume = Mathf.Lerp(0f, MaxVolume, volumeEvaluation);
                    }
                    else if (_soundEnabled)
                    {
                        SoundEffectSource.Stop();
                        _soundEnabled = false;
                    }
                }
            }

            if (DoesHeatAffectAccuracy && FireArm != null)
            {
                if (Attachment == null)
                {
                    if (!AccuracyUsesAdvancedCurve) FireArm.m_internalMechanicalMOA = Mathf.Lerp(1f, MaximumMOAMultiplier, Mathf.Pow(_heat, AccuracyHeatExponent)) * _origInternalMechanicalMOA;
                    else FireArm.m_internalMechanicalMOA = Mathf.Lerp(1f, MaximumMOAMultiplier, AccuracyCurve.Evaluate(_heat)) * _origInternalMechanicalMOA;
                }
                else if (Attachment != null && Attachment is MuzzleDevice muzzleDevice)
                {
                    if (!AccuracyUsesAdvancedCurve) muzzleDevice.m_mechanicalAccuracy = Mathf.Lerp(1f, MaximumMOAMultiplier, Mathf.Pow(_heat, AccuracyHeatExponent)) * _origInternalMechanicalMOA;
                    else muzzleDevice.m_mechanicalAccuracy = Mathf.Lerp(1f, MaximumMOAMultiplier, AccuracyCurve.Evaluate(_heat)) * _origInternalMechanicalMOA;
                }
                else if (Attachment != null && !(Attachment is MuzzleDevice))
                {
                    if (!AccuracyUsesAdvancedCurve) FireArm.m_internalMechanicalMOA = Mathf.Lerp(1f, MaximumMOAMultiplier, Mathf.Pow(_heat, AccuracyHeatExponent)) * _origInternalMechanicalMOA;
                    else FireArm.m_internalMechanicalMOA = Mathf.Lerp(1f, MaximumMOAMultiplier, AccuracyCurve.Evaluate(_heat)) * _origInternalMechanicalMOA;
                }
            }
            Log(_heat);
        }

        private void ChangeParticleEmissionRate(float heat)
        {
            float particleEmissionRate = Mathf.Lerp(0f, MaxEmissionRate, Mathf.Pow(heat, ParticleHeatExponent));
            ParticleSystem.EmissionModule emission = ParticleSystem.emission;
            ParticleSystem.MinMaxCurve rateOverTime = emission.rateOverTime;
            rateOverTime.mode = ParticleSystemCurveMode.Constant;
            rateOverTime.constantMax = particleEmissionRate;
            rateOverTime.constantMin = particleEmissionRate;
            emission.rateOverTime = rateOverTime;
        }


		void Unhook()
        {
            GM.CurrentSceneSettings.ShotFiredEvent -= OnShotFired;
        }

		void Hook()
        {
            GM.CurrentSceneSettings.ShotFiredEvent += OnShotFired;
        }

        private void OnShotFired(FVRFireArm firearm)
        {
            if (FireArm != null && firearm == FireArm) _heat += HeatPerShot;
        }

        private void Log(object message)
        {
            if (DebugEnabled) Log(message);
        }
#endif
    }
}
