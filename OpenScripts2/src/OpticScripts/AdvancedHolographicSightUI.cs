using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace OpenScripts2
{
    [Obsolete("USe vanilla reflex sight system instead.")]
    public class AdvancedHolographicSightUI : OpenScripts2_BasePlugin
    {
        public PIPScopeUI UI;

        private static readonly Dictionary<PIPScopeUI, AdvancedHolographicSightUI> _existingAdvancedHolographicSightUIs = new();
        private AdvancedHolographicSightController _controller;

        //public void Awake()
        //{
        //    _existingAdvancedHolographicSightUIs.Add(UI, this);
        //}

        public void OnDestroy()
        {
            _existingAdvancedHolographicSightUIs.Remove(UI);
        }

#if !DEBUG
        static AdvancedHolographicSightUI()
        {
            On.FistVR.PIPScopeUI.BTNPress_Close += PIPScopeUI_BTNPress_Close;
            On.FistVR.PIPScopeUI.BTNPress_OptionDetailNext += PIPScopeUI_BTNPress_OptionDetailNext;
            On.FistVR.PIPScopeUI.BTNPress_OptionDetailPrev += PIPScopeUI_BTNPress_OptionDetailPrev;
            On.FistVR.PIPScopeUI.RedrawUI += PIPScopeUI_RedrawUI;
        }

        private static void PIPScopeUI_RedrawUI(On.FistVR.PIPScopeUI.orig_RedrawUI orig, PIPScopeUI self)
        {
            if (_existingAdvancedHolographicSightUIs.TryGetValue(self, out AdvancedHolographicSightUI advancedHolographicSightUI))
            {
                if (self.m_displayedTypes.Count < 1)
                {
                    return;
                }
                PipScopeOptionType pipScopeOptionType = self.m_displayedTypes[self.m_optionDisplayed];
                string text = " DEG";
                if (advancedHolographicSightUI._controller.ZeroScaling == PipScopeZeroScaling.MOA)
                {
                    text = " MOA";
                }
                else if (advancedHolographicSightUI._controller.ZeroScaling == PipScopeZeroScaling.MIL)
                {
                    text = " MIL";
                }
                else if (advancedHolographicSightUI._controller.ZeroScaling == PipScopeZeroScaling.MRAD)
                {
                    text = " MRAD";
                }
                switch (pipScopeOptionType)
                {
                    case PipScopeOptionType.ZeroDistance:
                        self.TXT_OptionTypeName.text = "Base Zero";
                        self.TXT_OptionDetailName.text = advancedHolographicSightUI._controller.ZeroDistanceValues[advancedHolographicSightUI._controller.ZeroDistanceIndex].ToString() + "m";
                        if (advancedHolographicSightUI._controller.ZeroDistanceIndex < advancedHolographicSightUI._controller.ZeroDistanceValues.Count - 1)
                        {
                            self.TXT_OptionDetailNext.text = advancedHolographicSightUI._controller.ZeroDistanceValues[advancedHolographicSightUI._controller.ZeroDistanceIndex + 1].ToString() + "m";
                            self.OptionDetailNextButton.SetActive(true);
                        }
                        else
                        {
                            self.OptionDetailNextButton.SetActive(false);
                        }
                        if (advancedHolographicSightUI._controller.ZeroDistanceIndex > 0)
                        {
                            self.TXT_OptionDetailPrev.text = advancedHolographicSightUI._controller.ZeroDistanceValues[advancedHolographicSightUI._controller.ZeroDistanceIndex - 1].ToString() + "m";
                            self.OptionDetailPrevButton.SetActive(true);
                        }
                        else
                        {
                            self.OptionDetailPrevButton.SetActive(false);
                        }
                        break;
                    case PipScopeOptionType.Reticle:
                        {
                            self.TXT_OptionTypeName.text = "Reticle Type";
                            int num2 = advancedHolographicSightUI._controller.ReticleIndex;
                            int num3 = num2 - 1;
                            int num4 = num2 + 1;
                            if (num3 < 0)
                            {
                                num3 = advancedHolographicSightUI._controller.DisplayNames_Reticle.Count - 1;
                            }
                            if (num4 >= advancedHolographicSightUI._controller.DisplayNames_Reticle.Count)
                            {
                                num4 = 0;
                            }
                            self.TXT_OptionDetailName.text = advancedHolographicSightUI._controller.DisplayNames_Reticle[num2];
                            self.TXT_OptionDetailPrev.text = advancedHolographicSightUI._controller.DisplayNames_Reticle[num3];
                            self.TXT_OptionDetailNext.text = advancedHolographicSightUI._controller.DisplayNames_Reticle[num4];
                            self.OptionDetailNextButton.SetActive(true);
                            self.OptionDetailPrevButton.SetActive(true);
                            break;
                        }
                    case PipScopeOptionType.ReticleColor:
                        {
                            self.TXT_OptionTypeName.text = "Reticle Color";
                            int num2 = advancedHolographicSightUI._controller.ReticleColorIndex;
                            int num3 = num2 - 1;
                            int num4 = num2 + 1;
                            if (num3 < 0)
                            {
                                num3 = advancedHolographicSightUI._controller.DisplayNames_ReticleColor.Count - 1;
                            }
                            if (num4 >= advancedHolographicSightUI._controller.DisplayNames_ReticleColor.Count)
                            {
                                num4 = 0;
                            }
                            self.TXT_OptionDetailName.text = advancedHolographicSightUI._controller.DisplayNames_ReticleColor[num2];
                            self.TXT_OptionDetailPrev.text = advancedHolographicSightUI._controller.DisplayNames_ReticleColor[num3];
                            self.TXT_OptionDetailNext.text = advancedHolographicSightUI._controller.DisplayNames_ReticleColor[num4];
                            self.OptionDetailNextButton.SetActive(true);
                            self.OptionDetailPrevButton.SetActive(true);
                            break;
                        }
                    case PipScopeOptionType.ReticleElevation:
                        self.TXT_OptionTypeName.text = "Reticle Elevation";
                        self.TXT_OptionDetailName.text = ((float)advancedHolographicSightUI._controller.ReticleElevationMagnitude * advancedHolographicSightUI._controller.ReticleElevationAdjustmentPerTick).ToString("F1") + text;
                        self.TXT_OptionDetailNext.text = "+";
                        self.OptionDetailNextButton.SetActive(true);
                        self.TXT_OptionDetailPrev.text = "-";
                        self.OptionDetailPrevButton.SetActive(true);
                        break;
                    case PipScopeOptionType.ReticleWindage:
                        self.TXT_OptionTypeName.text = "Reticle Windage";
                        self.TXT_OptionDetailName.text = ((float)advancedHolographicSightUI._controller.ReticleWindageMagnitude * advancedHolographicSightUI._controller.ReticleWindageAdjustmentPerTick).ToString("F1") + text;
                        self.TXT_OptionDetailNext.text = "+";
                        self.OptionDetailNextButton.SetActive(true);
                        self.TXT_OptionDetailPrev.text = "-";
                        self.OptionDetailPrevButton.SetActive(true);
                        break;
                }
            }
            else orig(self);
        }

        private static void PIPScopeUI_BTNPress_OptionDetailPrev(On.FistVR.PIPScopeUI.orig_BTNPress_OptionDetailPrev orig, PIPScopeUI self)
        {
            if (_existingAdvancedHolographicSightUIs.TryGetValue(self, out AdvancedHolographicSightUI UI))
            {
                SM.PlayCoreSound(FVRPooledAudioType.GenericClose, self.AudEvent_Click, self.transform.position);
                bool didChange = false;
                UI._controller.DecrementOptionValue(self.m_displayedTypes[self.m_optionDisplayed], ref didChange, false, false);
            }
            else orig(self);
        }

        private static void PIPScopeUI_BTNPress_OptionDetailNext(On.FistVR.PIPScopeUI.orig_BTNPress_OptionDetailNext orig, PIPScopeUI self)
        {
            if (_existingAdvancedHolographicSightUIs.TryGetValue(self, out AdvancedHolographicSightUI UI))
            {
                SM.PlayCoreSound(FVRPooledAudioType.GenericClose, self.AudEvent_Click, self.transform.position);
                bool didChange = false;
                UI._controller.IncrementOptionValue(self.m_displayedTypes[self.m_optionDisplayed], ref didChange, false, false);
            }
            else orig(self);
        }

        private static void PIPScopeUI_BTNPress_Close(On.FistVR.PIPScopeUI.orig_BTNPress_Close orig, PIPScopeUI self)
        {
            if (_existingAdvancedHolographicSightUIs.TryGetValue(self, out AdvancedHolographicSightUI UI))
            {
                UI._controller.CloseUI();
                SM.PlayCoreSound(FVRPooledAudioType.GenericClose, self.AudEvent_Clack, self.transform.position);
            }
            else orig(self);
        }

#endif
        public void InitAndCache(AdvancedHolographicSightController cont, PIPScopeUI ui)
        {
            UI = ui;
            _existingAdvancedHolographicSightUIs.Add(UI, this);
            _controller = cont;
            if (cont.ZeroDistanceValues.Count > 0)
            {
                UI.m_displayedTypes.Add(PipScopeOptionType.ZeroDistance);
            }
            if (cont.ReticleColors.Count > 0)
            {
                UI.m_displayedTypes.Add(PipScopeOptionType.ReticleColor);
            }
            if (cont.Reticles.Count > 0)
            {
                UI.m_displayedTypes.Add(PipScopeOptionType.Reticle);
            }
            if (cont.ReticleElevationAdjustmentPerTick > 0f)
            {
                UI.m_displayedTypes.Add(PipScopeOptionType.ReticleElevation);
            }
            if (cont.ReticleWindageAdjustmentPerTick > 0f)
            {
                UI.m_displayedTypes.Add(PipScopeOptionType.ReticleWindage);
            }
            UI.m_optionDisplayed = 0;
            UI.RedrawUI();
        }
    }
}