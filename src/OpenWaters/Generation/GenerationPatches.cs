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

    [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome),
        new Type[] { typeof(float), typeof(float), typeof(float), typeof(bool) })]
    internal static class WorldGeneratorGetBiomePatch
    {
        [HarmonyPostfix]
        private static void Postfix(float wx, float wy, ref Heightmap.Biome __result)
        {
            if (GenerationSession.Current.Stage != GenerationStage.Ready || IslandAnalysis.Analyzing)
            {
                return;
            }

            if (__result == Heightmap.Biome.Ocean ||
                __result == Heightmap.Biome.AshLands ||
                __result == Heightmap.Biome.DeepNorth ||
                __result == Heightmap.Biome.Mountain)
            {
                return;
            }

            if (StartIslandModifier.TryGetBiomeOverride(wx, wy, __result, out Heightmap.Biome biome))
            {
                __result = biome;
            }
        }
    }

    [HarmonyPatch(typeof(WorldGenerator), "GetBaseHeight")]
    internal static class WorldGeneratorGetBaseHeightPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(float wx, float wy, bool menuTerrain, ref float __result)
        {
            GenerationSession session = GenerationSession.Current;
            if (menuTerrain || session.Stage != GenerationStage.Ready ||
                !session.UsesGlobalNoise || session.Field == null)
            {
                return true;
            }

            __result = NoiseModifier.ApplyGlobal(session, wx, wy);
            return false;
        }

        [HarmonyPostfix]
        private static void Postfix(float wx, float wy, bool menuTerrain, ref float __result)
        {
            GenerationSession session = GenerationSession.Current;
            if (!menuTerrain && session.Stage == GenerationStage.Ready && !session.UsesGlobalNoise)
            {
                __result = StartIslandModifier.Apply(__result, wx, wy);
            }
        }
    }
}
