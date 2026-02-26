using FistVR;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static FistVR.GBeamerModeLever;


namespace OpenScripts2 
{
    public class PressurePadAttachmentInterface : FVRFireArmAttachmentInterface
    {
        [Header("Pressure Pad Attachment Config")]
        public float DoubleTapWaitTime = 0.333f;

        private bool _isLightToggledOn = false;
        private bool _isLaserToggledOn = false;
        // Altgrip
        private FVRAlternateGrip _altGrip;
        private bool _wasAltGripGrabbed = false;
        // Double Tap
        private float _timeWaited = 0f;
        private bool _waitingForDoubleTap = false;

        private bool _irActive = false;
        private bool _strobeActive = false;

        private readonly List<LaserLightAttachment> _laserLights = new List<LaserLightAttachment>();

        // Patch stuff
        private static readonly Dictionary<FVRAlternateGrip,PressurePadAttachmentInterface> _existingPressurePadAttachments = new Dictionary<FVRAlternateGrip, PressurePadAttachmentInterface>();
        private static readonly IntPtr _baseUpdateInteractionPointer;

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (_altGrip != null) _existingPressurePadAttachments.Remove(_altGrip);
        }

        public override void OnAttach()
        {
            base.OnAttach();

            if (Attachment.curMount != null && Attachment.curMount.GetRootMount().MyObject is FVRFireArm)
            {
                if ((Attachment.curMount.GetRootMount().MyObject as FVRFireArm).Foregrip != null)
                {
                    _altGrip = (Attachment.curMount.GetRootMount().MyObject as FVRFireArm).Foregrip.GetComponent<FVRAlternateGrip>();
                }
                else
                {
                    _altGrip = Attachment.curMount.GetRootMount().MyObject.GetComponentInChildren<FVRAlternateGrip>(true);
                }
            }
        }

        public override void OnDetach()
        {
            if (_altGrip != null)
            {
                _existingPressurePadAttachments.Remove(_altGrip);
                _altGrip = null;
            }

            ToggleLights(false);
            ToggleLasers(false);

            base.OnDetach();
        }

