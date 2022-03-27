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

        public CaliberDefinition[] CaliberDefinitions;

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
        /*
        [SearchableEnum]
        public FireArmRoundClass defaultRoundClass;
        */
        [Tooltip("Only allows insertion of mag into firearm if the caliber of the mag and the gun are equal")]
        public bool ChecksFirearmCompatibility;

        //private CaliberDefinition originalCaliberDefinition;

        [Header("DEBUG required stuff. Use ContextMenu to populate.")]
        [SearchableEnum]
        public FireArmRoundType[] RoundTypes;
        public int[] Capacities;
        public GameObject[][] DisplayBulletss;
        public MeshFilter[][] DisplayMeshFilterss;
        public Renderer[][] DisplayRendererss;
        [SearchableEnum]
        public FVRFireArmMechanicalAccuracyClass[] AccuracyClasses;
        public FVRFireArmRecoilProfile[] RecoilProfiles;
        public FVRFireArmRecoilProfile[] RecoilProfilesStocked;
        public FVRFirearmAudioSet[] ReplacementFiringSoundss;
        public FVRObject[] ObjectWrappers;

        public bool IsDEBUG = false;

        private FVRFireArm _fireArm = null;
        private List<CaliberDefinition> _caliberDefinitionsList;

        private FVRFireArmMechanicalAccuracyClass _origAccuracyClass;
        private FVRFireArmRecoilProfile _origRecoilProfile;
        private FVRFireArmRecoilProfile _origRecoilProfileStocked;
        private FVRFirearmAudioSet _origAudioSet;

        private bool _isDebug = true;

        [ContextMenu("Populate DEBUG Lists")]
        public void PopulateDEBUGLists()
        {
            int definitionCount = CaliberDefinitions.Length;

            RoundTypes = new FireArmRoundType[definitionCount];
            Capacities = new int[definitionCount];
            DisplayBulletss = new GameObject[definitionCount][];
            DisplayMeshFilterss = new MeshFilter[definitionCount][];
            DisplayRendererss = new Renderer[definitionCount][];
            AccuracyClasses = new FVRFireArmMechanicalAccuracyClass[definitionCount];
            RecoilProfiles = new FVRFireArmRecoilProfile[definitionCount];
            RecoilProfilesStocked = new FVRFireArmRecoilProfile[definitionCount];
            ReplacementFiringSoundss = new FVRFirearmAudioSet[definitionCount];
            ObjectWrappers = new FVRObject[definitionCount];

            for (int i = 0; i < definitionCount; i++)
            {
                RoundTypes[i] = CaliberDefinitions[i].RoundType;
                Capacities[i] = CaliberDefinitions[i].Capacity;
                DisplayBulletss[i] = CaliberDefinitions[i].DisplayBullets;
                DisplayMeshFilterss[i] = CaliberDefinitions[i].DisplayMeshFilters;
                DisplayRendererss[i] = CaliberDefinitions[i].DisplayRenderers;
                AccuracyClasses[i] = CaliberDefinitions[i].AccuracyClass;
                RecoilProfiles[i] = CaliberDefinitions[i].RecoilProfile;
                RecoilProfilesStocked[i] = CaliberDefinitions[i].RecoilProfileStocked;
                ReplacementFiringSoundss[i] = CaliberDefinitions[i].ReplacementFiringSounds;

                ObjectWrappers[i] = CaliberDefinitions[i].ObjectWrapper;
            }

            if (_isDebug)
            {
                foreach (var DisplayBullets in DisplayBulletss)
                {
                    foreach (var DisplayBullet in DisplayBullets)
                    {
                        this.Log("DisplayBullets: " + DisplayBullet.name);
                    }
                }
            }

            IsDEBUG = true;
        }

