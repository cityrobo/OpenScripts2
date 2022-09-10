using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class AutomaticStripperClip : OpenScripts2_BasePlugin
    {
        public FVRFireArmClip Clip;
        [Tooltip("Delay in Seconds. If equal to 0, all rounds will be loaded at the same time.")]
        public float DelayBetweenRounds = 0f;

        private bool _loadingRounds = false;

        public void Update()
        {
            if (!_loadingRounds && Clip.FireArm != null)
            {
                if (DelayBetweenRounds != 0f) StartCoroutine(LoadRoundsOneByOne());
                else LoadAllRounds();
            }
        }

        private IEnumerator LoadRoundsOneByOne()
        {
            _loadingRounds = true;
            while (Clip.HasARound() && Clip.FireArm != null && Clip.FireArm.Magazine != null && !Clip.FireArm.Magazine.IsFull())
            {
                Clip.LoadOneRoundFromClipToMag();
                yield return new WaitForSeconds(DelayBetweenRounds);
            }
            _loadingRounds = false;
        }

        private void LoadAllRounds()
        {
            if (Clip.FireArm == null || Clip.FireArm.Magazine == null || Clip.FireArm.Magazine.IsFull() || !Clip.HasARound()) return;
            SM.PlayGenericSound(Clip.LoadFromClipToMag, transform.position);

            for (int i = 0; i < Clip.m_numRounds; i++)
            {
                if (Clip.FireArm.Magazine.IsFull() || !Clip.HasARound()) break;

                FireArmRoundClass rClass = Clip.RemoveRoundReturnClass();
                Clip.FireArm.Magazine.AddRound(rClass, false, true);
            }
        }
    }
}