        public override void FVRUpdate()
        {
            base.FVRUpdate();

            if (_altGrip != null)
            {
                if (!_existingPressurePadAttachments.ContainsKey(_altGrip)) _existingPressurePadAttachments.Add(_altGrip, this);
                FVRViveHand hand = _altGrip.m_hand;

                if (!_wasAltGripGrabbed && hand != null)
                {
                    _wasAltGripGrabbed = true;
                    CreateDictionaries();
                }
                else if (_wasAltGripGrabbed && hand == null)
                {
                    _wasAltGripGrabbed = false;
                }

                if (hand != null)
                {
                    // First Tap of Double Tap
                    if (!hand.IsInStreamlinedMode)
                    {
                        if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.up) < 45f)
                        {
                            if (!_waitingForDoubleTap) StartCoroutine(WaitForDoubleTap(true));
                        }
                        else if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.down) < 45f)
                        {
                            if (!_waitingForDoubleTap) StartCoroutine(WaitForDoubleTap(false));
                        }
                    }
                    else
                    {
                        if (hand.Input.BYButtonDown)
                        {
                            if (!_waitingForDoubleTap) StartCoroutine(WaitForDoubleTap(true));
                        }
                        else if (hand.Input.AXButtonDown)
                        {
                            if (!_waitingForDoubleTap) StartCoroutine(WaitForDoubleTap(false));
                        }
                    }

                    //// IR Toggle
                    //if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f)
                    //{
                    //    ToggleLights(false);
                    //    ToggleLasers(false);
                    //    _irActive = !_irActive;
                    //    ToggleLights(_isLightToggledOn);
                    //    ToggleLasers(_isLaserToggledOn);
                    //}
                    //else if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f)
                    //{
                    //    ToggleStrobe();
                    //}

                    // Flashlight Press
                    if (!hand.IsInStreamlinedMode)
                    {
                        if (hand.Input.TouchpadPressed && Vector2.Angle(hand.Input.TouchpadAxes,Vector2.up) < 45f)
                        {
                            ToggleLights(true);
                        }
                        else if (hand.Input.TouchpadUp && !_isLightToggledOn)
                        {
                            ToggleLights(false);
                        }
                    }
                    else
                    {
                        if (hand.Input.BYButtonPressed)
                        {
                            ToggleLights(true);
                        }
                        else if (hand.Input.BYButtonUp && !_isLightToggledOn)
                        {
                            ToggleLights(false);
                        }
                    }

                    // Laser Press
                    if (!hand.IsInStreamlinedMode)
                    {
                        if (hand.Input.TouchpadPressed && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.down) < 45f)
                        {
                            ToggleLasers(true);
                        }
                        else if (hand.Input.TouchpadUp && !_isLaserToggledOn)
                        {
                            ToggleLasers(false);
                        }
                    }
                    else
                    {
                        if (hand.Input.AXButtonPressed)
                        {
                            ToggleLasers(true);
                        }
                        else if (hand.Input.AXButtonUp && !_isLaserToggledOn)
                        {
                            ToggleLasers(false);
                        }
                    }
                }
            }
        }

        //public override void UpdateInteraction(FVRViveHand hand)
        //{
        //    base.UpdateInteraction(hand);

        //    if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f)
        //    {
        //        ToggleLights(false);
        //        ToggleLasers(false);
        //        _irActive = !_irActive;
        //        ToggleLights(_isLightToggledOn);
        //        ToggleLasers(_isLaserToggledOn);
        //    }
        //    else if (hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f)
        //    {
        //        ToggleStrobe();
        //    }
        //}

        public void CreateDictionaries()
        {
            _laserLights.Clear();

            foreach (FVRFireArmAttachment attachment in Attachment.curMount.GetRootMount().MyObject.AttachmentsList)
            {
                if (attachment.AttachmentInterface is LaserLightAttachment laserLight) _laserLights.Add(laserLight);
            }
        }

        public void ToggleLights(bool turnLightOn)
        {
            foreach (LaserLightAttachment laserLight in _laserLights)
            {
                int savedSettingsIndex = laserLight.m_savedSetting;
                LaserLightSetting savedSetting = laserLight.Settings[savedSettingsIndex];

                if (laserLight.Light != null)
                {
                    //int lightOnIndex = FindCorrectLaserLightSettingsIndex(laserLight, _isLaserToggledOn, turnLightOn, _irActive);

                    //if (lightOnIndex != -1)
                    //{
                    //    laserLight.SettingsIndex = lightOnIndex;
                    //    laserLight.UpdateParams();
                    //    laserLight.UI.RedrawUI();
                    //}
                    
                    // Turn light on, selected mode is light only or dual mode and laser is on: turn on selected setting
                    if (turnLightOn && (savedSetting.LaserMode == LaserAttachmentMode.Off || _isLaserToggledOn))
                    {
                        laserLight.SettingsIndex = savedSettingsIndex;
                        laserLight.UpdateParams();
                        laserLight.UI.RedrawUI();
                    }
                    // Turn light off, selected mode is light only: turn off
                    else if (!turnLightOn && savedSetting.LaserMode == LaserAttachmentMode.Off)
                    {
                        laserLight.SettingsIndex = 0;
                        laserLight.UpdateParams();
                        laserLight.UI.RedrawUI();
                    }
                    // Turn light on, selected mode is dual mode: choose equivalent light only mode
                    else if (turnLightOn && savedSetting.LaserMode != LaserAttachmentMode.Off)
                    {
                        laserLight.SettingsIndex = FindEquivalentLightOnlyMode(laserLight, savedSetting);
                        laserLight.UpdateParams();
                        laserLight.UI.RedrawUI();
                    }
                    // Turn light off, selected mode is dual mode and laser is on: choose equivalent laser only mode
                    else if (!turnLightOn && savedSetting.LaserMode != LaserAttachmentMode.Off && _isLaserToggledOn)
                    {
                        laserLight.SettingsIndex = FindEquivalentLaserOnlyMode(laserLight, savedSetting);
                        laserLight.UpdateParams();
                        laserLight.UI.RedrawUI();
                    }
                    // Turn light off, selected mode is dual mode and laser is off: turn off
                    else if (!turnLightOn && savedSetting.LaserMode != LaserAttachmentMode.Off && !_isLaserToggledOn)
                    {
                        laserLight.SettingsIndex = 0;
                        laserLight.UpdateParams();
                        laserLight.UI.RedrawUI();
                    }
                }
            }
        }

        private int FindEquivalentLightOnlyMode(LaserLightAttachment laserLight, LaserLightSetting setting)
        {
            int settingsIndex = 0;

            LightAttachmentMode LightMode = setting.LightMode;
            Vector2 LightIntensityRange = setting.LightIntensityRange;
            Vector2 LightAngleRange = setting.LightAngleRange;
            float LightRange = setting.LightRange;

            settingsIndex = laserLight.Settings.FindIndex
            (
                x => 
                x.LightMode == LightMode &&
                x.LaserMode == LaserAttachmentMode.Off &&
                x.LightIntensityRange == LightIntensityRange &&
                x.LightAngleRange == LightAngleRange &&
                x.LightRange == LightRange
            );

            // Try falling back to the next best setting using the same light mode if the correct brightness couldn't be found:
            if (settingsIndex == -1)
            {
                settingsIndex = laserLight.Settings.FindIndex(x => x.LightMode == LightMode);
            }

            return settingsIndex != -1 ? settingsIndex : 0;
        }

        public void ToggleLasers(bool turnLaserOn)
        {
            foreach (LaserLightAttachment laserLight in _laserLights)
            {
                int savedSettingsIndex = laserLight.m_savedSetting;
                LaserLightSetting savedSetting = laserLight.Settings[savedSettingsIndex];

                if (laserLight.LL != null)
                {
                    //int laserOnIndex = FindCorrectLaserLightSettingsIndex(laserLight, turnLightOn, _isLightToggledOn, _irActive);
                    //if (laserOnIndex != -1)
                    //{
                    //    laserLight.SettingsIndex = laserOnIndex;
                    //    laserLight.UpdateParams();
                    //    laserLight.UI.RedrawUI();
                    //}

                    // Turn laser on, selected mode is laser only or dual mode and light is on: turn on saved setting
                    if (turnLaserOn && (savedSetting.LightMode == LightAttachmentMode.Off || _isLightToggledOn))
                    {
                        laserLight.SettingsIndex = savedSettingsIndex;
                        laserLight.UpdateParams();
                        laserLight.UI.RedrawUI();
                    }
                    // Turn laser off, selected mode is laser only: turn off
                    else if (!turnLaserOn && savedSetting.LightMode == LightAttachmentMode.Off)
                    {
                        laserLight.SettingsIndex = 0;
                        laserLight.UpdateParams();
                        laserLight.UI.RedrawUI();
                    }
                    // Turn laser on, selected mode is dual mode: choose equivalent laser only mode
                    else if (turnLaserOn && savedSetting.LightMode != LightAttachmentMode.Off)
                    {
                        laserLight.SettingsIndex = FindEquivalentLaserOnlyMode(laserLight, savedSetting);
                        laserLight.UpdateParams();
                        laserLight.UI.RedrawUI();
                    }
                    // Turn laser off, selected mode is dual mode and light is on: choose equivalent light only mode
                    else if (!turnLaserOn && savedSetting.LightMode != LightAttachmentMode.Off && _isLightToggledOn)
                    {
                        laserLight.SettingsIndex = FindEquivalentLightOnlyMode(laserLight, savedSetting);
                        laserLight.UpdateParams();
                        laserLight.UI.RedrawUI();
                    }
                    // Turn laser off, selected mode is dual mode and light is off: turn off
                    else if (!turnLaserOn && savedSetting.LightMode != LightAttachmentMode.Off && !_isLightToggledOn)
                    {
                        laserLight.SettingsIndex = 0;
                        laserLight.UpdateParams();
                        laserLight.UI.RedrawUI();
                    }
                }
            }
        }

        private int FindEquivalentLaserOnlyMode(LaserLightAttachment laserLight, LaserLightSetting setting)
        {
            int settingsIndex = 0;

            LaserAttachmentMode LaserMode = setting.LaserMode;
            float LaserRadius = setting.LaserRadius;
            float LaserDivergence = setting.LaserDivergence;
            float LaserMaxRange = setting.LaserMaxRange;
            float LaserIntensityAtMaxDistance = setting.LaserIntensityAtMaxDistance;
            float LaserIntensityClamp = setting.LaserIntensityClamp;
            bool LaserClampDot = setting.LaserClampDot;

            settingsIndex = laserLight.Settings.FindIndex
            (
                x =>
                x.LaserMode == LaserMode &&
                x.LightMode == LightAttachmentMode.Off &&
                x.LaserRadius == LaserRadius &&
                x.LaserDivergence == LaserDivergence &&
                x.LaserMaxRange == LaserMaxRange &&
                x.LaserIntensityAtMaxDistance == LaserIntensityAtMaxDistance &&
                x.LaserIntensityClamp == LaserIntensityClamp &&
                x.LaserClampDot == LaserClampDot
            );

            // Try falling back to the next best setting using the same laser mode if the correct brightness couldn't be found:
            if (settingsIndex == -1)
            {
                settingsIndex = laserLight.Settings.FindIndex(x => x.LaserMode == LaserMode);
            }

            return settingsIndex != -1 ? settingsIndex : 0;
        }

        public void ToggleStrobe()
        {
            _strobeActive = !_strobeActive;
        }

        private IEnumerator WaitForDoubleTap(bool flashlightToggle)
        {
            yield return null;
            _timeWaited = 0f;
            _waitingForDoubleTap = true;
            FVRViveHand hand = _altGrip.m_hand;
            while (_timeWaited < DoubleTapWaitTime)
            {
                _timeWaited += Time.deltaTime;
                // Second Tap of Double Tap
                if (!hand.IsInStreamlinedMode)
                {
                    if (flashlightToggle && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.up) < 45f)
                    {
                        _isLightToggledOn = !_isLightToggledOn;
                        break;
                    }
                    else if (!flashlightToggle && hand.Input.TouchpadDown && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.down) < 45f)
                    {
                        _isLaserToggledOn = !_isLaserToggledOn;
                        break;
                    }
                }
                else
                {
                    if (flashlightToggle && hand.Input.BYButtonDown)
                    {
                        _isLightToggledOn = !_isLightToggledOn;
                        break;
                    }
                    else if (!flashlightToggle && hand.Input.AXButtonDown)
                    {
                        _isLaserToggledOn = !_isLaserToggledOn;
                        break;
                    }
                }
                yield return null;
            }
            _waitingForDoubleTap = false;
        }

        /// <summary>
        /// Finds the correct index for the laser light settings based turnLightOn the current state of the laser and light.
        /// </summary>
        /// <param name="laserLight">Laserlight attachment the settings index should be found for.</param>
        /// <param name="laserOn">Should the laser be turnLightOn?</param>
        /// <param name="lightOn">Should the light be turnLightOn?</param>
        /// <param name="IROn">Should IR be enabled?</param>
        /// <returns>Returns the correct settings index or -1 if it couldn't be found.</returns>
        private int FindCorrectLaserLightSettingsIndex(LaserLightAttachment laserLight, bool laserOn, bool lightOn, bool IROn)
        {
            int settingsIndex = -1;
            LaserLightSetting currentLightSetting = laserLight.Settings[laserLight.SettingsIndex];
            LaserAttachmentMode requestedLaserMode;
            LightAttachmentMode requestedLightMode;

            if (!IROn)
            {
                if (laserOn) requestedLaserMode = LaserAttachmentMode.LaserVisible;
                else requestedLaserMode = LaserAttachmentMode.Off;
                if (lightOn) requestedLightMode = LightAttachmentMode.LightVisibleContinuous;
                else requestedLightMode = LightAttachmentMode.Off;
            }
            else
            {
                if (laserOn) requestedLaserMode = LaserAttachmentMode.LaserIR;
                else requestedLaserMode = LaserAttachmentMode.Off;
                if (lightOn) requestedLightMode = LightAttachmentMode.LightIRContinuous;
                else requestedLightMode = LightAttachmentMode.Off;
            }

            settingsIndex = laserLight.Settings.FindIndex(x => x.LaserMode == requestedLaserMode && x.LightMode == requestedLightMode);

            // if only light or laser exist but both light and lasers should be turnLightOn, these will be used after all other options couldn't be found:
            if (settingsIndex == -1)
            {
                if (laserOn && lightOn && !IROn) settingsIndex = laserLight.Settings.FindIndex(x => x.LaserMode == LaserAttachmentMode.LaserVisible && x.LightMode == LightAttachmentMode.Off);
                else if (laserOn && lightOn && !IROn) settingsIndex = laserLight.Settings.FindIndex(x => x.LaserMode == LaserAttachmentMode.Off && x.LightMode == LightAttachmentMode.LightVisibleContinuous);
                else if (laserOn && lightOn && IROn) settingsIndex = laserLight.Settings.FindIndex(x => x.LaserMode == LaserAttachmentMode.LaserIR && x.LightMode == LightAttachmentMode.Off);
                else if (laserOn && lightOn && IROn) settingsIndex = laserLight.Settings.FindIndex(x => x.LaserMode == LaserAttachmentMode.Off && x.LightMode == LightAttachmentMode.LightIRContinuous);
            }  

            return settingsIndex;
        }

