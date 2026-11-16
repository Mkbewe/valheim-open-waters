using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenWaters
{
    internal sealed partial class WorldCreationUI
    {
        private const float PreviewOffset = 119f;
        private const float PreviewWindowWidth = 360f;
        private const float PreviewWindowHeight = 420f;
        private const float PreviewWindowMargin = 24f;
        private const float PreviewImageSize = 320f;
        private const int PreviewSamples = 512;
        private const float PreviewWaterEdge = 500f;
        private const float PreviewMinIslandAreaKm2 = 0.05f;
        private const float PreviewMajorIslandAreaKm2 = 0.30f;
        private const float PreviewIslandClusterDistance = 500f;
        private struct LandComponent
        {
            public List<int> Cells;
            public int MinX;
            public int MaxX;
            public int MinY;
            public int MaxY;
        }

        private RectTransform _previewRow;
        private RectTransform _previewWindow;
        private RawImage _previewImage;
        private TextMeshProUGUI _previewTitle;
        private TextMeshProUGUI _previewHint;
        private TextMeshProUGUI _previewLabel;
        private Toggle _previewToggle;
        private Button _previewButton;
        private bool _previewGenerating;

        private void CreatePreviewRow(FejdStartup startup, Transform parent)
        {
            _previewRow = CreateContainer(parent, "OpenWaters_PreviewRow");
            _previewRow.sizeDelta = new Vector2(400f, 30f);

            _previewToggle = BuildToggle(startup, _previewRow);
            if (_previewToggle == null)
            {
                OpenWatersPlugin.Log.LogWarning("[UI] Could not find a toggle to base on.");
                return;
            }

            SetAnchor((RectTransform)_previewToggle.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 30f));
            _previewToggle.SetIsOnWithoutNotify(OpenWatersSettings.PreviewEnabled);
            _previewToggle.onValueChanged.AddListener(value =>
            {
                OpenWatersSettings.PreviewEnabled = value;
                OpenWatersPlugin.Log.LogInfo($"[UI] Preview={value}");
            });

            TMP_Text source = FindText(startup, "Seed") ?? FindSourceLabel(startup);
            TextMeshProUGUI label = CreateText(_previewRow, "Label", "Preview", source, Orange, 16f, TextAlignmentOptions.Left);
            _previewLabel = label;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(150f, 26f);
            labelRect.anchoredPosition = new Vector2(38f, 0f);
        }

        private void CreatePreviewWindow(FejdStartup startup, Transform parent)
        {
            _previewWindow = CreateContainer(parent, "OpenWaters_PreviewWindow");
            _previewWindow.sizeDelta = new Vector2(PreviewWindowWidth, PreviewWindowHeight);

            Image background = _previewWindow.gameObject.AddComponent<Image>();
            background.color = new Color(0.02f, 0.02f, 0.02f, 0.88f);
            background.raycastTarget = false;

            TMP_Text source = FindSourceLabel(startup);
            TextMeshProUGUI title = CreateText(_previewWindow, "Title", "Map preview", source, Orange, 18f, TextAlignmentOptions.Center);
            SetAnchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(PreviewWindowWidth - 20f, 26f));
            title.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            _previewTitle = title;

            TextMeshProUGUI hint = CreateText(_previewWindow, "Hint", "Preview will appear here", source, new Color(0.75f, 0.72f, 0.66f), 14f, TextAlignmentOptions.Center);
            SetAnchor(hint.rectTransform, Center, Center, new Vector2(PreviewWindowWidth - 30f, 22f));
            _previewHint = hint;

            GameObject imageObject = new GameObject("Image", typeof(RectTransform), typeof(RawImage));
            RectTransform imageRect = (RectTransform)imageObject.transform;
            imageRect.SetParent(_previewWindow, false);
            imageRect.anchorMin = imageRect.anchorMax = Center;
            imageRect.pivot = Center;
            imageRect.sizeDelta = new Vector2(PreviewImageSize, PreviewImageSize);
            imageRect.anchoredPosition = new Vector2(0f, 6f);
            _previewImage = imageObject.GetComponent<RawImage>();
            _previewImage.raycastTarget = false;
            _previewImage.enabled = false;

            _previewButton = BuildButton(startup, _previewWindow);
            if (_previewButton != null)
            {
                SetAnchor((RectTransform)_previewButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(220f, 36f));
                ((RectTransform)_previewButton.transform).anchoredPosition = new Vector2(0f, 10f);
                _previewButton.onClick.AddListener(OnGeneratePreview);
            }

            _previewWindow.gameObject.SetActive(OpenWatersSettings.PreviewEnabled);
        }


        private void OnGeneratePreview()
        {
            if (_previewGenerating)
            {
                return;
            }

            FejdStartup startup = FejdStartup.instance;
            if (startup == null || startup.m_newWorldSeed == null)
            {
                return;
            }

            string seed = startup.m_newWorldSeed.text;
            string key = GetPreviewKey(seed);
            if (TryGetCachedPreview(key, out PreviewCacheEntry cached))
            {
                _lastPreviewKey = key;
                ShowCachedPreview(cached);
                return;
            }

            StartCoroutine(GeneratePreviewCoroutine(seed, key));
        }

        private IEnumerator GeneratePreviewCoroutine(string seed, string key)
        {
            SetPreviewGenerating(true);
            if (_previewTitle != null)
            {
                _previewTitle.text = "Generating preview...";
            }
            if (_previewHint != null)
            {
                _previewHint.gameObject.SetActive(true);
                _previewHint.text = "This can take a few seconds";
            }
            if (_previewImage != null)
            {
                _previewImage.enabled = false;
            }

            yield return null;

            Texture2D texture = null;
            int islands = 0;
            int components = 0;
            float landPercent = 0f;
            long started = DateTime.UtcNow.Ticks;

            try
            {
                World previewWorld = new World(OpenWatersSettings.PreviewWorldName, seed);
                WorldGenerator.Initialize(previewWorld);
                texture = RenderPreview(out islands, out landPercent, out components);
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogError($"[Preview] Generation failed: {exception}");
            }
            finally
            {
                try
                {
                    WorldGenerator.Initialize(World.GetMenuWorld());
                }
                catch (Exception exception)
                {
                    OpenWatersPlugin.Log.LogError($"[Preview] Failed to restore menu generator: {exception}");
                }
                SetPreviewGenerating(false);
            }

            if (texture == null)
            {
                _lastPreviewKey = key;
                if (_previewTitle != null)
                {
                    _previewTitle.text = "Map preview";
                }
                if (_previewHint != null)
                {
                    _previewHint.gameObject.SetActive(true);
                    _previewHint.text = "Preview failed - see log";
                }
                yield break;
            }

            PreviewCacheEntry entry = new PreviewCacheEntry
            {
                Key = key,
                Texture = texture,
                Islands = islands
            };
            StoreCachedPreview(entry);
            _lastPreviewKey = key;
            ShowCachedPreview(entry);

            long elapsedMs = (DateTime.UtcNow.Ticks - started) / TimeSpan.TicksPerMillisecond;
            OpenWatersPlugin.Log.LogInfo($"[Preview] Seed='{seed}', islands={islands}, components={components}, land={landPercent:P1}, time={elapsedMs} ms.");
        }

        private static Texture2D RenderPreview(out int islands, out float landPercent, out int components)
        {
            int size = PreviewSamples;
            float radius = OpenWatersSettings.WorldRadius + PreviewWaterEdge;
            Color32[] pixels = new Color32[size * size];
            bool[] land = new bool[size * size];
            int landCells = 0;
            int insideCells = 0;
            components = 0;

            for (int y = 0; y < size; y++)
            {
                float wy = -radius + (y + 0.5f) * (2f * radius / size);
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    float wx = -radius + (x + 0.5f) * (2f * radius / size);
                    if (wx * wx + wy * wy > radius * radius)
                    {
                        pixels[index] = new Color32(4, 8, 16, 255);
                        continue;
                    }

                    insideCells++;
                    bool isLand = WorldGenerator.instance.GetHeight(wx, wy) > 30f;
                    pixels[index] = isLand
                        ? new Color32(112, 170, 84, 255)
                        : new Color32(38, 82, 128, 255);

                    if (isLand)
                    {
                        land[index] = true;
                        landCells++;
                    }
                }
            }

            landPercent = insideCells > 0 ? (float)landCells / insideCells : 0f;
            float cellSize = 2f * radius / size;
            float cellArea = cellSize * cellSize;
            int minIslandCells = Mathf.Max(1, Mathf.CeilToInt(PreviewMinIslandAreaKm2 * 1000000f / cellArea));
            islands = CountIslands(land, size, minIslandCells, cellSize, pixels, out components);

            int center = size / 2;
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
                        pixels[py * size + px] = new Color32(255, 120, 20, 255);
                    }
                }
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        private static int CountIslands(bool[] land, int size, int minCells, float cellSize, Color32[] pixels, out int componentCount)
        {
            bool[] visited = new bool[land.Length];
            Stack<int> stack = new Stack<int>();
            List<LandComponent> components = new List<LandComponent>();

            for (int start = 0; start < land.Length; start++)
            {
                if (!land[start] || visited[start])
                {
                    continue;
                }

                List<int> cells = new List<int>();
                int minX = size;
                int maxX = -1;
                int minY = size;
                int maxY = -1;

                visited[start] = true;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    int index = stack.Pop();
                    cells.Add(index);
                    int x = index % size;
                    int y = index / size;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    PushIfLand(x - 1, y);
                    PushIfLand(x + 1, y);
                    PushIfLand(x, y - 1);
                    PushIfLand(x, y + 1);
                }

                components.Add(new LandComponent
                {
                    Cells = cells,
                    MinX = minX,
                    MaxX = maxX,
                    MinY = minY,
                    MaxY = maxY
                });
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

            int clusterGap = Mathf.Max(1, Mathf.CeilToInt(PreviewIslandClusterDistance / cellSize));
            float cellArea = cellSize * cellSize;
            int majorCells = Mathf.Max(minCells, Mathf.CeilToInt(PreviewMajorIslandAreaKm2 * 1000000f / cellArea));
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
                    pixels[cells[cell]] = new Color32(72, 100, 66, 255);
                }
            }

            return islands;

            void PushIfLand(int x, int y)
            {
                if (x < 0 || x >= size || y < 0 || y >= size)
                {
                    return;
                }
                int index = y * size + x;
                if (!land[index] || visited[index])
                {
                    return;
                }
                visited[index] = true;
                stack.Push(index);
            }
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

        private void SetPreviewGenerating(bool generating)
        {
            _previewGenerating = generating;
            if (_previewButton != null)
            {
                _previewButton.interactable = !generating;
            }
            if (_slider != null)
            {
                _slider.interactable = !generating;
            }

            FejdStartup startup = FejdStartup.instance;
            if (startup != null && startup.m_newWorldSeed != null)
            {
                startup.m_newWorldSeed.interactable = !generating;
            }
        }


        private void AlignPreviewToggle()
        {
            if (_previewToggle == null || _previewLabel == null)
            {
                return;
            }

            Graphic visual = _previewToggle.graphic != null
                ? (Graphic)_previewToggle.graphic
                : _previewToggle.targetGraphic;
            if (visual == null)
            {
                return;
            }

            Vector3[] corners = new Vector3[4];
            visual.rectTransform.GetWorldCorners(corners);
            float visualCenter = (corners[0].y + corners[2].y) * 0.5f;
            _previewLabel.rectTransform.GetWorldCorners(corners);
            float labelCenter = (corners[0].y + corners[2].y) * 0.5f;

            Transform toggleTransform = _previewToggle.transform;
            Vector3 position = toggleTransform.position;
            toggleTransform.position = new Vector3(position.x, position.y + labelCenter - visualCenter, position.z);
        }

    }
}