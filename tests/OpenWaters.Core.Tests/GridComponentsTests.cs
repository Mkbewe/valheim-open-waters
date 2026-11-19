namespace OpenWaters.Tests;

public sealed class GridComponentsTests
{
    [Fact]
    public void EmptyGridHasNoComponents()
    {
        bool[] land = new bool[16];

        int[] labels = GridComponents.Label(land, 4, out int count);

        Assert.Equal(0, count);
        Assert.All(labels, label => Assert.Equal(-1, label));
    }

    [Fact]
    public void DiagonalCellsAreSeparateWithFourConnectivity()
    {
        bool[] land =
        {
            true, false, false,
            false, true, false,
            false, false, false
        };

        int[] labels = GridComponents.Label(land, 3, out int count);

        Assert.Equal(2, count);
        Assert.NotEqual(labels[0], labels[4]);
    }

    [Fact]
    public void AdjacentCellsShareOneLabel()
    {
        bool[] land =
        {
            false, true, false,
            true, true, false,
            false, false, false
        };

        int[] labels = GridComponents.Label(land, 3, out int count);

        Assert.Equal(1, count);
        Assert.Equal(labels[1], labels[3]);
        Assert.Equal(labels[1], labels[4]);
        Assert.Equal(-1, labels[0]);
    }

    [Fact]
    public void SeparateIslandsGetDistinctLabels()
    {
        bool[] land =
        {
            true, false, false, true,
            true, false, false, true,
            false, false, false, false
        };

        int[] labels = GridComponents.Label(land, 4, out int count);

        Assert.Equal(2, count);
        Assert.Equal(labels[0], labels[4]);
        Assert.Equal(labels[3], labels[7]);
        Assert.NotEqual(labels[0], labels[3]);
    }
}