#if !DEBUG
        static PressurePadAttachmentInterface()
        {
            _baseUpdateInteractionPointer = typeof(FVRAlternateGrip).BaseType.GetMethod("UpdateInteraction").MethodHandle.GetFunctionPointer();
            On.FistVR.FVRAlternateGrip.UpdateInteraction += FVRAlternateGrip_UpdateInteraction;
        }

        // Patching the base UpdateInteraction of FVRAlternateGrip to stop attachable foregrips from being detached when using the pressure pad attachment.
        private static void FVRAlternateGrip_UpdateInteraction(On.FistVR.FVRAlternateGrip.orig_UpdateInteraction orig, FVRAlternateGrip self, FVRViveHand hand)
        {
            if (_existingPressurePadAttachments.ContainsKey(self))
            {
                var BaseMethod = (Action<FVRViveHand>)Activator.CreateInstance(typeof(Action<FVRViveHand>), self, _baseUpdateInteractionPointer);
                BaseMethod(hand);

                Vector2 touchpadAxes = hand.Input.TouchpadAxes;
                bool flag = true;
                if (!self.DoesBracing)
                {
                    flag = false;
                }
                if (self.m_wasGrabbedFromAttachableForegrip && !self.m_lastGrabbedInGrip.DoesBracing)
                {
                    flag = false;
                }
                if (self.PrimaryObject.IsAltHeld)
                {
                    flag = false;
                }
                if (flag && hand.Input.TriggerPressed)
                {
                    if (!self.m_hasSavedPalmPoint)
                    {
                        self.AttemptToGenerateSavedPalmPoint();
                        if (self.m_hasSavedPalmPoint)
                        {
                            hand.Buzz(hand.Buzzer.Buzz_BeginInteraction);
                        }
                    }
                    else if (Vector3.Distance(self.m_savedRigPalmPos, self.m_hand.PalmTransform.position) > 0.2f)
                    {
                        self.ClearSavedPalmPoint();
                    }
                }
                else if (self.m_hasSavedPalmPoint)
                {
                    self.ClearSavedPalmPoint();
                }
                if (hand.Input.TriggerUp)
                {
                    self.ClearSavedPalmPoint();
                }
                if (!self.m_wasGrabbedFromAttachableForegrip && self.HasQuickMagEject && self.PrimaryObject is FVRFireArm)
                {
                    FVRFireArm fvrfireArm = self.PrimaryObject as FVRFireArm;
                    if (fvrfireArm.UsesMagazines && fvrfireArm.Magazine != null && !fvrfireArm.Magazine.IsIntegrated)
                    {
                        if (hand.IsInStreamlinedMode)
                        {
                            if (hand.Input.AXButtonDown)
                            {
                                fvrfireArm.EjectMag(false);
                            }
                        }
                        else if (hand.Input.TouchpadDown && Vector2.Angle(Vector2.down, touchpadAxes) < 45f && touchpadAxes.magnitude > 0.2f)
                        {
                            fvrfireArm.EjectMag(false);
                        }
                    }
                }
                self.PassHandInput(hand, self);
            }
            else 
            {
                orig(self, hand);
            }
        }
#endif
    }
}