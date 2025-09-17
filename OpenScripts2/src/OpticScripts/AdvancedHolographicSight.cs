using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace OpenScripts2
{
    [Obsolete("USe vanilla reflex sight system instead.")]
    public class AdvancedHolographicSight : HolographicSight
    {
        public Texture2D ReticleTexture;
        [ColorUsage(true, true, 0f, float.MaxValue, 0f, float.MaxValue)]
        public Color ReticleColor;

        private static readonly List<AdvancedHolographicSight> _existingAdvanvcedHolographicSights = new();


        public void Awake()
        {
            _existingAdvanvcedHolographicSights.Add(this);
        }

        public void OnDestroy()
        {
            _existingAdvanvcedHolographicSights.Remove(this);
        }

#if !DEBUG
        static AdvancedHolographicSight()
        {
            On.HolographicSight.OnWillRenderObject += HolographicSight_OnWillRenderObject;
        }


        private static void HolographicSight_OnWillRenderObject(On.HolographicSight.orig_OnWillRenderObject orig, HolographicSight self)
        {
            if (self is AdvancedHolographicSight sight && _existingAdvanvcedHolographicSights.Contains(sight)) 
            {
                self.m_block.SetTexture("_MainTex", sight.ReticleTexture);
                self.m_block.SetColor("_Color", sight.ReticleColor);
            }
            orig(self);
        }
#endif
    }
}
