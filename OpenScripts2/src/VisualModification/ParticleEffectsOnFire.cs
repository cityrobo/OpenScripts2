using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class ParticleEffectsOnFire : OpenScripts2_BasePlugin
    {
        public FVRFireArm FireArm;

        [Serializable]
        public class ParticleEmitter
        {
            public ParticleSystem ParticleSystem;
            public int NumbersOfParticlesToEmit;
            public bool EmitWhenGunSuppressed = true;
            public bool EmitWhenGunHasMuzzleDevices = true;
            public bool MoveWithMuzzle = false;
        }

        public ParticleEmitter[] ParticleEmitters;

		public void Awake()
        {
			GM.CurrentSceneSettings.ShotFiredEvent += ShotFired;
        }

		public void OnDestroy()
        {
			GM.CurrentSceneSettings.ShotFiredEvent -= ShotFired;
		}

        [ContextMenu("Test Emission")]
        private void EmitParticles(FVRFireArm firearm)
		{
            foreach (var pEmitter in ParticleEmitters)
            {
                if (pEmitter.MoveWithMuzzle)
                {
                    pEmitter.ParticleSystem.transform.position = firearm.CurrentMuzzle.transform.position;
                }

                if ((pEmitter.EmitWhenGunHasMuzzleDevices || !pEmitter.EmitWhenGunHasMuzzleDevices && firearm.MuzzleDevices.Count == 0) && (pEmitter.EmitWhenGunSuppressed || !pEmitter.EmitWhenGunSuppressed && !firearm.IsSuppressed())) pEmitter.ParticleSystem.Emit(pEmitter.NumbersOfParticlesToEmit);
            }
		}

		private void ShotFired(FVRFireArm firearm)
        {
			if (firearm == FireArm)
            {
				EmitParticles(firearm);
			}
        }
	}
}
