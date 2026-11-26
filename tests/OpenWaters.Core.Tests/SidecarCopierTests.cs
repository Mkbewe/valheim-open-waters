namespace OpenWaters.Core.Tests;

public class SidecarCopierTests
{
    private const string SourcePath = "worlds_local/Test/OpenWaters.worldSettings";
    private const string TargetPath = "worlds/Test/OpenWaters.worldSettings";

    private static WorldSettings Sample(string uid) =>
        new WorldSettings(WorldSettings.MediumIslands, 8000, 0, 1, false, "0.2.0", uid: uid);

    [Fact]
    public void TryCopy_CopiesSidecarWithMatchingUid()
    {
        FakeStorage source = new FakeStorage
        {
            [SourcePath] = WorldSettingsCodec.Serialize(Sample("uid-a"), "seed-a")
        };
        FakeStorage target = new FakeStorage();

        Assert.True(SidecarCopier.TryCopy(source, SourcePath, target, TargetPath, "uid-a"));
        Assert.True(target.ContainsKey(TargetPath));
        Assert.Equal(source[SourcePath], target[TargetPath]);
    }

    [Fact]
    public void TryCopy_RejectsWrongUid()
    {
        FakeStorage source = new FakeStorage
        {
            [SourcePath] = WorldSettingsCodec.Serialize(Sample("uid-a"), "seed-a")
        };
        FakeStorage target = new FakeStorage();

        Assert.False(SidecarCopier.TryCopy(source, SourcePath, target, TargetPath, "uid-b"));
        Assert.False(target.ContainsKey(TargetPath));
    }

    [Fact]
    public void TryCopy_DoesNotOverwriteExistingTarget()
    {
        string original = WorldSettingsCodec.Serialize(Sample("uid-old"), "seed-a");
        FakeStorage source = new FakeStorage
        {
            [SourcePath] = WorldSettingsCodec.Serialize(Sample("uid-a"), "seed-a")
        };
        FakeStorage target = new FakeStorage { [TargetPath] = original };

        Assert.False(SidecarCopier.TryCopy(source, SourcePath, target, TargetPath, "uid-a"));
        Assert.Equal(original, target[TargetPath]);
    }

    private sealed class FakeStorage : Dictionary<string, string>, ISidecarStorage
    {
        public bool Exists(string path) => ContainsKey(path);
        public string Read(string path) => this[path];
        public void Write(string path, string text) => this[path] = text;
        public void Delete(string path) => Remove(path);

        public void Move(string source, string destination)
        {
            this[destination] = this[source];
            Remove(source);
        }

        public string[] GetDirectories(string path) => Array.Empty<string>();
    }
}
