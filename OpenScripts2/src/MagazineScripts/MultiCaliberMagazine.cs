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
    public class MultiCaliberMagazine : OpenScripts2_BasePlugin
    {
        public FVRFireArmMagazine Magazine;

        public List<CaliberDefinition> CaliberDefinitions;

        [Header("Text field that shows current selected caliber on switching calibers manually.")]
        public GameObject TextRoot;
        public Text TextField;
        public bool AlwaysShowText;

        public int CurrentCaliberDefinition = 0;
        [Serializable]
        public class CaliberDefinition
        {
            [SearchableEnum]
            public FireArmRoundType RoundType;
            public int Capacity;
            public GameObject[] DisplayBullets;
            public MeshFilter[] DisplayMeshFilters;
            public Renderer[] DisplayRenderers;
            [SearchableEnum]
            public FVRFireArmMechanicalAccuracyClass AccuracyClass;
            public FVRFireArmRecoilProfile RecoilProfile;
            public FVRFireArmRecoilProfile RecoilProfileStocked;
            public FVRFirearmAudioSet ReplacementFiringSounds;
            public FVRObject ObjectWrapper;
        }

        [Tooltip("Only allows insertion of mag into firearm if the caliber of the mag and the gun are equal")]
        public bool ChecksFirearmCompatibility;

        private FVRFireArm _fireArm = null;
        
        private FVRFireArmMechanicalAccuracyClass _origAccuracyClass;

        private FVRFireArmRecoilProfile _origRecoilProfile;
        private FVRFireArmRecoilProfile _origRecoilProfileStocked;

        private AudioEvent _origFiringSounds;
        private AudioEvent _origSuppressedSounds;
        private AudioEvent _origLowPressureSounds;

        public void Awake()
        {
            Hook();
        }

        public void Start()
        {
            PrepareCaliberDefinitions();
            if (!AlwaysShowText && TextRoot != null) TextRoot.SetActive(false);
        }

        public void OnDestroy()
        {
            Unhook();
        }

        public void Update()
        {
            FVRViveHand hand = Magazine.m_hand;
            if (hand != null)
            {
                if (Magazine.m_numRounds == 0)
                {
                    if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f)
                    {
                        NextCartridge();
                        if (!AlwaysShowText && TextField != null)
                        {
                            StopCoroutine("ShowCaliberText");
                            StartCoroutine("ShowCaliberText");
                        }
                    }
                }
            }

            if (Magazine.State == FVRFireArmMagazine.MagazineState.Locked && _fireArm == null)
            {
                _fireArm = Magazine.FireArm;

                _origAccuracyClass = _fireArm.AccuracyClass;

                _origRecoilProfile = _fireArm.RecoilProfile;
                _origRecoilProfileStocked = _fireArm.RecoilProfileStocked;

                _origFiringSounds = _fireArm.AudioClipSet.Shots_Main;
                _origSuppressedSounds = _fireArm.AudioClipSet.Shots_Suppressed;
                _origLowPressureSounds = _fireArm.AudioClipSet.Shots_LowPressure;

                if (CaliberDefinitions[CurrentCaliberDefinition].AccuracyClass != FVRFireArmMechanicalAccuracyClass.None)
                    _fireArm.AccuracyClass = CaliberDefinitions[CurrentCaliberDefinition].AccuracyClass;
                if (CaliberDefinitions[CurrentCaliberDefinition].RecoilProfile != null)
                    _fireArm.RecoilProfile = CaliberDefinitions[CurrentCaliberDefinition].RecoilProfile;
                if (CaliberDefinitions[CurrentCaliberDefinition].RecoilProfileStocked != null)
                    _fireArm.RecoilProfileStocked = CaliberDefinitions[CurrentCaliberDefinition].RecoilProfileStocked;

                if (CaliberDefinitions[CurrentCaliberDefinition].ReplacementFiringSounds != null) ReplaceFiringSounds(CaliberDefinitions[CurrentCaliberDefinition].ReplacementFiringSounds);

            }
            else if (Magazine.State == FVRFireArmMagazine.MagazineState.Free && _fireArm != null)
            {
                _fireArm.AccuracyClass = _origAccuracyClass;

                _fireArm.RecoilProfile = _origRecoilProfile;
                _fireArm.RecoilProfileStocked = _origRecoilProfileStocked;

                _fireArm.AudioClipSet.Shots_Main = _origFiringSounds;
                _fireArm.AudioClipSet.Shots_Suppressed = _origSuppressedSounds;
                _fireArm.AudioClipSet.Shots_LowPressure = _origLowPressureSounds;

                _fireArm = null;
            }
            else if (Magazine.State == FVRFireArmMagazine.MagazineState.Locked && _fireArm != null && _fireArm.RoundType != CaliberDefinitions[CurrentCaliberDefinition].RoundType)
            {
                if (!SetCartridge(_fireArm.RoundType) && Magazine.m_numRounds == 0 && ChecksFirearmCompatibility)
                {
                    _fireArm.EjectMag();
                }
            }

            if (AlwaysShowText && TextField != null)
            {
                FireArmRoundType roundType = CaliberDefinitions[CurrentCaliberDefinition].RoundType;
                if (AM.SRoundDisplayDataDic.ContainsKey(roundType))
                {
                    string name = AM.SRoundDisplayDataDic[roundType].DisplayName;

                    TextField.text = name;
                }
            }
        }

        public void NextCartridge()
        {
            CurrentCaliberDefinition++;
            if (CurrentCaliberDefinition >= CaliberDefinitions.Count)
            {
                CurrentCaliberDefinition = 0;
            }

            ConfigureMagazine(CurrentCaliberDefinition);
        }

        public bool SetCartridge(FireArmRoundType fireArmRoundType)
        {
            if (Magazine.m_numRounds != 0) return false;
            
            int chosenDefinition = 0;
            foreach (var caliberDefinition in CaliberDefinitions)
            {
                if (caliberDefinition.RoundType != fireArmRoundType)
                {
                    chosenDefinition++;
                }
                else break;
            }

            if (chosenDefinition == CaliberDefinitions.Count)
            {
                return false;
            }
            else 
            {
                ConfigureMagazine(chosenDefinition);
                CurrentCaliberDefinition = chosenDefinition;
                return true;
            }
        }

        public void ConfigureMagazine(int CaliberDefinitionIndex)
        {
            Magazine.RoundType = CaliberDefinitions[CaliberDefinitionIndex].RoundType;
            if (CaliberDefinitions[CaliberDefinitionIndex].Capacity > 0)
            {
                Magazine.m_capacity = CaliberDefinitions[CaliberDefinitionIndex].Capacity;
            }
            if (CaliberDefinitions[CaliberDefinitionIndex].DisplayBullets.Length > 0)
            {
                Magazine.m_roundInsertionTarget.localPosition = CaliberDefinitions[CaliberDefinitionIndex].DisplayBullets[0].transform.localPosition;
                Magazine.m_roundInsertionTarget.localRotation = CaliberDefinitions[CaliberDefinitionIndex].DisplayBullets[0].transform.localRotation;
                Magazine.DisplayBullets = CaliberDefinitions[CaliberDefinitionIndex].DisplayBullets;
                Magazine.DisplayMeshFilters = CaliberDefinitions[CaliberDefinitionIndex].DisplayMeshFilters;
                Magazine.DisplayRenderers = CaliberDefinitions[CaliberDefinitionIndex].DisplayRenderers;

                Magazine.m_DisplayStartPositions = new Vector3[CaliberDefinitions[CaliberDefinitionIndex].DisplayBullets.Length];
                for (int i = 0; i < CaliberDefinitions[CaliberDefinitionIndex].DisplayBullets.Length; i++)
                {
                    if (CaliberDefinitions[CaliberDefinitionIndex].DisplayBullets[i] != null)
                    {
                        Magazine.m_DisplayStartPositions[i] = CaliberDefinitions[CaliberDefinitionIndex].DisplayBullets[i].transform.localPosition;
                    }
                }
            }
            Magazine.ObjectWrapper = CaliberDefinitions[CaliberDefinitionIndex].ObjectWrapper;
        }

        public void ReplaceFiringSounds(FVRFirearmAudioSet set)
        {
            if (set.Shots_Main.Clips.Count > 0) _fireArm.AudioClipSet.Shots_Main = set.Shots_Main;
            if (set.Shots_Suppressed.Clips.Count > 0) _fireArm.AudioClipSet.Shots_Suppressed = set.Shots_Suppressed;
            if (set.Shots_LowPressure.Clips.Count > 0) _fireArm.AudioClipSet.Shots_LowPressure = set.Shots_LowPressure;
        }

        public void Unhook()
        {
#if !DEBUG
            On.FistVR.FVRFireArmRound.OnTriggerEnter -= FVRFireArmRound_OnTriggerEnter;

            if (ChecksFirearmCompatibility)
            {
                On.FistVR.FVRFireArmReloadTriggerMag.OnTriggerEnter -= FVRFireArmReloadTriggerMag_OnTriggerEnter;
            }
#endif
        }

        public void Hook()
        {
#if !DEBUG
            On.FistVR.FVRFireArmRound.OnTriggerEnter += FVRFireArmRound_OnTriggerEnter;

            if (ChecksFirearmCompatibility)
            {
                On.FistVR.FVRFireArmReloadTriggerMag.OnTriggerEnter += FVRFireArmReloadTriggerMag_OnTriggerEnter;
            }
#endif
        }

