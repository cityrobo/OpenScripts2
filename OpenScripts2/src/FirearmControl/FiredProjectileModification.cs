using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;
using System.Reflection;

namespace OpenScripts2
{
    public class FiredProjectileModification : OpenScripts2_BasePlugin
    {
        public FVRFireArm FireArm;
        [Header("Complete Override")]
        public BallisticProjectile OverrideProjectile;
        [Header("Partial Override")]
        public ProjectileProxy PartialOverrideProjectileContainer;

        [HideInInspector]
        public static readonly ProjectileProxy ProjectileProxyDefault = new();

        [Serializable]
        public class ProjectileProxy
        {
            [Header("Projectile Parameters")]
            public float Mass;
            public Vector3 Dimensions;
            public float FrontArea;
            public float MuzzleVelocityBase;
            public BallisticProjectileType ProjType;
            public bool DoesIgniteOnHit;
            public float IgnitionChance = 0.2f;
            public bool DoesVaporizeOnHit;
            public float VaporizeChange;
            public float KETotalForHit;
            public float KEPerSquareMeterBase;
            public float FlightVelocityMultiplier = 1f;
            public float AirDragMultiplier = 1f;
            public float GravityMultiplier = 1f;
            public bool IsDisabledOnFirstImpact;
            public bool GeneratesImpactSound;
            public ImpactType ImpactSoundType = ImpactType.GunshotImpact;
            public bool GeneratesImpactDecals;
            public ImpactEffectMagnitude ImpactFXMagnitude = ImpactEffectMagnitude.Medium;
            public bool GeneratesSuppressionEvent = true;
            public float SuppressionIntensity = 1f;
            public float SuppressionRange = 5f;
            public bool DeletesOnStraightDown = true;
            public int Source_IFF;
            public bool UsesIFFMatSwap;
            public List<Material> IFFSwapMats = new();
            [Header("Life and Timeouts")]
            public float MaxRange = 500f;
            public float MaxRangeRandom;

            [Header("Tracer")]
            public Transform tracer;
            public Renderer TracerRenderer;
            public Renderer BulletRenderer;
            public GameObject ExtraDisplay;
            public float TracerLengthMultiplier = 1f;
            public float TracerWidthMultiplier = 1f;

            [Header("Trails")]
            public bool UsesTrails = true;
            public VRTrail Trail;
            public Color TrailStartColor;
            public BallisticImpactEffectType ImpactEffectTypeOverride = BallisticImpactEffectType.None;
            public BulletHoleDecalType BulletHoleDecalOverride;

            [Header("Submunitions")]
            public List<BallisticProjectile.Submunition> Submunitions = new();
            public bool PassesFirearmReferenceToSubmunitions;

            [Header("BlastJump")]
            public bool DoesBlastJumpOnFire;
            public float BlastJumpAmount;
        }


        public void Awake()
        {
            ProjectileFiredEvent += FiredProjectile;
        }

        public void OnDestroy()
        {
            ProjectileFiredEvent -= FiredProjectile;
        }

        private void FiredProjectile(FVRFireArm fireArm, ref BallisticProjectile projectile)
        {
            if (fireArm == FireArm)
            {
                if (OverrideProjectile == null)
                {
                    ApplyNewSettings(PartialOverrideProjectileContainer, projectile);
                }
                else
                {
                    GameObject projectileObject = projectile.gameObject;
                    Destroy(projectile);

                    if (projectileObject.transform.position != fireArm.CurrentMuzzle.position - fireArm.CurrentMuzzle.forward * 0.005f) projectileObject.transform.position = fireArm.CurrentMuzzle.position - fireArm.CurrentMuzzle.forward * 0.005f;

                    projectile = UniversalCopy.CopyComponent(OverrideProjectile, projectileObject);

                    //FVRFireArmChamber curChamber = GetCurrentChamber(fireArm);
                    //float chamberVelMult = AM.GetChamberVelMult(curChamber.RoundType, Vector3.Distance(curChamber.transform.position, fireArm.CurrentMuzzle.position));
                    //projectile.Fire(projectile.MuzzleVelocityBase * curChamber.ChamberVelocityMultiplier * chamberVelMult, gameObject.transform.forward, fireArm, true);
                }
            }
        }

        private void ApplyNewSettings(ProjectileProxy source, BallisticProjectile destination)
        {
            // Get the types of the source and destination classes
            Type sourceType = source.GetType();
            Type destinationType = destination.GetType();

            // Get all public fields of the source class
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            FieldInfo[] sourceFields = sourceType.GetFields(flags);

            // Copy values from source to destination for fields with matching names and types
            foreach (FieldInfo sourceField in sourceFields)
            {
                object fieldValue = sourceField.GetValue(source);
                object defaultFieldValue = sourceField.GetValue(ProjectileProxyDefault);

                List<BallisticProjectile.Submunition> listTestSubMun = fieldValue as List<BallisticProjectile.Submunition>;
                List<Material> listTestSwapMats = fieldValue as List<Material>;
                //Log($"Field \"{sourceField.Name}\" value and default are {(fieldValue != null && !fieldValue.Equals(defaultFieldValue) ? "not" : "")} identical.");
                bool listCheck = listTestSubMun == null && listTestSwapMats == null;
                listCheck = listCheck || (listTestSubMun != null && listTestSubMun.Count != 0) || (listTestSwapMats != null && listTestSwapMats.Count != 0);
                if ((fieldValue != null && !fieldValue.Equals(defaultFieldValue)) && listCheck)
                {
                    Log($"Changed field: \"{sourceField.Name}\" Modified Value: {fieldValue} Default Value: {defaultFieldValue}");
                    
                    // Get the corresponding field in the destination class
                    FieldInfo destinationField = destinationType.GetField(sourceField.Name);

                    // Check if the destination field exists and has the same type
                    if (destinationField != null && destinationField.FieldType == sourceField.FieldType)
                    {
                        // Copy the value from source to destination
                        destinationField.SetValue(destination, fieldValue);
                    }
                }
            }
        }

        static void CopyProperties(object source, object destination)
        {
            // Get the types of the source and destination classes
            Type sourceType = source.GetType();
            Type destinationType = destination.GetType();

            // Get all public properties of the source class
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            PropertyInfo[] sourceProperties = sourceType.GetProperties(flags);

            // Copy values from source to destination for properties with matching names and types
            foreach (PropertyInfo sourceProperty in sourceProperties)
            {
                // Get the corresponding property in the destination class
                PropertyInfo destinationProperty = destinationType.GetProperty(sourceProperty.Name);

                // Check if the destination property exists and has the same type
                if (destinationProperty != null && destinationProperty.PropertyType == sourceProperty.PropertyType)
                {
                    // Copy the value from source to destination
                    object value = sourceProperty.GetValue(source, null);
                    destinationProperty.SetValue(destination, value, null);
                }
            }
        }
    }
}
