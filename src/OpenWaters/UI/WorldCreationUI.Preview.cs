using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenWaters
{
    internal sealed partial class WorldCreationUI
    {
        private const float PreviewOffset = 277f;
        private const float PreviewWindowWidth = 440f;
        private const float PreviewWindowHeight = 500f;
        private const float PreviewWindowMargin = 24f;
        private const float PreviewImageSize = 400f;
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
        private Toggle _enabledToggle;
        private TextMeshProUGUI _enabledLabel;
        private Button _previewButton;
        private Button _biomeViewButton;
        private TMP_Text _biomeViewLabel;
        private bool _biomeView;
        private PreviewCacheEntry _currentPreviewEntry;
        private bool _previewGenerating;
        private WorldGenerator _previewGenerator;
        private World _previewGeneratorWorld;
        private string _previewGeneratorKey;

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
                SetAnchor((RectTransform)_previewButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(200f, 36f));
                ((RectTransform)_previewButton.transform).anchoredPosition = new Vector2(-54f, 10f);
                _previewButton.onClick.AddListener(OnGeneratePreview);
            }

            _biomeViewButton = BuildButton(startup, _previewWindow, "Biomes");
            if (_biomeViewButton != null)
            {
                SetAnchor((RectTransform)_biomeViewButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(100f, 36f));
                ((RectTransform)_biomeViewButton.transform).anchoredPosition = new Vector2(104f, 10f);
                _biomeViewButton.onClick.AddListener(OnToggleBiomeView);
                _biomeViewButton.interactable = false;
                _biomeViewLabel = _biomeViewButton.GetComponentInChildren<TMP_Text>(true);
                if (_biomeViewLabel != null)
                {
                    _biomeViewLabel.rectTransform.sizeDelta = new Vector2(90f, 24f);
                    _biomeViewLabel.fontSize = 18f;
                }
            }

            _previewWindow.gameObject.SetActive(OpenWatersSettings.Enabled && OpenWatersSettings.PreviewEnabled);
            UpdateEnabledState();
        }

        private void OnToggleBiomeView()
        {
            _biomeView = !_biomeView;
            if (_biomeViewLabel != null)
            {
                _biomeViewLabel.text = _biomeView ? "Land" : "Biomes";
            }
            if (_currentPreviewEntry != null)
            {
                ShowCachedPreview(_currentPreviewEntry);
            }
        }


        private void OnGeneratePreview()
        {
            if (_previewGenerating || !OpenWatersSettings.Enabled)
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

        private sealed class PreviewResult
        {
            public Texture2D Texture;
            public Texture2D BiomeTexture;
            public int Islands;
            public int Components;
            public float LandPercent;
        }

        private static string GetGeneratorKey(string seed)
        {
            StringBuilder key = new StringBuilder(seed);
            key.Append('|').Append((int)OpenWatersSettings.IslandAmount);
            key.Append('|').Append(OpenWatersSettings.WorldRadius);
            key.Append('|').Append(OpenWatersSettings.StartBiomeLimit);
            key.Append('|').Append(OpenWatersSettings.ExcludeMountains ? 1 : 0);
            AppendConfigValue(key, OpenWatersSettings.FewLandFraction);
            AppendConfigValue(key, OpenWatersSettings.FewFrequencyScale);
            AppendConfigValue(key, OpenWatersSettings.FewPeakProtection);
            AppendConfigValue(key, OpenWatersSettings.FewDetailDamping);
            AppendConfigValue(key, OpenWatersSettings.FewMountainCompressionStart);
            AppendConfigValue(key, OpenWatersSettings.FewMountainCompressionFactor);
            AppendConfigValue(key, OpenWatersSettings.MediumLandFraction);
            AppendConfigValue(key, OpenWatersSettings.MediumFrequencyScale);
            AppendConfigValue(key, OpenWatersSettings.MediumPeakProtection);
            AppendConfigValue(key, OpenWatersSettings.MediumDetailDamping);
            return key.ToString();
        }

        private static void AppendConfigValue(StringBuilder key, float value)
        {
            key.Append('|').Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static bool SetGeneratorInstance(WorldGenerator generator)
        {
            FieldInfo field = typeof(WorldGenerator).GetField(
                "m_instance", BindingFlags.Static | BindingFlags.NonPublic);
            if (field == null)
            {
                OpenWatersPlugin.Log.LogError(
                    "[Preview] Could not find WorldGenerator.m_instance; preview cache disabled.");
                return false;
            }
            field.SetValue(null, generator);
            return true;
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

            long started = DateTime.UtcNow.Ticks;
            PreviewResult result = new PreviewResult();
            bool initialized = false;
            string generatorKey = GetGeneratorKey(seed);
            try
            {
                bool useCache = _previewGenerator != null &&
                                _previewGeneratorKey == generatorKey &&
                                SetGeneratorInstance(_previewGenerator);
                if (useCache)
                {
                    NoiseModifier.SetWorld(_previewGeneratorWorld);
                    GenerationSession.Current.DeferIslandAnalysis = true;
                    try
                    {
                        NoiseModifier.Build(_previewGenerator);
                        initialized = true;
                    }
                    finally
                    {
                        GenerationSession.Current.DeferIslandAnalysis = false;
                    }
                }
                else
                {
                    _previewGenerator = null;
                    _previewGeneratorWorld = null;
                    _previewGeneratorKey = null;
                    GenerationSession.DeferNextSession();
                    try
                    {
                        World previewWorld = new World(OpenWatersSettings.PreviewWorldName, seed);
                        WorldGenerator.Initialize(previewWorld);
                        _previewGenerator = WorldGenerator.instance;
                        _previewGeneratorWorld = previewWorld;
                        _previewGeneratorKey = generatorKey;
                        initialized = true;
                    }
                    finally
                    {
                        GenerationSession.ClearDeferredSession();
                        GenerationSession.Current.DeferIslandAnalysis = false;
                    }
                }
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogError($"[Preview] World generation failed: {exception}");
            }


            if (initialized)
            {
                yield return null;

                if (OpenWatersSettings.ExcludeMountains || OpenWatersSettings.StartBiomeLimit != 0)
                {
                    GenerationSession.Current.BeginAnalysis();
                    bool begun = false;
                    try
                    {
                        begun = IslandAnalysis.Begin(
                            WorldGenerator.instance, OpenWatersSettings.WorldRadius + PreviewWaterEdge);
                    }
                    catch (Exception exception)
                    {
                        OpenWatersPlugin.Log.LogError($"[Preview] Island analysis failed: {exception}");
                    }

                    if (begun)
                    {
                        while (true)
                        {
                            bool more = false;
                            try
                            {
                                more = IslandAnalysis.SampleRows(16);
                            }
                            catch (Exception exception)
                            {
                                OpenWatersPlugin.Log.LogError($"[Preview] Island analysis failed: {exception}");
                                IslandAnalysis.Clear();
                                break;
                            }
                            if (!more)
                            {
                                IslandAnalysis.Finish();
                                break;
                            }
                            yield return null;
                        }
                    }
                    GenerationSession.Current.MarkReady();
                    yield return null;
                }

                IEnumerator render = RenderPreviewRoutine(result);
                while (true)
                {
                    bool moved = false;
                    try
                    {
                        moved = render.MoveNext();
                    }
                    catch (Exception exception)
                    {
                        OpenWatersPlugin.Log.LogError($"[Preview] Rendering failed: {exception}");
                        break;
                    }
                    if (!moved)
                    {
                        break;
                    }
                    yield return null;
                }
            }

            try
            {
                WorldGenerator.Deitialize();
                WorldGenerator.Initialize(World.GetMenuWorld());
            }
            catch (Exception exception)
            {
                OpenWatersPlugin.Log.LogError($"[Preview] Failed to restore menu generator: {exception}");
            }
            SetPreviewGenerating(false);

            Texture2D texture = result.Texture;
            if (texture == null)
            {
                if (result.BiomeTexture != null)
                {
                    UnityEngine.Object.Destroy(result.BiomeTexture);
                }
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
                BiomeTexture = result.BiomeTexture,
                Islands = result.Islands
            };
            StoreCachedPreview(entry);
            _lastPreviewKey = key;
            ShowCachedPreview(entry);

            long elapsedMs = (DateTime.UtcNow.Ticks - started) / TimeSpan.TicksPerMillisecond;
            OpenWatersPlugin.Log.LogInfo(
                $"[Preview] Seed='{seed}', islands={result.Islands}, components={result.Components}, " +
                $"land={result.LandPercent:P1}, time={elapsedMs} ms.");
        }

        private static IEnumerator RenderPreviewRoutine(PreviewResult result)
        {
            int size = PreviewSamples;
            float radius = OpenWatersSettings.WorldRadius + PreviewWaterEdge;
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
                            Color32 outside = BiomePalette.Outside;
                            pixels[index] = outside;
                            biomePixels[index] = outside;
                            continue;
                        }

                        insideCells++;
                        bool isLand = WorldGenerator.instance.GetHeight(wx, wy) > 30f;
                        Color32 water = BiomePalette.Water;
                        pixels[index] = isLand
                            ? BiomePalette.Land
                            : water;

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
            int minIslandCells = Mathf.Max(1, Mathf.CeilToInt(PreviewMinIslandAreaKm2 * 1000000f / cellArea));
            result.Islands = CountIslands(land, size, minIslandCells, cellSize, pixels, biomePixels, out result.Components);

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

            result.BiomeTexture = CreatePreviewTexture(biomePixels);
            result.Texture = CreatePreviewTexture(pixels);
        }

        private static Texture2D CreatePreviewTexture(Color32[] pixels)
        {
            Texture2D texture = new Texture2D(PreviewSamples, PreviewSamples, TextureFormat.RGBA32, false);
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

        private static int CountIslands(bool[] land, int size, int minCells, float cellSize, Color32[] pixels, Color32[] biomePixels, out int componentCount)
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
                    Color32 hidden = BiomePalette.HiddenLand;
                    pixels[cells[cell]] = hidden;
                    biomePixels[cells[cell]] = hidden;
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

        private void SetPreviewGenerating(bool generating)
        {
            _previewGenerating = generating;
            if (_previewButton != null)
            {
                _previewButton.interactable = OpenWatersSettings.Enabled && !generating;
            }
            if (_slider != null)
            {
                _slider.interactable = OpenWatersSettings.Enabled && !generating;
            }
            if (_biomeSlider != null)
            {
                _biomeSlider.interactable = OpenWatersSettings.Enabled && !generating;
            }
            if (_mountainsToggle != null)
            {
                _mountainsToggle.interactable = OpenWatersSettings.Enabled && !generating;
            }
            if (_previewToggle != null)
            {
                _previewToggle.interactable = OpenWatersSettings.Enabled && !generating;
            }
            if (_enabledToggle != null)
            {
                _enabledToggle.interactable = !generating;
            }
            if (_biomeViewButton != null)
            {
                _biomeViewButton.interactable = OpenWatersSettings.Enabled && !generating && _currentPreviewEntry != null;
            }

            FejdStartup startup = FejdStartup.instance;
            if (startup != null && startup.m_newWorldSeed != null)
            {
                startup.m_newWorldSeed.interactable = !generating;
            }
        }

        private void OnEnabledChanged(bool enabled)
        {
            OpenWatersPlugin.SetEnabled(enabled);
            UpdateEnabledState();
        }

        private void UpdateEnabledState()
        {
            bool enabled = OpenWatersSettings.Enabled;
            if (_slider != null)
            {
                _slider.interactable = enabled && !_previewGenerating;
            }
            if (_sliderHeader != null)
            {
                _sliderHeader.gameObject.SetActive(enabled);
            }
            if (_sliderRow != null)
            {
                _sliderRow.gameObject.SetActive(enabled);
            }
            if (_titleRow != null)
            {
                _titleRow.gameObject.SetActive(enabled);
            }
            if (_startingIslandHeader != null)
            {
                _startingIslandHeader.gameObject.SetActive(enabled);
            }
            if (_startingIslandRow != null)
            {
                _startingIslandRow.gameObject.SetActive(enabled);
            }
            if (_mountainsRow != null)
            {
                _mountainsRow.gameObject.SetActive(enabled);
            }
            if (_biomeRow != null)
            {
                _biomeRow.gameObject.SetActive(enabled);
            }
            if (_previewRow != null)
            {
                _previewRow.gameObject.SetActive(enabled);
            }
            if (_mountainsToggle != null)
            {
                _mountainsToggle.interactable = enabled && !_previewGenerating;
            }
            if (_biomeSlider != null)
            {
                _biomeSlider.interactable = enabled && !_previewGenerating;
            }
            if (_previewToggle != null)
            {
                _previewToggle.interactable = enabled && !_previewGenerating;
            }
            if (_previewButton != null)
            {
                _previewButton.interactable = enabled && !_previewGenerating;
            }
            if (_biomeViewButton != null)
            {
                _biomeViewButton.interactable = enabled && !_previewGenerating && _currentPreviewEntry != null;
            }
            if (_enabledToggle != null)
            {
                _enabledToggle.interactable = !_previewGenerating;
            }
            if (_previewWindow != null)
            {
                bool visible = enabled && OpenWatersSettings.PreviewEnabled;
                if (_previewWindow.gameObject.activeSelf != visible)
                {
                    _previewWindow.gameObject.SetActive(visible);
                }
            }
        }


        private void AlignPreviewToggle()
        {
            AlignToggle(_previewToggle, _previewLabel);
            AlignToggle(_enabledToggle, _enabledLabel);
            AlignToggle(_mountainsToggle, _mountainsLabel);
        }

        private static void AlignToggle(Toggle toggle, TextMeshProUGUI label)
        {
            if (toggle == null || label == null)
            {
                return;
            }

            Graphic visual = toggle.graphic != null
                ? (Graphic)toggle.graphic
                : toggle.targetGraphic;
            if (visual == null)
            {
                return;
            }

            Vector3[] corners = new Vector3[4];
            visual.rectTransform.GetWorldCorners(corners);
            float visualCenter = (corners[0].y + corners[2].y) * 0.5f;
            label.rectTransform.GetWorldCorners(corners);
            float labelCenter = (corners[0].y + corners[2].y) * 0.5f;

            Transform toggleTransform = toggle.transform;
            Vector3 position = toggleTransform.position;
            toggleTransform.position = new Vector3(position.x, position.y + labelCenter - visualCenter, position.z);
        }

    }
}