#if !DEBUG
        private void FVRFireArmReloadTriggerMag_OnTriggerEnter(On.FistVR.FVRFireArmReloadTriggerMag.orig_OnTriggerEnter orig, FVRFireArmReloadTriggerMag self, Collider collider)
        {
            if (Magazine == self.Magazine)
            {
                if (self.Magazine == null || self.Magazine.FireArm != null || self.Magazine.QuickbeltSlot != null || collider.gameObject.CompareTag(nameof(FVRFireArmReloadTriggerWell))) return;
                FVRFireArmReloadTriggerWell reloadTriggerWell = collider.gameObject.GetComponent<FVRFireArmReloadTriggerWell>();
                bool beltCheck = false;
                if (reloadTriggerWell != null && !self.Magazine.IsBeltBox && reloadTriggerWell.FireArm.HasBelt) beltCheck = true;
                if (reloadTriggerWell == null || reloadTriggerWell.IsBeltBox != self.Magazine.IsBeltBox || reloadTriggerWell.FireArm == null || reloadTriggerWell.FireArm.Magazine != null || beltCheck) return;
                FireArmMagazineType fireArmMagazineType = reloadTriggerWell.FireArm.MagazineType;
                if (reloadTriggerWell.UsesTypeOverride) fireArmMagazineType = reloadTriggerWell.TypeOverride;
                if (fireArmMagazineType != self.Magazine.MagazineType || reloadTriggerWell.FireArm.EjectDelay > 0.0f && self.Magazine == reloadTriggerWell.FireArm.LastEjectedMag || reloadTriggerWell.FireArm.Magazine != null) return;
                if (ChecksFirearmCompatibility && Magazine.RoundType != reloadTriggerWell.FireArm.RoundType) return;
                self.Magazine.Load(reloadTriggerWell.FireArm);
            }
            else orig(self, collider);
        }

        private void FVRFireArmRound_OnTriggerEnter(On.FistVR.FVRFireArmRound.orig_OnTriggerEnter orig, FVRFireArmRound self, Collider collider)
        {
            if (self.IsSpent)
                return;
            if (self.isManuallyChamberable && !self.IsSpent && self.HoveredOverChamber == null && self.m_hoverOverReloadTrigger == null && !self.IsSpent && collider.gameObject.CompareTag(nameof(FVRFireArmChamber)))
            {
                FVRFireArmChamber component = collider.gameObject.GetComponent<FVRFireArmChamber>();
                if (component.RoundType == self.RoundType && component.IsManuallyChamberable && component.IsAccessible && !component.IsFull)
                {
                    self.HoveredOverChamber = component;
                }
            }
            if (self.isMagazineLoadable && self.HoveredOverChamber == null && !self.IsSpent && collider.gameObject.CompareTag(nameof(FVRFireArmMagazineReloadTrigger)))
            {
                FVRFireArmMagazineReloadTrigger component = collider.gameObject.GetComponent<FVRFireArmMagazineReloadTrigger>();
                if (component.IsClipTrigger)
                {
                    if (component != null && component.Clip != null && component.Clip.RoundType == self.RoundType && !component.Clip.IsFull() && (component.Clip.FireArm == null || component.Clip.IsDropInLoadable))
                    {
                        self.m_hoverOverReloadTrigger = component;
                    }
                }
                else if (component.IsSpeedloaderTrigger)
                {
                    if (!component.SpeedloaderChamber.IsLoaded)
                    {
                        self.m_hoverOverReloadTrigger = component;
                    }
                }
                else if (component != null && component.Magazine != null && component.Magazine.RoundType == self.RoundType && !component.Magazine.IsFull() && (component.Magazine.FireArm == null || component.Magazine.IsDropInLoadable))
                {
                    self.m_hoverOverReloadTrigger = component;
                }
                else if (component != null && component.Magazine == Magazine && !component.Magazine.IsFull() && (component.Magazine.FireArm == null || component.Magazine.IsDropInLoadable))
                {
                    MultiCaliberMagazine multiCaliberMagazine = component.Magazine.GetComponent<MultiCaliberMagazine>();
                    if (multiCaliberMagazine.SetCartridge(self.RoundType))
                    {
                        self.m_hoverOverReloadTrigger = component;
                    }
                }
            }
            if (!self.isPalmable || self.ProxyRounds.Count >= self.MaxPalmedAmount || self.IsSpent || !collider.gameObject.CompareTag(nameof(FVRFireArmRound))) return;
            FVRFireArmRound component1 = collider.gameObject.GetComponent<FVRFireArmRound>();
            if (component1.RoundType != self.RoundType || component1.IsSpent || component1.QuickbeltSlot != null) return;
            self.HoveredOverRound = component1;
        }
#endif
        private IEnumerator ShowCaliberText()
        {
            FireArmRoundType roundType = CaliberDefinitions[CurrentCaliberDefinition].RoundType;
            if (AM.SRoundDisplayDataDic.ContainsKey(roundType))
            {
                string name = AM.SRoundDisplayDataDic[roundType].DisplayName;

                TextField.text = name;
                TextRoot.SetActive(true);
                yield return new WaitForSeconds(1f);
            }

            TextRoot.SetActive(false);
            yield return null;
        }

        private void PrepareCaliberDefinitions()
        {
            foreach (var caliberDefinition in CaliberDefinitions)
            {
                for (int i = 0; i < caliberDefinition.DisplayMeshFilters.Length; i++)
                {
                    caliberDefinition.DisplayMeshFilters[i].mesh = null;
                }
                for (int i = 0; i < caliberDefinition.DisplayRenderers.Length; i++)
                {
                    caliberDefinition.DisplayRenderers[i].material = null;
                }
            }
        }
    }
}