#if!(UNITY_EDITOR || UNITY_5)
        public void Awake()
        {
            Hook();
        }

        public void Start()
        {
            if (!IsDEBUG) _caliberDefinitionsList = new List<CaliberDefinition>(CaliberDefinitions);
            else _caliberDefinitionsList = CreateListFromDEBUGDefines();

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
                _origAudioSet = _fireArm.AudioClipSet;

                if (_caliberDefinitionsList[CurrentCaliberDefinition].AccuracyClass != FVRFireArmMechanicalAccuracyClass.None)
                    _fireArm.AccuracyClass = _caliberDefinitionsList[CurrentCaliberDefinition].AccuracyClass;
                if (_caliberDefinitionsList[CurrentCaliberDefinition].RecoilProfile != null)
                    _fireArm.RecoilProfile = _caliberDefinitionsList[CurrentCaliberDefinition].RecoilProfile;
                if (_caliberDefinitionsList[CurrentCaliberDefinition].RecoilProfileStocked != null)
                    _fireArm.RecoilProfileStocked = _caliberDefinitionsList[CurrentCaliberDefinition].RecoilProfileStocked;

                if (_caliberDefinitionsList[CurrentCaliberDefinition].ReplacementFiringSounds != null) ReplaceFiringSounds(_caliberDefinitionsList[CurrentCaliberDefinition].ReplacementFiringSounds);

            }
            else if (Magazine.State == FVRFireArmMagazine.MagazineState.Free && _fireArm != null)
            {
                _fireArm.AccuracyClass = _origAccuracyClass;
                _fireArm.RecoilProfile = _origRecoilProfile;
                _fireArm.RecoilProfileStocked = _origRecoilProfileStocked;
                _fireArm.AudioClipSet = _origAudioSet;

                _fireArm = null;
            }
            else if (Magazine.State == FVRFireArmMagazine.MagazineState.Locked && _fireArm != null && _fireArm.RoundType != _caliberDefinitionsList[CurrentCaliberDefinition].RoundType)
            {
                if (!SetCartridge(_fireArm.RoundType) && Magazine.m_numRounds == 0 && ChecksFirearmCompatibility)
                {
                    _fireArm.EjectMag();
                }
            }

            if (AlwaysShowText && TextField != null)
            {
                FireArmRoundType roundType = _caliberDefinitionsList[CurrentCaliberDefinition].RoundType;
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
            if (CurrentCaliberDefinition >= _caliberDefinitionsList.Count)
            {
                CurrentCaliberDefinition = 0;
            }

            ConfigureMagazine(CurrentCaliberDefinition);
        }

        public bool SetCartridge(FireArmRoundType fireArmRoundType)
        {
            if (Magazine.m_numRounds != 0) return false;
            
            int chosenDefinition = 0;
            foreach (var caliberDefinition in _caliberDefinitionsList)
            {
                if (caliberDefinition.RoundType != fireArmRoundType)
                {
                    chosenDefinition++;
                }
                else break;
            }

            if (chosenDefinition == _caliberDefinitionsList.Count)
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
            Magazine.RoundType = _caliberDefinitionsList[CaliberDefinitionIndex].RoundType;
            if (_caliberDefinitionsList[CaliberDefinitionIndex].Capacity > 0)
                Magazine.m_capacity = _caliberDefinitionsList[CaliberDefinitionIndex].Capacity;
            if (_caliberDefinitionsList[CaliberDefinitionIndex].DisplayBullets.Length > 0)
            {
                Magazine.m_roundInsertionTarget.localPosition = _caliberDefinitionsList[CaliberDefinitionIndex].DisplayBullets[0].transform.localPosition;
                Magazine.m_roundInsertionTarget.localRotation = _caliberDefinitionsList[CaliberDefinitionIndex].DisplayBullets[0].transform.localRotation;
                Magazine.DisplayBullets = _caliberDefinitionsList[CaliberDefinitionIndex].DisplayBullets;
                Magazine.DisplayMeshFilters = _caliberDefinitionsList[CaliberDefinitionIndex].DisplayMeshFilters;
                Magazine.DisplayRenderers = _caliberDefinitionsList[CaliberDefinitionIndex].DisplayRenderers;

                Magazine.m_DisplayStartPositions = new Vector3[_caliberDefinitionsList[CaliberDefinitionIndex].DisplayBullets.Length];
                for (int i = 0; i < _caliberDefinitionsList[CaliberDefinitionIndex].DisplayBullets.Length; i++)
                {
                    if (_caliberDefinitionsList[CaliberDefinitionIndex].DisplayBullets[i] != null)
                    {
                        Magazine.m_DisplayStartPositions[i] = _caliberDefinitionsList[CaliberDefinitionIndex].DisplayBullets[i].transform.localPosition;
                    }
                }
            }
            Magazine.ObjectWrapper = _caliberDefinitionsList[CaliberDefinitionIndex].ObjectWrapper;
        }

        public void ReplaceFiringSounds(FVRFirearmAudioSet set)
        {
            if (set.Shots_Main.Clips.Count > 0) _fireArm.AudioClipSet.Shots_Main = set.Shots_Main;
            if (set.Shots_Suppressed.Clips.Count > 0) _fireArm.AudioClipSet.Shots_Suppressed = set.Shots_Suppressed;
            if (set.Shots_LowPressure.Clips.Count > 0) _fireArm.AudioClipSet.Shots_LowPressure = set.Shots_LowPressure;
        }

        public void Unhook()
        {
#if !(DEBUG)
            On.FistVR.FVRFireArmRound.OnTriggerEnter -= FVRFireArmRound_OnTriggerEnter;
            //On.FistVR.FVRFireArmMagazine.ReloadMagWithList -= FVRFireArmMagazine_ReloadMagWithList;

            if (ChecksFirearmCompatibility)
            {
                On.FistVR.FVRFireArmReloadTriggerMag.OnTriggerEnter -= FVRFireArmReloadTriggerMag_OnTriggerEnter;
            }
#endif
        }

        public void Hook()
        {
#if !(DEBUG)
            On.FistVR.FVRFireArmRound.OnTriggerEnter += FVRFireArmRound_OnTriggerEnter;
            //On.FistVR.FVRFireArmMagazine.ReloadMagWithList += FVRFireArmMagazine_ReloadMagWithList;

            if (ChecksFirearmCompatibility)
            {
                On.FistVR.FVRFireArmReloadTriggerMag.OnTriggerEnter += FVRFireArmReloadTriggerMag_OnTriggerEnter;
            }
#endif
        }
        /*
        private void FVRFireArmMagazine_ReloadMagWithList(On.FistVR.FVRFireArmMagazine.orig_ReloadMagWithList orig, FVRFireArmMagazine self, List<FireArmRoundClass> list)
        {
            if (magazine == self)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i] = defaultRoundClass;
                }
            }
            orig(self, list);
        }
        */
