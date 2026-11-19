using System;

namespace OpenWaters
{
    internal sealed class LandCalibrator
    {
        private const int Samples = 512;
        private const float EdgeSize = 500f;
        private const float OceanThreshold = 0.02f;
        private const float MountainThreshold = 0.4f;
        private const float MaxOffset = 0.6f;
        private const int SearchIterations = 24;

        private readonly float _radius;

        public float[] Modified { get; private set; }
        public float[] Fade { get; private set; }
        public float[] Peak { get; private set; }
        public int VanillaLand { get; private set; }
        public int TargetLand { get; private set; }
        public float Offset { get; private set; }
        public int ActualLand { get; private set; }
        public int ActualMountains { get; private set; }

        public LandCalibrator(float radius)
        {
            _radius = radius + EdgeSize;
        }

        public void Run(NoiseField field, Func<float, float, bool> isVanillaLand)
        {
            int size = Samples;
            float cellSize = 2f * _radius / size;
            Modified = new float[size * size];
            Fade = new float[size * size];
            Peak = new float[size * size];
            VanillaLand = 0;

            for (int y = 0; y < size; y++)
            {
                float wy = -_radius + (y + 0.5f) * cellSize;
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    float wx = -_radius + (x + 0.5f) * cellSize;
                    if (wx * wx + wy * wy > _radius * _radius)
                    {
                        Modified[index] = -1f;
                        continue;
                    }
                    if (isVanillaLand(wx, wy))
                    {
                        VanillaLand++;
                    }
                    float value = field.BaseHeight(wx, wy, 0f);
                    Modified[index] = value;
                    Fade[index] = field.Fade(wx, wy);
                    Peak[index] = field.PeakFactor(value);
                }
            }

            TargetLand = (int)Math.Round(VanillaLand * (double)field.LandFraction);
            Offset = 0f;
            if (TargetLand < VanillaLand)
            {
                float low = 0f;
                float high = MaxOffset;
                for (int i = 0; i < SearchIterations; i++)
                {
                    float mid = (low + high) * 0.5f;
                    if (Count(field, mid, OceanThreshold) > TargetLand)
                    {
                        low = mid;
                    }
                    else
                    {
                        high = mid;
                    }
                }
                Offset = (low + high) * 0.5f;
            }

            ActualLand = Count(field, Offset, OceanThreshold);
            ActualMountains = Count(field, Offset, MountainThreshold);
        }

        private int Count(NoiseField field, float offset, float threshold)
        {
            int count = 0;
            for (int i = 0; i < Modified.Length; i++)
            {
                if (Modified[i] < 0f)
                {
                    continue;
                }
                if (field.Compress(Modified[i] - offset * Fade[i] * Peak[i]) > threshold)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
