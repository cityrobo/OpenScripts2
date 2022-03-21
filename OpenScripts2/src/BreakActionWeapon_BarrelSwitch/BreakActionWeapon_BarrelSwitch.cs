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

        public float primaryMode;
        public float secondaryMode;

#if!DEBUG

        private enum SelectedBarrelGroup
        {
            primary,
            secondary
        }

        private class _BarrelGroup
        {
            public _BarrelGroup()
            {
                this.IndexList = new List<int>();
                this.Barrels = new List<BreakActionWeapon.BreakActionBarrel>();
            }
            public List<int> IndexList;
            public List<BreakActionWeapon.BreakActionBarrel> Barrels;
        }

        private _BarrelGroup _PrimaryBarrelGroup = new _BarrelGroup();
        private _BarrelGroup _SecondaryBarrelGroup = new _BarrelGroup();

        private SelectedBarrelGroup _SelectedBarrelGroup = SelectedBarrelGroup.primary;

        private Transform _OrigTransformFireSelector;

        public void Start()
        {
            foreach (int index in PrimaryBarrelGroupIndex)
            {
                _PrimaryBarrelGroup.IndexList.Add(index);
                _PrimaryBarrelGroup.Barrels.Add(BreakActionWeapon.Barrels[index]);
            }

            foreach (int index in SecondaryBarrelGroupIndex)
            {
                _SecondaryBarrelGroup.IndexList.Add(index);
                _SecondaryBarrelGroup.Barrels.Add(BreakActionWeapon.Barrels[index]);
            }

            _OrigTransformFireSelector = FireSelector;

            Hook();

            if (HasFireSelector) UpdateFireSelector();
        }

        public void OnDestroy()
        {
            Unhook();
        }

        public void NextBarrelGroup()
        {
            switch (_SelectedBarrelGroup)
            {
                case SelectedBarrelGroup.primary:
                    _SelectedBarrelGroup = SelectedBarrelGroup.secondary;
                    break;
                case SelectedBarrelGroup.secondary:
                    _SelectedBarrelGroup = SelectedBarrelGroup.primary;
                    break;
                default:
                    _SelectedBarrelGroup = SelectedBarrelGroup.primary;
                    break;
            }

            if (HasFireSelector) UpdateFireSelector();
        }

        public void UpdateFireSelector()
        {
            if (!HasFireSelector) return;
            switch (_SelectedBarrelGroup)
            {
                case SelectedBarrelGroup.primary:
                    switch (transformType)
                    {
                        case TransformType.Movement:
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
                            break;
                        case TransformType.Rotation:
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
                            break;
                        default:
                            break;
                    }

                    break;
                case SelectedBarrelGroup.secondary:
                    switch (transformType)
                    {
                        case TransformType.Movement:
                            switch (TransformAxis)
                            {
                                case Axis.X:
                                    FireSelector.transform.localPosition = new Vector3(secondaryMode, _OrigTransformFireSelector.localPosition.y, _OrigTransformFireSelector.localPosition.z);
                                    break;
                                case Axis.Y:
                                    FireSelector.transform.localPosition = new Vector3(_OrigTransformFireSelector.localPosition.x, secondaryMode, _OrigTransformFireSelector.localPosition.z);
                                    break;
                                case Axis.Z:
                                    FireSelector.transform.localPosition = new Vector3(_OrigTransformFireSelector.localPosition.x, _OrigTransformFireSelector.localPosition.y, secondaryMode);
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case TransformType.Rotation:
                            switch (TransformAxis)
                            {
                                case Axis.X:
                                    FireSelector.transform.localEulerAngles = new Vector3(secondaryMode, _OrigTransformFireSelector.localEulerAngles.y, _OrigTransformFireSelector.localEulerAngles.z);
                                    break;
                                case Axis.Y:
                                    FireSelector.transform.localEulerAngles = new Vector3(_OrigTransformFireSelector.localEulerAngles.x, secondaryMode, _OrigTransformFireSelector.localEulerAngles.z);
                                    break;
                                case Axis.Z:
                                    FireSelector.transform.localEulerAngles = new Vector3(_OrigTransformFireSelector.localEulerAngles.x, _OrigTransformFireSelector.localEulerAngles.y, secondaryMode);
                                    break;
                                default:
                                    break;
                            }
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
            On.FistVR.BreakActionWeapon.DropHammer -= BreakActionWeapon_DropHammer;
            On.FistVR.BreakActionWeapon.UpdateInputAndAnimate -= BreakActionWeapon_UpdateInputAndAnimate;
        }

        public void Hook()
        {
            On.FistVR.BreakActionWeapon.DropHammer += BreakActionWeapon_DropHammer;
            On.FistVR.BreakActionWeapon.UpdateInputAndAnimate += BreakActionWeapon_UpdateInputAndAnimate;
        }

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

                switch (_SelectedBarrelGroup)
                {
                    case SelectedBarrelGroup.primary:
                        for (int i = 0; i < _PrimaryBarrelGroup.Barrels.Count; i++)
                        {
                            if (_PrimaryBarrelGroup.Barrels[i].m_isHammerCocked)
                            {
                                self.PlayAudioEvent(FirearmAudioEventType.HammerHit, 1f);
                                _PrimaryBarrelGroup.Barrels[i].m_isHammerCocked = false;
                                self.UpdateVisualHammers();
                                self.Fire(_PrimaryBarrelGroup.IndexList[i], self.FireAllBarrels, _PrimaryBarrelGroup.IndexList[i]);
                                if (!self.FireAllBarrels)
                                {
                                    break;
                                }
                            }
                        }
                        break;
                    case SelectedBarrelGroup.secondary:
                        for (int i = 0; i < _SecondaryBarrelGroup.Barrels.Count; i++)
                        {
                            if (_SecondaryBarrelGroup.Barrels[i].m_isHammerCocked)
                            {
                                self.PlayAudioEvent(FirearmAudioEventType.HammerHit, 1f);
                                _SecondaryBarrelGroup.Barrels[i].m_isHammerCocked = false;
                                self.UpdateVisualHammers();
                                self.Fire(_SecondaryBarrelGroup.IndexList[i], self.FireAllBarrels, _SecondaryBarrelGroup.IndexList[i]);
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