#if !(DEBUG)
        private void FVRFireArmReloadTriggerMag_OnTriggerEnter(On.FistVR.FVRFireArmReloadTriggerMag.orig_OnTriggerEnter orig, FVRFireArmReloadTriggerMag self, Collider collider)
        {
            if (this.Magazine == self.Magazine)
            {
                if (!(self.Magazine != null) || !(self.Magazine.FireArm == null) || !(self.Magazine.QuickbeltSlot == null) || !(collider.gameObject.tag == "FVRFireArmReloadTriggerWell"))
                    return;
                FVRFireArmReloadTriggerWell component = collider.gameObject.GetComponent<FVRFireArmReloadTriggerWell>();
                bool flag = false;
                if (component != null && !self.Magazine.IsBeltBox && component.FireArm.HasBelt)
                    flag = true;
                if (!(component != null) || component.IsBeltBox != self.Magazine.IsBeltBox || !(component.FireArm != null) || !(component.FireArm.Magazine == null) || flag)
                    return;
                FireArmMagazineType fireArmMagazineType = component.FireArm.MagazineType;
                if (component.UsesTypeOverride)
                    fireArmMagazineType = component.TypeOverride;
                if (fireArmMagazineType != self.Magazine.MagazineType || (double)component.FireArm.EjectDelay > 0.0 && !(self.Magazine != component.FireArm.LastEjectedMag) || !(component.FireArm.Magazine == null))
                    return;
                if (ChecksFirearmCompatibility && Magazine.RoundType != component.FireArm.RoundType)
                    return;
                self.Magazine.Load(component.FireArm);
            }
            else orig(self, collider);
        }

        private void FVRFireArmRound_OnTriggerEnter(On.FistVR.FVRFireArmRound.orig_OnTriggerEnter orig, FVRFireArmRound self, Collider collider)
        {
            if (self.IsSpent)
                return;
            if (self.isManuallyChamberable && !self.IsSpent && (UnityEngine.Object)self.HoveredOverChamber == (UnityEngine.Object)null && (UnityEngine.Object)self.m_hoverOverReloadTrigger == (UnityEngine.Object)null && !self.IsSpent && collider.gameObject.CompareTag("FVRFireArmChamber"))
            {
                FVRFireArmChamber component = collider.gameObject.GetComponent<FVRFireArmChamber>();
                if (component.RoundType == self.RoundType && component.IsManuallyChamberable && component.IsAccessible && !component.IsFull)
                    self.HoveredOverChamber = component;
            }
            if (self.isMagazineLoadable && (UnityEngine.Object)self.HoveredOverChamber == (UnityEngine.Object)null && !self.IsSpent && collider.gameObject.CompareTag("FVRFireArmMagazineReloadTrigger"))
            {
                FVRFireArmMagazineReloadTrigger component = collider.gameObject.GetComponent<FVRFireArmMagazineReloadTrigger>();
                if (component.IsClipTrigger)
                {
                    if ((UnityEngine.Object)component != (UnityEngine.Object)null && (UnityEngine.Object)component.Clip != (UnityEngine.Object)null && component.Clip.RoundType == self.RoundType && !component.Clip.IsFull() && ((UnityEngine.Object)component.Clip.FireArm == (UnityEngine.Object)null || component.Clip.IsDropInLoadable))
                        self.m_hoverOverReloadTrigger = component;
                }
                else if (component.IsSpeedloaderTrigger)
                {
                    if (!component.SpeedloaderChamber.IsLoaded)
                        self.m_hoverOverReloadTrigger = component;
                }
                else if ((UnityEngine.Object)component != (UnityEngine.Object)null && (UnityEngine.Object)component.Magazine != (UnityEngine.Object)null && component.Magazine.RoundType == self.RoundType && !component.Magazine.IsFull() && ((UnityEngine.Object)component.Magazine.FireArm == (UnityEngine.Object)null || component.Magazine.IsDropInLoadable))
                    self.m_hoverOverReloadTrigger = component;
                else if ((UnityEngine.Object)component != (UnityEngine.Object)null && component.Magazine == Magazine && !component.Magazine.IsFull() && ((UnityEngine.Object)component.Magazine.FireArm == (UnityEngine.Object)null || component.Magazine.IsDropInLoadable))
                {
                    MultiCaliberMagazine multiCaliberMagazine = component.Magazine.GetComponent<MultiCaliberMagazine>();
                    if (multiCaliberMagazine.SetCartridge(self.RoundType))
                    {
                        self.m_hoverOverReloadTrigger = component;
                    }
                }
            }
            if (!self.isPalmable || self.ProxyRounds.Count >= self.MaxPalmedAmount || self.IsSpent || !collider.gameObject.CompareTag(nameof(FVRFireArmRound)))
                return;
            FVRFireArmRound component1 = collider.gameObject.GetComponent<FVRFireArmRound>();
            if (component1.RoundType != self.RoundType || component1.IsSpent || !((UnityEngine.Object)component1.QuickbeltSlot == (UnityEngine.Object)null))
                return;
            self.HoveredOverRound = component1;
        }
