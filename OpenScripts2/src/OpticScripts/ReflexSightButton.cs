using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FistVR;

namespace OpenScripts2
{
    public class ReflexSightButton : FVRInteractiveObject
    {
        [Header("Only use one of these two fields")]
        public CustomReflexSightInterface ReflexSight;
        
        public enum EMode
        {
            Zero,
            Reticle,
            Brightness
        }
        [Header("Button Settings")]
        public EMode Mode;

        public bool IncreasesSetting = false;
        public override void Awake()
        {
            base.Awake();

            IsSimpleInteract = true;
        }

        public override void SimpleInteraction(FVRViveHand hand)
        {
            base.SimpleInteraction(hand);
            if (ReflexSight != null)
            {
                switch (Mode)
                {
                    case EMode.Zero:
                        if (IncreasesSetting) ReflexSight.NextZeroDistance();
                        else ReflexSight.PreviousZeroDistance();
                        break;
                    case EMode.Reticle:
                        if (IncreasesSetting) ReflexSight.NextReticleTexture();
                        else ReflexSight.PreviousReticleTexture();
                        break;
                    case EMode.Brightness:
                        if (IncreasesSetting) ReflexSight.NextBrightnessSetting();
                        else ReflexSight.PreviousBrightnessSetting();
                        break;
                }
            }
            else OpenScripts2_BepInExPlugin.LogWarning(this, "No ReflexSight assigned!");
        }
    }
}

