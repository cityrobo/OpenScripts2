using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace OpenScripts2
{
    public class ModifiedScopeCamera : OpenScripts2_BasePlugin
    {
        public ScopeCam ScopeCamComponent;
        [Range(0.011f, 1f)]
        public float ReticleDistanceOverride = 0.011f;

        private static readonly Dictionary<ScopeCam, float> _existingModifiedScopeCameras = new();

        public void Awake()
        {
            ScopeCamComponent.Reticule.transform.position = ScopeCamComponent.ScopeCamera.transform.position + ScopeCamComponent.transform.forward * ReticleDistanceOverride;
            _existingModifiedScopeCameras.Add(ScopeCamComponent, ReticleDistanceOverride);
        }

        public void OnDestroy()
        {
            _existingModifiedScopeCameras.Remove(ScopeCamComponent);
        }

#if !DEBUG
        static ModifiedScopeCamera()
        {
            IL.ScopeCam.OnWillRenderObject += ScopeCam_OnWillRenderObject;
        }

        private static void ScopeCam_OnWillRenderObject(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext
            (
                MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<ScopeCam>(nameof(ScopeCam.ScopeCamera)),
                i => i.MatchCallvirt<Component>("get_transform"),
                i => i.MatchCallvirt<Transform>("get_position"),
                i => i.MatchLdarg(0),
                i => i.MatchCall<Component>("get_transform"),
                i => i.MatchCallvirt<Transform>("get_forward")
            );
            c.Remove();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(GetReticleDistanceOverride);
        }

        private static float GetReticleDistanceOverride(ScopeCam scopeCam)
        {
            if (_existingModifiedScopeCameras.TryGetValue(scopeCam, out float reticleDistanceOverride)) return reticleDistanceOverride;
            else return 0.1f;
        }
#endif
    }
}
