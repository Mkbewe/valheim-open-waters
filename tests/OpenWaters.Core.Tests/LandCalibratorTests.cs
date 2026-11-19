namespace OpenWaters.Core.Tests;

public class LandCalibratorTests
{
    private sealed class ConstantNoise : INoise
    {
        public float Perlin(double x, double y) => 0.5f;
    }

    private static NoiseField CreateField(float landFraction)
    {
        return new NoiseField(new ConstantNoise(), new NoiseFieldConfig
        {
            FrequencyScale = 1f,
            LandFraction = landFraction,
            DetailDamping = 1f,
            PeakProtection = 0f,
            MountainCompressionStart = 0f,
            MountainCompressionFactor = 1f
        });
    }

    [Fact]
    public void FullLandFraction_DoesNotLowerTerrain()
    {
        LandCalibrator calibrator = new LandCalibrator(3000f);
        calibrator.Run(CreateField(1f), (wx, wy) => true);
        Assert.Equal(0f, calibrator.Offset);
        Assert.Equal(calibrator.VanillaLand, calibrator.TargetLand);
        Assert.True(calibrator.ActualLand > 0);
    }

    [Fact]
    public void ReducedLandFraction_LowersTerrain()
    {
        LandCalibrator calibrator = new LandCalibrator(3000f);
        calibrator.Run(CreateField(0.5f), (wx, wy) => true);
        Assert.True(calibrator.Offset > 0f);
        Assert.True(calibrator.TargetLand < calibrator.VanillaLand);
        Assert.True(calibrator.ActualLand < calibrator.VanillaLand);
    }
}
