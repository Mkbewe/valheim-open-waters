using System;
using System.Collections.Generic;

namespace OpenWaters
{
    internal struct IslandCandidate
    {
        public int Id;
        public int Cells;
        public int MinX;
        public int MaxX;
        public int MinY;
        public int MaxY;
    }

    internal sealed class StartIslandSelection
    {
        public int StartId = -1;
        public readonly List<int> AbsorbedIds = new List<int>();
    }

    internal static class StartIslandSelector
    {
        public static StartIslandSelection Select(
            IslandCandidate[] islands,
            int anchorId,
            float anchorX,
            float anchorY,
            int minCells,
            float cellSize,
            float radius,
            float searchRadius,
            float seamRadius)
        {
            StartIslandSelection result = new StartIslandSelection();
            IslandCandidate anchor = Find(islands, anchorId);

            if (anchorId >= 0 && anchor.Cells >= minCells)
            {
                result.StartId = anchorId;
            }
            else
            {
                float best = float.MaxValue;
                float searchSqr = searchRadius * searchRadius;
                float seamSqr = seamRadius * seamRadius;
                foreach (IslandCandidate info in islands)
                {
                    if (info.Cells < minCells)
                    {
                        continue;
                    }
                    if (anchorId >= 0 && BBoxDistanceSqr(anchor, info, cellSize, radius) > seamSqr)
                    {
                        continue;
                    }
                    float minX = info.MinX * cellSize - radius;
                    float maxX = (info.MaxX + 1) * cellSize - radius;
                    float minY = info.MinY * cellSize - radius;
                    float maxY = (info.MaxY + 1) * cellSize - radius;
                    float dx = Math.Max(0f, Math.Max(minX - anchorX, anchorX - maxX));
                    float dy = Math.Max(0f, Math.Max(minY - anchorY, anchorY - maxY));
                    float distance = dx * dx + dy * dy;
                    if (distance < best && distance <= searchSqr)
                    {
                        best = distance;
                        result.StartId = info.Id;
                    }
                }
            }

            if (result.StartId < 0 && anchorId >= 0)
            {
                result.StartId = anchorId;
            }
            if (result.StartId >= 0 && anchorId >= 0 &&
                anchor.Id != result.StartId && anchor.Cells < minCells)
            {
                result.AbsorbedIds.Add(anchor.Id);
            }
            return result;
        }

        private static IslandCandidate Find(IslandCandidate[] islands, int id)
        {
            foreach (IslandCandidate info in islands)
            {
                if (info.Id == id)
                {
                    return info;
                }
            }
            return default;
        }

        private static float BBoxDistanceSqr(
            IslandCandidate first,
            IslandCandidate second,
            float cellSize,
            float radius)
        {
            float firstMinX = first.MinX * cellSize - radius;
            float firstMaxX = (first.MaxX + 1) * cellSize - radius;
            float firstMinY = first.MinY * cellSize - radius;
            float firstMaxY = (first.MaxY + 1) * cellSize - radius;
            float secondMinX = second.MinX * cellSize - radius;
            float secondMaxX = (second.MaxX + 1) * cellSize - radius;
            float secondMinY = second.MinY * cellSize - radius;
            float secondMaxY = (second.MaxY + 1) * cellSize - radius;
            float dx = Math.Max(0f, Math.Max(firstMinX - secondMaxX, secondMinX - firstMaxX));
            float dy = Math.Max(0f, Math.Max(firstMinY - secondMaxY, secondMinY - firstMaxY));
            return dx * dx + dy * dy;
        }
    }
}
