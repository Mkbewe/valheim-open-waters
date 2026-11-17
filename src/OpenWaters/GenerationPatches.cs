using System;
using HarmonyLib;

namespace OpenWaters
{
    [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.Initialize))]
    internal static class WorldGeneratorInitializePatch
    {
        [HarmonyPrefix]
        private static void Prefix(World world)
        {
            WorldSettingsStore.Apply(world);
            NoiseModifier.SetWorld(world);
            OpenWatersPlugin.Log.LogInfo(
                $"[Gen] Initialize world='{world.m_worldName}' seed='{world.m_seedName}', menu={world.m_menu}, " +
                $"enabled={OpenWatersSettings.Enabled}, preset={OpenWatersSettings.IslandAmountName}, " +
                $"land={OpenWatersSettings.LandFraction:P0}");
        }
    }

    [HarmonyPatch(typeof(WorldGenerator), "Pregenerate")]
    internal static class WorldGeneratorPregeneratePatch
    {
        [HarmonyPrefix]
        private static void Prefix(WorldGenerator __instance)
        {
            try
            {
                NoiseModifier.Build(__instance);
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogError($"[Gen] Noise frequency failed: {exception}");
                NoiseModifier.Cancel();
            }
        }
    }

    [HarmonyPatch(typeof(WorldGenerator), "GetBaseHeight")]
    internal static class WorldGeneratorGetBaseHeightPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(float wx, float wy, bool menuTerrain, ref float __result)
        {
            if (menuTerrain || !NoiseModifier.Active || !NoiseModifier.UsesGlobalNoise)
            {
                return true;
            }

            __result = NoiseModifier.ApplyGlobal(wx, wy);
            return false;
        }

        [HarmonyPostfix]
        private static void Postfix(float wx, float wy, bool menuTerrain, ref float __result)
        {
            if (!menuTerrain && NoiseModifier.Active && !NoiseModifier.UsesGlobalNoise)
            {
                __result = NoiseModifier.ApplyStartIslandOnly(__result, wx, wy);
            }
        }
    }
}
