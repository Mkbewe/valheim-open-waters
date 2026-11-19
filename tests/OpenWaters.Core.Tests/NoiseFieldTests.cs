namespace OpenWaters.Core.Tests;

public class NoiseFieldTests
{
    private sealed class ConstantNoise : INoise
    {
        public float Perlin(double x, double y) => 0.5f;
    }

    private static NoiseField CreateField()
    {
        return new NoiseField(new ConstantNoise(), new NoiseFieldConfig
        {
            FrequencyScale = 1f,
            LandFraction = 1f,
            DetailDamping = 1f,
            PeakProtection = 0f,
            MountainCompressionStart = 0f,
            MountainCompressionFactor = 1f
        });
    }

    [Fact]
    public void BaseHeight_IsDeterministic()
    {
        NoiseField field = CreateField();
        float first = field.BaseHeight(120f, -340f, 0.05f);
        float second = field.BaseHeight(120f, -340f, 0.05f);
        Assert.Equal(first, second);
    }

    [Fact]
    public void BaseHeight_OffsetLowersTerrainNearCenter()
    {
        NoiseField field = CreateField();
        float baseValue = field.BaseHeight(3000f, 0f, 0f);
        float lowered = field.BaseHeight(3000f, 0f, 0.1f);
        Assert.True(lowered < baseValue);
    }

    [Fact]
    public void Compress_IsIdentityWhenFactorIsOne()
    {
        NoiseField field = CreateField();
        Assert.Equal(0.5f, field.Compress(0.5f));
    }
}
