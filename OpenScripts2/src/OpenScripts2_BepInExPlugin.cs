using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace OpenScripts2
{
    [BepInPlugin("h3vr.OpenScripts2", "OpenScripts2", "2.1.1")]
    public class OpenScripts2_BepInExPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> FirearmHeatingEffect_CanExplode;
        public static ConfigEntry<bool> FirearmHeatingEffect_CanRecover;
        public static ConfigEntry<float> FirearmHeatingEffect_RecoverThreshold;
        public static ConfigEntry<bool> FirearmHeatingEffect_CanChangeFirerate;
        public static ConfigEntry<bool> FirearmHeatingEffect_CanChangeAccuracy;
        public static ConfigEntry<bool> FirearmHeatingEffect_CanCookOff;

        public BepInEx.Logging.ManualLogSource Logging
        {
            get {return Logger; }
        }

        public static OpenScripts2_BepInExPlugin Instance;
        public OpenScripts2_BepInExPlugin()
        {
            Instance = this;

            // FirearmHeatingEffect Config Bindings
            FirearmHeatingEffect_CanExplode = Config.Bind("Firearm Heating Effect", "Part can explode", true, "If true, and the part is setup to do so, the parts with heating effects can explode.");
            FirearmHeatingEffect_CanRecover = Config.Bind("Firearm Heating Effect", "Part can recover", false, "If true, parts can recover from being exploded.");
            FirearmHeatingEffect_RecoverThreshold = Config.Bind("Firearm Heating Effect", "Recover heat threshold", 0f, new ConfigDescription("Defines the heat value, at which the part will recover from being exploded", new AcceptableValueRange<float>(0, 1)));
            FirearmHeatingEffect_CanChangeFirerate = Config.Bind("Firearm Heating Effect", "Gun can change firerate", true, "If true, enables firearm firerate changes based on heat.");
            FirearmHeatingEffect_CanChangeAccuracy = Config.Bind("Firearm Heating Effect", "Gun can change accuracy", true, "If true, enables firearm accuracy changes based on heat.");
            FirearmHeatingEffect_CanCookOff = Config.Bind("Firearm Heating Effect", "Gun can cook off", true, "If true, enables firearm cookoff chance based on heat.");
        }

        public static void Log(MonoBehaviour plugin, string message)
        {
            Instance.Logging.LogMessage($"{nameof(plugin)}: {message}");
        }
        public static void LogWarning(MonoBehaviour plugin, string message)
        {
            Instance.Logging.LogWarning($"{nameof(plugin)}: {message}");
        }
        public static void LogError(MonoBehaviour plugin, string message)
        {
            Instance.Logging.LogError($"{nameof(plugin)}: {message}");
        }
        public static void LogException(MonoBehaviour plugin, Exception e)
        {
            Instance.Logging.LogError($"{nameof(plugin)}: {e.Message}");
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