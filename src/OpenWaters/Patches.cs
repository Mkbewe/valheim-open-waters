using HarmonyLib;

namespace OpenWaters
{
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnNewWorldDone))]
    internal static class FejdStartupOnNewWorldDonePatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            OpenWatersPlugin.Log.LogInfo(
                $"New world settings: LandCoverage={OpenWatersSettings.LandCoverage:0.00}, " +
                $"WorldRadius={OpenWatersSettings.WorldRadius}, IslandAmount={OpenWatersSettings.IslandAmount}, " +
                $"ActiveForGeneration={OpenWatersSettings.AffectsGeneration}, " +
                $"BiomeLimit={OpenWatersSettings.BiomeLimit}, " +
                $"StartBiomeLimit={OpenWatersSettings.StartBiomeLimit}, ExcludeMountains={OpenWatersSettings.ExcludeMountains}");
        }
    }
}
