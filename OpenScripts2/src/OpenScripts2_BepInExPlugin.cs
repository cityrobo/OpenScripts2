#if !(DEBUG || MEATKIT)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using UnityEngine;

namespace OpenScripts2
{
    [BepInPlugin("h3vr.openscripts2", "OpenScripts2: Main Plugin", "1.0.0")]
    public class OpenScripts2_BepInExPlugin : BaseUnityPlugin
    {
        public BepInEx.Logging.ManualLogSource Logging
        {
            get {return Logger; }
        }

        public static OpenScripts2_BepInExPlugin Instance;
        public OpenScripts2_BepInExPlugin()
        {
            Logger.LogInfo("OpenScripts2 loaded!");
            Instance = this;
        }

        public static void Log(MonoBehaviour plugin, string message)
        {
            Instance.Logging.LogMessage(string.Format("{0}: {1}", nameof(plugin), message));
        }
        public static void LogWarning(MonoBehaviour plugin, string message)
        {
            Instance.Logging.LogWarning(string.Format("{0}: {1}", nameof(plugin), message));
        }
        public static void LogError(MonoBehaviour plugin, string message)
        {
            Instance.Logging.LogError(string.Format("{0}: {1}", nameof(plugin), message));
        }
    }

    public static class OpenScripts2_Extensions
    {
        public static void Log(this OpenScripts2_BasePlugin plugin, string message)
        {
            OpenScripts2_BepInExPlugin.Instance.Logging.LogMessage(string.Format("{0}: {1}", nameof(plugin), message));
        }
        public static void LogWarning(this OpenScripts2_BasePlugin plugin, string message)
        {
            OpenScripts2_BepInExPlugin.Instance.Logging.LogWarning(string.Format("{0}: {1}", nameof(plugin), message));
        }
        public static void LogError(this OpenScripts2_BasePlugin plugin, string message)
        {
            OpenScripts2_BepInExPlugin.Instance.Logging.LogError(string.Format("{0}: {1}", nameof(plugin), message));
        }
    }
}
#endif