#endif
        public IEnumerator ShowCaliberText()
        {
            FireArmRoundType roundType = _caliberDefinitionsList[CurrentCaliberDefinition].RoundType;
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

        void PrepareCaliberDefinitions()
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

        List<CaliberDefinition> CreateListFromDEBUGDefines()
        {
            List<CaliberDefinition> caliberDefinitions = new List<CaliberDefinition>();
            for (int i = 0; i < RoundTypes.Length; i++)
            {
                CaliberDefinition caliberDefinition = new CaliberDefinition();
                caliberDefinition.RoundType = RoundTypes[i];
                caliberDefinition.Capacity = Capacities[i];
                caliberDefinition.DisplayBullets = DisplayBulletss[i];
                caliberDefinition.DisplayMeshFilters = DisplayMeshFilterss[i];
                caliberDefinition.DisplayRenderers = DisplayRendererss[i];
                caliberDefinition.AccuracyClass = AccuracyClasses[i];
                caliberDefinition.RecoilProfile = RecoilProfiles[i];
                caliberDefinition.RecoilProfileStocked = RecoilProfilesStocked[i];
                caliberDefinition.ReplacementFiringSounds = ReplacementFiringSoundss[i];
                caliberDefinition.ObjectWrapper = ObjectWrappers[i];
            }

            return caliberDefinitions;
        }
#endif
    }
}
