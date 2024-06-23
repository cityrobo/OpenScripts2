using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    public class ParticleEffectOnFire : OpenScripts2_BasePlugin
    {
        public FVRFireArm FireArm;
		public ParticleSystem ParticleSystem;
		public int ParticleCount;
        public bool EmitWhenGunSuppressed = true;
        public bool EmitWhenGunHasMuzzleDevices = true;
        public bool MoveWithMuzzle = false;

        public void Awake()
        {
			GM.CurrentSceneSettings.ShotFiredEvent += ShotFired;
        }

		public void OnDestroy()
        {
			GM.CurrentSceneSettings.ShotFiredEvent -= ShotFired;
		}

        [ContextMenu("Test Emission")]
        private void EmitParticle()
		{
			ParticleSystem.Emit(ParticleCount);
		}

		private void ShotFired(FVRFireArm firearm)
        {
			if (firearm == FireArm)
            {
                if (MoveWithMuzzle)
                {
                    ParticleSystem.transform.position = firearm.CurrentMuzzle.transform.position;
                }

                if ((EmitWhenGunHasMuzzleDevices || !EmitWhenGunHasMuzzleDevices && firearm.MuzzleDevices.Count == 0) && (EmitWhenGunSuppressed || !EmitWhenGunSuppressed && !firearm.IsSuppressed())) EmitParticle();
			}
        }
	}
}
