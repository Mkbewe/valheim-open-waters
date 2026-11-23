using System;

namespace OpenWaters
{
    internal static class SelfTest
    {
        public static void Run(Action<string> log)
        {
            int passed = 0;
            int failed = 0;
            log("[SelfTest] Running built-in checks...");

            Check(log, "no extension stays vanilla", ref passed, ref failed, () =>
                PeerInfoPayload.Read(new byte[PeerInfoProtocol.VanillaTrailingBytes], 0,
                    PeerInfoProtocol.VanillaTrailingBytes, "OWS1:", out _, out _) ==
                PeerInfoPayloadKind.NoExtension);

            Check(log, "valid OWS1 payload decodes and parses", ref passed, ref failed, () =>
            {
                WorldSettings settings = Sample();
                byte[] package = Package("OWS1:" + WorldSettingsCodec.Serialize(settings, "seed-a"));
                PeerInfoPayloadKind kind = PeerInfoPayload.Read(
                    package, 0, package.Length, "OWS1:", out string text, out _);
                return kind == PeerInfoPayloadKind.OpenWaters &&
                       WorldSettingsCodec.TryParse(text.Substring("OWS1:".Length), "seed-a", out _);
            });

            Check(log, "another mod's extension is ignored", ref passed, ref failed, () =>
            {
                byte[] package = Package("OTHERMOD:data");
                return PeerInfoPayload.Read(package, 0, package.Length, "OWS1:", out _, out _) ==
                       PeerInfoPayloadKind.OtherMod;
            });

            Check(log, "unsupported OWS version is detected", ref passed, ref failed, () =>
            {
                byte[] package = Package("OWS2:data");
                return PeerInfoPayload.Read(package, 0, package.Length, "OWS1:", out _, out _) ==
                       PeerInfoPayloadKind.UnsupportedVersion;
            });

            Check(log, "wrong seed invalidates the payload", ref passed, ref failed, () =>
                !WorldSettingsCodec.TryParse(
                    WorldSettingsCodec.Serialize(Sample(), "seed-a"), "seed-b", out _));

            Check(log, "unsupported generator version is detected", ref passed, ref failed, () =>
            {
                RemoteSettingsState state = new RemoteSettingsState();
                state.Set(Sample(generatorVersion: 2), "seed-a");
                return state.Resolve("seed-a", out _) == RemoteSettingsStatus.IncompatibleGenerator;
            });

            Check(log, "seed mismatch is detected", ref passed, ref failed, () =>
            {
                RemoteSettingsState state = new RemoteSettingsState();
                state.Set(Sample(), "seed-a");
                return state.Resolve("seed-b", out _) == RemoteSettingsStatus.SeedMismatch;
            });

            Check(log, "new peer info clears the previous snapshot", ref passed, ref failed, () =>
            {
                RemoteSettingsState state = new RemoteSettingsState();
                state.Set(Sample(), "seed-a");
                state.MarkPeerInfoReceived();
                return state.Resolve("seed-a", out _) == RemoteSettingsStatus.NoSnapshot;
            });

            log($"[SelfTest] Done: {passed} passed, {failed} failed.");
        }

        private static WorldSettings Sample(int generatorVersion = WorldSettings.GeneratorVersion) =>
            new WorldSettings(WorldSettings.MediumIslands, 8000, 0, 1, false, "0.1.2",
                generatorVersion: generatorVersion);

        private static byte[] Package(string text)
        {
            byte[] extension = PeerInfoPayload.EncodeString(text);
            byte[] result = new byte[extension.Length + PeerInfoProtocol.VanillaTrailingBytes];
            Buffer.BlockCopy(extension, 0, result, 0, extension.Length);
            return result;
        }

        private static void Check(
            Action<string> log, string name, ref int passed, ref int failed, Func<bool> test)
        {
            bool ok;
            try
            {
                ok = test();
            }
            catch (Exception exception)
            {
                ok = false;
                log($"[SelfTest] {name} threw: {exception.Message}");
            }
            if (ok)
            {
                passed++;
                log($"[SelfTest] PASS: {name}");
            }
            else
            {
                failed++;
                log($"[SelfTest] FAIL: {name}");
            }
        }
    }
}
