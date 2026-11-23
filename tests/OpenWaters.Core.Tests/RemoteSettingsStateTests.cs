namespace OpenWaters.Core.Tests;

public class RemoteSettingsStateTests
{
    private static WorldSettings Sample(int generatorVersion = WorldSettings.GeneratorVersion) =>
        new WorldSettings(WorldSettings.MediumIslands, 8000, 0, 1, false, "0.1.2", generatorVersion: generatorVersion);

    [Fact]
    public void NoPeerInfo_ReturnsNotReceived()
    {
        RemoteSettingsState state = new RemoteSettingsState();

        Assert.Equal(RemoteSettingsStatus.NotReceived, state.Resolve("seed-a", out _));
    }

    [Fact]
    public void PeerInfoWithoutPayload_ReturnsNoSnapshot()
    {
        RemoteSettingsState state = new RemoteSettingsState();
        state.MarkPeerInfoReceived();

        Assert.Equal(RemoteSettingsStatus.NoSnapshot, state.Resolve("seed-a", out _));
    }

    [Fact]
    public void MatchingSeedAndVersion_ReturnsAcceptedSettings()
    {
        WorldSettings settings = Sample();
        RemoteSettingsState state = new RemoteSettingsState();
        state.Set(settings, "seed-a");

        Assert.Equal(RemoteSettingsStatus.Accepted, state.Resolve("seed-a", out WorldSettings resolved));
        Assert.Same(settings, resolved);
    }

    [Fact]
    public void DifferentSeed_ReturnsSeedMismatch()
    {
        RemoteSettingsState state = new RemoteSettingsState();
        state.Set(Sample(), "seed-a");

        Assert.Equal(RemoteSettingsStatus.SeedMismatch, state.Resolve("seed-b", out _));
    }

    [Fact]
    public void UnsupportedGeneratorVersion_ReturnsIncompatibleGenerator()
    {
        RemoteSettingsState state = new RemoteSettingsState();
        state.Set(Sample(generatorVersion: 2), "seed-a");

        Assert.Equal(
            RemoteSettingsStatus.IncompatibleGenerator,
            state.Resolve("seed-a", out WorldSettings resolved));
        Assert.Equal(2, resolved.GeneratorVersionValue);
    }

    [Fact]
    public void InvalidPayload_DiscardsPreviousSnapshot()
    {
        RemoteSettingsState state = new RemoteSettingsState();
        state.Set(Sample(), "seed-a");

        state.MarkPayloadInvalid();

        Assert.Equal(RemoteSettingsStatus.InvalidPayload, state.Resolve("seed-a", out _));
    }

    [Fact]
    public void NewPeerInfo_DiscardsSnapshotFromPreviousServer()
    {
        RemoteSettingsState state = new RemoteSettingsState();
        state.Set(Sample(), "seed-a");

        state.MarkPeerInfoReceived();

        Assert.Equal(RemoteSettingsStatus.NoSnapshot, state.Resolve("seed-a", out _));
    }

    [Fact]
    public void Clear_RestoresInitialState()
    {
        RemoteSettingsState state = new RemoteSettingsState();
        state.Set(Sample(), "seed-a");

        state.Clear();

        Assert.Equal(RemoteSettingsStatus.NotReceived, state.Resolve("seed-a", out _));
    }
}
