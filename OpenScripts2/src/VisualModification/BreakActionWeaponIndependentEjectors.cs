using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FistVR;
using OpenScripts2;
using System.Linq;
using System;

namespace OpenScripts2
{
    public class BreakActionWeaponIndependentEjectors : OpenScripts2_BasePlugin
    {
        public BreakActionWeapon BreakActionFirearm;

        [Serializable]
        public class EjectorConfig
        {
            public Transform Ejector;
            public Axis EjectorAxis;
            [Tooltip("Speed at which the ejector moves, in m/s")]
            public float EjectorSpeed = 1f;
            [Tooltip("Ejector resting postion with closed fore")]
            public float EjectorClosed;
            [Tooltip("Ejector position when extracting round on an unfired barrel")]
            public float EjectorExtracted;
            [Tooltip("Ejector position when fully ejected")]
            public float EjectorEjected;
            [HideInInspector]
            public bool IsEjecting;
            [HideInInspector]
            public bool HasEjected;
            [HideInInspector]
            public bool WasUncocked;

            public IEnumerator EjectorCoroutine()
            {
                IsEjecting = true;
                WasUncocked = false;
                Vector3 targetPos = Ejector.localPosition.ModifyAxisValue(EjectorAxis, EjectorEjected);
                while (!Mathf.Approximately(Ejector.GetLocalPositionAxisValue(EjectorAxis), EjectorEjected))
                {
                    Ejector.localPosition = Vector3.MoveTowards(Ejector.localPosition, targetPos, EjectorSpeed * Time.deltaTime);
                    yield return null;
                }
                IsEjecting = false;
                HasEjected = true;
            }
        }

        public EjectorConfig[] EjectorConfigs;

        public void Update()
        {
            // Store uncocked state per barrel, because it will be reset when the barrel ejects.
            for (int i = 0; i < BreakActionFirearm.Barrels.Length; i++)
            {
                if (!BreakActionFirearm.Barrels[i].m_isHammerCocked) EjectorConfigs[i].WasUncocked = true;
            }

            // Either start ejecting, when barrel was fired, or return the ejectors to their resting positions, when the break-fore is latched.
            if (!BreakActionFirearm.m_isLatched && BreakActionFirearm.Hinge.transform.localEulerAngles.x >= BreakActionFirearm.HingeEjectLimit)
            {
                foreach (var ejectorConfig in EjectorConfigs)
                {
                    if (!ejectorConfig.HasEjected && ejectorConfig.WasUncocked) StartCoroutine(ejectorConfig.EjectorCoroutine());
                }
            }
            else if (BreakActionFirearm.m_isLatched)
            {
                foreach (var ejectorConfig in EjectorConfigs)
                {
                    ejectorConfig.HasEjected = false;

                    ejectorConfig.Ejector.ModifyLocalPositionAxisValue(ejectorConfig.EjectorAxis, ejectorConfig.EjectorClosed);
                }
            }

            // Lerp unfired and fully fired ejectors to either from rest to extraction back to rest, or from ejection to rest.
            if (!BreakActionFirearm.m_isLatched && BreakActionFirearm.Hinge.transform.localEulerAngles.x < BreakActionFirearm.HingeEjectLimit)
            {
                for (int i = 0; i < BreakActionFirearm.Barrels.Length; i++)
                {
                    BreakActionWeapon.BreakActionBarrel barrel = BreakActionFirearm.Barrels[i];
                    EjectorConfig ejectorConfig = EjectorConfigs[i];
                    if (barrel.m_isHammerCocked && !ejectorConfig.IsEjecting)
                    {
                        float inverseLerp = Mathf.InverseLerp(0f, BreakActionFirearm.HingeEjectLimit, BreakActionFirearm.Hinge.transform.localEulerAngles.x);
                        float topLerp = ejectorConfig.HasEjected ? Mathf.Lerp(ejectorConfig.EjectorClosed, ejectorConfig.EjectorEjected, inverseLerp) : Mathf.Lerp(ejectorConfig.EjectorClosed, ejectorConfig.EjectorExtracted, inverseLerp);

                        ejectorConfig.Ejector.ModifyLocalPositionAxisValue(ejectorConfig.EjectorAxis, topLerp);
                    }
                }
            }
        }
    }
}