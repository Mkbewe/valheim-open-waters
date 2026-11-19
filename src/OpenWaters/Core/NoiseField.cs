using System;

namespace OpenWaters
{
    internal interface INoise
    {
        float Perlin(double x, double y);
    }

    internal sealed class NoiseFieldConfig
    {
        public float FrequencyScale = 1f;
        public float LandFraction = 1f;
        public float DetailDamping = 1f;
        public float PeakProtection;
        public float MountainCompressionStart;
        public float MountainCompressionFactor = 1f;
        public float Offset0;
        public float Offset1;
        public float MinMountainDistance = 1000f;
    }

    internal static class MathUtil
    {
        public static float Clamp01(float value)
        {
            return value < 0f ? 0f : value > 1f ? 1f : value;
        }

        public static float Lerp(float from, float to, float t)
        {
            return from + (to - from) * Clamp01(t);
        }

        public static float LerpStep(float from, float to, float value)
        {
            return Clamp01((value - from) / (to - from));
        }

        public static float SmoothStep(float from, float to, float value)
        {
            float t = Clamp01((value - from) / (to - from));
            return t * t * (3f - 2f * t);
        }

        public static float Sqrt(float value)
        {
            return (float)Math.Sqrt(value);
        }
    }

    internal sealed class NoiseField
    {
        private const float OceanEdgeStart = 10000f;
        private const float OceanEdgeEnd = 10500f;
        private const float OceanEdgeInner = 10490f;
        private const float StartFadeInner = 800f;
        private const float StartFadeOuter = 1600f;
        private const float PeakProtectStart = 0.28f;
        private const float PeakProtectEnd = 0.45f;
        private const float MountainProtectStart = 0.28f;
        private const float MountainProtectSpan = 0.1f;
        private const float MountainProtectCap = 0.38f;

        private readonly INoise _noise;
        private readonly NoiseFieldConfig _config;

        public NoiseField(INoise noise, NoiseFieldConfig config)
        {
            _noise = noise;
            _config = config;
        }

        public float LandFraction => _config.LandFraction;
        public float FrequencyScale => _config.FrequencyScale;
        public float DetailDamping => _config.DetailDamping;
        public float PeakProtection => _config.PeakProtection;

        public float Fade(float wx, float wy)
        {
            return MathUtil.SmoothStep(
                StartFadeInner, StartFadeOuter, MathUtil.Sqrt(wx * wx + wy * wy));
        }

        public float PeakFactor(float value)
        {
            return 1f - _config.PeakProtection *
                MathUtil.SmoothStep(PeakProtectStart, PeakProtectEnd, value);
        }

        public float Compress(float value)
        {
            if (value <= _config.MountainCompressionStart)
            {
                return value;
            }
            return _config.MountainCompressionStart +
                (value - _config.MountainCompressionStart) * _config.MountainCompressionFactor;
        }

        public float BaseHeight(float wx, float wy, float offset)
        {
            float frequency = _config.FrequencyScale;
            float distance = MathUtil.Sqrt(wx * wx + wy * wy);
            double nx = wx + 100000.0 + _config.Offset0;
            double ny = wy + 100000.0 + _config.Offset1;

            float value = 0f;
            value += _noise.Perlin(nx * 0.002 * 0.5 * frequency, ny * 0.002 * 0.5 * frequency)
                * _noise.Perlin(nx * 0.003 * 0.5 * frequency, ny * 0.003 * 0.5 * frequency);
            value += _noise.Perlin(nx * 0.002 * frequency, ny * 0.002 * frequency)
                * _noise.Perlin(nx * 0.003 * frequency, ny * 0.003 * frequency)
                * value * 0.9f;
            value += _noise.Perlin(nx * 0.005 * frequency, ny * 0.005 * frequency)
                * _noise.Perlin(nx * 0.01 * frequency, ny * 0.01 * frequency)
                * 0.5f * _config.DetailDamping * value;
            value -= 0.07f;

            float firstBand = _noise.Perlin(
                nx * 0.002 * 0.25 * frequency + 0.123,
                ny * 0.002 * 0.25 * frequency + 0.15123);
            float secondBand = _noise.Perlin(
                nx * 0.002 * 0.25 * frequency + 0.321,
                ny * 0.002 * 0.25 * frequency + 0.231);
            float band = 1f - MathUtil.LerpStep(
                0.02f, 0.12f, Math.Abs(firstBand - secondBand));
            band *= MathUtil.SmoothStep(744f, 1000f, distance);
            value *= 1f - band;

            value -= offset * Fade(wx, wy) * PeakFactor(value);

            if (distance > OceanEdgeStart)
            {
                float t = MathUtil.LerpStep(OceanEdgeStart, OceanEdgeEnd, distance);
                value = MathUtil.Lerp(value, -0.2f, t);
                if (distance > OceanEdgeInner)
                {
                    float t2 = MathUtil.LerpStep(OceanEdgeInner, OceanEdgeEnd, distance);
                    value = MathUtil.Lerp(value, -2f, t2);
                }
                return value;
            }

            if (distance < _config.MinMountainDistance && value > MountainProtectStart)
            {
                float t = MathUtil.Clamp01(
                    (value - MountainProtectStart) / MountainProtectSpan);
                value = MathUtil.Lerp(
                    MathUtil.Lerp(MountainProtectStart, MountainProtectCap, t),
                    value,
                    MathUtil.LerpStep(
                        _config.MinMountainDistance - 400f,
                        _config.MinMountainDistance,
                        distance));
            }

            return value;
        }
    }
}
