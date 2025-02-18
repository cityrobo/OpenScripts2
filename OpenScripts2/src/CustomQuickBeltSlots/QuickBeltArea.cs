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
    public class QuickBeltArea : FVRQuickBeltSlot
    {
        public FVRPhysicalObject MainObject;
        [Tooltip("Preconfigured QBSlot that will be used as a reference to create all other slots.")]
        public GameObject SubQBSlotPrefab;
        [Tooltip("Please try to use base 2 numbers, like 2 ,4 ,8 ,16 ,32 ,64 etc.")]
        public int ItemLimit = 32;

        [Header("Advanced Size Options")]
        public bool UsesAdvancedSizeMode = false;
        [Tooltip("Capacity requirement for items of physicalObjectTagSize Small, Medium, Large, Massive, CantCarryBig")]
        public int[] Sizes = { 1, 2, 5, 10, 25 };
        public bool UsesFirearmTagSizeMode = false;
        [Tooltip("Capacity requirement for firearms of physicalObjectTagSize Pocket, Pistol, Compact, Carbine, FullSize, Bulky, Oversize")]
        public int[] FirearmSizes = { 2, 4, 10, 16, 24, 32, 50 };
        public int TotalCapacity = 50;

        [Header("Collision Settings")]
        public bool ObjectsKeepCollision = false;
        [Tooltip("This setting requires a manually placed QuickBeltAreaCollisionDetector on the FVRPhysicalObject.")]
        public bool CollisionActivatedFreeze = false;
        public QuickBeltAreaCollisionDetector CollisionDetector;
        public bool SetKinematic = false;

        [HideInInspector]
        public bool ItemDidCollide = false;
        private readonly Dictionary<FVRQuickBeltSlot, FVRPhysicalObject> _subQBSlotsDictionary = new();
        private readonly FVRPhysicalObject.FVRPhysicalObjectSize[] _sizes =
        {
            FVRPhysicalObject.FVRPhysicalObjectSize.Small,
            FVRPhysicalObject.FVRPhysicalObjectSize.Medium,
            FVRPhysicalObject.FVRPhysicalObjectSize.Large,
            FVRPhysicalObject.FVRPhysicalObjectSize.Massive,
            FVRPhysicalObject.FVRPhysicalObjectSize.CantCarryBig
        };

        private readonly FVRObject.OTagFirearmSize[] _firearmsizes =
        {
           FVRObject.OTagFirearmSize.Pocket,
           FVRObject.OTagFirearmSize.Pistol,
           FVRObject.OTagFirearmSize.Compact,
           FVRObject.OTagFirearmSize.Carbine,
           FVRObject.OTagFirearmSize.FullSize,
           FVRObject.OTagFirearmSize.Bulky,
           FVRObject.OTagFirearmSize.Oversize
        };

        private readonly Dictionary<FVRPhysicalObject.FVRPhysicalObjectSize, int> _SizeRequirements = new();
        private readonly Dictionary<FVRObject.OTagFirearmSize, int> _FirearmSizeRequirements = new();
        private int _currentLoad = 0;
        private bool _wasUnvaulted = false;

        private class SubQBSlot
        {
            SubQBSlot(FVRQuickBeltSlot slot, Vector3 localPos, Quaternion localRot)
            {
                this.slot = slot;
                this.localPos = localPos;
                this.localRot = localRot;
            }

            public FVRQuickBeltSlot slot;
            public Vector3 localPos;
            public Quaternion localRot;
        }

        public void OnDestroy()
        {
#if !DEBUG
            Unhook();
#endif
        }

        public void OnAwake()
        {
            if (MainObject == null)
            {
                MainObject = GetComponentInParent<FVRPhysicalObject>();
            }

            for (int i = 0; i < _sizes.Length; i++)
            {
                _SizeRequirements.Add(_sizes[i], Sizes[i]);
            }

            for (int i = 0; i < _firearmsizes.Length; i++)
            {
                _FirearmSizeRequirements.Add(_firearmsizes[i], FirearmSizes[i]);
            }

            for (int i = 0; i < ItemLimit; i++)
            {
                FVRQuickBeltSlot qbSlot = Instantiate(SubQBSlotPrefab).GetComponent<FVRQuickBeltSlot>();

                qbSlot.gameObject.name = "QuickBeltAreaSubSlot_" + _subQBSlotsDictionary.Count;

                qbSlot.gameObject.transform.parent = transform;
                qbSlot.gameObject.transform.localPosition = Vector3.zero;
                qbSlot.gameObject.transform.localRotation = Quaternion.identity;

                _subQBSlotsDictionary.Add(qbSlot, null);
            }

            if (MainObject != null)
            {
                MainObject.Slots = MainObject.Slots.Concat(_subQBSlotsDictionary.Keys).ToArray();

                if (!MainObject.Slots.All(GM.CurrentPlayerBody.QBSlots_Added.Contains))
                {
                    MainObject.RegisterQuickbeltSlots();
                }
                foreach (FVRQuickBeltSlot mainObjSlot in MainObject.Slots) {
                    mainObjSlot.SetAffixedTo(MainObject);
                }
            }
            else
            {
                GM.CurrentPlayerBody.QBSlots_Added.AddRange(_subQBSlotsDictionary.Keys);
            }
            SubQBSlotPrefab.SetActive(false);
        }

        public void LateUpdate()
        {
            if (!CollisionActivatedFreeze && CurObject != null)
            {
                CreateNewQBSlotPos(CurObject);
            }
            else if (CurObject != null)
            {
                StartCoroutine(WaitForCollision(CurObject));
            }

            //List<GameObject> slotsToDelete = new List<GameObject>();
            List<FVRQuickBeltSlot> slotsToEmpty = new();
            foreach (var quickBeltSlot in _subQBSlotsDictionary)
            {
                FVRQuickBeltSlot slot = quickBeltSlot.Key;

                //if (slot.CurObject == null) slotsToDelete.Add(quickBeltSlot.Key);
                if (slot.CurObject != null && !slot.CurObject.m_isSpawnLock && !slot.CurObject.m_isHardnessed) slot.HoverGeo.SetActive(false);

                if (ObjectsKeepCollision && slot.CurObject != null) slot.CurObject.SetAllCollidersToLayer(false, "Default");

                if (SetKinematic && slot.CurObject != null && slot.CurObject.transform.localPosition != Vector3.zero) slot.CurObject.transform.localPosition = Vector3.zero;
                if (SetKinematic && slot.CurObject != null && slot.CurObject.transform.localRotation != Quaternion.identity) slot.CurObject.transform.localRotation = Quaternion.identity;
            }
            /*
            foreach (var slotToDelete in slotsToDelete)
            {
                _quickBeltSlots.Remove(slotToDelete);

                FVRPhysicalObject physicalObject = slotToDelete.GetComponentInChildren<FVRPhysicalObject>();
                if (physicalObject != null && physicalObject.m_hand != null)
                {
                    physicalObject.SetParentage(physicalObject.m_hand.WholeRig);
                }
                if (UsesAdvancedSizeMode)
                {
                    FVRPhysicalObject.FVRPhysicalObjectSize size = slotToDelete.GetComponent<FVRQuickBeltSlot>().SizeLimit;
                    int sizeRequirement = 0;
                    _SizeRequirements.TryGetValue(size, out sizeRequirement);

                    _currentLoad -= sizeRequirement;
                }
                Destroy(slotToDelete);
            }
            

            if (ItemLimit != 0)
            {
                if (_quickBeltSlots.Count >= ItemLimit) this.IsSelectable = false;
                else this.IsSelectable = true;
            }
            */

            foreach (var qbSlot in _subQBSlotsDictionary)
            {
                if (qbSlot.Value != qbSlot.Key.CurObject)
                {
                    slotsToEmpty.Add(qbSlot.Key);
                }
            }

            foreach (var clearQB in slotsToEmpty)
            {
                if (UsesAdvancedSizeMode)
                {
                    FVRPhysicalObject.FVRPhysicalObjectSize size = clearQB.SizeLimit;
                    FVRObject.OTagFirearmSize firearmsize = _subQBSlotsDictionary[clearQB].ObjectWrapper != null ? _subQBSlotsDictionary[clearQB].ObjectWrapper.TagFirearmSize : FVRObject.OTagFirearmSize.None;

                    if (UsesFirearmTagSizeMode && firearmsize != FVRObject.OTagFirearmSize.None)
                    {
                        _FirearmSizeRequirements.TryGetValue(firearmsize, out int firearmsizeRequirement);

                        _currentLoad -= firearmsizeRequirement;
                    }
                    else
                    {
                        _SizeRequirements.TryGetValue(size, out int sizeRequirement);

                        _currentLoad -= sizeRequirement;
                    }
                }

                _subQBSlotsDictionary[clearQB] = null;
            }


            if (UsesAdvancedSizeMode)
            {
                if (_currentLoad >= TotalCapacity) IsSelectable = false;
                else IsSelectable = true;
            }
        }

        public void CreateNewQBSlotPos(FVRPhysicalObject physicalObject)
        {
            FVRPhysicalObject.FVRPhysicalObjectSize physicalObjectTagSize = physicalObject.Size;
            FVRObject.OTagFirearmSize firearmTagSize = physicalObject.ObjectWrapper != null ? physicalObject.ObjectWrapper.TagFirearmSize : FVRObject.OTagFirearmSize.None;

            Vector3 pos = physicalObject.transform.position;
            Quaternion rot = physicalObject.transform.rotation;
            if (!SetKinematic && physicalObject.QBPoseOverride != null)
            {
                pos = physicalObject.QBPoseOverride.position;
                rot = physicalObject.QBPoseOverride.rotation;
            }
            else if (!SetKinematic && physicalObject.PoseOverride_Touch != null && (GM.HMDMode == ControlMode.Oculus || GM.HMDMode == ControlMode.Index))
            {
                pos = physicalObject.PoseOverride_Touch.position;
                rot = physicalObject.PoseOverride_Touch.rotation;
            }
            else if (!SetKinematic && physicalObject.PoseOverride != null)
            {
                pos = physicalObject.PoseOverride.position;
                rot = physicalObject.PoseOverride.rotation;
            }

            if (UsesAdvancedSizeMode)
            {
                // if the QBArea is using FirearmTagSizeMode and the size is not empty increase the current load according to the look up table
                if (UsesFirearmTagSizeMode && firearmTagSize != FVRObject.OTagFirearmSize.None)
                {
                    _FirearmSizeRequirements.TryGetValue(firearmTagSize, out int firearmTagSizeRequirement);
                    if (_currentLoad + firearmTagSizeRequirement > TotalCapacity)
                    {
                        physicalObject.SetParentage(null);
                        physicalObject.ClearQuickbeltState();
                        return;
                    }
                    else
                    {
                        _currentLoad += firearmTagSizeRequirement;
                    }
                }
                // else do the same, but use the QBSlot size tag instead
                else
                {
                    _SizeRequirements.TryGetValue(physicalObjectTagSize, out int physicalObjectSizeRequirement);
                    if (_currentLoad + physicalObjectSizeRequirement > TotalCapacity)
                    {
                        physicalObject.SetParentage(null);
                        physicalObject.ClearQuickbeltState();
                        return;
                    }
                    else
                    {
                        _currentLoad += physicalObjectSizeRequirement;
                    }
                }
            }

            FVRQuickBeltSlot slot = GetEmptySlot();
            if (slot == null) 
            {
                physicalObject.SetParentage(null);
                physicalObject.ClearQuickbeltState();
                return; 
            }

            slot.transform.position = pos;
            slot.SizeLimit = physicalObjectTagSize;
            physicalObject.ForceObjectIntoInventorySlot(slot);

            _subQBSlotsDictionary[slot] = physicalObject;
            if (SetKinematic)
            {
                physicalObject.RootRigidbody.isKinematic = true;
                slot.transform.rotation = rot;
            }
            else slot.PoseOverride.rotation = rot;
        }

        // External colission trigger coroutine
        IEnumerator WaitForCollision(FVRPhysicalObject physicalObject)
        {
            physicalObject.SetParentage(null);
            physicalObject.SetQuickBeltSlot(null);
            ItemDidCollide = false;
            CollisionDetector.PhysicalObjectToDetect = physicalObject;
            while (!ItemDidCollide) yield return null;
            ItemDidCollide = false;
            CreateNewQBSlotPos(physicalObject);
        }

        FVRQuickBeltSlot GetEmptySlot()
        {
            foreach (var qbSlotKeyValuePair in _subQBSlotsDictionary)
            {
                if (qbSlotKeyValuePair.Value == null) return qbSlotKeyValuePair.Key;
            }

            return null;
        }

#if !DEBUG
        static QuickBeltArea()
        {
            On.FistVR.FVRQuickBeltSlot.Awake += FVRQuickBeltSlot_Awake;
            //On.FistVR.FVRPhysicalObject.ConfigureFromFlagDic += FVRPhysicalObject_ConfigureFromFlagDic;
        }

        private static void FVRQuickBeltSlot_Awake(On.FistVR.FVRQuickBeltSlot.orig_Awake orig, FVRQuickBeltSlot self)
        {
            orig(self);
            if (self is QuickBeltArea qbArea)
            {
                qbArea.Hook();
                qbArea.OnAwake();
            }
        }

        private void Unhook()
        {

            On.FistVR.FVRPhysicalObject.ConfigureFromFlagDic -= FVRPhysicalObject_ConfigureFromFlagDic;
            On.FistVR.FVRPhysicalObject.GetFlagDic -= FVRPhysicalObject_GetFlagDic;

        }

        private void Hook()
        {
            On.FistVR.FVRPhysicalObject.ConfigureFromFlagDic += FVRPhysicalObject_ConfigureFromFlagDic;
            On.FistVR.FVRPhysicalObject.GetFlagDic += FVRPhysicalObject_GetFlagDic;
        }

        private Dictionary<string, string> FVRPhysicalObject_GetFlagDic(On.FistVR.FVRPhysicalObject.orig_GetFlagDic orig, FVRPhysicalObject self)
        {
            Dictionary<string, string> flagDic = orig(self);

            if (self == MainObject)
            {
                for (int i = 0; i < _subQBSlotsDictionary.Count; i++)
                {
                    var keyValuePair = _subQBSlotsDictionary.ElementAt(i);
                    flagDic.Add(gameObject.name + "_QuickBeltAreaSubSlot_" + i, keyValuePair.Key.transform.localPosition.ToString("F6") + ";" + keyValuePair.Key.PoseOverride.localRotation.ToString("F6"));
                }
            }

            return flagDic;
        }

        private void FVRPhysicalObject_ConfigureFromFlagDic(On.FistVR.FVRPhysicalObject.orig_ConfigureFromFlagDic orig, FVRPhysicalObject self, Dictionary<string, string> f)
        {
            orig(self, f);
            if (MainObject == self)
            {
                for (int i = 0; i < ItemLimit; i++)
                {
                    if (f.TryGetValue(gameObject.name + "_QuickBeltAreaSubSlot_" + i, out string posRot))
                    {
                        posRot = posRot.Replace(" ", "").Replace("(", "").Replace(")", "");
                        string[] posRotSep = posRot.Split(';');

                        string[] posString = posRotSep[0].Split(',');
                        string[] rotString = posRotSep[1].Split(',');

                        Vector3 pos = new(float.Parse(posString[0]), float.Parse(posString[1]), float.Parse(posString[2]));
                        Quaternion rot = new(float.Parse(rotString[0]), float.Parse(rotString[1]), float.Parse(rotString[2]), float.Parse(rotString[3]));
                        _subQBSlotsDictionary.ElementAt(i).Key.transform.localPosition = pos;
                        _subQBSlotsDictionary.ElementAt(i).Key.PoseOverride.localRotation = rot;
                    }
                }
                _wasUnvaulted = true;
            }
        }
#endif
    }
}
