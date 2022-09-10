using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class BreakActionWeaponBarrelSwitch : OpenScripts2_BasePlugin
    {
        public BreakActionWeapon BreakActionWeapon;
        public int[] PrimaryBarrelGroupIndex;
        public int[] SecondaryBarrelGroupIndex;

        public bool HasFireSelector = false;
        public Transform FireSelector;
        public TransformType transformType = TransformType.Movement;
        public Axis TransformAxis = Axis.X;

        public float FireSelectorInPrimaryMode;
        public float FireSelectorInSecondaryMode;

        private enum SelectedBarrelGroup
        {
            Primary,
            Secondary
        }

        private class BarrelGroup
        {
            public BarrelGroup()
            {
                this.IndexList = new List<int>();
                this.Barrels = new List<BreakActionWeapon.BreakActionBarrel>();
            }
            public List<int> IndexList;
            public List<BreakActionWeapon.BreakActionBarrel> Barrels;
        }

        private BarrelGroup _primaryBarrelGroup = new BarrelGroup();
        private BarrelGroup _secondaryBarrelGroup = new BarrelGroup();

        private SelectedBarrelGroup _selectedBarrelGroup = SelectedBarrelGroup.Primary;

        private static Dictionary<BreakActionWeapon, BreakActionWeaponBarrelSwitch> _existingBreakActionWeaponBarrelSwitches = new();

        static BreakActionWeaponBarrelSwitch()
        {
            Hook();
        }

        public void Awake()
        {
            _existingBreakActionWeaponBarrelSwitches.Add(BreakActionWeapon, this);

            foreach (int index in PrimaryBarrelGroupIndex)
            {
                _primaryBarrelGroup.IndexList.Add(index);
                _primaryBarrelGroup.Barrels.Add(BreakActionWeapon.Barrels[index]);
            }

            foreach (int index in SecondaryBarrelGroupIndex)
            {
                _secondaryBarrelGroup.IndexList.Add(index);
                _secondaryBarrelGroup.Barrels.Add(BreakActionWeapon.Barrels[index]);
            }
            if (HasFireSelector) UpdateFireSelector();
        }

        public void OnDestroy()
        {
            _existingBreakActionWeaponBarrelSwitches.Remove(BreakActionWeapon);
        }

        public void NextBarrelGroup()
        {
            switch (_selectedBarrelGroup)
            {
                case SelectedBarrelGroup.Primary:
                    _selectedBarrelGroup = SelectedBarrelGroup.Secondary;
                    break;
                case SelectedBarrelGroup.Secondary:
                    _selectedBarrelGroup = SelectedBarrelGroup.Primary;
                    break;
                default:
                    _selectedBarrelGroup = SelectedBarrelGroup.Primary;
                    break;
            }

            if (HasFireSelector) UpdateFireSelector();
        }

        public void UpdateFireSelector()
        {
            if (!HasFireSelector) return;

            switch (_selectedBarrelGroup)
            {
                case SelectedBarrelGroup.Primary:
                    switch (transformType)
                    {
                        case TransformType.Movement:
                            FireSelector.transform.ModifyLocalPositionAxis(TransformAxis, FireSelectorInPrimaryMode);
                            /*
                            switch (TransformAxis)
                            {
                                case Axis.X:
                                    FireSelector.transform.localPosition = new Vector3(primaryMode, _OrigTransformFireSelector.localPosition.y, _OrigTransformFireSelector.localPosition.z);
                                    break;
                                case Axis.Y:
                                    FireSelector.transform.localPosition = new Vector3(_OrigTransformFireSelector.localPosition.x ,primaryMode, _OrigTransformFireSelector.localPosition.z);
                                    break;
                                case Axis.Z:
                                    FireSelector.transform.localPosition = new Vector3(_OrigTransformFireSelector.localPosition.x, _OrigTransformFireSelector.localPosition.y, primaryMode);
                                    break;
                                default:
                                    break;
                            }
                            */
                            break;
                        case TransformType.Rotation:
                            FireSelector.transform.ModifyLocalRotationAxis(TransformAxis, FireSelectorInPrimaryMode);
                            /*
                            switch (TransformAxis)
                            {
                                case Axis.X:
                                    FireSelector.transform.localEulerAngles = new Vector3(primaryMode, _OrigTransformFireSelector.localEulerAngles.y, _OrigTransformFireSelector.localEulerAngles.z);
                                    break;
                                case Axis.Y:
                                    FireSelector.transform.localEulerAngles = new Vector3(_OrigTransformFireSelector.localEulerAngles.x, primaryMode, _OrigTransformFireSelector.localEulerAngles.z);
                                    break;
                                case Axis.Z:
                                    FireSelector.transform.localEulerAngles = new Vector3(_OrigTransformFireSelector.localEulerAngles.x, _OrigTransformFireSelector.localEulerAngles.y, primaryMode);
                                    break;
                                default:
                                    break;
                            }
                            */
                            break;
                        default:
                            break;
                    }

                    break;
                case SelectedBarrelGroup.Secondary:
                    switch (transformType)
                    {
                        case TransformType.Movement:
                            FireSelector.transform.ModifyLocalPositionAxis(TransformAxis, FireSelectorInSecondaryMode);
                            break;
                        case TransformType.Rotation:
                            FireSelector.transform.ModifyLocalRotationAxis(TransformAxis, FireSelectorInSecondaryMode);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        public static void Hook()
        {
#if !DEBUG
            On.FistVR.BreakActionWeapon.DropHammer += BreakActionWeapon_DropHammer;
            On.FistVR.BreakActionWeapon.UpdateInputAndAnimate += BreakActionWeapon_UpdateInputAndAnimate;
#endif
        }
#if !DEBUG
        private static void BreakActionWeapon_UpdateInputAndAnimate(On.FistVR.BreakActionWeapon.orig_UpdateInputAndAnimate orig, BreakActionWeapon self, FVRViveHand hand)
        {
            orig(self, hand);

            BreakActionWeaponBarrelSwitch breakActionWeaponBarrelSwitch;
            if (_existingBreakActionWeaponBarrelSwitches.TryGetValue(self,out breakActionWeaponBarrelSwitch))
            {
                
                if (TouchpadDirPressed(hand, Vector2.down))
                {
                    breakActionWeaponBarrelSwitch.NextBarrelGroup();
                    self.PlayAudioEvent(FirearmAudioEventType.FireSelector);
                }
            }
        }

        private static void BreakActionWeapon_DropHammer(On.FistVR.BreakActionWeapon.orig_DropHammer orig, BreakActionWeapon self)
        {
            BreakActionWeaponBarrelSwitch breakActionWeaponBarrelSwitch;
            if (_existingBreakActionWeaponBarrelSwitches.TryGetValue(self, out breakActionWeaponBarrelSwitch))
            {
                if (!self.m_isLatched)
                {
                    return;
                }
                self.firedOneShot = false;

                switch (breakActionWeaponBarrelSwitch._selectedBarrelGroup)
                {
                    case SelectedBarrelGroup.Primary:
                        for (int i = 0; i < breakActionWeaponBarrelSwitch._primaryBarrelGroup.Barrels.Count; i++)
                        {
                            if (breakActionWeaponBarrelSwitch._primaryBarrelGroup.Barrels[i].m_isHammerCocked)
                            {
                                self.PlayAudioEvent(FirearmAudioEventType.HammerHit, 1f);
                                breakActionWeaponBarrelSwitch._primaryBarrelGroup.Barrels[i].m_isHammerCocked = false;
                                self.UpdateVisualHammers();
                                self.Fire(breakActionWeaponBarrelSwitch._primaryBarrelGroup.IndexList[i], self.FireAllBarrels, breakActionWeaponBarrelSwitch._primaryBarrelGroup.IndexList[i]);
                                if (!self.FireAllBarrels)
                                {
                                    break;
                                }
                            }
                        }
                        break;
                    case SelectedBarrelGroup.Secondary:
                        for (int i = 0; i < breakActionWeaponBarrelSwitch._secondaryBarrelGroup.Barrels.Count; i++)
                        {
                            if (breakActionWeaponBarrelSwitch._secondaryBarrelGroup.Barrels[i].m_isHammerCocked)
                            {
                                self.PlayAudioEvent(FirearmAudioEventType.HammerHit, 1f);
                                breakActionWeaponBarrelSwitch._secondaryBarrelGroup.Barrels[i].m_isHammerCocked = false;
                                self.UpdateVisualHammers();
                                self.Fire(breakActionWeaponBarrelSwitch._secondaryBarrelGroup.IndexList[i], self.FireAllBarrels, breakActionWeaponBarrelSwitch._secondaryBarrelGroup.IndexList[i]);
                                if (!self.FireAllBarrels)
                                {
                                    break;
                                }
                            }
                        }
                        break;
                }
            }
            else orig(self);
        }
#endif
    }
}
