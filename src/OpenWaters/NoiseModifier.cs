using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OpenWaters
{
    internal static class NoiseModifier
    {
        private delegate float GetBaseHeightDelegate(float wx, float wy, bool menuTerrain);

        private const int CalibrationSamples = 512;
        private const float EdgeSize = 500f;
        private const float OceanThreshold = 0.02f;
        private const float StartFadeInner = 800f;
        private const float StartFadeOuter = 1600f;
        private const float PeakProtectStart = 0.28f;
        private const float PeakProtectEnd = 0.45f;
        private const float MaxOffset = 0.6f;
        private const float MediumFrequencyScale = 0.7f;
        private const float FewFrequencyScale = 0.6f;
        private const float MediumPeakProtection = 0.6f;
        private const float FewPeakProtection = 0.2f;
        private const float FewMountainCompressionStart = 0.34f;
        private const float FewMountainCompressionFactor = 0.55f;
        private const float MountainThreshold = 0.4f;
        private const float DetailDamping = 0.35f;

        private static World _world;
        private static float _offset0;
        private static float _offset1;
        private static float _minMountainDistance = 1000f;
        private static float _heightOffset;

        public static bool Active { get; private set; }
        public static float HeightOffset => _heightOffset;

        private static bool IsFew => OpenWatersSettings.IslandAmount == OpenWatersSettings.FewIslands;
        private static float FrequencyScale => IsFew ? FewFrequencyScale : MediumFrequencyScale;
        private static float PeakProtection => IsFew ? FewPeakProtection : MediumPeakProtection;

        public static void SetWorld(World world)
        {
            _world = world;
            Active = false;
            _heightOffset = 0f;
        }

        public static void Cancel()
        {
            Active = false;
            _heightOffset = 0f;
        }

        public static void Build(WorldGenerator generator)
        {
            Active = false;
            _heightOffset = 0f;

            World world = _world;
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
                _offset0 = (float)offset0.GetValue(generator);
                _offset1 = (float)offset1.GetValue(generator);
                _minMountainDistance = mountainDistance != null ? (float)mountainDistance.GetValue(generator) : 1000f;
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
                original = (GetBaseHeightDelegate)Delegate.CreateDelegate(typeof(GetBaseHeightDelegate), generator, method);
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogError($"[Gen] Could not bind GetBaseHeight: {exception}");
                return;
            }

            int size = CalibrationSamples;
            float radius = OpenWatersSettings.WorldRadius + EdgeSize;
            float cellSize = 2f * radius / size;
            float[] modified = new float[size * size];
            float[] fade = new float[size * size];
            float[] peak = new float[size * size];
            int vanillaLand = 0;

            for (int y = 0; y < size; y++)
            {
                float wy = -radius + (y + 0.5f) * cellSize;
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    float wx = -radius + (x + 0.5f) * cellSize;
                    if (wx * wx + wy * wy > radius * radius)
                    {
                        modified[index] = -1f;
                        continue;
                    }

                    if (original(wx, wy, false) > OceanThreshold)
                    {
                        vanillaLand++;
                    }

                    float value = ComputeBaseHeight(wx, wy, 0f);
                    modified[index] = value;
                    fade[index] = GetStartFade(wx, wy);
                    peak[index] = GetPeakFactor(value);
                }
            }

            int targetLand = Mathf.RoundToInt(vanillaLand * OpenWatersSettings.LandFraction);
            float offset = 0f;
            if (targetLand < vanillaLand)
            {
                float low = 0f;
                float high = MaxOffset;
                for (int i = 0; i < 24; i++)
                {
                    float mid = (low + high) * 0.5f;
                    if (CountLand(modified, fade, peak, mid) > targetLand)
                    {
                        low = mid;
                    }
                    else
                    {
                        high = mid;
                    }
                }
                offset = (low + high) * 0.5f;
            }

            _heightOffset = offset;
            Active = true;

            int actualLand = CountLand(modified, fade, peak, offset);
            int actualMountains = CountMountains(modified, fade, peak, offset);
            long elapsedMs = (DateTime.UtcNow.Ticks - started) / TimeSpan.TicksPerMillisecond;
            OpenWatersPlugin.Log.LogInfo(
                $"[Gen] Noise frequency: preset={OpenWatersSettings.IslandAmountName}, scale={FrequencyScale:0.00}, " +
                $"detail={DetailDamping:0.00}, peak={PeakProtection:0.00}, vanillaLand={vanillaLand}, target={targetLand}, " +
                $"actual={actualLand}, mountains={actualMountains} ({(actualLand > 0 ? 100f * actualMountains / actualLand : 0f):0}% of land), " +
                $"offset={offset:0.000}, time={elapsedMs} ms.");
        }

        private static int CountLand(float[] modified, float[] fade, float[] peak, float offset)
        {
            int land = 0;
            for (int i = 0; i < modified.Length; i++)
            {
                if (modified[i] < 0f)
                {
                    continue;
                }
                float value = CompressMountains(modified[i] - offset * fade[i] * peak[i]);
                if (value > OceanThreshold)
                {
                    land++;
                }
            }
            return land;
        }

        private static int CountMountains(float[] modified, float[] fade, float[] peak, float offset)
        {
            int mountains = 0;
            for (int i = 0; i < modified.Length; i++)
            {
                if (modified[i] < 0f)
                {
                    continue;
                }
                float value = CompressMountains(modified[i] - offset * fade[i] * peak[i]);
                if (value > MountainThreshold)
                {
                    mountains++;
                }
            }
            return mountains;
        }

        public static float Apply(float wx, float wy)
        {
            return CompressMountains(ComputeBaseHeight(wx, wy, _heightOffset));
        }

        private static float ComputeBaseHeight(float wx, float wy, float offset)
        {
            float distance = Mathf.Sqrt(wx * wx + wy * wy);
            double nx = wx + 100000.0 + _offset0;
            double ny = wy + 100000.0 + _offset1;

            float value = 0f;
            value += Perlin(nx * 0.002 * 0.5 * FrequencyScale, ny * 0.002 * 0.5 * FrequencyScale)
                * Perlin(nx * 0.003 * 0.5 * FrequencyScale, ny * 0.003 * 0.5 * FrequencyScale);
            value += Perlin(nx * 0.002 * FrequencyScale, ny * 0.002 * FrequencyScale)
                * Perlin(nx * 0.003 * FrequencyScale, ny * 0.003 * FrequencyScale)
                * value * 0.9f;
            value += Perlin(nx * 0.005 * FrequencyScale, ny * 0.005 * FrequencyScale)
                * Perlin(nx * 0.01 * FrequencyScale, ny * 0.01 * FrequencyScale)
                * 0.5f
                * DetailDamping
                * value;
            value -= 0.07f;

            float firstBand = Perlin(nx * 0.002 * 0.25 * FrequencyScale + 0.123, ny * 0.002 * 0.25 * FrequencyScale + 0.15123);
            float secondBand = Perlin(nx * 0.002 * 0.25 * FrequencyScale + 0.321, ny * 0.002 * 0.25 * FrequencyScale + 0.231);
            float band = 1f - LerpStep(0.02f, 0.12f, Mathf.Abs(firstBand - secondBand));
            band *= SmoothStep(744f, 1000f, distance);
            value *= 1f - band;

            value -= offset * GetStartFade(wx, wy) * GetPeakFactor(value);

            if (distance > 10000f)
            {
                float t = LerpStep(10000f, 10500f, distance);
                value = Mathf.Lerp(value, -0.2f, t);
                if (distance > 10490f)
                {
                    float t2 = LerpStep(10490f, 10500f, distance);
                    value = Mathf.Lerp(value, -2f, t2);
                }
                return value;
            }

            if (distance < _minMountainDistance && value > 0.28f)
            {
                float t = Mathf.Clamp01((value - 0.28f) / 0.1f);
                value = Mathf.Lerp(
                    Mathf.Lerp(0.28f, 0.38f, t),
                    value,
                    LerpStep(_minMountainDistance - 400f, _minMountainDistance, distance));
            }

            return value;
        }

        private static float Perlin(double x, double y)
        {
            return Mathf.PerlinNoise((float)x, (float)y);
        }

        private static float GetStartFade(float wx, float wy)
        {
            float distance = Mathf.Sqrt(wx * wx + wy * wy);
            return SmoothStep(StartFadeInner, StartFadeOuter, distance);
        }

        private static float GetPeakFactor(float value)
        {
            return 1f - PeakProtection * SmoothStep(PeakProtectStart, PeakProtectEnd, value);
        }

        private static float CompressMountains(float value)
        {
            if (!IsFew || value <= FewMountainCompressionStart)
            {
                return value;
            }

            return FewMountainCompressionStart +
                   (value - FewMountainCompressionStart) * FewMountainCompressionFactor;
        }

        private static float SmoothStep(float from, float to, float value)
        {
            float t = Mathf.InverseLerp(from, to, value);
            return Mathf.SmoothStep(0f, 1f, t);
        }

        private static float LerpStep(float from, float to, float value)
        {
            return Mathf.Clamp01((value - from) / (to - from));
        }
    }
}
