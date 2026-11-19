namespace OpenWaters.Core.Tests;

public class StartIslandSelectorTests
{
    private const float CellSize = 20f;
    private const float Radius = 10500f;
    private const float SearchRadius = 500f;
    private const float SeamRadius = 120f;
    private const int MinCells = 100;

    private static IslandCandidate Candidate(
        int id, int cells, int minX, int maxX, int minY, int maxY) =>
        new IslandCandidate
        {
            Id = id,
            Cells = cells,
            MinX = minX,
            MaxX = maxX,
            MinY = minY,
            MaxY = maxY
        };

    private static StartIslandSelection Select(
        IslandCandidate[] islands, int anchorId) =>
        StartIslandSelector.Select(
            islands, anchorId, 0f, 0f, MinCells, CellSize, Radius, SearchRadius, SeamRadius);

    [Fact]
    public void LargeAnchorIslandWinsWithoutAbsorption()
    {
        IslandCandidate[] islands =
        {
            Candidate(0, 500, 510, 540, 510, 540),
            Candidate(1, 300, 600, 630, 510, 540)
        };

        StartIslandSelection result = Select(islands, 0);

        Assert.Equal(0, result.StartId);
        Assert.Empty(result.AbsorbedIds);
    }

    [Fact]
    public void SmallAnchorIsAbsorbedIntoAdjacentLargeIsland()
    {
        IslandCandidate[] islands =
        {
            Candidate(0, 9, 524, 526, 524, 526),
            Candidate(1, 500, 528, 560, 500, 550)
        };

        StartIslandSelection result = Select(islands, 0);

        Assert.Equal(1, result.StartId);
        Assert.Equal(new[] { 0 }, result.AbsorbedIds);
    }

    [Fact]
    public void DistantLargeIslandIsNotSelected()
    {
        IslandCandidate[] islands =
        {
            Candidate(0, 9, 524, 526, 524, 526),
            Candidate(1, 500, 600, 640, 500, 550)
        };

        StartIslandSelection result = Select(islands, 0);

        Assert.Equal(0, result.StartId);
        Assert.Empty(result.AbsorbedIds);
    }

    [Fact]
    public void AnchorInWaterFallsBackToNearestQualifyingIsland()
    {
        IslandCandidate[] islands =
        {
            Candidate(0, 500, 530, 560, 515, 545)
        };

        StartIslandSelection result = Select(islands, -1);

        Assert.Equal(0, result.StartId);
        Assert.Empty(result.AbsorbedIds);
    }
}
