using System.Text;

namespace OpenWaters.Core.Tests;

public class PeerInfoProtocolTests
{
    private static readonly byte[] Marker = Encoding.UTF8.GetBytes("OWC1");

    [Theory]
    [InlineData(100, 92, false)]
    [InlineData(100, 91, true)]
    [InlineData(100, 100, false)]
    [InlineData(100, 105, false)]
    public void ShouldReadServerExtension_LeavesTheVanillaTimeFieldAlone(int size, int position, bool expected)
    {
        Assert.Equal(expected, PeerInfoProtocol.ShouldReadServerExtension(size, position));
    }

    [Fact]
    public void EndsWith_FindsSuffixAtEnd()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("peer-info-dataOWC1");
        Assert.True(PeerInfoProtocol.EndsWith(bytes, bytes.Length, Marker));
    }

    [Fact]
    public void EndsWith_IgnoresBytesBeyondLength()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("peer-info-dataOWC1garbage");
        Assert.True(PeerInfoProtocol.EndsWith(bytes, 18, Marker));
        Assert.False(PeerInfoProtocol.EndsWith(bytes, bytes.Length, Marker));
    }

    [Fact]
    public void EndsWith_RequiresFullSuffix()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("peer-info-dataOW");
        Assert.False(PeerInfoProtocol.EndsWith(bytes, bytes.Length, Marker));
    }

    [Fact]
    public void EndsWith_ReturnsFalseForInvalidInput()
    {
        Assert.False(PeerInfoProtocol.EndsWith(null, 10, Marker));
        Assert.False(PeerInfoProtocol.EndsWith(Marker, 10, null));
        Assert.False(PeerInfoProtocol.EndsWith(Marker, 10, Array.Empty<byte>()));
        Assert.False(PeerInfoProtocol.EndsWith(Encoding.UTF8.GetBytes("abc"), 3, Marker));
        Assert.False(PeerInfoProtocol.EndsWith(Marker, Marker.Length + 1, Marker));
    }
}
