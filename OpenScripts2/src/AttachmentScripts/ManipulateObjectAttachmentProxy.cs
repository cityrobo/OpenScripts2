using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace OpenScripts2
{
    public class ManipulateObjectAttachmentProxy : OpenScripts2_BasePlugin
    {
        public FVRFireArmAttachment Attachment;

        public enum TargetType
        {
            Bolt,
            Trigger,
            BoltHandle,
            Safety,
            FireSelector,
            MagazineRelease,
            BoltRelease,
            Hammer
        }
        
        public TargetType targetType;

        [Header("Alternative target by name:")]
        public bool UseAlternativeMethod;
        [Tooltip("If the part you wanna monitor doesn't exist as a type, you can put in the exact path of the part (without the parent) that you wanna proxy and it will get that one on the gun instead.")]
        public string TargetPath;

        private FVRPhysicalObject _weapon;
        private Transform _proxy = null;

        private bool _debug = false;

        public void Update()
        {
            if (Attachment.curMount != null && !UseAlternativeMethod)
            {
                if (_proxy == null)
                {
                    DebugMessage("Grabbing mounted item.");

                    _weapon = Attachment.curMount.GetRootMount().MyObject;

                    DebugMessage("Mounted Item: " + _weapon.name);

                    switch (_weapon)
                    {
                        case OpenBoltReceiver s:
                            DebugMessage("OpenBoltReceiver found!");
                            SetProxy(s);
                            break;
                        case ClosedBoltWeapon s:
                            DebugMessage("ClosedBoltWeapon found!");
                            SetProxy(s);
                            break;
                        case Handgun s:
                            DebugMessage("Handgun found!");
                            SetProxy(s);
                            break;
                        case TubeFedShotgun s:
                            DebugMessage("TubeFedShotgun found!");
                            SetProxy(s);
                            break;
                        case BoltActionRifle s:
                            DebugMessage("BoltActionRifle found!");
                            SetProxy(s);
                            break;
                        case Revolver s:
                            DebugMessage("Revolver found!");
                            SetProxy(s);
                            break;
                        default:
                            LogWarning($"Parent object is not a supported FireArm type ({_weapon.GetType()})!");
                            break;
                    }
                }
                if (_proxy != null)
                {
                    transform.localPosition = _proxy.localPosition;
                    transform.localRotation = _proxy.localRotation;
                    transform.localScale = _proxy.localScale;
                }

            }
            else if (Attachment.curMount != null && UseAlternativeMethod)
            {
                if (_proxy == null)
                {
                    DebugMessage("Grabbing mounted item.");

                    _weapon = Attachment.curMount.GetRootMount().MyObject;

                    DebugMessage("Mounted Item: " + _weapon.name);

                    _proxy = _weapon.transform.Find(TargetPath);
                }
                if (_proxy != null)
                {
                    transform.localPosition = _proxy.localPosition;
                    transform.localRotation = _proxy.localRotation;
                    transform.localScale = _proxy.localScale;
                }
                else
                {
                    LogWarning("Could not find target with alternative mode path!");
                }
            }
            else
            {
                _proxy = null;
            }
        }

        private void SetProxy(OpenBoltReceiver s)
        {
            switch (targetType)
            {
                case TargetType.Bolt:
                    _proxy = s.Bolt.transform;
                    break;
                case TargetType.Trigger:
                    _proxy = s.Trigger;
                    break;
                case TargetType.BoltHandle:
                    OpenBoltChargingHandle openBoltChargingHandle = s.GetComponentInChildren<OpenBoltChargingHandle>();
                    _proxy = openBoltChargingHandle.transform;
                    break;
                case TargetType.Safety:
                    _proxy = s.FireSelectorSwitch;
                    break;
                case TargetType.FireSelector:
                    _proxy = s.FireSelectorSwitch2;
                    break;
                case TargetType.MagazineRelease:
                    _proxy = s.MagReleaseButton;
                    break;
                default:
                    LogWarning($"TargetType not available for this type of FireArm ({s.GetType()}/{targetType})!");
                    break;
            }
        }
        private void SetProxy(ClosedBoltWeapon s)
        {
            switch (targetType)
            {
                case TargetType.Bolt:
                    _proxy = s.Bolt.transform;
                    break;
                case TargetType.Trigger:
                    _proxy = s.Trigger;
                    break;
                case TargetType.BoltHandle:
                    _proxy = s.Handle.transform;
                    break;
                case TargetType.Safety:
                    _proxy = s.FireSelectorSwitch;
                    break;
                case TargetType.FireSelector:
                    _proxy = s.FireSelectorSwitch2;
                    break;
                case TargetType.Hammer:
                    _proxy = s.Bolt.Hammer;
                    break;
                default:
                    LogWarning($"TargetType not available for this type of FireArm ({s.GetType()}/{targetType})!");
                    break;
            }
        }
        private void SetProxy(Handgun s)
        {
            switch (targetType)
            {
                case TargetType.Bolt:
                    _proxy = s.Slide.transform;
                    break;
                case TargetType.Trigger:
                    _proxy = s.Trigger;
                    break;
                case TargetType.MagazineRelease:
                    _proxy = s.MagazineReleaseButton;
                    break;
                case TargetType.Safety:
                    if (_debug && s.Safety == null) LogWarning("Handgun.Safety == null");
                    if (_debug) DebugMessage("Safety: " + s.Safety);
                    _proxy = s.Safety;
                    if (_debug) DebugMessage("proxy: " + _proxy);
                    break;
                case TargetType.FireSelector:
                    _proxy = s.FireSelector;
                    break;
                case TargetType.BoltRelease:
                    _proxy = s.SlideRelease;
                    break;
                case TargetType.Hammer:
                    _proxy = s.Hammer;
                    break;
                default:
                    LogWarning($"TargetType not available for this type of FireArm ({s.GetType()}/{targetType})!");
                    break;
            }
            if (_debug && _proxy == null) LogWarning("Proxy should be set but isn't!");
        }
        private void SetProxy(TubeFedShotgun s)
        {
            switch (targetType)
            {
                case TargetType.Bolt:
                    _proxy = s.Bolt.transform;
                    break;
                case TargetType.Trigger:
                    _proxy = s.Trigger;
                    break;
                case TargetType.Safety:
                    _proxy = s.Safety;
                    break;
                case TargetType.Hammer:
                    _proxy = s.Bolt.Hammer;
                    break;
                default:
                    LogWarning($"TargetType not available for this type of FireArm ({s.GetType()}/{targetType})!");
                    break;
            }
        }
        private void SetProxy(BoltActionRifle s)
        {
            switch (targetType)
            {
                case TargetType.Bolt:
                    _proxy = s.BoltHandle.transform;
                    break;
                case TargetType.Trigger:
                    _proxy = s.Trigger_Display.transform;
                    break;
                case TargetType.Safety:
                    _proxy = s.FireSelector_Display;
                    break;
                case TargetType.Hammer:
                    _proxy = s.Hammer;
                    break;
                default:
                    LogWarning($"TargetType not available for this type of FireArm ({s.GetType()}/{targetType})!");
                    break;
            }
        }

        private void SetProxy(Revolver s)
        {
            switch (targetType)
            {
                case TargetType.Trigger:
                    _proxy = s.Trigger.transform;
                    break;
                case TargetType.Hammer:
                    _proxy = s.Hammer;
                    break;
                default:
                    LogWarning($"TargetType not available for this type of FireArm ({s.GetType()}/{targetType})!");
                    break;
            }
        }
        private void DebugMessage(string message)
        {
            if (_debug) Log(message);
        }
    }
}
