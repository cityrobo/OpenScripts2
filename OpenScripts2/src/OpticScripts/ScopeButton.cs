using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FistVR;

namespace OpenScripts2
{
    public class ScopeButton : FVRInteractiveObject
    {
        [Header("Only use one of these two fields")]
        public CustomScopeInterface Scope;
        public CustomLinearZoomScopeInterface LinearZoomScope;
        
        public enum EMode
        {
            Zoom,
            Zero,
            Elevation,
            Windage,
            Reticle
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
            if (Scope != null)
            {
                switch (Mode)
                {
                    case EMode.Zoom:
                        if (IncreasesSetting) Scope.NextZoomLevel();
                        else Scope.PreviousZoomLevel();
                        break;
                    case EMode.Zero:
                        if (IncreasesSetting) Scope.NextZeroRange();
                        else Scope.PreviousZeroRange();
                        break;
                    case EMode.Elevation:
                        if (IncreasesSetting) Scope.IncreaseElevationAdjustment();
                        else Scope.DecreaseElevationAdjustment();
                        break;
                    case EMode.Windage:
                        if (IncreasesSetting) Scope.IncreaseWindageAdjustment();
                        else Scope.DecreaseWindageAdjustment();
                        break;
                    case EMode.Reticle:
                        if (IncreasesSetting) Scope.NextReticleTexture();
                        else Scope.PreviousReticleTexture();
                        break;
                }
            }
            else if (LinearZoomScope != null)
            {
                switch (Mode)
                {
                    case EMode.Zoom:
                        OpenScripts2_BepInExPlugin.LogWarning(this, "Zoom mode not supported on ScopeShaderLinearZoom");
                        break;
                    case EMode.Zero:
                        if (IncreasesSetting) LinearZoomScope.NextZeroRange();
                        else LinearZoomScope.PreviousZeroRange();
                        break;
                    case EMode.Elevation:
                        if (IncreasesSetting) LinearZoomScope.IncreaseElevationAdjustment();
                        else LinearZoomScope.DecreaseElevationAdjustment();
                        break;
                    case EMode.Windage:
                        if (IncreasesSetting) LinearZoomScope.IncreaseWindageAdjustment();
                        else LinearZoomScope.DecreaseWindageAdjustment();
                        break;
                    case EMode.Reticle:
                        if (IncreasesSetting) LinearZoomScope.NextReticleTexture();
                        else LinearZoomScope.PreviousReticleTexture();
                        break;
                }
            }
            else OpenScripts2_BepInExPlugin.LogWarning(this, "No Scope or LinearZoomScope assigned!");
        }
    }
}

