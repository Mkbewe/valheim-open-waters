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
        public const string PluginVersion = "0.2.1";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> EnabledConfig;
        internal static ConfigEntry<bool> DebugSelfTestConfig;
        internal static ConfigEntry<bool> LocalSidecarFallbackConfig;
        internal static ConfigEntry<float> FewLandFractionConfig;
        internal static ConfigEntry<float> FewFrequencyScaleConfig;
        internal static ConfigEntry<float> FewPeakProtectionConfig;
        internal static ConfigEntry<float> FewDetailDampingConfig;
        internal static ConfigEntry<float> FewMountainCompressionStartConfig;
        internal static ConfigEntry<float> FewMountainCompressionFactorConfig;
        internal static ConfigEntry<float> MediumLandFractionConfig;
        internal static ConfigEntry<float> MediumFrequencyScaleConfig;
        internal static ConfigEntry<float> MediumPeakProtectionConfig;
        internal static ConfigEntry<float> MediumDetailDampingConfig;

        private void Awake()
        {
            Log = Logger;
            EnabledConfig = Config.Bind(
                "General",
                "Enabled",
                true,
                "Enable Open Waters world generation and UI controls.");
            DebugSelfTestConfig = Config.Bind(
                "General",
                "DebugSelfTest",
                false,
                "Run built-in generation and network checks at startup and write results to the log.");
            LocalSidecarFallbackConfig = Config.Bind(
                "General",
                "LocalSidecarFallback",
                false,
                "Use a manually copied OpenWaters.worldSettings when the server does not provide compatible settings.");

            FewLandFractionConfig = BindFloat(
                "Few",
                "LandFraction",
                0.27f,
                0.01f,
                1f,
                "Target land fraction for Few (relative to vanilla land).");
            FewFrequencyScaleConfig = BindFloat(
                "Few",
                "FrequencyScale",
                0.60f,
                0.10f,
                2f,
                "Noise frequency multiplier for Few. Lower values create larger islands.");
            FewPeakProtectionConfig = BindFloat(
                "Few",
                "PeakProtection",
                0.20f,
                0f,
                1f,
                "How strongly high peaks resist the land-lowering offset for Few.");
            FewDetailDampingConfig = BindFloat(
                "Few",
                "DetailDamping",
                0.35f,
                0f,
                1f,
                "Detail noise multiplier for Few.");
            FewMountainCompressionStartConfig = BindFloat(
                "Few",
                "MountainCompressionStart",
                0.34f,
                0f,
                1f,
                "Height above which Few compresses mountain terrain.");
            FewMountainCompressionFactorConfig = BindFloat(
                "Few",
                "MountainCompressionFactor",
                0.55f,
                0f,
                1f,
                "Compression factor for high terrain in Few. Lower values flatten mountains.");

            MediumLandFractionConfig = BindFloat(
                "Medium",
                "LandFraction",
                0.50f,
                0.01f,
                1f,
                "Target land fraction for Medium (relative to vanilla land).");
            MediumFrequencyScaleConfig = BindFloat(
                "Medium",
                "FrequencyScale",
                0.70f,
                0.10f,
                2f,
                "Noise frequency multiplier for Medium. Lower values create larger islands.");
            MediumPeakProtectionConfig = BindFloat(
                "Medium",
                "PeakProtection",
                0.60f,
                0f,
                1f,
                "How strongly high peaks resist the land-lowering offset for Medium.");
            MediumDetailDampingConfig = BindFloat(
                "Medium",
                "DetailDamping",
                0.35f,
                0f,
                1f,
                "Detail noise multiplier for Medium.");

            OpenWatersSettings.Enabled = EnabledConfig.Value;
            OpenWatersSettings.DebugSelfTest = DebugSelfTestConfig.Value;
            OpenWatersSettings.LocalSidecarFallback = LocalSidecarFallbackConfig.Value;

            new Harmony(PluginGuid).PatchAll();
            if (OpenWatersSettings.DebugSelfTest)
            {
                SelfTest.Run(message => Log.LogInfo(message));
            }
            gameObject.AddComponent<WorldCreationUI>();
            Log.LogInfo($"Open Waters plugin loaded (enabled={OpenWatersSettings.Enabled}).");
        }

        private ConfigEntry<float> BindFloat(
            string section,
            string key,
            float defaultValue,
            float min,
            float max,
            string description)
        {
            return Config.Bind(
                section,
                key,
                defaultValue,
                new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));
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
