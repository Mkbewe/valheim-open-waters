using System;
using HarmonyLib;

namespace OpenWaters
{
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnNewWorldDone))]
    internal static class FejdStartupOnNewWorldDonePatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            FejdStartup startup = FejdStartup.instance;
            if (startup != null && startup.m_newWorldName != null && startup.m_newWorldSeed != null)
            {
                string name = startup.m_newWorldName.text;
                if (!string.IsNullOrEmpty(name) && !World.HaveWorld(name))
                {
                    WorldSettingsStore.Capture(name, startup.m_newWorldSeed.text);
                }
            }

            OpenWatersPlugin.Log.LogInfo(
                $"New world settings: WorldRadius={OpenWatersSettings.WorldRadius}, IslandAmount={OpenWatersSettings.IslandAmountName} ({OpenWatersSettings.IslandAmount}), " +
                $"ActiveForGeneration={OpenWatersSettings.AffectsGeneration}, " +
                $"Land={OpenWatersSettings.LandFraction:P0}, " +
                $"BiomeLimit={OpenWatersSettings.BiomeLimit}, " +
                $"StartBiomeLimit={OpenWatersSettings.StartBiomeLimit}, ExcludeMountains={OpenWatersSettings.ExcludeMountains}, " +
                $"Preview={OpenWatersSettings.PreviewEnabled}, Enabled={OpenWatersSettings.Enabled}");
        }

        [HarmonyPostfix]
        private static void Postfix(FejdStartup __instance)
        {
            try
            {
                World world = Traverse.Create(__instance).Field("m_world").GetValue<World>();
                WorldSettingsStore.Persist(world);
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogWarning($"[Settings] Could not persist world settings: {exception.Message}");
            }
        }
    }
}
