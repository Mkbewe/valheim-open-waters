namespace OpenWaters.Core.Tests;

public class SidecarLocatorTests
{
    private static readonly string[] Directories = { "worlds/one", "worlds/two" };

    private static string ToPath(string directory) => directory + "/OpenWaters.worldSettings";

    [Fact]
    public void FindsSidecarBySeedWhenWorldNameIsMissing()
    {
        string sidecar = WorldSettingsCodec.Serialize(
            new WorldSettings(WorldSettings.FewIslands, 10000, 0, 1, true), "seed-a");

        string found = SidecarLocator.FindBySeed(
            Directories,
            "seed-a",
            path => path == ToPath("worlds/two") ? sidecar : null,
            ToPath);

        Assert.Equal(ToPath("worlds/two"), found);
    }

    [Fact]
    public void WrongSeedOrMissingSidecar_ReturnsNull()
    {
        string sidecar = WorldSettingsCodec.Serialize(
            new WorldSettings(WorldSettings.FewIslands, 10000, 0, 1, true), "seed-a");

        Assert.Null(SidecarLocator.FindBySeed(
            Directories, "seed-b", path => sidecar, ToPath));
        Assert.Null(SidecarLocator.FindBySeed(
            Directories, "seed-a", path => null, ToPath));
    }

    [Fact]
    public void UnreadableDirectory_DoesNotStopSearch()
    {
        string sidecar = WorldSettingsCodec.Serialize(
            new WorldSettings(WorldSettings.FewIslands, 10000, 0, 1, true), "seed-a");

        string found = SidecarLocator.FindBySeed(
            Directories,
            "seed-a",
            path =>
            {
                if (path == ToPath("worlds/one"))
                {
                    throw new System.IO.IOException("locked");
                }
                return sidecar;
            },
            ToPath);

        Assert.Equal(ToPath("worlds/two"), found);
    }
}
