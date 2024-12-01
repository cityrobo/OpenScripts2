using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using MonoMod.Cil;
using UnityEngine;
using FistVR;

namespace OpenScripts2
{
#if !DEBUG
    [BepInPlugin("h3vr.OpenScripts2", "OpenScripts2", "2.11.1")]
#endif
    public class OpenScripts2_BepInExPlugin : BaseUnityPlugin
    {
        // FirearmHeatingEffect Config Entries
        public static ConfigEntry<bool> FirearmHeatingEffect_CanExplode;
        public static ConfigEntry<bool> FirearmHeatingEffect_CanRecover;
        public static ConfigEntry<float> FirearmHeatingEffect_RecoverThreshold;
        public static ConfigEntry<bool> FirearmHeatingEffect_CanChangeFirerate;
        public static ConfigEntry<bool> FirearmHeatingEffect_CanChangeAccuracy;
        public static ConfigEntry<bool> FirearmHeatingEffect_CanCookOff;

        // Advanced MagGrab Trigger Config Entries
        public static ConfigEntry<bool> AdvancedMagGrabSimpleMagRelease;

        // Handgun and Physical Object Spin Config Entries
        public static ConfigEntry<float> SpinReleaseDelayTime;
        public static ConfigEntry<bool> SpinToggle;
        public static ConfigEntry<bool> SpinGrabHelper;
        public static ConfigEntry<float> SpinGrabHelperScale;

        public BepInEx.Logging.ManualLogSource Logging => Logger;

        public static OpenScripts2_BepInExPlugin Instance;

        public void Awake()
        {
            Instance = this;

            // FirearmHeatingEffect Config Bindings
            FirearmHeatingEffect_CanExplode = Config.Bind("Firearm Heating Effect", "Part can explode", true, "If true, and the part is setup to do so, the parts with heating effects can explode.");
            FirearmHeatingEffect_CanRecover = Config.Bind("Firearm Heating Effect", "Part can recover", false, "If true, parts can recover from being exploded.");
            FirearmHeatingEffect_RecoverThreshold = Config.Bind("Firearm Heating Effect", "Recover heat threshold", 0f, new ConfigDescription("Defines the heat value, at which the part will recover from being exploded", new AcceptableValueRange<float>(0f, 1f)));
            FirearmHeatingEffect_CanChangeFirerate = Config.Bind("Firearm Heating Effect", "Gun can change firerate", true, "If true, enables firearm firerate changes based on heat.");
            FirearmHeatingEffect_CanChangeAccuracy = Config.Bind("Firearm Heating Effect", "Gun can change accuracy", true, "If true, enables firearm accuracy changes based on heat.");
            FirearmHeatingEffect_CanCookOff = Config.Bind("Firearm Heating Effect", "Gun can cook off", true, "If true, enables firearm cookoff chance based on heat.");

            // Advanced MagGrab Trigger Config Bindings
            AdvancedMagGrabSimpleMagRelease = Config.Bind("Advanced Magazine Grab Trigger", "Simple Magazine Release", false, "If true, disables input requirements from advanced magazine wells.");

            // Handgun and Physical Object Spin Config Bindings
            SpinReleaseDelayTime = Config.Bind("Handgun and Physical Object Spin", "Spin Release Delay Time", 0.25f, "Delay between letting go of the spin input and stopping to spin.");
            SpinToggle = Config.Bind("Handgun and Physical Object Spin", "Spin Toggle", false, "This option lets you toggle spinning. Great when you wanna catch the revolver while spining!");
            SpinGrabHelper = Config.Bind("Handgun and Physical Object Spin", "Spin Grab Helper", true, "If this option is true, the grab collider of the revolver will be scaled up by below value to help with grabbing after tossing the gun.");
            SpinGrabHelperScale = Config.Bind("Handgun and Physical Object Spin", "Spin Grab Helper Scale", 3f);

#if !DEBUG
            IL.FistVR.FVRFireArm.Fire += OpenScripts2_BasePlugin.FVRFireArm_Fire_ProjectileFiredEventHook;
#endif
        }

        public static void Log(MonoBehaviour plugin, string message)
        {
            Instance.Logging.LogMessage($"{plugin}: {message}");
        }
        public static void LogWarning(MonoBehaviour plugin, string message)
        {
            Instance.Logging.LogWarning($"{plugin}: {message}");
        }
        public static void LogError(MonoBehaviour plugin, string message)
        {
            Instance.Logging.LogError($"{plugin}: {message}");
        }
        public static void LogException(MonoBehaviour plugin, Exception e)
        {
            Instance.Logging.LogError($"{plugin}: {e.Message}");
        }
    }



    /*
    public static class OpenScripts2_Extensions
    {
        public static void Log(this OpenScripts2_BasePlugin plugin, string message)
        {
            OpenScripts2_BepInExPlugin.Instance.Logging.LogMessage($"{nameof(plugin)}: {message}");
        }
        public static void LogWarning(this OpenScripts2_BasePlugin plugin, string message)
        {
            OpenScripts2_BepInExPlugin.Instance.Logging.LogWarning($"{nameof(plugin)}: {message}");
        }
        public static void LogError(this OpenScripts2_BasePlugin plugin, string message)
        {
            OpenScripts2_BepInExPlugin.Instance.Logging.LogError($"{nameof(plugin)}: {message}");
        }

        public static void LogException(this OpenScripts2_BasePlugin plugin, Exception e)
        {
            OpenScripts2_BepInExPlugin.Instance.Logging.LogError($"{nameof(plugin)}: {e.Message}");
        }
    }
    */
}