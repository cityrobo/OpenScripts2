using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        }

        [Tooltip("Only allows insertion of mag into firearm if the caliber of the mag and the gun are equal")]
        public bool ChecksFirearmCompatibility;

        private FVRFireArm _fireArm = null;
        private bool _isPrepared = false;
        private bool _alreadyStartConfigured = false;

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

        private const string MULTICALIBER_MAG_ROUNDTYPE_FLAG = "RoundType";

        private static readonly IntPtr _methodPointer;

        // ModularWorkshop compatibility requirements
        private static readonly Type _modularWorkshopMagazineExtension;
        private static readonly FieldInfo _additionalRoundsMagExtensionField;

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
            if (!_isPrepared) PrepareCaliberDefinitions();
            if (!AlwaysShowText && TextRoot != null) TextRoot.SetActive(false);

            if(!_alreadyStartConfigured) ConfigureMagazine(CurrentCaliberDefinition);
        }

        public void OnDestroy()
        {
            _existingMultiCaliberMagazines.Remove(Magazine);
        }

        private void PrepareCaliberDefinitions()
        {
            _isPrepared = true;
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

            if (hand != null && AlwaysShowText && TextField != null)
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
            else if (ChecksFirearmCompatibility && Magazine.State == FVRFireArmMagazine.MagazineState.Locked && _fireArm != null && _fireArm.RoundType != CaliberDefinitions[CurrentCaliberDefinition].RoundType)
            {
                if (!SetCartridge(_fireArm.RoundType))
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

        public bool SetCartridge(FireArmRoundType fireArmRoundType, bool unvaulting = false)
        {
            if (!unvaulting && Magazine.m_numRounds != 0) return false;

            CaliberDefinition foundDefinition = CaliberDefinitions.SingleOrDefault(obj => obj.RoundType == fireArmRoundType);
            if (foundDefinition == null)
            {
                return false;
            }
            else
            {
                int index = Array.IndexOf(CaliberDefinitions.ToArray(), foundDefinition);
                ConfigureMagazine(index);
                return true;
            }
        }

        public void ConfigureMagazine(int caliberDefinitionIndex)
        {
            CurrentCaliberDefinition = caliberDefinitionIndex;
            CaliberDefinition caliberDefinition = CaliberDefinitions[caliberDefinitionIndex];
            Magazine.RoundType = caliberDefinition.RoundType;

            Magazine.ObjectWrapper.RoundType = caliberDefinition.RoundType;

            if (caliberDefinition.MagazineType != FireArmMagazineType.mNone) Magazine.MagazineType = caliberDefinition.MagazineType;

            if (caliberDefinition.Capacity > 0)
            {
                // Magazine Extension Test
                int magazineExtension = 0;
                if (_additionalRoundsMagExtensionField != null)
                {
                    Component[] magazineExtensionComponents = Magazine.GetComponentsInChildren(_modularWorkshopMagazineExtension);
                    for (int i = 0; i < magazineExtensionComponents.Length; i++)
                    {
                        magazineExtension += (int)_additionalRoundsMagExtensionField.GetValue(magazineExtensionComponents[i]);
                    }
                }

                if (Magazine.m_numRounds > caliberDefinition.Capacity + magazineExtension)
                {
                    int difference = Magazine.m_numRounds - caliberDefinition.Capacity + magazineExtension;

                    for (int i = 0; i < difference; i++)
                    {
                        Magazine.RemoveRound();
                    }
                }

                if (Magazine.LoadedRounds != null && Magazine.LoadedRounds.Length < caliberDefinition.Capacity + magazineExtension)
                {
                    int difference = caliberDefinition.Capacity + magazineExtension - Magazine.LoadedRounds.Length;
                    Array.Resize(ref Magazine.LoadedRounds, caliberDefinition.Capacity + magazineExtension);

                    for (int i = Magazine.LoadedRounds.Length - difference; i < Magazine.LoadedRounds.Length; i++)
                    {
                        Magazine.LoadedRounds[i] = new();
                    }
                }
                else if (Magazine.LoadedRounds != null && Magazine.m_numRounds < caliberDefinition.Capacity + magazineExtension)
                {
                    for (int i = Magazine.m_numRounds; i < caliberDefinition.Capacity + magazineExtension; i++)
                    {
                        if (Magazine.LoadedRounds[i] == null) Magazine.LoadedRounds[i] = new();
                    }
                }
                else if (Magazine.LoadedRounds == null)
                {
                    Magazine.LoadedRounds = new FVRLoadedRound[caliberDefinition.Capacity + magazineExtension];

                    for (int i = 0; i < Magazine.LoadedRounds.Length; i++)
                    {
                        Magazine.LoadedRounds[i] = new();
                    }
                }

                Magazine.m_capacity = caliberDefinition.Capacity + magazineExtension;
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

            if (caliberDefinition.MagazineOriginOverridePointProxy != null)
            {
                (caliberDefinition.MagazineOriginOverridePointProxy.localRotation * Quaternion.Inverse(_currentOriginOffsetRotation)).ToAngleAxis(out float angle, out Vector3 axis);
                Transform[] _directChildren = Magazine.GetComponentsInDirectChildren<Transform>();
                foreach (var directChild in _directChildren)
                {
                    directChild.localPosition -= caliberDefinition.MagazineOriginOverridePointProxy.localPosition - _currentOriginOffsetPosition;
                    if (!Mathf.Approximately(angle, 0f)) directChild.RotateAround(directChild.position, axis, -angle);
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

#if !DEBUG
        static MultiCaliberMagazine()
        {
            On.FistVR.FVRFireArmRound.OnTriggerEnter += FVRFireArmRound_OnTriggerEnter;
            On.FistVR.FVRFireArmReloadTriggerMag.OnTriggerEnter += FVRFireArmReloadTriggerMag_OnTriggerEnter;
            On.FistVR.FVRPhysicalObject.GetFlagDic += FVRPhysicalObject_GetFlagDic;
            On.FistVR.FVRPhysicalObject.ConfigureFromFlagDic += FVRPhysicalObject_ConfigureFromFlagDic;
            On.FistVR.FVRPhysicalObject.DuplicateFromSpawnLock += FVRPhysicalObject_DuplicateFromSpawnLock;

            Assembly modularWorkshopAssembly = Assembly.Load("ModularWorkshop");
            if (modularWorkshopAssembly != null)
            {
                Debug.Log("ModularWorkshop Assembly found!");
                _modularWorkshopMagazineExtension = modularWorkshopAssembly.GetType("ModularWorkshop.ModularMagazineExtension");
                if (_modularWorkshopMagazineExtension != null)
                {
                    Debug.Log("MagExtension type found!");
                    _additionalRoundsMagExtensionField = _modularWorkshopMagazineExtension.GetField("AdditionalNumberOfRoundsInMagazine");
                    if (_additionalRoundsMagExtensionField != null) Debug.Log("AdditionalRounds field found!");
                    else Debug.LogError("AdditionalRounds field not found!");
                }
                else Debug.LogError("ModularWorkshop.ModularMagazineExtension not found!");
            }
        }

        private static GameObject FVRPhysicalObject_DuplicateFromSpawnLock(On.FistVR.FVRPhysicalObject.orig_DuplicateFromSpawnLock orig, FVRPhysicalObject self, FVRViveHand hand)
        {
            GameObject copy = orig(self, hand);

            if (self is FVRFireArmMagazine mag && _existingMultiCaliberMagazines.TryGetValue(mag, out MultiCaliberMagazine multiCaliberMagazine))
            {
                MultiCaliberMagazine copyMag = copy.GetComponentInChildren<MultiCaliberMagazine>();
                if (!copyMag._isPrepared) copyMag.PrepareCaliberDefinitions();
                copyMag.ConfigureMagazine(multiCaliberMagazine.CurrentCaliberDefinition);
                copyMag._alreadyStartConfigured = true;

                for (int i = 0; i < Mathf.Min(mag.LoadedRounds.Length, copyMag.Magazine.LoadedRounds.Length); i++)
                {
                    if (copyMag.Magazine.LoadedRounds[i] == null) copyMag.Magazine.LoadedRounds[i] = new();
                }
            }
            return copy;
        }

        private static void FVRPhysicalObject_ConfigureFromFlagDic(On.FistVR.FVRPhysicalObject.orig_ConfigureFromFlagDic orig, FVRPhysicalObject self, Dictionary<string, string> f)
        {
            orig(self, f);

            if (self is FVRFireArmMagazine mag && _existingMultiCaliberMagazines.TryGetValue(mag, out MultiCaliberMagazine multiCaliberMagazine))
            {
                if (f.TryGetValue(MULTICALIBER_MAG_ROUNDTYPE_FLAG, out string type))
                {
                    if (!multiCaliberMagazine._isPrepared) multiCaliberMagazine.PrepareCaliberDefinitions();
                    multiCaliberMagazine.SetCartridge(MiscUtilities.ParseEnum<FireArmRoundType>(type), true);
                    multiCaliberMagazine._alreadyStartConfigured = true;
                }
            }
        }

        private static Dictionary<string, string> FVRPhysicalObject_GetFlagDic(On.FistVR.FVRPhysicalObject.orig_GetFlagDic orig, FVRPhysicalObject self)
        {
            Dictionary<string, string> flagDic = orig(self);

            if (self is FVRFireArmMagazine mag && _existingMultiCaliberMagazines.ContainsKey(mag))
            {
                flagDic.Add(MULTICALIBER_MAG_ROUNDTYPE_FLAG, mag.RoundType.ToString());
            }

            return flagDic;
        }

        // FireArm round type compatibility patch
        private static void FVRFireArmReloadTriggerMag_OnTriggerEnter(On.FistVR.FVRFireArmReloadTriggerMag.orig_OnTriggerEnter orig, FVRFireArmReloadTriggerMag self, Collider collider)
        {
            if (_existingMultiCaliberMagazines.TryGetValue(self.Magazine, out MultiCaliberMagazine multiCaliberMagazine))
            {
                if (self.Magazine != null && self.Magazine.FireArm == null && self.Magazine.QuickbeltSlot == null)
                {
                    if (collider.gameObject.tag == "FVRFireArmReloadTriggerWell")
                    {
                        FVRFireArmReloadTriggerWell reloadTriggerWell = collider.gameObject.GetComponent<FVRFireArmReloadTriggerWell>();
                        if (reloadTriggerWell != null)
                        {
                            if (reloadTriggerWell.IsAttachableWell && reloadTriggerWell.AFireArm != null && reloadTriggerWell.AFireArm.Magazine == null)
                            {
                                FireArmMagazineType attachableFireArmMagazineType = reloadTriggerWell.AFireArm.MagazineType;
                                if (reloadTriggerWell.UsesTypeOverride)
                                {
                                    attachableFireArmMagazineType = reloadTriggerWell.TypeOverride;
                                }
                                if (attachableFireArmMagazineType == self.Magazine.MagazineType && (reloadTriggerWell.AFireArm.EjectDelay <= 0f || self.Magazine != reloadTriggerWell.AFireArm.LastEjectedMag) && reloadTriggerWell.AFireArm.Magazine == null)
                                {
                                    if (multiCaliberMagazine.ChecksFirearmCompatibility && multiCaliberMagazine.Magazine.RoundType == reloadTriggerWell.AFireArm.RoundType)
                                    {
                                        self.Magazine.Load(reloadTriggerWell.AFireArm);
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                bool flag = false;
                                if (!self.Magazine.IsBeltBox && reloadTriggerWell.FireArm != null && reloadTriggerWell.FireArm.HasBelt)
                                {
                                    flag = true;
                                }
                                if (reloadTriggerWell.IsBeltBox == self.Magazine.IsBeltBox && reloadTriggerWell.FireArm != null && reloadTriggerWell.FireArm.Magazine == null && !flag)
                                {
                                    FireArmMagazineType fireArmMagazineType = reloadTriggerWell.FireArm.MagazineType;
                                    if (reloadTriggerWell.UsesTypeOverride)
                                    {
                                        fireArmMagazineType = reloadTriggerWell.TypeOverride;
                                    }
                                    if (fireArmMagazineType == self.Magazine.MagazineType && (reloadTriggerWell.FireArm.EjectDelay <= 0f || self.Magazine != reloadTriggerWell.FireArm.LastEjectedMag) && reloadTriggerWell.FireArm.Magazine == null)
                                    {
                                        if (multiCaliberMagazine.ChecksFirearmCompatibility && multiCaliberMagazine.Magazine.RoundType == reloadTriggerWell.FireArm.RoundType)
                                        {
                                            self.Magazine.Load(reloadTriggerWell.FireArm);
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            orig(self, collider);
        }

        private static void FVRFireArmRound_OnTriggerEnter(On.FistVR.FVRFireArmRound.orig_OnTriggerEnter orig, FVRFireArmRound self, Collider collider)
        {
            if (self.IsSpent) return;

            if (self.isMagazineLoadable && self.HoveredOverChamber == null && !self.IsSpent && collider.gameObject.CompareTag(nameof(FVRFireArmMagazineReloadTrigger)))
            {
                FVRFireArmMagazineReloadTrigger component = collider.gameObject.GetComponent<FVRFireArmMagazineReloadTrigger>();
                if (component != null && _existingMultiCaliberMagazines.TryGetValue(component.Magazine, out MultiCaliberMagazine multiCaliberMagazine) && !component.Magazine.IsFull() && (component.Magazine.FireArm == null || component.Magazine.IsDropInLoadable))
                {
                    if (multiCaliberMagazine.SetCartridge(self.RoundType))
                    {
                        self.m_hoverOverReloadTrigger = component;
                    }
                }
            }

            orig(self, collider);
        }
#endif
    }
}