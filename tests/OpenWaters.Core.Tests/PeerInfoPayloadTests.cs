namespace OpenWaters.Core.Tests;

public class PeerInfoPayloadTests
{
    private const string Marker = "OWS1:";

    [Fact]
    public void NoExtension_WhenOnlyVanillaTimeFieldRemains()
    {
        byte[] bytes = new byte[PeerInfoProtocol.VanillaTrailingBytes];

        PeerInfoPayloadKind kind = PeerInfoPayload.Read(bytes, 0, bytes.Length, Marker, out _, out int consumed);

        Assert.Equal(PeerInfoPayloadKind.NoExtension, kind);
        Assert.Equal(0, consumed);
    }

    [Fact]
    public void OpenWaters_ReadsStringAndReportsConsumedBytes()
    {
        byte[] encoded = PeerInfoPayload.EncodeString(Marker + "seed=abc");
        byte[] bytes = WithVanillaTail(encoded);

        PeerInfoPayloadKind kind = PeerInfoPayload.Read(bytes, 0, bytes.Length, Marker, out string text, out int consumed);

        Assert.Equal(PeerInfoPayloadKind.OpenWaters, kind);
        Assert.Equal(Marker + "seed=abc", text);
        Assert.Equal(encoded.Length, consumed);
    }

    [Fact]
    public void OtherMod_IsClassifiedWithoutRejection()
    {
        byte[] bytes = WithVanillaTail(PeerInfoPayload.EncodeString("OTHERMOD:payload"));

        PeerInfoPayloadKind kind = PeerInfoPayload.Read(bytes, 0, bytes.Length, Marker, out _, out _);

        Assert.Equal(PeerInfoPayloadKind.OtherMod, kind);
    }

    [Fact]
    public void UnsupportedVersion_IsClassifiedForRejection()
    {
        byte[] bytes = WithVanillaTail(PeerInfoPayload.EncodeString("OWS2:payload"));

        PeerInfoPayloadKind kind = PeerInfoPayload.Read(bytes, 0, bytes.Length, Marker, out _, out _);

        Assert.Equal(PeerInfoPayloadKind.UnsupportedVersion, kind);
    }

    [Fact]
    public void EmptyString_IsClassifiedAsEmptyExtension()
    {
        byte[] bytes = WithVanillaTail(PeerInfoPayload.EncodeString(string.Empty));

        PeerInfoPayloadKind kind = PeerInfoPayload.Read(bytes, 0, bytes.Length, Marker, out _, out _);

        Assert.Equal(PeerInfoPayloadKind.EmptyExtension, kind);
    }

    [Fact]
    public void TruncatedString_IsMalformed()
    {
        byte[] bytes = new byte[PeerInfoProtocol.VanillaTrailingBytes + 2];
        bytes[0] = 0x10;
        bytes[1] = (byte)'A';

        PeerInfoPayloadKind kind = PeerInfoPayload.Read(bytes, 0, bytes.Length, Marker, out _, out int consumed);

        Assert.Equal(PeerInfoPayloadKind.Malformed, kind);
        Assert.Equal(0, consumed);
    }

    [Fact]
    public void InvalidBounds_AreMalformed()
    {
        Assert.Equal(
            PeerInfoPayloadKind.Malformed,
            PeerInfoPayload.Read(null, 0, 8, Marker, out _, out _));
        Assert.Equal(
            PeerInfoPayloadKind.Malformed,
            PeerInfoPayload.Read(new byte[8], 4, 2, Marker, out _, out _));
        Assert.Equal(
            PeerInfoPayloadKind.Malformed,
            PeerInfoPayload.Read(new byte[8], 0, 16, Marker, out _, out _));
    }

    [Fact]
    public void LongString_UsesMultiByteLengthPrefix()
    {
        string value = Marker + new string('x', 300);

        byte[] encoded = PeerInfoPayload.EncodeString(value);

        PeerInfoPayloadKind kind = PeerInfoPayload.Read(
            encoded, 0, encoded.Length, Marker, out string text, out int consumed);
        Assert.Equal(PeerInfoPayloadKind.OpenWaters, kind);
        Assert.Equal(value, text);
        Assert.Equal(encoded.Length, consumed);
    }

    [Fact]
    public void Utf8Seed_RoundTrips()
    {
        string value = Marker + "seed=żółw";

        byte[] encoded = PeerInfoPayload.EncodeString(value);

        PeerInfoPayloadKind kind = PeerInfoPayload.Read(
            encoded, 0, encoded.Length, Marker, out string text, out _);
        Assert.Equal(PeerInfoPayloadKind.OpenWaters, kind);
        Assert.Equal(value, text);
    }

    private static byte[] WithVanillaTail(byte[] extension)
    {
        byte[] tail = new byte[PeerInfoProtocol.VanillaTrailingBytes];
        byte[] result = new byte[extension.Length + tail.Length];
        Buffer.BlockCopy(extension, 0, result, 0, extension.Length);
        Buffer.BlockCopy(tail, 0, result, extension.Length, tail.Length);
        return result;
    }
}
