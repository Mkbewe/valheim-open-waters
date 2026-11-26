namespace OpenWaters.Core.Tests;

public class WorldSettingsCodecTests
{
    private static WorldSettings Sample(string uid = null) =>
        new WorldSettings(WorldSettings.MediumIslands, 8000, 2, 1, true, uid: uid);

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        WorldSettings original = Sample();
        string text = WorldSettingsCodec.Serialize(original, "my-seed");

        Assert.True(WorldSettingsCodec.TryParse(text, out WorldSettings parsed));
        Assert.Equal(original.IslandAmount, parsed.IslandAmount);
        Assert.Equal(original.WorldRadius, parsed.WorldRadius);
        Assert.Equal(original.BiomeLimit, parsed.BiomeLimit);
        Assert.Equal(original.StartBiomeLimit, parsed.StartBiomeLimit);
        Assert.Equal(original.ExcludeMountains, parsed.ExcludeMountains);
    }

    [Fact]
    public void Uid_RoundTrips()
    {
        string text = WorldSettingsCodec.Serialize(Sample("abc123"), "my-seed");

        Assert.True(WorldSettingsCodec.TryParseByUid(text, "abc123", out WorldSettings parsed));
        Assert.Equal("abc123", parsed.Uid);
    }

    [Fact]
    public void WrongVersion_IsRejected()
    {
        string text = WorldSettingsCodec.Serialize(Sample(), "seed-a")
            .Replace("version=1", "version=2");
        Assert.False(WorldSettingsCodec.TryParse(text, out _));
    }

    [Fact]
    public void OutOfRangeRadius_IsRejected()
    {
        WorldSettings invalid = new WorldSettings(WorldSettings.FewIslands, 100, 0, 0, false);
        string text = WorldSettingsCodec.Serialize(invalid, "seed-a");
        Assert.False(WorldSettingsCodec.TryParse(text, out _));
    }

    [Fact]
    public void VanillaSettings_DoNotAffectGeneration()
    {
        Assert.False(WorldSettings.Vanilla.AffectsGeneration);
        Assert.True(Sample().AffectsGeneration);
    }

    [Fact]
    public void SeedWithSpecialCharacters_RoundTrips()
    {
        const string seed = "ab=cd/xyz+1";
        string text = WorldSettingsCodec.Serialize(Sample(), seed);
        Assert.True(WorldSettingsCodec.TryParse(text, out _));
    }

    [Fact]
    public void EmptySeed_RoundTrips()
    {
        string text = WorldSettingsCodec.Serialize(Sample(), string.Empty);
        Assert.True(WorldSettingsCodec.TryParse(text, out _));
    }

    [Fact]
    public void EffectiveTuningValues_RoundTrip()
    {
        WorldSettings original = new WorldSettings(
            WorldSettings.FewIslands, 9000, 0, 2, true, "0.4.0",
            0.31f, 0.55f, 0.12f, 0.44f, 0.30f, 0.75f,
            0.62f, 0.81f, 0.71f, 0.22f);
        string text = WorldSettingsCodec.Serialize(original, "seed-a");

        Assert.True(WorldSettingsCodec.TryParse(text, out WorldSettings parsed));
        Assert.Equal(0.31f, parsed.FewLandFraction);
        Assert.Equal(0.55f, parsed.FewFrequencyScale);
        Assert.Equal(0.12f, parsed.FewPeakProtection);
        Assert.Equal(0.44f, parsed.FewDetailDamping);
        Assert.Equal(0.30f, parsed.FewMountainCompressionStart);
        Assert.Equal(0.75f, parsed.FewMountainCompressionFactor);
        Assert.Equal(0.62f, parsed.MediumLandFraction);
        Assert.Equal(0.81f, parsed.MediumFrequencyScale);
        Assert.Equal(0.71f, parsed.MediumPeakProtection);
        Assert.Equal(0.22f, parsed.MediumDetailDamping);
        Assert.Equal("0.4.0", parsed.ModVersion);
    }

    [Fact]
    public void MissingField_IsRejected()
    {
        string text = WorldSettingsCodec.Serialize(Sample(), "seed-a")
            .Replace("mediumDetailDamping=0.35\n", string.Empty);
        Assert.False(WorldSettingsCodec.TryParse(text, out _));
    }

    [Fact]
    public void InvalidTuningValues_AreRejected()
    {
        WorldSettings invalid = new WorldSettings(
            WorldSettings.FewIslands, 10000, 0, 0, false, "0.1.0",
            float.NaN, 0.6f, 0.2f, 0.35f, 0.34f, 0.55f,
            0.5f, 0.7f, 0.6f, 0.35f);
        string text = WorldSettingsCodec.Serialize(invalid, "seed-a");
        Assert.False(WorldSettingsCodec.TryParse(text, out _));

        WorldSettings outOfRange = new WorldSettings(
            WorldSettings.FewIslands, 10000, 0, 0, false, "0.1.0",
            5f, 0.6f, 0.2f, 0.35f, 0.34f, 0.55f,
            0.5f, 0.7f, 0.6f, 0.35f);
        text = WorldSettingsCodec.Serialize(outOfRange, "seed-a");
        Assert.False(WorldSettingsCodec.TryParse(text, out _));
    }

    [Fact]
    public void GeneratorVersionControlsCompatibility()
    {
        string text = WorldSettingsCodec.Serialize(Sample(), "seed-a")
            .Replace("generatorVersion=1", "generatorVersion=999");
        Assert.True(WorldSettingsCodec.TryParse(text, out WorldSettings parsed));
        Assert.False(parsed.IsGeneratorCompatible);
        Assert.True(Sample().IsGeneratorCompatible);
    }

    [Fact]
    public void TryParseByUid_MatchesRegardlessOfSeed()
    {
        string text = WorldSettingsCodec.Serialize(Sample("uid-a"), "seed-a");

        Assert.True(WorldSettingsCodec.TryParseByUid(text, "UID-A", out WorldSettings parsed));
        Assert.Equal("uid-a", parsed.Uid);
    }

    [Fact]
    public void TryParseByUid_RejectsWrongUid()
    {
        string text = WorldSettingsCodec.Serialize(Sample("uid-a"), "seed-a");

        Assert.False(WorldSettingsCodec.TryParseByUid(text, "uid-b", out _));
    }

    [Fact]
    public void FindByUid_SelectsMatchingSidecarWithSameSeed()
    {
        string first = WorldSettingsCodec.Serialize(Sample("uid-a"), "same-seed");
        string second = WorldSettingsCodec.Serialize(Sample("uid-b"), "same-seed");

        string found = SidecarLocator.FindByUid(
            new[] { "dir-a", "dir-b" },
            "uid-b",
            path => path.EndsWith("dir-a/OpenWaters.worldSettings") ? first : second,
            directory => directory + "/OpenWaters.worldSettings");

        Assert.Equal("dir-b/OpenWaters.worldSettings", found);
    }

    [Fact]
    public void PayloadSerialization_DoesNotIncludeUid()
    {
        string text = WorldSettingsCodec.Serialize(Sample("uid-a"), "seed-a", includeUid: false);

        Assert.DoesNotContain("uid=", text);
    }

    [Fact]
    public void TryParseByUid_WorksWithEmptySeed()
    {
        string text = WorldSettingsCodec.Serialize(Sample("uid-a"), string.Empty);

        Assert.True(WorldSettingsCodec.TryParseByUid(text, "uid-a", out WorldSettings parsed));
        Assert.Equal("uid-a", parsed.Uid);
    }

    [Fact]
    public void TryParseByUid_RejectsSidecarWithoutUid()
    {
        string text = WorldSettingsCodec.Serialize(Sample(), "seed-a");

        Assert.False(WorldSettingsCodec.TryParseByUid(text, "uid-a", out _));
    }
}
