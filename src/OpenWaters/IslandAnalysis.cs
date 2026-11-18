using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OpenWaters
{
    internal delegate Heightmap.Biome GetBiomeDelegate(
        float wx, float wy, float oceanLevel, bool waterAlwaysOcean);

    internal delegate float GetHeightDelegate(float wx, float wy);

    internal sealed class IslandInfo
    {
        public ushort Id;
        public int Cells;
        public float CenterX;
        public float CenterY;
        public int MinX;
        public int MaxX;
        public int MinY;
        public int MaxY;
        public readonly int[] BiomeCells = new int[9];
        public Heightmap.Biome DominantBiome = Heightmap.Biome.None;
        public float DominantShare;
    }

    internal static class IslandAnalysis
    {
        private const int Samples = 1024;
        private const float WaterLevel = 30f;
        private const float AnchorSearchRadius = 2500f;

        private static Heightmap.Biome[] _cellBiome;
        private static ushort[] _cellIsland;
        private static IslandInfo[] _islands;
        private static bool[] _land;
        private static int[] _parent;
        private static int[] _rank;
        private static GetBiomeDelegate _getBiome;
        private static GetHeightDelegate _getHeight;
        private static int _size;
        private static float _radius;
        private static float _cellSize;
        private static Vector2 _anchor;
        private static ushort _startIsland = ushort.MaxValue;
        private static int _startCells;
        private static int _sampleRow;
        private static long _started;
        private static bool _analyzing;

        public static bool DeferAnalysis;

        public static bool Analyzing => _analyzing;
        public static bool Ready => _cellIsland != null;
        public static int StartCells => _startCells;
        public static IReadOnlyList<IslandInfo> Islands => _islands;
        public static IslandInfo StartIsland => _startIsland == ushort.MaxValue || _islands == null
            ? null
            : _islands[_startIsland];

        public static void Clear()
        {
            _cellBiome = null;
            _cellIsland = null;
            _islands = null;
            _land = null;
            _parent = null;
            _rank = null;
            _getBiome = null;
            _getHeight = null;
            _startIsland = ushort.MaxValue;
            _startCells = 0;
            _sampleRow = 0;
            _analyzing = false;
        }

        public static void Build(WorldGenerator generator, float radius)
        {
            if (DeferAnalysis)
            {
                return;
            }
            if (!Begin(generator, radius))
            {
                return;
            }
            while (SampleRows(16))
            {
            }
            Finish();
        }

        public static bool Begin(WorldGenerator generator, float radius)
        {
            Clear();
            MethodInfo biomeMethod = AccessTools.Method(
                typeof(WorldGenerator),
                "GetBiome",
                new[] { typeof(float), typeof(float), typeof(float), typeof(bool) });
            MethodInfo heightMethod = AccessTools.Method(
                typeof(WorldGenerator),
                "GetHeight",
                new[] { typeof(float), typeof(float) });
            if (biomeMethod == null || heightMethod == null)
            {
                OpenWatersPlugin.Log.LogError("[Gen] Could not find WorldGenerator.GetBiome/GetHeight.");
                return false;
            }

            try
            {
                _getBiome = (GetBiomeDelegate)Delegate.CreateDelegate(
                    typeof(GetBiomeDelegate), generator, biomeMethod);
                _getHeight = (GetHeightDelegate)Delegate.CreateDelegate(
                    typeof(GetHeightDelegate), generator, heightMethod);
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogError($"[Gen] Could not bind world generator methods: {exception}");
                return false;
            }

            int size = Samples;
            _size = size;
            _radius = radius;
            _cellSize = 2f * radius / size;
            _land = new bool[size * size];
            _cellBiome = new Heightmap.Biome[size * size];
            _parent = new int[size * size];
            _rank = new int[size * size];
            for (int i = 0; i < _parent.Length; i++)
            {
                _parent[i] = i;
            }

            _sampleRow = 0;
            _started = DateTime.UtcNow.Ticks;
            _analyzing = true;
            return true;
        }

        public static bool SampleRows(int rows)
        {
            if (!_analyzing)
            {
                return false;
            }
            int end = Mathf.Min(_sampleRow + rows, _size);
            for (int y = _sampleRow; y < end; y++)
            {
                float wy = -_radius + (y + 0.5f) * _cellSize;
                for (int x = 0; x < _size; x++)
                {
                    float wx = -_radius + (x + 0.5f) * _cellSize;
                    int index = y * _size + x;
                    if (wx * wx + wy * wy > _radius * _radius || _getHeight(wx, wy) <= WaterLevel)
                    {
                        continue;
                    }
                    _land[index] = true;
                    _cellBiome[index] = _getBiome(wx, wy, 0.02f, false);
                    if (x > 0 && _land[index - 1])
                    {
                        Unite(_parent, _rank, index - 1, index);
                    }
                    if (y > 0 && _land[index - _size])
                    {
                        Unite(_parent, _rank, index - _size, index);
                    }
                }
            }
            _sampleRow = end;
            if (_sampleRow >= _size)
            {
                _analyzing = false;
                return false;
            }
            return true;
        }

        public static bool Finish()
        {
            if (_land == null || _sampleRow < _size)
            {
                return false;
            }
            _analyzing = false;

            bool[] land = _land;
            int[] parent = _parent;
            int[] rank = _rank;
            int size = _size;
            float radius = _radius;
            float cellSize = _cellSize;
            Heightmap.Biome[] biomes = _cellBiome;

            int[] rootCells = new int[parent.Length];
            List<int> roots = new List<int>();
            int landCells = 0;
            for (int i = 0; i < land.Length; i++)
            {
                if (!land[i])
                {
                    continue;
                }
                int root = Find(parent, i);
                if (rootCells[root] == 0)
                {
                    roots.Add(root);
                }
                rootCells[root]++;
                landCells++;
            }

            roots.Sort((a, b) =>
            {
                int bySize = rootCells[b].CompareTo(rootCells[a]);
                return bySize != 0 ? bySize : a.CompareTo(b);
            });

            Dictionary<int, ushort> ids = new Dictionary<int, ushort>(roots.Count);
            for (int i = 0; i < roots.Count; i++)
            {
                ids[roots[i]] = (ushort)i;
            }

            _size = size;
            _radius = radius;
            _cellSize = cellSize;
            _cellBiome = biomes;
            _cellIsland = new ushort[size * size];
            for (int i = 0; i < _cellIsland.Length; i++)
            {
                _cellIsland[i] = ushort.MaxValue;
            }
            _islands = new IslandInfo[roots.Count];
            for (int i = 0; i < _islands.Length; i++)
            {
                _islands[i] = new IslandInfo
                {
                    Id = (ushort)i,
                    MinX = size,
                    MinY = size,
                    MaxX = -1,
                    MaxY = -1
                };
            }

            for (int i = 0; i < land.Length; i++)
            {
                if (!land[i])
                {
                    continue;
                }
                ushort id = ids[Find(parent, i)];
                _cellIsland[i] = id;
                IslandInfo info = _islands[id];
                int x = i % size;
                int y = i / size;
                info.Cells++;
                info.CenterX += x;
                info.CenterY += y;
                if (x < info.MinX) info.MinX = x;
                if (x > info.MaxX) info.MaxX = x;
                if (y < info.MinY) info.MinY = y;
                if (y > info.MaxY) info.MaxY = y;
                info.BiomeCells[BiomeSlot(biomes[i])]++;
            }

            foreach (IslandInfo info in _islands)
            {
                if (info.Cells == 0)
                {
                    continue;
                }
                info.CenterX = (info.CenterX / info.Cells + 0.5f) * cellSize - radius;
                info.CenterY = (info.CenterY / info.Cells + 0.5f) * cellSize - radius;
                int best = 0;
                for (int slot = 1; slot < info.BiomeCells.Length; slot++)
                {
                    if (info.BiomeCells[slot] > info.BiomeCells[best])
                    {
                        best = slot;
                    }
                }
                info.DominantBiome = SlotBiome(best);
                info.DominantShare = (float)info.BiomeCells[best] / info.Cells;
            }

            _anchor = Vector2.zero;

            int anchorX = Mathf.Clamp(Mathf.FloorToInt((_anchor.x + radius) / cellSize), 0, size - 1);
            int anchorY = Mathf.Clamp(Mathf.FloorToInt((_anchor.y + radius) / cellSize), 0, size - 1);
            int anchorIndex = anchorY * size + anchorX;
            if (land[anchorIndex])
            {
                _startIsland = ids[Find(parent, anchorIndex)];
            }
            else
            {
                float best = float.MaxValue;
                foreach (IslandInfo info in _islands)
                {
                    float minX = info.MinX * cellSize - radius;
                    float maxX = (info.MaxX + 1) * cellSize - radius;
                    float minY = info.MinY * cellSize - radius;
                    float maxY = (info.MaxY + 1) * cellSize - radius;
                    float dx = Mathf.Max(0f, Mathf.Max(minX - _anchor.x, _anchor.x - maxX));
                    float dy = Mathf.Max(0f, Mathf.Max(minY - _anchor.y, _anchor.y - maxY));
                    float distance = dx * dx + dy * dy;
                    if (distance < best)
                    {
                        best = distance;
                        _startIsland = info.Id;
                    }
                }
                if (best > AnchorSearchRadius * AnchorSearchRadius)
                {
                    _startIsland = ushort.MaxValue;
                }
            }

            CountStartCells();

            long elapsedMs = (DateTime.UtcNow.Ticks - _started) / TimeSpan.TicksPerMillisecond;
            IslandInfo start = StartIsland;
            OpenWatersPlugin.Log.LogInfo(
                $"[Gen] Island analysis: islands={_islands.Length}, land={landCells}, " +
                $"anchor=({_anchor.x:0},{_anchor.y:0}), " +
                $"startIsland={(start != null ? start.Id.ToString() : "none")}, " +
                $"startCells={_startCells}, " +
                $"center={(start != null ? $"({start.CenterX:0},{start.CenterY:0})" : "none")}, " +
                $"extent={(start != null ? $"({(start.MaxX - start.MinX + 1) * cellSize:0}x{(start.MaxY - start.MinY + 1) * cellSize:0} m)" : "none")}, " +
                $"dominant={(start != null ? start.DominantBiome.ToString() : "none")} " +
                $"({(start != null ? start.DominantShare : 0f):P0}), time={elapsedMs} ms.");
            return true;
        }

        private static void CountStartCells()
        {
            IslandInfo start = StartIsland;
            _startCells = start != null ? start.Cells : 0;
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

        private static void Unite(int[] parent, int[] rank, int first, int second)
        {
            int a = Find(parent, first);
            int b = Find(parent, second);
            if (a == b)
            {
                return;
            }
            if (rank[a] < rank[b])
            {
                parent[a] = b;
            }
            else if (rank[a] > rank[b])
            {
                parent[b] = a;
            }
            else
            {
                parent[b] = a;
                rank[a]++;
            }
        }

        private static int BiomeSlot(Heightmap.Biome biome)
        {
            switch (biome)
            {
                case Heightmap.Biome.Meadows: return 1;
                case Heightmap.Biome.Swamp: return 2;
                case Heightmap.Biome.Mountain: return 3;
                case Heightmap.Biome.BlackForest: return 4;
                case Heightmap.Biome.Plains: return 5;
                case Heightmap.Biome.AshLands: return 6;
                case Heightmap.Biome.DeepNorth: return 7;
                case Heightmap.Biome.Mistlands: return 8;
                default: return 0;
            }
        }

        private static Heightmap.Biome SlotBiome(int slot)
        {
            switch (slot)
            {
                case 1: return Heightmap.Biome.Meadows;
                case 2: return Heightmap.Biome.Swamp;
                case 3: return Heightmap.Biome.Mountain;
                case 4: return Heightmap.Biome.BlackForest;
                case 5: return Heightmap.Biome.Plains;
                case 6: return Heightmap.Biome.AshLands;
                case 7: return Heightmap.Biome.DeepNorth;
                case 8: return Heightmap.Biome.Mistlands;
                default: return Heightmap.Biome.None;
            }
        }

        public static bool IsStartIsland(float wx, float wy)
        {
            if (!Ready || _startIsland == ushort.MaxValue)
            {
                return false;
            }
            int x = Mathf.FloorToInt((wx + _radius) / _cellSize);
            int y = Mathf.FloorToInt((wy + _radius) / _cellSize);
            if (x < 0 || x >= _size || y < 0 || y >= _size)
            {
                return false;
            }
            return _cellIsland[y * _size + x] == _startIsland;
        }

        public static bool TryGetIslandAt(float wx, float wy, out IslandInfo info)
        {
            info = null;
            if (!Ready)
            {
                return false;
            }
            int x = Mathf.FloorToInt((wx + _radius) / _cellSize);
            int y = Mathf.FloorToInt((wy + _radius) / _cellSize);
            if (x < 0 || x >= _size || y < 0 || y >= _size)
            {
                return false;
            }
            ushort id = _cellIsland[y * _size + x];
            if (id == ushort.MaxValue || _islands == null)
            {
                return false;
            }
            info = _islands[id];
            return true;
        }

        public static bool TryGetOriginalBiome(float wx, float wy, out Heightmap.Biome biome)
        {
            biome = Heightmap.Biome.None;
            if (!Ready)
            {
                return false;
            }
            int x = Mathf.FloorToInt((wx + _radius) / _cellSize);
            int y = Mathf.FloorToInt((wy + _radius) / _cellSize);
            if (x < 0 || x >= _size || y < 0 || y >= _size)
            {
                return false;
            }
            biome = _cellBiome[y * _size + x];
            return true;
        }

        public static bool IsIsland(float wx, float wy, ushort id)
        {
            return TryGetIslandAt(wx, wy, out IslandInfo info) && info.Id == id;
        }

        public static List<IslandInfo> FindIslands(Predicate<IslandInfo> predicate)
        {
            List<IslandInfo> result = new List<IslandInfo>();
            if (_islands == null)
            {
                return result;
            }
            foreach (IslandInfo info in _islands)
            {
                if (predicate(info))
                {
                    result.Add(info);
                }
            }
            return result;
        }
    }
}
