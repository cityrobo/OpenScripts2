using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenScripts2
{
    public class FireModeAttachment : OpenScripts2_BasePlugin
    {
        [FormerlySerializedAs("attachment")]
        public FVRFireArmAttachment Attachment;

        public ClosedBoltWeapon.FireSelectorModeType[] FireSelectorModeTypes;
        [FormerlySerializedAs("burstAmounts")]
        public int[] BurstAmounts;

        [FormerlySerializedAs("selectorPositions")]
        public float[] SelectorPositions;

        private bool _attached = false;
        private FVRFireArm _firearm;
        private Handgun.FireSelectorMode[] _originalHandgunFireModes;
        private ClosedBoltWeapon.FireSelectorMode[] _originalClosedBoltFireModes;
        private OpenBoltReceiver.FireSelectorMode[] _originalOpenBoltFireModes;
        private List<OpenBoltBurstFire> _openBoltBursts;

        private bool _handgunHadFireSelectorButton = false;

        private bool _addsSelectorPositions = false;

        public void Awake()
        {
            _openBoltBursts = new List<OpenBoltBurstFire>();

            if (SelectorPositions.Length > 0) _addsSelectorPositions = true;
        }

        public void Update()
        {
            if (!_attached && Attachment.curMount != null)
            {
                //Debug.Log(Attachment.curMount);
                ChangeFireMode(true);
                _attached = true;
            }

            if (_attached && Attachment.curMount == null)
            {
                //Debug.Log(Attachment.curMount);
                ChangeFireMode(false);
                _attached = false;

                _firearm = null;
            }
        }
        public void ChangeFireMode(bool activate)
        {
            if (Attachment.curMount != null) _firearm = Attachment.curMount.GetRootMount().MyObject as FVRFireArm;

            if (_firearm is OpenBoltReceiver) ChangeFireMode(_firearm as OpenBoltReceiver, activate);
            else if (_firearm is ClosedBoltWeapon) ChangeFireMode(_firearm as ClosedBoltWeapon, activate);
            else if (_firearm is Handgun) ChangeFireMode(_firearm as Handgun, activate);
        }

        public void ChangeFireMode(OpenBoltReceiver openBoltReceiver, bool activate)
        {
            if (activate)
            {
                _originalOpenBoltFireModes = openBoltReceiver.FireSelector_Modes;
                int burstIndex = 0;
                int selectorPosIndex = 0;

                foreach (var FireSelectorModeType in FireSelectorModeTypes)
                {
                    OpenBoltReceiver.FireSelectorMode newFireSelectorMode = new OpenBoltReceiver.FireSelectorMode();
                    switch (FireSelectorModeType)
                    {
                        case ClosedBoltWeapon.FireSelectorModeType.Safe:
                            newFireSelectorMode.ModeType = OpenBoltReceiver.FireSelectorModeType.Safe;
                            break;
                        case ClosedBoltWeapon.FireSelectorModeType.Single:
                            newFireSelectorMode.ModeType = OpenBoltReceiver.FireSelectorModeType.Single;
                            break;
                        case ClosedBoltWeapon.FireSelectorModeType.Burst:
                            newFireSelectorMode.ModeType = OpenBoltReceiver.FireSelectorModeType.FullAuto;
                            OpenBoltBurstFire openBoltBurst = openBoltReceiver.gameObject.AddComponent<OpenBoltBurstFire>();
                            _openBoltBursts.Add(openBoltBurst);
                            openBoltBurst.OpenBoltReceiver = openBoltReceiver;
                            openBoltBurst.SelectorSetting = openBoltReceiver.FireSelector_Modes.Length;
                            openBoltBurst.BurstAmount = BurstAmounts[burstIndex];
                            //openBoltBurstGM.SetActive(true);

                            burstIndex++;
                            break;
                        case ClosedBoltWeapon.FireSelectorModeType.FullAuto:
                            newFireSelectorMode.ModeType = OpenBoltReceiver.FireSelectorModeType.FullAuto;
                            break;
                        default:
                            LogError("FireSelectorMode not supported: " + FireSelectorModeType);
                            continue;
                    }

                    if (!_addsSelectorPositions) newFireSelectorMode.SelectorPosition = _originalOpenBoltFireModes[_originalOpenBoltFireModes.Length - 1].SelectorPosition;
                    else newFireSelectorMode.SelectorPosition = SelectorPositions[selectorPosIndex];
                    openBoltReceiver.FireSelector_Modes = openBoltReceiver.FireSelector_Modes.Concat(new OpenBoltReceiver.FireSelectorMode[] { newFireSelectorMode }).ToArray();

                    selectorPosIndex++;
                }
            }
            else
            {
                openBoltReceiver.m_fireSelectorMode = _originalOpenBoltFireModes.Length - 1;
                openBoltReceiver.FireSelector_Modes = _originalOpenBoltFireModes;
                if (_openBoltBursts.Count != 0)
                {
                    foreach (var openBoltBurst in _openBoltBursts)
                    {
                        Destroy(openBoltBurst);
                    }

                    _openBoltBursts.Clear();
                }
            }
        }

        public void ChangeFireMode(ClosedBoltWeapon closedBoltWeapon, bool activate)
        {
            if (activate)
            {
                _originalClosedBoltFireModes = closedBoltWeapon.FireSelector_Modes;

                int burstIndex = 0;
                int selectorPosIndex = 0;
                foreach (var FireSelectorModeType in FireSelectorModeTypes)
                {
                    ClosedBoltWeapon.FireSelectorMode newFireSelectorMode = new ClosedBoltWeapon.FireSelectorMode();
                    switch (FireSelectorModeType)
                    {
                        case ClosedBoltWeapon.FireSelectorModeType.Safe:
                            newFireSelectorMode.ModeType = ClosedBoltWeapon.FireSelectorModeType.Safe;
                            break;
                        case ClosedBoltWeapon.FireSelectorModeType.Single:
                            newFireSelectorMode.ModeType = ClosedBoltWeapon.FireSelectorModeType.Single;
                            break;
                        case ClosedBoltWeapon.FireSelectorModeType.Burst:
                            newFireSelectorMode.ModeType = ClosedBoltWeapon.FireSelectorModeType.Burst;
                            newFireSelectorMode.BurstAmount = BurstAmounts[burstIndex];
                            burstIndex++;
                            break;
                        case ClosedBoltWeapon.FireSelectorModeType.FullAuto:
                            newFireSelectorMode.ModeType = ClosedBoltWeapon.FireSelectorModeType.FullAuto;
                            break;
                        default:
                            LogError("FireSelectorMode not supported: " + FireSelectorModeType);
                            continue;
                    }
                    if (!_addsSelectorPositions) newFireSelectorMode.SelectorPosition = _originalClosedBoltFireModes[_originalClosedBoltFireModes.Length - 1].SelectorPosition;
                    else newFireSelectorMode.SelectorPosition = SelectorPositions[selectorPosIndex];
                    closedBoltWeapon.FireSelector_Modes = closedBoltWeapon.FireSelector_Modes.Concat(new ClosedBoltWeapon.FireSelectorMode[] { newFireSelectorMode }).ToArray();

                    selectorPosIndex++;
                }
            }
            else
            {
                closedBoltWeapon.m_fireSelectorMode = _originalClosedBoltFireModes.Length - 1;
                closedBoltWeapon.FireSelector_Modes = _originalClosedBoltFireModes;
            }
        }

        public void ChangeFireMode(Handgun handgun, bool activate)
        {
            if (activate)
            {
                _handgunHadFireSelectorButton = handgun.HasFireSelector;

                _originalHandgunFireModes = handgun.FireSelectorModes;
                handgun.HasFireSelector = true;

                if (handgun.FireSelector == null) handgun.FireSelector = new GameObject().transform;
                int burstIndex = 0;
                int selectorPosIndex = 0;

                foreach (var FireSelectorModeType in FireSelectorModeTypes)
                {
                    Handgun.FireSelectorMode newFireSelectorMode = new Handgun.FireSelectorMode();
                    switch (FireSelectorModeType)
                    {
                        case ClosedBoltWeapon.FireSelectorModeType.Safe:
                            newFireSelectorMode.ModeType = Handgun.FireSelectorModeType.Safe;
                            break;
                        case ClosedBoltWeapon.FireSelectorModeType.Single:
                            newFireSelectorMode.ModeType = Handgun.FireSelectorModeType.Single;
                            break;
                        case ClosedBoltWeapon.FireSelectorModeType.Burst:
                            newFireSelectorMode.ModeType = Handgun.FireSelectorModeType.Burst;
                            newFireSelectorMode.BurstAmount = BurstAmounts[burstIndex];
                            burstIndex++;
                            break;
                        case ClosedBoltWeapon.FireSelectorModeType.FullAuto:
                            newFireSelectorMode.ModeType = Handgun.FireSelectorModeType.FullAuto;
                            break;
                        default:
                            LogError("FireSelectorMode not supported: " + FireSelectorModeType);
                            continue;
                    }

                    if (!_addsSelectorPositions) newFireSelectorMode.SelectorPosition = _originalHandgunFireModes[_originalHandgunFireModes.Length - 1].SelectorPosition;
                    else newFireSelectorMode.SelectorPosition = SelectorPositions[selectorPosIndex];
                    handgun.FireSelectorModes = handgun.FireSelectorModes.Concat(new Handgun.FireSelectorMode[] { newFireSelectorMode }).ToArray();

                    selectorPosIndex++;
                }
            }
            else
            {
                handgun.m_fireSelectorMode = _originalHandgunFireModes.Length - 1;
                handgun.FireSelectorModes = _originalHandgunFireModes;

                if (!_handgunHadFireSelectorButton)
                {
                    Destroy(handgun.FireSelector.gameObject);
                    handgun.HasFireSelector = false;
                }
            }
        }
        /*
        public void ChangeFireMode(TubeFedShotgun shotgun, bool activate)
        {
            if (activate)
            {
                originalClosedBoltFireModes = shotgun.FireSelector_Modes;

                int burstIndex = 0;
                int selectorPosIndex = 0;
                foreach (var FireSelectorModeType in FireSelectorModeTypes)
                {
                    ClosedBoltWeapon.FireSelectorMode newFireSelectorMode = new ClosedBoltWeapon.FireSelectorMode();
                    switch (FireSelectorModeType)
                    {
                        case ClosedBoltWeapon.FireSelectorModeType.Safe:
                            newFireSelectorMode.ModeType = ClosedBoltWeapon.FireSelectorModeType.Safe;
                            break;
                        case ClosedBoltWeapon.FireSelectorModeType.Single:
                            newFireSelectorMode.ModeType = ClosedBoltWeapon.FireSelectorModeType.Single;
                            break;
                        case ClosedBoltWeapon.FireSelectorModeType.Burst:
                            newFireSelectorMode.ModeType = ClosedBoltWeapon.FireSelectorModeType.Burst;
                            newFireSelectorMode.BurstAmount = burstAmounts[burstIndex];
                            burstIndex++;
                            break;
                        case ClosedBoltWeapon.FireSelectorModeType.FullAuto:
                            newFireSelectorMode.ModeType = ClosedBoltWeapon.FireSelectorModeType.FullAuto;
                            break;
                        default:
                            Debug.LogError("FireSelectorMode not supported: " + FireSelectorModeType);
                            continue;
                    }
                    if (!addsSelectorPositions) newFireSelectorMode.SelectorPosition = originalClosedBoltFireModes[originalClosedBoltFireModes.Length - 1].SelectorPosition;
                    else newFireSelectorMode.SelectorPosition = selectorPositions[selectorPosIndex];
                    shotgun.FireSelector_Modes = originalClosedBoltFireModes.Concat(new ClosedBoltWeapon.FireSelectorMode[] { newFireSelectorMode }).ToArray();

                    selectorPosIndex++;
                }
            }
            else
            {
                shotgun.m_fireSelectorMode = originalClosedBoltFireModes.Length - 1;
                shotgun.FireSelector_Modes = originalClosedBoltFireModes;
            }
        }
        */
    }
}
