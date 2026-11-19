using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OpenWaters
{
    internal static class NoiseModifier
    {
        private delegate float GetBaseHeightDelegate(float wx, float wy, bool menuTerrain);

        private sealed class UnityNoise : INoise
        {
            public float Perlin(double x, double y)
            {
                return Mathf.PerlinNoise((float)x, (float)y);
            }
        }

        private const float OceanThreshold = 0.02f;

        public static bool Active => GenerationSession.Current.Active;
        public static bool UsesGlobalNoise => GenerationSession.Current.UsesGlobalNoise;
        public static float HeightOffset => GenerationSession.Current.HeightOffset;

        private static bool IsFew =>
            OpenWatersSettings.IslandAmount == OpenWatersSettings.FewIslands;

        public static void SetWorld(World world)
        {
            GenerationSession.Begin(world);
        }

        public static void Cancel()
        {
            GenerationSession.Current.Cancel();
        }

        public static void LogGeneratorState(string scope, WorldGenerator generator)
        {
            if (generator == null)
            {
                OpenWatersPlugin.Log.LogWarning($"[Gen] Generator state ({scope}): no generator.");
                return;
            }

            World world = generator.m_world;
            string worldName = world != null ? world.m_worldName : "none";
            string seedName = world != null ? world.m_seedName : "none";
            int seed = world != null ? world.m_seed : 0;
            int version = world != null ? world.m_worldGenVersion : -1;
            OpenWatersPlugin.Log.LogInfo(
                $"[Gen] Generator state: scope={scope}, world='{worldName}', seedName='{seedName}', " +
                $"seed={seed}, version={version}, " +
                $"offset0={ReadFloat(generator, "m_offset0"):0.0000}, " +
                $"offset1={ReadFloat(generator, "m_offset1"):0.0000}, " +
                $"riverSeed={ReadInt(generator, "m_riverSeed")}, " +
                $"streamSeed={ReadInt(generator, "m_streamSeed")}, " +
                $"rivers={ReadCount(generator, "m_rivers")}, " +
                $"streams={ReadCount(generator, "m_streams")}, " +
                $"lakes={ReadCount(generator, "m_lakes")}.");
        }

        private static float ReadFloat(WorldGenerator generator, string field)
        {
            FieldInfo info = AccessTools.Field(typeof(WorldGenerator), field);
            return info != null ? (float)info.GetValue(generator) : float.NaN;
        }

        private static int ReadInt(WorldGenerator generator, string field)
        {
            FieldInfo info = AccessTools.Field(typeof(WorldGenerator), field);
            return info != null ? (int)info.GetValue(generator) : -1;
        }

        private static int ReadCount(WorldGenerator generator, string field)
        {
            FieldInfo info = AccessTools.Field(typeof(WorldGenerator), field);
            if (info == null)
            {
                return -1;
            }
            System.Collections.ICollection collection = info.GetValue(generator) as System.Collections.ICollection;
            return collection != null ? collection.Count : 0;
        }

        public static void Build(WorldGenerator generator)
        {
            GenerationSession session = GenerationSession.Current;
            session.BeginBuild();

            World world = session.World;
            if (world == null || world.m_menu || !OpenWatersSettings.AffectsGeneration)
            {
                return;
            }

            long started = DateTime.UtcNow.Ticks;
            try
            {
                FieldInfo offset0 = AccessTools.Field(typeof(WorldGenerator), "m_offset0");
                FieldInfo offset1 = AccessTools.Field(typeof(WorldGenerator), "m_offset1");
                FieldInfo mountainDistance = AccessTools.Field(typeof(WorldGenerator), "m_minMountainDistance");
                session.Offset0 = (float)offset0.GetValue(generator);
                session.Offset1 = (float)offset1.GetValue(generator);
                session.MinMountainDistance = mountainDistance != null
                    ? (float)mountainDistance.GetValue(generator)
                    : 1000f;
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogError($"[Gen] Could not read noise offsets: {exception}");
                return;
            }

            MethodInfo method = AccessTools.Method(typeof(WorldGenerator), "GetBaseHeight");
            if (method == null)
            {
                OpenWatersPlugin.Log.LogError("[Gen] Could not find WorldGenerator.GetBaseHeight.");
                return;
            }

            GetBaseHeightDelegate original;
            try
            {
                original = (GetBaseHeightDelegate)Delegate.CreateDelegate(
                    typeof(GetBaseHeightDelegate), generator, method);
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogError($"[Gen] Could not bind GetBaseHeight: {exception}");
                return;
            }

            session.UsesGlobalNoise =
                OpenWatersSettings.IslandAmount != OpenWatersSettings.VanillaIslandAmount;
            NoiseField field = new NoiseField(new UnityNoise(), CreateConfig(session));
            session.Field = field;
            float radius = OpenWatersSettings.WorldRadius;

            if (!session.UsesGlobalNoise)
            {
                if (!OpenWatersSettings.ExcludeMountains &&
                    OpenWatersSettings.StartBiomeLimit == 0)
                {
                    return;
                }
                IslandAnalysis.Build(generator, radius + 500f);
                session.MarkReady();
                long elapsed = (DateTime.UtcNow.Ticks - started) / TimeSpan.TicksPerMillisecond;
                OpenWatersPlugin.Log.LogInfo(
                    $"[Gen] Start island only: preset=Vanilla, startIsland={IslandAnalysis.StartCells}, " +
                    $"biomeLimit={OpenWatersSettings.StartBiomeLimit}, time={elapsed} ms.");
                return;
            }

            LandCalibrator calibrator = new LandCalibrator(radius);
            calibrator.Run(field, (wx, wy) => original(wx, wy, false) > OceanThreshold);

            session.HeightOffset = calibrator.Offset;
            session.MarkReady();

            int startIslandCells = 0;
            if (OpenWatersSettings.ExcludeMountains ||
                OpenWatersSettings.StartBiomeLimit != 0)
            {
                IslandAnalysis.Build(generator, radius + 500f);
                startIslandCells = IslandAnalysis.StartCells;
            }

            long elapsedMs = (DateTime.UtcNow.Ticks - started) / TimeSpan.TicksPerMillisecond;
            OpenWatersPlugin.Log.LogInfo(
                $"[Gen] Noise frequency: preset={OpenWatersSettings.IslandAmountName}, " +
                $"scale={field.FrequencyScale:0.00}, detail={field.DetailDamping:0.00}, " +
                $"peak={field.PeakProtection:0.00}, vanillaLand={calibrator.VanillaLand}, " +
                $"target={calibrator.TargetLand}, actual={calibrator.ActualLand}, " +
                $"mountains={calibrator.ActualMountains} " +
                $"({(calibrator.ActualLand > 0 ? 100f * calibrator.ActualMountains / calibrator.ActualLand : 0f):0}% of land), " +
                $"startIsland={startIslandCells}, offset={calibrator.Offset:0.000}, time={elapsedMs} ms.");
        }

        private static NoiseFieldConfig CreateConfig(GenerationSession session)
        {
            bool few = IsFew;
            return new NoiseFieldConfig
            {
                FrequencyScale = few
                    ? OpenWatersSettings.FewFrequencyScale
                    : OpenWatersSettings.MediumFrequencyScale,
                LandFraction = OpenWatersSettings.LandFraction,
                DetailDamping = few
                    ? OpenWatersSettings.FewDetailDamping
                    : OpenWatersSettings.MediumDetailDamping,
                PeakProtection = few
                    ? OpenWatersSettings.FewPeakProtection
                    : OpenWatersSettings.MediumPeakProtection,
                MountainCompressionStart = few
                    ? OpenWatersSettings.FewMountainCompressionStart
                    : 0f,
                MountainCompressionFactor = few
                    ? OpenWatersSettings.FewMountainCompressionFactor
                    : 1f,
                Offset0 = session.Offset0,
                Offset1 = session.Offset1,
                MinMountainDistance = session.MinMountainDistance
            };
        }

        public static float ApplyGlobal(GenerationSession session, float wx, float wy)
        {
            NoiseField field = session.Field;
            float value = field.Compress(field.BaseHeight(wx, wy, session.HeightOffset));
            return StartIslandModifier.Apply(value, wx, wy);
        }
    }
}
