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
            [SearchableEnum]
            public FireArmMagazineType MagazineType;
            public Transform MagazineOriginOverridePoint;
            [HideInInspector]
            public TransformProxy MagazineOriginOverridePointProxy;
            public int Capacity;
            public GameObject[] DisplayBullets;

            [HideInInspector]
            public List<TransformProxy> DisplayTransformProxies = new();
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

        // Origin Moving Params
        private Transform _viz;

        private Collider _magGrabTrigger;
        private Vector3 _origGrabTriggerPos;

        private Vector3 _currentOriginOffsetPosition = Vector3.zero;
        private Quaternion _currentOriginOffsetRotation = Quaternion.identity;

        private static readonly Dictionary<FVRFireArmMagazine, MultiCaliberMagazine> _existingMultiCaliberMagazines = new();

        public void Awake()
        {
            _existingMultiCaliberMagazines.Add(Magazine, this);
            _magGrabTrigger = Magazine.GetComponent<Collider>();
            _origGrabTriggerPos = _magGrabTrigger switch
            {
                SphereCollider c => c.center,
                CapsuleCollider c => c.center,
                BoxCollider c => c.center,
                _ => Vector3.zero
            };

            _viz = Magazine.Viz;
        }

        public void Start()
        {
            PrepareCaliberDefinitions();
            if (!AlwaysShowText && TextRoot != null) TextRoot.SetActive(false);

            ConfigureMagazine(CurrentCaliberDefinition);
        }

        public void OnDestroy()
        {
            _existingMultiCaliberMagazines.Remove(Magazine);
        }

        private void PrepareCaliberDefinitions()
        {
            foreach (var caliberDefinition in CaliberDefinitions)
            {
                for (int i = 0; i < caliberDefinition.DisplayBullets.Length; i++)
                {
                    caliberDefinition.DisplayTransformProxies.Add(new(caliberDefinition.DisplayBullets[i].transform, true));
                }

                if (caliberDefinition.MagazineOriginOverridePoint != null) caliberDefinition.MagazineOriginOverridePointProxy = new(caliberDefinition.MagazineOriginOverridePoint, true);
            }
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

            UpdateFirearm();

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

        private void UpdateFirearm()
        {
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

            CaliberDefinition foundDefinition = CaliberDefinitions.SingleOrDefault(obj => obj.RoundType == fireArmRoundType);

            if (foundDefinition == null)
            {
                return false;
            }
            else 
            {
                int index = Array.IndexOf(CaliberDefinitions.ToArray(), foundDefinition);

                ConfigureMagazine(index);
                CurrentCaliberDefinition = index;
                return true;
            }
        }

        public void ConfigureMagazine(int CaliberDefinitionIndex)
        {
            CaliberDefinition caliberDefinition = CaliberDefinitions[CaliberDefinitionIndex];
            Magazine.RoundType = caliberDefinition.RoundType;

            if (caliberDefinition.MagazineType != FireArmMagazineType.mNone) Magazine.MagazineType = caliberDefinition.MagazineType;

            if (caliberDefinition.Capacity > 0)
            {
                Magazine.m_capacity = caliberDefinition.Capacity;
            }
            if (caliberDefinition.DisplayBullets.Length > 0)
            {
                Magazine.m_roundInsertionTarget.localPosition = caliberDefinition.DisplayTransformProxies[0].localPosition;
                Magazine.m_roundInsertionTarget.localRotation = caliberDefinition.DisplayTransformProxies[0].localRotation;

                for (int i = 0; i < Magazine.DisplayBullets.Length; i++)
                {
                    Magazine.DisplayBullets[i].transform.GoToTransformProxy(caliberDefinition.DisplayTransformProxies[i]);
                }

                Magazine.m_DisplayStartPositions = caliberDefinition.DisplayTransformProxies.Select(obj => obj.localPosition).ToArray();
            }
            Magazine.ObjectWrapper = caliberDefinition.ObjectWrapper;

            if (caliberDefinition.MagazineOriginOverridePointProxy != null)
            {
                (caliberDefinition.MagazineOriginOverridePointProxy.localRotation * Quaternion.Inverse(_currentOriginOffsetRotation)).ToAngleAxis(out float angle, out Vector3 axis);
                Transform[] _directChildren = Magazine.GetComponentsInDirectChildren<Transform>();
                foreach (var directChild in _directChildren)
                {
                    directChild.localPosition -= caliberDefinition.MagazineOriginOverridePointProxy.localPosition - _currentOriginOffsetPosition;
                    if (!Mathf.Approximately(angle,0f)) directChild.RotateAround(directChild.position, axis, -angle);
                }

                if (_viz != null)
                {
                    _viz.transform.localPosition = Vector3.zero;
                    _viz.transform.localRotation = Quaternion.identity;

                    _directChildren = _viz.GetComponentsInDirectChildren<Transform>();
                    foreach (var directChild in _directChildren)
                    {
                        directChild.localPosition -= (caliberDefinition.MagazineOriginOverridePointProxy.localPosition - _currentOriginOffsetPosition);
                        if (!Mathf.Approximately(angle, 0f)) directChild.RotateAround(directChild.position, axis, -angle);
                    }
                }

                _currentOriginOffsetPosition = caliberDefinition.MagazineOriginOverridePointProxy.localPosition;
                _currentOriginOffsetRotation = caliberDefinition.MagazineOriginOverridePointProxy.localRotation;

                switch (_magGrabTrigger)
                {
                    case SphereCollider c:
                        c.center = _origGrabTriggerPos - caliberDefinition.MagazineOriginOverridePointProxy.localPosition;
                        break;
                    case CapsuleCollider c:
                        c.center = _origGrabTriggerPos - caliberDefinition.MagazineOriginOverridePointProxy.localPosition;
                        break;
                    case BoxCollider c:
                        c.center = _origGrabTriggerPos - caliberDefinition.MagazineOriginOverridePointProxy.localPosition;
                        break;
                }
            }
            else
            {
                Quaternion.Inverse(_currentOriginOffsetRotation).ToAngleAxis(out float angle, out Vector3 axis);
                Transform[] _directChildren = Magazine.GetComponentsInDirectChildren<Transform>();
                foreach (var directChild in _directChildren)
                {
                    directChild.localPosition += _currentOriginOffsetPosition;
                    if (!Mathf.Approximately(angle, 0f)) directChild.RotateAround(directChild.position, axis, -angle);
                }

                if (_viz != null)
                {
                    _viz.transform.localPosition = Vector3.zero;
                    _viz.transform.localRotation = Quaternion.identity;

                    _directChildren = _viz.GetComponentsInDirectChildren<Transform>();
                    foreach (var directChild in _directChildren)
                    {
                        directChild.localPosition += _currentOriginOffsetPosition;
                        if (!Mathf.Approximately(angle, 0f)) directChild.RotateAround(directChild.position, axis, -angle);
                    }
                }

                _currentOriginOffsetPosition = Vector3.zero;
                _currentOriginOffsetRotation = Quaternion.identity;

                switch (_magGrabTrigger)
                {
                    case SphereCollider c:
                        c.center = _origGrabTriggerPos;
                        break;
                    case CapsuleCollider c:
                        c.center = _origGrabTriggerPos;
                        break;
                    case BoxCollider c:
                        c.center = _origGrabTriggerPos;
                        break;
                }
            }
        }

        public void ReplaceFiringSounds(FVRFirearmAudioSet set)
        {
            if (set.Shots_Main.Clips.Count > 0) _fireArm.AudioClipSet.Shots_Main = set.Shots_Main;
            if (set.Shots_Suppressed.Clips.Count > 0) _fireArm.AudioClipSet.Shots_Suppressed = set.Shots_Suppressed;
            if (set.Shots_LowPressure.Clips.Count > 0) _fireArm.AudioClipSet.Shots_LowPressure = set.Shots_LowPressure;
        }

#if !DEBUG
        static MultiCaliberMagazine()
        {
            On.FistVR.FVRFireArmRound.OnTriggerEnter += FVRFireArmRound_OnTriggerEnter;
            On.FistVR.FVRFireArmReloadTriggerMag.OnTriggerEnter += FVRFireArmReloadTriggerMag_OnTriggerEnter;
        }

        //private static void FVRFireArmReloadTriggerMag_OnTriggerEnter(On.FistVR.FVRFireArmReloadTriggerMag.orig_OnTriggerEnter orig, FVRFireArmReloadTriggerMag self, Collider collider)
        //{
        //    if (_existingMultiCaliberMagazines.TryGetValue(self.Magazine, out MultiCaliberMagazine multiCaliberMagazine))
        //    {
        //        if (self.Magazine == null || self.Magazine.FireArm != null || self.Magazine.QuickbeltSlot != null || collider.gameObject.CompareTag(nameof(FVRFireArmReloadTriggerWell))) return;
        //        FVRFireArmReloadTriggerWell reloadTriggerWell = collider.gameObject.GetComponent<FVRFireArmReloadTriggerWell>();
        //        bool beltCheck = false;
        //        if (reloadTriggerWell != null && !self.Magazine.IsBeltBox && reloadTriggerWell.FireArm.HasBelt) beltCheck = true;
        //        if (reloadTriggerWell == null || reloadTriggerWell.IsBeltBox != self.Magazine.IsBeltBox || reloadTriggerWell.FireArm == null || reloadTriggerWell.FireArm.Magazine != null || beltCheck) return;
        //        FireArmMagazineType fireArmMagazineType = reloadTriggerWell.FireArm.MagazineType;
        //        if (reloadTriggerWell.UsesTypeOverride) fireArmMagazineType = reloadTriggerWell.TypeOverride;
        //        if (fireArmMagazineType != self.Magazine.MagazineType || reloadTriggerWell.FireArm.EjectDelay > 0.0f && self.Magazine == reloadTriggerWell.FireArm.LastEjectedMag || reloadTriggerWell.FireArm.Magazine != null) return;
        //        if (multiCaliberMagazine.ChecksFirearmCompatibility && multiCaliberMagazine.Magazine.RoundType != reloadTriggerWell.FireArm.RoundType) return;
        //        self.Magazine.Load(reloadTriggerWell.FireArm);
        //    }
        //    else orig(self, collider);
        //}

        private static void FVRFireArmReloadTriggerMag_OnTriggerEnter(On.FistVR.FVRFireArmReloadTriggerMag.orig_OnTriggerEnter orig, FVRFireArmReloadTriggerMag self, Collider collider)
        {
            if (_existingMultiCaliberMagazines.TryGetValue(self.Magazine, out MultiCaliberMagazine multiCaliberMagazine))
            {
                if (self.Magazine != null && self.Magazine.FireArm == null && self.Magazine.QuickbeltSlot == null)
                {
                    if (collider.gameObject.tag == "FVRFireArmReloadTriggerWell")
                    {
                        FVRFireArmReloadTriggerWell component = collider.gameObject.GetComponent<FVRFireArmReloadTriggerWell>();
                        if (component != null)
                        {
                            if (component.IsAttachableWell && component.AFireArm != null && component.AFireArm.Magazine == null)
                            {
                                FireArmMagazineType fireArmMagazineType = component.AFireArm.MagazineType;
                                if (component.UsesTypeOverride)
                                {
                                    fireArmMagazineType = component.TypeOverride;
                                }
                                if (fireArmMagazineType == self.Magazine.MagazineType && (component.AFireArm.EjectDelay <= 0f || self.Magazine != component.AFireArm.LastEjectedMag) && component.AFireArm.Magazine == null)
                                {
                                    if (multiCaliberMagazine.ChecksFirearmCompatibility && multiCaliberMagazine.Magazine.RoundType != component.FireArm.RoundType) return;
                                    self.Magazine.Load(component.AFireArm);
                                }
                            }
                            else if (component.UsesSecondaryMagSlots && component.FireArm != null && component.FireArm.SecondaryMagazineSlots[component.SecondaryMagSlotIndex].Magazine == null)
                            {
                                FireArmMagazineType fireArmMagazineType2 = component.FireArm.MagazineType;
                                if (component.UsesTypeOverride)
                                {
                                    fireArmMagazineType2 = component.TypeOverride;
                                }
                                if (fireArmMagazineType2 == self.Magazine.MagazineType && (component.FireArm.SecondaryMagazineSlots[component.SecondaryMagSlotIndex].m_ejectDelay <= 0f || self.Magazine != component.FireArm.SecondaryMagazineSlots[component.SecondaryMagSlotIndex].m_lastEjectedMag) && component.FireArm.SecondaryMagazineSlots[component.SecondaryMagSlotIndex].Magazine == null)
                                {
                                    self.Magazine.LoadIntoSecondary(component.FireArm, component.SecondaryMagSlotIndex);
                                }
                            }
                            else
                            {
                                bool flag = false;
                                if (!self.Magazine.IsBeltBox && component.FireArm != null && component.FireArm.HasBelt)
                                {
                                    flag = true;
                                }
                                if (component.IsBeltBox == self.Magazine.IsBeltBox && component.FireArm != null && component.FireArm.Magazine == null && !flag)
                                {
                                    FireArmMagazineType fireArmMagazineType3 = component.FireArm.MagazineType;
                                    if (component.UsesTypeOverride)
                                    {
                                        fireArmMagazineType3 = component.TypeOverride;
                                    }
                                    if (fireArmMagazineType3 == self.Magazine.MagazineType && (component.FireArm.EjectDelay <= 0f || self.Magazine != component.FireArm.LastEjectedMag) && component.FireArm.Magazine == null)
                                    {
                                        if (multiCaliberMagazine.ChecksFirearmCompatibility && multiCaliberMagazine.Magazine.RoundType != component.FireArm.RoundType) return;
                                        self.Magazine.Load(component.FireArm);
                                    }
                                }
                            }
                        }
                    }
                    else if (collider.gameObject.GetComponent<DerringerTrigger>() != null)
                    {
                        DerringerTrigger component2 = collider.gameObject.GetComponent<DerringerTrigger>();
                        Derringer derringer = component2.Derringer;
                        if (multiCaliberMagazine.ChecksFirearmCompatibility && multiCaliberMagazine.Magazine.RoundType != derringer.RoundType) return;
                        if (derringer.RoundType == self.Magazine.RoundType && derringer.GetHingeState() == Derringer.HingeState.Open)
                        {
                            for (int i = 0; i < derringer.Barrels.Count; i++)
                            {
                                if (self.Magazine.HasARound() && !derringer.Barrels[i].Chamber.IsFull)
                                {
                                    FVRLoadedRound fvrloadedRound = self.Magazine.RemoveRound(0);
                                    Transform transform = self.Magazine.DisplayBullets[Mathf.Clamp(0, self.Magazine.DisplayBullets.Length - 1, i)].transform;
                                    derringer.Barrels[i].Chamber.Autochamber(fvrloadedRound.LR_Class, transform.position, transform.rotation);
                                    derringer.PlayAudioEvent(FirearmAudioEventType.MagazineIn, 1f);
                                }
                            }
                        }
                    }
                }
            }
            else orig(self, collider);
        }

        private static void FVRFireArmRound_OnTriggerEnter(On.FistVR.FVRFireArmRound.orig_OnTriggerEnter orig, FVRFireArmRound self, Collider collider)
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
                else if (component != null && _existingMultiCaliberMagazines.TryGetValue(component.Magazine, out MultiCaliberMagazine multiCaliberMagazine)&& !component.Magazine.IsFull() && (component.Magazine.FireArm == null || component.Magazine.IsDropInLoadable))
                {
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
    }
}
