using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace OpenWaters
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    public class OpenWatersPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "valheim.openwaters";
        public const string PluginName = "Open Waters";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> EnabledConfig;

        private void Awake()
        {
            Log = Logger;
            EnabledConfig = Config.Bind(
                "General",
                "Enabled",
                true,
                "Enable Open Waters world generation and UI controls.");
            OpenWatersSettings.Enabled = EnabledConfig.Value;

            new Harmony(PluginGuid).PatchAll();
            gameObject.AddComponent<WorldCreationUI>();
            Log.LogInfo($"Open Waters plugin loaded (enabled={OpenWatersSettings.Enabled}).");
        }

        internal static void SetEnabled(bool enabled)
        {
            OpenWatersSettings.Enabled = enabled;
            if (EnabledConfig != null)
            {
                EnabledConfig.Value = enabled;
                EnabledConfig.ConfigFile.Save();
            }
            Log.LogInfo($"Open Waters enabled={enabled}");
        }
    }
}
