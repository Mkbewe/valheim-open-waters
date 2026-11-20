using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenWaters
{
    internal sealed class PreviewResult
    {
        public Texture2D Texture;
        public Texture2D BiomeTexture;
        public int Islands;
        public int Components;
        public float LandPercent;
    }

    internal static class PreviewRenderer
    {
        private const int Samples = 512;
        private const float WaterEdge = 500f;
        private const float MinIslandAreaKm2 = 0.05f;
        private const float MajorIslandAreaKm2 = 0.30f;
        private const float IslandClusterDistance = 500f;

        private struct LandComponent
        {
            public List<int> Cells;
            public int MinX;
            public int MaxX;
            public int MinY;
            public int MaxY;
        }

        public static IEnumerator Render(PreviewResult result, WorldSettings settings)
        {
            int size = Samples;
            float radius = settings.WorldRadius + WaterEdge;
            float cellSize = 2f * radius / size;
            Color32[] pixels = new Color32[size * size];
            Color32[] biomePixels = new Color32[size * size];
            bool[] land = new bool[size * size];
            bool[] swamp = new bool[size * size];
            int landCells = 0;
            int insideCells = 0;

            for (int row = 0; row < size; row += 16)
            {
                int end = Mathf.Min(row + 16, size);
                for (int y = row; y < end; y++)
                {
                    float wy = -radius + (y + 0.5f) * cellSize;
                    for (int x = 0; x < size; x++)
                    {
                        int index = y * size + x;
                        float wx = -radius + (x + 0.5f) * cellSize;
                        if (wx * wx + wy * wy > radius * radius)
                        {
                            pixels[index] = BiomePalette.Outside;
                            biomePixels[index] = BiomePalette.Outside;
                            continue;
                        }

                        insideCells++;
                        bool isLand = WorldGenerator.instance.GetHeight(wx, wy) > 30f;
                        Color32 water = BiomePalette.Water;
                        pixels[index] = isLand ? BiomePalette.Land : water;

                        Heightmap.Biome biome = WorldGenerator.instance.GetBiome(wx, wy);
                        bool isSwamp = biome == Heightmap.Biome.Swamp;
                        swamp[index] = isSwamp;
                        if (isSwamp)
                        {
                            biomePixels[index] = BiomePalette.ForBiome(biome);
                        }
                        else if (isLand && biome != Heightmap.Biome.Ocean)
                        {
                            biomePixels[index] = BiomePalette.ForBiome(biome);
                        }
                        else
                        {
                            biomePixels[index] = water;
                        }

                        if (isLand)
                        {
                            land[index] = true;
                            landCells++;
                        }
                    }
                }
                yield return null;
            }

            Color32 swampWater = BiomePalette.SwampWater;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    if (land[index] || swamp[index] || !HasSwampNeighbor(swamp, size, x, y))
                    {
                        continue;
                    }
                    biomePixels[index] = swampWater;
                }
            }

            result.LandPercent = insideCells > 0 ? (float)landCells / insideCells : 0f;
            float cellArea = cellSize * cellSize;
            int minIslandCells = Mathf.Max(1, Mathf.CeilToInt(MinIslandAreaKm2 * 1000000f / cellArea));
            result.Islands = CountIslands(
                land, size, minIslandCells, cellSize, pixels, biomePixels, out result.Components);

            int center = size / 2;
            Color32 marker = BiomePalette.SpawnMarker;
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    if (dx * dx + dy * dy > 5)
                    {
                        continue;
                    }
                    int px = center + dx;
                    int py = center + dy;
                    if (px >= 0 && px < size && py >= 0 && py < size)
                    {
                        int index = py * size + px;
                        pixels[index] = marker;
                        biomePixels[index] = marker;
                    }
                }
            }

            result.BiomeTexture = CreateTexture(biomePixels);
            result.Texture = CreateTexture(pixels);
        }

        private static Texture2D CreateTexture(Color32[] pixels)
        {
            Texture2D texture = new Texture2D(Samples, Samples, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        private static bool HasSwampNeighbor(bool[] swamp, int size, int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx < 0 || nx >= size || ny < 0 || ny >= size)
                    {
                        continue;
                    }
                    if (swamp[ny * size + nx])
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static int CountIslands(
            bool[] land,
            int size,
            int minCells,
            float cellSize,
            Color32[] pixels,
            Color32[] biomePixels,
            out int componentCount)
        {
            int[] labels = GridComponents.Label(land, size, out componentCount);
            List<LandComponent> components = new List<LandComponent>(componentCount);
            for (int i = 0; i < componentCount; i++)
            {
                components.Add(new LandComponent
                {
                    Cells = new List<int>(),
                    MinX = size,
                    MaxX = -1,
                    MinY = size,
                    MaxY = -1
                });
            }

            for (int i = 0; i < land.Length; i++)
            {
                if (!land[i])
                {
                    continue;
                }
                int label = labels[i];
                LandComponent component = components[label];
                component.Cells.Add(i);
                int x = i % size;
                int y = i / size;
                if (x < component.MinX) component.MinX = x;
                if (x > component.MaxX) component.MaxX = x;
                if (y < component.MinY) component.MinY = y;
                if (y > component.MaxY) component.MaxY = y;
                components[label] = component;
            }
            componentCount = components.Count;
            if (components.Count == 0)
            {
                return 0;
            }

            int[] parent = new int[components.Count];
            for (int i = 0; i < parent.Length; i++)
            {
                parent[i] = i;
            }

            int clusterGap = Mathf.Max(1, Mathf.CeilToInt(IslandClusterDistance / cellSize));
            float cellArea = cellSize * cellSize;
            int majorCells = Mathf.Max(minCells, Mathf.CeilToInt(MajorIslandAreaKm2 * 1000000f / cellArea));
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i].Cells.Count >= majorCells)
                {
                    continue;
                }
                for (int j = i + 1; j < components.Count; j++)
                {
                    if (components[j].Cells.Count >= majorCells)
                    {
                        continue;
                    }
                    if (!Overlaps(components[i], components[j], clusterGap))
                    {
                        continue;
                    }
                    int rootA = Find(parent, i);
                    int rootB = Find(parent, j);
                    if (rootA != rootB)
                    {
                        parent[rootB] = rootA;
                    }
                }
            }

            Dictionary<int, int> clusterCells = new Dictionary<int, int>();
            for (int i = 0; i < components.Count; i++)
            {
                int root = Find(parent, i);
                clusterCells.TryGetValue(root, out int total);
                clusterCells[root] = total + components[i].Cells.Count;
            }

            int islands = 0;
            HashSet<int> countedRoots = new HashSet<int>();
            foreach (KeyValuePair<int, int> cluster in clusterCells)
            {
                if (cluster.Value >= minCells)
                {
                    islands++;
                    countedRoots.Add(cluster.Key);
                }
            }

            for (int i = 0; i < components.Count; i++)
            {
                if (countedRoots.Contains(Find(parent, i)))
                {
                    continue;
                }
                List<int> cells = components[i].Cells;
                for (int cell = 0; cell < cells.Count; cell++)
                {
                    pixels[cells[cell]] = BiomePalette.HiddenLand;
                    biomePixels[cells[cell]] = BiomePalette.HiddenLand;
                }
            }

            return islands;
        }

        private static bool Overlaps(LandComponent first, LandComponent second, int gap)
        {
            return first.MinX - gap <= second.MaxX
                && second.MinX - gap <= first.MaxX
                && first.MinY - gap <= second.MaxY
                && second.MinY - gap <= first.MaxY;
        }

        private static int Find(int[] parent, int index)
        {
            while (parent[index] != index)
            {
                parent[index] = parent[parent[index]];
                index = parent[index];
            }
            return index;
        }
    }
}
