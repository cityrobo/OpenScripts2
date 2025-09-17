using System;
using System.Collections.Generic;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
    [Obsolete("USe vanilla reflex sight system instead.")]
    public class AdvancedHolographicSightController : FVRFireArmAttachmentInterface
    {
        //public PIPScope PScope;
        public AdvancedHolographicSight Sight;
        public Transform TargetAimer;
        public Transform BackupMuzzle;
        public Transform UISpawnPoint;
        public PIPScopeUI UI;
        private AdvancedHolographicSightUI _uiPatcher;

        public ZeroingMode ZeroingMode;
        public PipScopeZeroScaling ZeroScaling = PipScopeZeroScaling.MOA;
        //public List<float> MagnificationValues;
        public List<float> ZeroDistanceValues;
        //public float ScopeElevationAdjustmentPerTick;
        //public float ScopeWindageAdjustmentPerTick;

        private Vector2 _reticleAdjustmentDegrees = Vector2.zero;

        [Header("Reticle")]
        public List<PIPScope.Reticle> Reticles;

        [ColorUsage(true, true, 0f, float.MaxValue, 0f, float.MaxValue)]
        public List<Color> ReticleColors;
        //public List<float> ReticleFullIllumination;
        public float ReticleElevationAdjustmentPerTick;
        public float ReticleWindageAdjustmentPerTick;

        //[Header("NightVision")]
        //public List<NightVisionDefinition> NVDefs;

        //[Header("Thermals")]
        //public List<PIPScopeThermalColorSetting> ThermalColorSettings;
        //public List<float> ThermalDigitalZoomMagnifications;
        //public List<bool> ThermalReticleColorInverts;

        [Header("DisplayNames")]
        public List<string> DisplayNames_Reticle;
        public List<string> DisplayNames_ReticleColor;
        public List<string> DisplayNames_ThermalColors;

        [Header("SpecialComponents")]
        //public Vector2 FlipRange = new Vector2(0f, 0f);
        //private float m_curFlip;

        //public Transform FlipTransform;
        //public Transform LaserHitAtZeroPoint;
        //public int MagnificationIndex;
        public int ZeroDistanceIndex;
        //public int ScopeElevationMagnitude;
        //public int ScopeWindageMagnitude;
        public int ReticleIndex;
        public int ReticleColorIndex;
        public int ReticleElevationMagnitude;
        public int ReticleWindageMagnitude;
        //public int Flipped;
        //public int NVDefIndex;
        //public int NVColorIndex;
        //public int ThermalColorIndex;
        //public int ThermalZoomIndex;
        //public float MagnificationOverride;

        //[NonSerialized]
        //public PIPScopeComponent MagnificationComponent;
        [NonSerialized]
        public PIPScopeComponent ZeroDistanceComponent;
        //[NonSerialized]
        //public PIPScopeComponent ScopeElevationComponent;
        //[NonSerialized]
        //public PIPScopeComponent ScopeWindageComponent;
        [NonSerialized]
        public PIPScopeComponent ReticleComponent;
        [NonSerialized]
        public PIPScopeComponent IlluminationComponent;
        [NonSerialized]
        public PIPScopeComponent ReticleElevationComponent;
        [NonSerialized]
        public PIPScopeComponent ReticleWindageComponent;
        //[NonSerialized]
        //public PIPScopeComponent FlipComponent;
        //[NonSerialized]
        //public PIPScopeComponent NVDefComponent;
        //[NonSerialized]
        //public PIPScopeComponent NVColorComponent;
        //[NonSerialized]
        //public PIPScopeComponent ThermalColorComponent;
        //[NonSerialized]
        //public PIPScopeComponent ThermalZoomComponent;
        public List<PIPScopeComponent> Components;
        [Header("Override Components")]
        public Transform OverrideMuzzle;
        public FVRFireArm OverrideFireArm;
        public float FixedBaseZero = 100f;

        private bool isUIActive;
        private RaycastHit m_hit;
        private float ZDist = 100f;
        private float Yoffset;


        [ContextMenu("Generate Reticle Color Interps")]
        public void GenerateReticleColorInterps()
        {
            for (int i = 0; i < ReticleColors.Count; i++)
            {
                float num = (float)i / (float)(ReticleColors.Count - 1);
                Color color = Color.Lerp(ReticleColors[0], ReticleColors[ReticleColors.Count - 1], num);
                Debug.Log(color.r);
                if (i != 0 && i != ReticleColors.Count - 1)
                {
                    ReticleColors[i] = color;
                }
            }
        }

        public override void Awake()
        {
            base.Awake();
            for (int i = 0; i < Components.Count; i++)
            {
                switch (Components[i].OptionType)
                {
                    //case PipScopeOptionType.Magnification:
                    //    MagnificationComponent = Components[i];
                    //    break;
                    case PipScopeOptionType.ZeroDistance:
                        ZeroDistanceComponent = Components[i];
                        break;
                    //case PipScopeOptionType.ScopeElevation:
                    //    ScopeElevationComponent = Components[i];
                    //    break;
                    //case PipScopeOptionType.ScopeWindage:
                    //    ScopeWindageComponent = Components[i];
                    //    break;
                    case PipScopeOptionType.Reticle:
                        ReticleComponent = Components[i];
                        break;
                    case PipScopeOptionType.ReticleColor:
                        IlluminationComponent = Components[i];
                        break;
                    case PipScopeOptionType.ReticleElevation:
                        ReticleElevationComponent = Components[i];
                        break;
                    case PipScopeOptionType.ReticleWindage:
                        ReticleWindageComponent = Components[i];
                        break;
                    //case PipScopeOptionType.Flipping:
                    //    FlipComponent = Components[i];
                    //    break;
                    //case PipScopeOptionType.NightVisionColor:
                    //    NVColorComponent = Components[i];
                    //    break;
                    //case PipScopeOptionType.ThermalColorMap:
                    //    ThermalColorComponent = Components[i];
                    //    break;
                    //case PipScopeOptionType.ThermalDigitalZoom:
                    //    ThermalZoomComponent = Components[i];
                    //    break;
                    //case PipScopeOptionType.NightVisionDef:
                    //    NVDefComponent = Components[i];
                    //    break;
                }
            }
            ZeroingMode = ZeroingMode.Reticle;

            GameObject UIGameObject = Instantiate<GameObject>(ManagerSingleton<AM>.Instance.Prefab_PIPScopeUI, UISpawnPoint.position, UISpawnPoint.rotation);
            UI = UIGameObject.GetComponent<PIPScopeUI>();
            _uiPatcher = UIGameObject.AddComponent<AdvancedHolographicSightUI>();
            _uiPatcher.InitAndCache(this, UI);
            UIGameObject.SetActive(false);
            //if (LaserHitAtZeroPoint != null)
            //{
            //    LaserHitAtZeroPoint.SetParent(null);
            //}
        }

        public override void Start()
        {
            base.Start();
            if (OverrideFireArm != null || OverrideMuzzle != null)
            {
                UpdateScopeParams();
            }
        }

        public override void OnDestroy()
        {
            if (UI != null)
            {
                UnityEngine.Object.Destroy(UI.gameObject);
            }
            //if (LaserHitAtZeroPoint != null)
            //{
            //    UnityEngine.Object.Destroy(LaserHitAtZeroPoint.gameObject);
            //}
            base.OnDestroy();
        }

        // Vaulting Support Patches
#if !DEBUG
        static AdvancedHolographicSightController()
        {
            On.FistVR.FVRFireArmAttachment.ConfigureFromFlagDic += FVRFireArmAttachment_ConfigureFromFlagDic;
            On.FistVR.FVRFireArmAttachment.GetFlagDic += FVRFireArmAttachment_GetFlagDic;
        }

        private static Dictionary<string, string> FVRFireArmAttachment_GetFlagDic(On.FistVR.FVRFireArmAttachment.orig_GetFlagDic orig, FVRFireArmAttachment self)
        {
            Dictionary<string, string> flagDic = orig(self);
            if (self.AttachmentInterface != null && self.AttachmentInterface is AdvancedHolographicSightController advancedHolographicSightController)
            {
                if (advancedHolographicSightController.ZeroDistanceValues.Count > 0)
                {
                    flagDic.Add("ZeroDistanceIndex", advancedHolographicSightController.ZeroDistanceIndex.ToString());
                }
                if (advancedHolographicSightController.ReticleColors.Count > 0)
                {
                    flagDic.Add("ReticleColorIndex", advancedHolographicSightController.ReticleColorIndex.ToString());
                }
                if (advancedHolographicSightController.Reticles.Count > 0)
                {
                    flagDic.Add("ReticleIndex", advancedHolographicSightController.ReticleIndex.ToString());
                }
                if (advancedHolographicSightController.ReticleElevationAdjustmentPerTick > 0f)
                {
                    flagDic.Add("ReticleElevationMagnitude", advancedHolographicSightController.ReticleElevationMagnitude.ToString());
                }
                if (advancedHolographicSightController.ReticleWindageAdjustmentPerTick > 0f)
                {
                    flagDic.Add("ReticleWindageMagnitude", advancedHolographicSightController.ReticleWindageMagnitude.ToString());
                }
                advancedHolographicSightController.UpdateScopeParams();
                advancedHolographicSightController.UI?.RedrawUI();
            }
            return flagDic;
        }

        private static void FVRFireArmAttachment_ConfigureFromFlagDic(On.FistVR.FVRFireArmAttachment.orig_ConfigureFromFlagDic orig, FVRFireArmAttachment self, Dictionary<string, string> f)
        {
            orig(self, f);
            if (self.AttachmentInterface != null && self.AttachmentInterface is AdvancedHolographicSightController advancedHolographicSightController)
            {
                // this method is regularly on the attachment itself but the nuget package doesn't have the latest version of the game
                SetIntFromFlagIfPresent(f, "ZeroDistanceIndex", ref advancedHolographicSightController.ZeroDistanceIndex);
                SetIntFromFlagIfPresent(f, "ReticleColorIndex", ref advancedHolographicSightController.ReticleColorIndex);
                SetIntFromFlagIfPresent(f, "ReticleIndex", ref advancedHolographicSightController.ReticleIndex);
                SetIntFromFlagIfPresent(f, "ReticleElevationMagnitude", ref advancedHolographicSightController.ReticleElevationMagnitude);
                SetIntFromFlagIfPresent(f, "ReticleWindageMagnitude", ref advancedHolographicSightController.ReticleWindageMagnitude);
            }
        }

        private static void SetIntFromFlagIfPresent(Dictionary<string, string> dic, string flag, ref int value)
        {
            if (dic.ContainsKey(flag))
            {
                value = Convert.ToInt32(dic[flag]);
            }
        }
#endif

        public override void FVRUpdate()
        {
            base.FVRUpdate();
            //if (LaserHitAtZeroPoint != null)
            //{
            //    if (PScope.enabled)
            //    {
            //        FVRFireArm fvrfireArm = null;
            //        Transform transform = null;
            //        if (OverrideFireArm != null)
            //        {
            //            fvrfireArm = OverrideFireArm;
            //        }
            //        else if (Attachment != null && Attachment.curMount != null && Attachment.curMount.Parent is FVRFireArm)
            //        {
            //            fvrfireArm = Attachment.curMount.Parent as FVRFireArm;
            //        }
            //        if (OverrideMuzzle != null)
            //        {
            //            transform = OverrideMuzzle;
            //        }
            //        else if (fvrfireArm != null)
            //        {
            //            transform = fvrfireArm.GetMuzzle();
            //        }
            //        Vector3 vector = transform.position + transform.forward * ZDist + transform.up * Yoffset;
            //        if (Physics.Raycast(transform.position, (vector - transform.position).normalized, out m_hit, GM.CurrentSceneSettings.ScopeMaxDrawRange, AM.PLM, QueryTriggerInteraction.Ignore))
            //        {
            //            LaserHitAtZeroPoint.position = m_hit.point;
            //            LaserHitAtZeroPoint.gameObject.SetActive(true);
            //            Vector2 vector2 = PScope.ZeroToWorldPoint(m_hit.point, false);
            //            PScope.reticleAdjustmentDegrees = vector2 - PScope.scopeAdjustmentDegrees;
            //        }
            //        else
            //        {
            //            LaserHitAtZeroPoint.gameObject.SetActive(false);
            //            PScope.reticleAdjustmentDegrees = Vector2.zero;
            //        }
            //    }
            //    else
            //    {
            //        LaserHitAtZeroPoint.gameObject.SetActive(false);
            //        PScope.reticleAdjustmentDegrees = Vector2.zero;
            //    }
            //}
            //if (FlipTransform != null)
            //{
            //    if (Flipped == 1)
            //    {
            //        float num = Mathf.MoveTowards(m_curFlip, FlipRange.y, 500f * Time.deltaTime);
            //        if (Mathf.Abs(m_curFlip - num) > Mathf.Epsilon)
            //        {
            //            m_curFlip = num;
            //            FlipTransform.localEulerAngles = new Vector3(0f, 0f, m_curFlip);
            //        }
            //    }
            //    else
            //    {
            //        float num2 = Mathf.MoveTowards(m_curFlip, FlipRange.x, 500f * Time.deltaTime);
            //        if (Mathf.Abs(m_curFlip - num2) > Mathf.Epsilon)
            //        {
            //            m_curFlip = num2;
            //            FlipTransform.localEulerAngles = new Vector3(0f, 0f, m_curFlip);
            //        }
            //    }
            //}
            if (isUIActive || base.IsHeld)
            {
                UI.transform.position = UISpawnPoint.position;
                UI.transform.rotation = UISpawnPoint.rotation;
            }
        }

        public void OpenUI()
        {
            isUIActive = true;
            UI.gameObject.SetActive(isUIActive);
        }

        public void CloseUI()
        {
            isUIActive = false;
            UI.gameObject.SetActive(isUIActive);
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            Vector2 touchpadAxes = hand.Input.TouchpadAxes;
            if (UI.GetNumDisplayedTypes() > 0)
            {
                if (hand.IsInStreamlinedMode)
                {
                    if (hand.Input.BYButtonDown)
                    {
                        isUIActive = !isUIActive;
                        UI.gameObject.SetActive(isUIActive);
                    }
                }
                else if (hand.Input.TouchpadDown && Vector2.Angle(touchpadAxes, Vector2.up) <= 45f)
                {
                    isUIActive = !isUIActive;
                    UI.gameObject.SetActive(isUIActive);
                }
            }
            base.UpdateInteraction(hand);
        }

        public override void OnAttach()
        {
            base.OnAttach();
            UpdateScopeParams();
        }

        public override void OnDetach()
        {
            base.OnDetach();
            isUIActive = false;
            if (UI != null)
            {
                UI.gameObject.SetActive(isUIActive);
            }
        }

        public void OpenUIAndSetValue(PipScopeOptionType oType)
        {
            if (UI != null && UI.AttemptToSetOptionType(oType) && !isUIActive)
            {
                OpenUI();
            }
        }

        public void IncrementOptionValue(PipScopeOptionType oType, ref bool didChange, bool forceLoop = false, bool forceClamp = false)
        {
            switch (oType)
            {
                //case PipScopeOptionType.Magnification:
                //    if (forceLoop)
                //    {
                //        if (MagnificationIndex < MagnificationValues.Count - 1)
                //        {
                //            MagnificationIndex++;
                //        }
                //        else
                //        {
                //            MagnificationIndex = 0;
                //        }
                //        MagnificationOverride = 0f;
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    else if (MagnificationIndex < MagnificationValues.Count - 1)
                //    {
                //        MagnificationIndex++;
                //        MagnificationOverride = 0f;
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    break;
                case PipScopeOptionType.ZeroDistance:
                    if (forceLoop)
                    {
                        if (ZeroDistanceIndex < ZeroDistanceValues.Count - 1)
                        {
                            ZeroDistanceIndex++;
                        }
                        else
                        {
                            ZeroDistanceIndex = 0;
                        }
                        UpdateScopeParams();
                        didChange = true;
                    }
                    else if (ZeroDistanceIndex < ZeroDistanceValues.Count - 1)
                    {
                        ZeroDistanceIndex++;
                        UpdateScopeParams();
                        didChange = true;
                    }
                    break;
                //case PipScopeOptionType.ScopeElevation:
                //    ScopeElevationMagnitude++;
                //    UpdateScopeParams();
                //    didChange = true;
                //    break;
                //case PipScopeOptionType.ScopeWindage:
                //    ScopeWindageMagnitude++;
                //    UpdateScopeParams();
                //    didChange = true;
                //    break;
                case PipScopeOptionType.Reticle:
                    if (!forceClamp)
                    {
                        if (ReticleIndex < Reticles.Count - 1)
                        {
                            ReticleIndex++;
                        }
                        else
                        {
                            ReticleIndex = 0;
                        }
                        UpdateScopeParams();
                        didChange = true;
                    }
                    else if (ReticleIndex < Reticles.Count - 1)
                    {
                        ReticleIndex++;
                        UpdateScopeParams();
                        didChange = true;
                    }
                    break;
                case PipScopeOptionType.ReticleColor:
                    if (!forceClamp)
                    {
                        if (ReticleColorIndex < ReticleColors.Count - 1)
                        {
                            ReticleColorIndex++;
                        }
                        else
                        {
                            ReticleColorIndex = 0;
                        }
                        UpdateScopeParams();
                        didChange = true;
                    }
                    else if (ReticleColorIndex < ReticleColors.Count - 1)
                    {
                        ReticleColorIndex++;
                        UpdateScopeParams();
                        didChange = true;
                    }
                    break;
                case PipScopeOptionType.ReticleElevation:
                    ReticleElevationMagnitude++;
                    UpdateScopeParams();
                    didChange = true;
                    break;
                case PipScopeOptionType.ReticleWindage:
                    ReticleWindageMagnitude++;
                    UpdateScopeParams();
                    didChange = true;
                    break;
                //case PipScopeOptionType.Flipping:
                //    if (Flipped == 0)
                //    {
                //        Flipped = 1;
                //    }
                //    else
                //    {
                //        Flipped = 0;
                //    }
                //    UI.PlayClick();
                //    didChange = true;
                //    break;
                //case PipScopeOptionType.NightVisionColor:
                //    if (!forceClamp)
                //    {
                //        if (NVColorIndex < PScope.nvDef.availableColors.Length - 1)
                //        {
                //            NVColorIndex++;
                //        }
                //        else
                //        {
                //            NVColorIndex = 0;
                //        }
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    else if (NVColorIndex < PScope.nvDef.availableColors.Length - 1)
                //    {
                //        NVColorIndex++;
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    break;
                //case PipScopeOptionType.ThermalColorMap:
                //    if (!forceClamp)
                //    {
                //        if (ThermalColorIndex < ThermalColorSettings.Count - 1)
                //        {
                //            ThermalColorIndex++;
                //        }
                //        else
                //        {
                //            ThermalColorIndex = 0;
                //        }
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    else if (ThermalColorIndex < ThermalColorSettings.Count - 1)
                //    {
                //        ThermalColorIndex++;
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    break;
                //case PipScopeOptionType.ThermalDigitalZoom:
                //    if (!forceClamp)
                //    {
                //        if (ThermalZoomIndex < ThermalDigitalZoomMagnifications.Count - 1)
                //        {
                //            ThermalZoomIndex++;
                //        }
                //        else
                //        {
                //            ThermalZoomIndex = 0;
                //        }
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    else if (ThermalZoomIndex < ThermalDigitalZoomMagnifications.Count - 1)
                //    {
                //        ThermalZoomIndex++;
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    break;
                //case PipScopeOptionType.NightVisionDef:
                //    if (!forceClamp)
                //    {
                //        if (NVDefIndex < NVDefs.Count - 1)
                //        {
                //            NVDefIndex++;
                //        }
                //        else
                //        {
                //            NVDefIndex = 0;
                //        }
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    else if (NVDefIndex < NVDefs.Count - 1)
                //    {
                //        NVDefIndex++;
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    break;
            }
            if (UI != null)
            {
                UI.RedrawUI();
            }
        }

        public void DecrementOptionValue(PipScopeOptionType oType, ref bool didChange, bool forceLoop = false, bool forceClamp = false)
        {
            switch (oType)
            {
                //case PipScopeOptionType.Magnification:
                //    if (forceLoop)
                //    {
                //        if (MagnificationIndex > 0)
                //        {
                //            MagnificationIndex--;
                //        }
                //        else
                //        {
                //            MagnificationIndex = MagnificationValues.Count - 1;
                //        }
                //        MagnificationOverride = 0f;
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    else if (MagnificationIndex > 0)
                //    {
                //        MagnificationIndex--;
                //        MagnificationOverride = 0f;
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    break;
                case PipScopeOptionType.ZeroDistance:
                    if (forceLoop)
                    {
                        if (ZeroDistanceIndex > 0)
                        {
                            ZeroDistanceIndex--;
                        }
                        else
                        {
                            ZeroDistanceIndex = ZeroDistanceValues.Count - 1;
                        }
                        UpdateScopeParams();
                        didChange = true;
                    }
                    else if (ZeroDistanceIndex > 0)
                    {
                        ZeroDistanceIndex--;
                        UpdateScopeParams();
                        didChange = true;
                    }
                    break;
                //case PipScopeOptionType.ScopeElevation:
                //    ScopeElevationMagnitude--;
                //    UpdateScopeParams();
                //    didChange = true;
                //    break;
                //case PipScopeOptionType.ScopeWindage:
                //    ScopeWindageMagnitude--;
                //    UpdateScopeParams();
                //    didChange = true;
                //    break;
                case PipScopeOptionType.Reticle:
                    if (!forceClamp)
                    {
                        if (ReticleIndex > 0)
                        {
                            ReticleIndex--;
                        }
                        else
                        {
                            ReticleIndex = Reticles.Count - 1;
                        }
                        UpdateScopeParams();
                        didChange = true;
                    }
                    else if (ReticleIndex > 0)
                    {
                        ReticleIndex--;
                        UpdateScopeParams();
                        didChange = true;
                    }
                    break;
                case PipScopeOptionType.ReticleColor:
                    if (!forceClamp)
                    {
                        if (ReticleColorIndex > 0)
                        {
                            ReticleColorIndex--;
                        }
                        else
                        {
                            ReticleColorIndex = ReticleColors.Count - 1;
                        }
                        UpdateScopeParams();
                        didChange = true;
                    }
                    else if (ReticleColorIndex > 0)
                    {
                        ReticleColorIndex--;
                        UpdateScopeParams();
                        didChange = true;
                    }
                    break;
                case PipScopeOptionType.ReticleElevation:
                    ReticleElevationMagnitude--;
                    UpdateScopeParams();
                    didChange = true;
                    break;
                case PipScopeOptionType.ReticleWindage:
                    ReticleWindageMagnitude--;
                    UpdateScopeParams();
                    didChange = true;
                    break;
                //case PipScopeOptionType.Flipping:
                //    if (Flipped == 0)
                //    {
                //        Flipped = 1;
                //    }
                //    else
                //    {
                //        Flipped = 0;
                //    }
                //    UI.PlayClack();
                //    didChange = true;
                //    break;
                //case PipScopeOptionType.NightVisionColor:
                //    if (!forceClamp)
                //    {
                //        if (NVColorIndex > 0)
                //        {
                //            NVColorIndex--;
                //        }
                //        else
                //        {
                //            NVColorIndex = PScope.nvDef.availableColors.Length - 1;
                //        }
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    else if (NVColorIndex > 0)
                //    {
                //        NVColorIndex--;
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    break;
                //case PipScopeOptionType.ThermalColorMap:
                //    if (!forceClamp)
                //    {
                //        if (ThermalColorIndex > 0)
                //        {
                //            ThermalColorIndex--;
                //        }
                //        else
                //        {
                //            ThermalColorIndex = ThermalColorSettings.Count - 1;
                //        }
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    else if (ThermalColorIndex > 0)
                //    {
                //        ThermalColorIndex--;
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    break;
                //case PipScopeOptionType.ThermalDigitalZoom:
                //    if (!forceClamp)
                //    {
                //        if (ThermalZoomIndex > 0)
                //        {
                //            ThermalZoomIndex--;
                //        }
                //        else
                //        {
                //            ThermalZoomIndex = ThermalDigitalZoomMagnifications.Count - 1;
                //        }
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    else if (ThermalZoomIndex > 0)
                //    {
                //        ThermalZoomIndex--;
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    break;
                //case PipScopeOptionType.NightVisionDef:
                //    if (!forceClamp)
                //    {
                //        if (NVDefIndex > 0)
                //        {
                //            NVDefIndex--;
                //        }
                //        else
                //        {
                //            NVDefIndex = NVDefs.Count - 1;
                //        }
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    else if (NVDefIndex > 0)
                //    {
                //        NVDefIndex--;
                //        UpdateScopeParams();
                //        didChange = true;
                //    }
                //    break;
            }
            if (UI != null)
            {
                UI.RedrawUI();
            }
        }

        public void UpdateScopeParams()
        {
            FVRFireArm fvrfireArm = null;
            Transform muzzlePoint = null;
            if (GM.Options.SimulationOptions.ParallaxSim == SimulationOptions.ScopeParallaxSim.On)
            {
                PIPScope.useParallax = true;
            }
            else
            {
                PIPScope.useParallax = false;
            }
            if (OverrideFireArm != null)
            {
                fvrfireArm = OverrideFireArm;
            }
            else if (Attachment != null && Attachment.curMount != null && Attachment.curMount.Parent is FVRFireArm)
            {
                fvrfireArm = Attachment.curMount.Parent as FVRFireArm;
            }
            if (OverrideMuzzle != null)
            {
                muzzlePoint = OverrideMuzzle;
            }
            else if (fvrfireArm != null)
            {
                muzzlePoint = fvrfireArm.GetMuzzle();
            }
            //if (MagnificationValues.Count > 0)
            //{
            //    PScope.currentMagnification = ((MagnificationOverride <= 0f) ? MagnificationValues[MagnificationIndex] : MagnificationOverride);
            //}
            float currentZeroDistance = FixedBaseZero;
            if (ZeroDistanceValues.Count > 0)
            {
                currentZeroDistance = ZeroDistanceValues[ZeroDistanceIndex];
            }
            float bulletDropAtDistance = 0f;
            if (fvrfireArm != null)
            {
                FireArmRoundType roundType = fvrfireArm.RoundType;
                if (AM.SRoundDisplayDataDic.ContainsKey(roundType))
                {
                    FVRFireArmRoundDisplayData fvrfireArmRoundDisplayData = AM.SRoundDisplayDataDic[roundType];
                    bulletDropAtDistance = fvrfireArmRoundDisplayData.BulletDropCurve.Evaluate(currentZeroDistance * 0.001f);
                }
            }
            if (muzzlePoint != null)
            {
                Vector3 vector = muzzlePoint.position + muzzlePoint.forward * currentZeroDistance + muzzlePoint.up * bulletDropAtDistance;
                ZDist = currentZeroDistance;
                Yoffset = bulletDropAtDistance;
                //bool behindView = Vector3.Dot(muzzlePoint.forward, PScope.scopeCamTransform.forward) < 0f;
                //Vector2 vector2 = PScope.ZeroToWorldPoint(vector, behindView);
                //if (FixedBaseZero <= 0f)
                //{
                //    vector2 = Vector2.zero;
                //}
                //if (ZeroingMode == ZeroingMode.Scope)
                //{
                //    float num3 = (float)ScopeElevationMagnitude * ScopeElevationAdjustmentPerTick;
                //    if (ZeroScaling == PipScopeZeroScaling.MOA)
                //    {
                //        num3 *= 0.016666668f;
                //    }
                //    else if (ZeroScaling == PipScopeZeroScaling.MIL)
                //    {
                //        num3 *= 0.05625f;
                //    }
                //    else if (ZeroScaling == PipScopeZeroScaling.MRAD)
                //    {
                //        num3 *= 0.05729578f;
                //    }
                //    float num4 = (float)ScopeWindageMagnitude * ScopeWindageAdjustmentPerTick;
                //    if (ZeroScaling == PipScopeZeroScaling.MOA)
                //    {
                //        num4 *= 0.016666668f;
                //    }
                //    else if (ZeroScaling == PipScopeZeroScaling.MIL)
                //    {
                //        num4 *= 0.05625f;
                //    }
                //    else if (ZeroScaling == PipScopeZeroScaling.MRAD)
                //    {
                //        num4 *= 0.05729578f;
                //    }
                //    PScope.scopeAdjustmentDegrees = new Vector2(vector2.x + num4, vector2.y + num3);
                //}
                if (ZeroingMode == ZeroingMode.Reticle)
                {
                    float elevationAdjustment = (float)ReticleElevationMagnitude * ReticleElevationAdjustmentPerTick;
                    if (ZeroScaling == PipScopeZeroScaling.MOA)
                    {
                        elevationAdjustment *= 0.016666668f;
                    }
                    else if (ZeroScaling == PipScopeZeroScaling.MIL)
                    {
                        elevationAdjustment *= 0.05625f;
                    }
                    else if (ZeroScaling == PipScopeZeroScaling.MRAD)
                    {
                        elevationAdjustment *= 0.05729578f;
                    }
                    float windageAdjustment = (float)ReticleWindageMagnitude * ReticleWindageAdjustmentPerTick;
                    if (ZeroScaling == PipScopeZeroScaling.MOA)
                    {
                        windageAdjustment *= 0.016666668f;
                    }
                    else if (ZeroScaling == PipScopeZeroScaling.MIL)
                    {
                        windageAdjustment *= 0.05625f;
                    }
                    else if (ZeroScaling == PipScopeZeroScaling.MRAD)
                    {
                        windageAdjustment *= 0.05729578f;
                    }
                    _reticleAdjustmentDegrees = new Vector2(/* vector2.x + */ windageAdjustment,/* vector2.y + */ elevationAdjustment);

                    Zero();
                }
            }
            if (Reticles.Count > 0)
            {
                //PScope.reticle = Reticles[ReticleIndex];
                Sight.ReticleTexture = Reticles[ReticleIndex].reticleTexture;
            }
            if (ReticleColors.Count > 0)
            {
                //PScope.reticleIllumination = ReticleColors[ReticleColorIndex];
                //if (ThermalReticleColorInverts != null && ThermalReticleColorInverts.Count > ReticleColorIndex)
                //{
                //    PScope.thermalInvertReticle = ThermalReticleColorInverts[ReticleColorIndex];
                //}
                Sight.ReticleColor = ReticleColors[ReticleColorIndex];
            }
            //if (ReticleFullIllumination.Count > 0)
            //{
            //    PScope.reticleFullIlluminationOverride = ReticleFullIllumination[Mathf.Clamp(ReticleColorIndex, 0, ReticleFullIllumination.Count - 1)];
            //}
            //if (NVDefs.Count > 0)
            //{
            //    PScope.nvDef = NVDefs[NVDefIndex];
            //}
            //if (PScope.nvDef != null && PScope.nvDef.availableColors.Length > 0)
            //{
            //    if (NVColorIndex >= PScope.nvDef.availableColors.Length)
            //    {
            //        NVColorIndex = 0;
            //    }
            //    PScope.nightVisionColor = PScope.nvDef.availableColors[NVColorIndex];
            //}
            //if (ThermalColorSettings.Count > 0)
            //{
            //    PScope.thermalLUT = ThermalColorSettings[ThermalColorIndex].ThermalColorMap;
            //    PScope.enableThermalEdgeDetect = ThermalColorSettings[ThermalColorIndex].UsesEdgeDetect;
            //}
            //if (ThermalDigitalZoomMagnifications.Count > 0)
            //{
            //    PScope.thermalDigitalZoom = ThermalDigitalZoomMagnifications[ThermalZoomIndex];
            //}
            for (int i = 0; i < Components.Count; i++)
            {
                PIPScopeComponent pipscopeComponent = Components[i];
                int indexValueOfOption = GetIndexValueOfOption(pipscopeComponent.OptionType);
                float value;
                if (pipscopeComponent.UsesIncrementalValues)
                {
                    value = pipscopeComponent.IncrementalValue * (float)indexValueOfOption;
                }
                else
                {
                    value = pipscopeComponent.Values[indexValueOfOption];
                }
                SetComponentState(pipscopeComponent.Geo, value, pipscopeComponent.Interp, pipscopeComponent.Axis);
            }
            //PScope.UpdateParameters();
        }

        public int GetIndexValueOfOption(PipScopeOptionType oType)
        {
            switch (oType)
            {
                //case PipScopeOptionType.Magnification:
                //    return MagnificationIndex;
                case PipScopeOptionType.ZeroDistance:
                    return ZeroDistanceIndex;
                case PipScopeOptionType.Reticle:
                    return ReticleIndex;
                case PipScopeOptionType.ReticleColor:
                    return ReticleColorIndex;
                case PipScopeOptionType.ReticleElevation:
                    return ReticleElevationMagnitude;
                case PipScopeOptionType.ReticleWindage:
                    return ReticleWindageMagnitude;
                case PipScopeOptionType.Flipping:
                //    return Flipped;
                //case PipScopeOptionType.NightVisionColor:
                //    return NVColorIndex;
                //case PipScopeOptionType.ThermalColorMap:
                //    return ThermalColorIndex;
                //case PipScopeOptionType.ThermalDigitalZoom:
                //    return ThermalZoomIndex;
                //case PipScopeOptionType.NightVisionDef:
                //    return NVDefIndex;
                default:
                    return 0;
            }
        }

        public void SetComponentState(Transform t, float val, FVRPhysicalObject.InterpStyle interp, FVRPhysicalObject.Axis axis)
        {
            if (interp != FVRPhysicalObject.InterpStyle.Rotation)
            {
                if (interp == FVRPhysicalObject.InterpStyle.Translate)
                {
                    Vector3 localPosition = t.localPosition;
                    if (axis != FVRPhysicalObject.Axis.X)
                    {
                        if (axis != FVRPhysicalObject.Axis.Y)
                        {
                            if (axis == FVRPhysicalObject.Axis.Z)
                            {
                                localPosition.z = val;
                            }
                        }
                        else
                        {
                            localPosition.y = val;
                        }
                    }
                    else
                    {
                        localPosition.x = val;
                    }
                    t.localPosition = localPosition;
                }
            }
            else
            {
                Vector3 zero = Vector3.zero;
                if (axis != FVRPhysicalObject.Axis.X)
                {
                    if (axis != FVRPhysicalObject.Axis.Y)
                    {
                        if (axis == FVRPhysicalObject.Axis.Z)
                        {
                            zero.z = val;
                        }
                    }
                    else
                    {
                        zero.y = val;
                    }
                }
                else
                {
                    zero.x = val;
                }
                t.localEulerAngles = zero;
            }
        }

        private void Zero()
        {
            if (Attachment != null && Attachment.curMount != null && Attachment.curMount.Parent != null && Attachment.curMount.Parent is FVRFireArm fvrfireArm)
            {
                Vector3 target = fvrfireArm.CurrentMuzzle.position + fvrfireArm.CurrentMuzzle.forward * ZeroDistanceValues[ZeroDistanceIndex] + fvrfireArm.CurrentMuzzle.up * Yoffset;
                
                TargetAimer.LookAt(target, Vector3.up);
                TargetAimer.Rotate(transform.up, _reticleAdjustmentDegrees.x);
                TargetAimer.Rotate(-transform.right, _reticleAdjustmentDegrees.y);
            }
            else if (BackupMuzzle != null)
            {
                Vector3 target = BackupMuzzle.position + BackupMuzzle.forward * ZeroDistanceValues[ZeroDistanceIndex] + BackupMuzzle.up * Yoffset;
                TargetAimer.LookAt(target, Vector3.up);
                TargetAimer.Rotate(transform.up, _reticleAdjustmentDegrees.x);
                TargetAimer.Rotate(-transform.right, _reticleAdjustmentDegrees.y);
            }
            else
            {
                TargetAimer.localRotation = Quaternion.identity;
                TargetAimer.Rotate(transform.up, _reticleAdjustmentDegrees.x);
                TargetAimer.Rotate(-transform.right, _reticleAdjustmentDegrees.y);
            }
            //this.ZeroingText.text = "Zero Distance: " + this.m_zeroDistances[this.m_zeroDistanceIndex].ToString() + "m";
        }
    }
}
