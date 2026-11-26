namespace OpenWaters.Core.Tests;

public class SidecarLocatorTests
{
    private static readonly string[] Directories = { "worlds/one", "worlds/two" };

    private static string ToPath(string directory) => directory + "/OpenWaters.worldSettings";

    private static WorldSettings Sample(string uid) =>
        new WorldSettings(WorldSettings.FewIslands, 10000, 0, 1, true, uid: uid);

    [Fact]
    public void FindsSidecarByUidWhenWorldNameIsMissing()
    {
        string sidecar = WorldSettingsCodec.Serialize(Sample("uid-a"), "seed-a");

        string found = SidecarLocator.FindByUid(
            Directories,
            "uid-a",
            path => path == ToPath("worlds/two") ? sidecar : null,
            ToPath);

        Assert.Equal(ToPath("worlds/two"), found);
    }

    [Fact]
    public void SameSeedDifferentUids_SelectsMatchingSidecar()
    {
        string first = WorldSettingsCodec.Serialize(Sample("uid-a"), "same-seed");
        string second = WorldSettingsCodec.Serialize(Sample("uid-b"), "same-seed");

        string found = SidecarLocator.FindByUid(
            Directories,
            "uid-b",
            path => path == ToPath("worlds/one") ? first : second,
            ToPath);

        Assert.Equal(ToPath("worlds/two"), found);
    }

    [Fact]
    public void WrongUidOrMissingSidecar_ReturnsNull()
    {
        string sidecar = WorldSettingsCodec.Serialize(Sample("uid-a"), "seed-a");

        Assert.Null(SidecarLocator.FindByUid(
            Directories, "uid-b", path => sidecar, ToPath));
        Assert.Null(SidecarLocator.FindByUid(
            Directories, "uid-a", path => null, ToPath));
    }

    [Fact]
    public void UnreadableDirectory_DoesNotStopSearch()
    {
        string sidecar = WorldSettingsCodec.Serialize(Sample("uid-a"), "seed-a");

        string found = SidecarLocator.FindByUid(
            Directories,
            "uid-a",
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
