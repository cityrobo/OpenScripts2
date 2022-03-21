using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace OpenScripts2
{
    public class BreakActionWeapon_BarrelSwitch : OpenScripts2_BasePlugin
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

        public void Start()
        {
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

            Hook();

            if (HasFireSelector) UpdateFireSelector();
        }

        public void OnDestroy()
        {
            Unhook();
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

        public void Unhook()
        {
#if !MEATKIT
            On.FistVR.BreakActionWeapon.DropHammer -= BreakActionWeapon_DropHammer;
            On.FistVR.BreakActionWeapon.UpdateInputAndAnimate -= BreakActionWeapon_UpdateInputAndAnimate;
#endif
        }

        public void Hook()
        {
#if !MEATKIT
            On.FistVR.BreakActionWeapon.DropHammer += BreakActionWeapon_DropHammer;
            On.FistVR.BreakActionWeapon.UpdateInputAndAnimate += BreakActionWeapon_UpdateInputAndAnimate;
#endif
        }
#if !MEATKIT
        private void BreakActionWeapon_UpdateInputAndAnimate(On.FistVR.BreakActionWeapon.orig_UpdateInputAndAnimate orig, BreakActionWeapon self, FVRViveHand hand)
        {
            orig(self, hand);
            if (self == BreakActionWeapon)
            {
                if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.down) < 45f)
                {
                    NextBarrelGroup();
                    self.PlayAudioEvent(FirearmAudioEventType.FireSelector);
                }
            }
        }

        private void BreakActionWeapon_DropHammer(On.FistVR.BreakActionWeapon.orig_DropHammer orig, BreakActionWeapon self)
        {
            if (self == BreakActionWeapon)
            {
                if (!self.m_isLatched)
                {
                    return;
                }
                self.firedOneShot = false;

                switch (_selectedBarrelGroup)
                {
                    case SelectedBarrelGroup.Primary:
                        for (int i = 0; i < _primaryBarrelGroup.Barrels.Count; i++)
                        {
                            if (_primaryBarrelGroup.Barrels[i].m_isHammerCocked)
                            {
                                self.PlayAudioEvent(FirearmAudioEventType.HammerHit, 1f);
                                _primaryBarrelGroup.Barrels[i].m_isHammerCocked = false;
                                self.UpdateVisualHammers();
                                self.Fire(_primaryBarrelGroup.IndexList[i], self.FireAllBarrels, _primaryBarrelGroup.IndexList[i]);
                                if (!self.FireAllBarrels)
                                {
                                    break;
                                }
                            }
                        }
                        break;
                    case SelectedBarrelGroup.Secondary:
                        for (int i = 0; i < _secondaryBarrelGroup.Barrels.Count; i++)
                        {
                            if (_secondaryBarrelGroup.Barrels[i].m_isHammerCocked)
                            {
                                self.PlayAudioEvent(FirearmAudioEventType.HammerHit, 1f);
                                _secondaryBarrelGroup.Barrels[i].m_isHammerCocked = false;
                                self.UpdateVisualHammers();
                                self.Fire(_secondaryBarrelGroup.IndexList[i], self.FireAllBarrels, _secondaryBarrelGroup.IndexList[i]);
                                if (!self.FireAllBarrels)
                                {
                                    break;
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            else orig(self);
        }
#endif
    }
}
