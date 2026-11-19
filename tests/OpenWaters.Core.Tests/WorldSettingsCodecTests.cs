namespace OpenWaters.Core.Tests;

public class WorldSettingsCodecTests
{
    private static WorldSettings Sample() =>
        new WorldSettings(WorldSettings.MediumIslands, 8000, 2, 1, true);

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        WorldSettings original = Sample();
        string text = WorldSettingsCodec.Serialize(original, "my-seed");

        Assert.True(WorldSettingsCodec.TryParse(text, "my-seed", out WorldSettings parsed));
        Assert.Equal(original.IslandAmount, parsed.IslandAmount);
        Assert.Equal(original.WorldRadius, parsed.WorldRadius);
        Assert.Equal(original.BiomeLimit, parsed.BiomeLimit);
        Assert.Equal(original.StartBiomeLimit, parsed.StartBiomeLimit);
        Assert.Equal(original.ExcludeMountains, parsed.ExcludeMountains);
    }

    [Fact]
    public void WrongSeed_IsRejected()
    {
        string text = WorldSettingsCodec.Serialize(Sample(), "seed-a");
        Assert.False(WorldSettingsCodec.TryParse(text, "seed-b", out _));
    }

    [Fact]
    public void WrongVersion_IsRejected()
    {
        string text = WorldSettingsCodec.Serialize(Sample(), "seed-a")
            .Replace("version=1", "version=2");
        Assert.False(WorldSettingsCodec.TryParse(text, "seed-a", out _));
    }

    [Fact]
    public void OutOfRangeRadius_IsRejected()
    {
        WorldSettings invalid = new WorldSettings(WorldSettings.FewIslands, 100, 0, 0, false);
        string text = WorldSettingsCodec.Serialize(invalid, "seed-a");
        Assert.False(WorldSettingsCodec.TryParse(text, "seed-a", out _));
    }

    [Fact]
    public void VanillaSettings_DoNotAffectGeneration()
    {
        Assert.False(WorldSettings.Vanilla.AffectsGeneration);
        Assert.True(Sample().AffectsGeneration);
    }
}
