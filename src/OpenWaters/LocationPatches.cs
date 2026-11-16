using System;
using HarmonyLib;

namespace OpenWaters
{
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GenerateLocations))]
    internal static class ZoneSystemGenerateLocationsPatch
    {
        private const float ModdedDragonqueenMinAltitude = 40f;

        [HarmonyPrefix]
        private static void Prefix(ZoneSystem __instance)
        {
            if (!OpenWatersSettings.AffectsGeneration)
            {
                return;
            }

            foreach (ZoneSystem.ZoneLocation location in __instance.m_locations)
            {
                if (location == null)
                {
                    continue;
                }

                string name = string.IsNullOrEmpty(location.m_prefabName)
                    ? location.m_name
                    : location.m_prefabName;
                if (string.IsNullOrEmpty(name) ||
                    name.IndexOf("Dragonqueen", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (location.m_minAltitude > ModdedDragonqueenMinAltitude)
                {
                    OpenWatersPlugin.Log.LogInfo(
                        $"[Gen] {OpenWatersSettings.IslandAmountName}: lowering Dragonqueen min altitude " +
                        $"{location.m_minAltitude:0} -> {ModdedDragonqueenMinAltitude:0}");
                    location.m_minAltitude = ModdedDragonqueenMinAltitude;
                }
            }
        }
    }
}
