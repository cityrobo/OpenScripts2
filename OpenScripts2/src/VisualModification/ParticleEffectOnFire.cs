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

		public void Awake()
        {
			GM.CurrentSceneSettings.ShotFiredEvent += ShotFired;
        }

		public void OnDestroy()
        {
			GM.CurrentSceneSettings.ShotFiredEvent -= ShotFired;
		}

		private void EmitParticle()
		{
			ParticleSystem.Emit(ParticleCount);
		}

		private void ShotFired(FVRFireArm firearm)
        {
			if (firearm == FireArm)
            {
				EmitParticle();
			}
        }
	}
}
