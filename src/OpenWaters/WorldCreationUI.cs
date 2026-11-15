using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenWaters
{
    internal sealed class WorldCreationUI : MonoBehaviour
    {
        private const float PanelMargin = 36f;
        private const float HeaderOffset = 25f;
        private const float SubHeaderOffset = 55f;
        private const float SliderOffset = 86f;
        private const float ButtonsOffset = 137f;
        private const float PreviewOffset = 119f;
        private const float ExtraPanelHeight = 75f;
        private const float SliderRowHeight = 30f;
        private const float LabelWidth = 110f;
        private const float ValueWidth = 100f;
        private const float Gap = 8f;
        private const float BarHeight = 10f;
        private const float HandleSize = 18f;
        private const float SliderEndPadding = 20f;
        private const float PreviewWindowWidth = 360f;
        private const float PreviewWindowHeight = 420f;
        private const float PreviewWindowMargin = 24f;
        private const float PreviewImageSize = 320f;
        private const int PreviewSamples = 512;
        private const float PreviewWaterEdge = 500f;
        private const float PreviewMinIslandAreaKm2 = 0.05f;
        private const float PreviewMajorIslandAreaKm2 = 0.30f;
        private const float PreviewIslandClusterDistance = 500f;

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Color Orange = new Color(1f, 0.714f, 0.361f, 1f);
        private static readonly Color LineColor = new Color(1f, 0.714f, 0.361f, 0.85f);
        private static readonly Color ValueColor = new Color(0.9f, 0.86f, 0.78f, 1f);
        private static readonly string[] IslandLabels = { "Few", "Medium", "Vanilla" };
        private static Sprite _whiteSprite;

        private struct LandComponent
        {
            public List<int> Cells;
            public int MinX;
            public int MaxX;
            public int MinY;
            public int MaxY;
        }

        private RectTransform _section;
        private RectTransform _sliderRow;
        private RectTransform _previewRow;
        private RectTransform _previewWindow;
        private RawImage _previewImage;
        private TextMeshProUGUI _previewTitle;
        private TextMeshProUGUI _previewHint;
        private Texture2D _previewTexture;
        private RectTransform _sliderRect;
        private TextMeshProUGUI _sliderHeader;
        private TextMeshProUGUI _valueLabel;
        private TextMeshProUGUI _previewLabel;
        private Slider _slider;
        private Toggle _previewToggle;
        private Button _previewButton;
        private bool _previewGenerating;
        private RectTransform _layoutParent;
        private TextMeshProUGUI _header;
        private RectTransform _lineLeft;
        private RectTransform _lineRight;

        private void Update()
        {
            FejdStartup startup = FejdStartup.instance;
            if (startup == null || startup.m_createWorldPanel == null || !startup.m_createWorldPanel.activeInHierarchy)
            {
                return;
            }
            if (startup.m_newWorldSeed == null || startup.m_newWorldDone == null)
            {
                return;
            }

            if (_section == null)
            {
                CreateSection(startup);
            }
            if (_section != null)
            {
                PositionAll(startup);
            }
        }

        private void CreateSection(FejdStartup startup)
        {
            RectTransform seed = startup.m_newWorldSeed.GetComponent<RectTransform>();
            Transform parent = FindCommonAncestor(seed.transform, startup.m_newWorldDone.transform) ?? startup.m_createWorldPanel.transform;
            ApplyPanelLayout(startup, parent as RectTransform);

            _section = CreateContainer(parent, "OpenWaters_Section");
            _section.sizeDelta = new Vector2(seed.rect.width, 26f);

            TMP_Text titleSource = FindSourceLabel(startup);
            _header = CreateText(_section, "Label", "Open Waters", titleSource, Orange, 22f, TextAlignmentOptions.Center);
            if (titleSource != null)
            {
                _header.fontStyle = titleSource.fontStyle;
                _header.outlineWidth = titleSource.outlineWidth;
                _header.outlineColor = titleSource.outlineColor;
            }
            _header.rectTransform.sizeDelta = new Vector2(160f, 26f);
            _lineLeft = CreateLine(_section, "LineLeft");
            _lineRight = CreateLine(_section, "LineRight");

            CreateSliderRow(startup, parent);
            CreatePreviewRow(startup, parent);
            CreatePreviewWindow(startup, parent);
            OpenWatersPlugin.Log.LogInfo($"[UI] Created Open Waters section under '{parent.name}'.");
        }

        private void CreateSliderRow(FejdStartup startup, Transform parent)
        {
            TMP_Text titleSource = FindSourceLabel(startup);
            TMP_Text labelSource = FindText(startup, "Seed") ?? titleSource;
            int initial = Mathf.Clamp(OpenWatersSettings.IslandAmount, 0, IslandLabels.Length - 1);

            _sliderHeader = CreateText(parent, "Header", "Number of islands", titleSource, Orange, 20f, TextAlignmentOptions.Center);
            _sliderHeader.rectTransform.sizeDelta = new Vector2(400f, 22f);

            _sliderRow = CreateContainer(parent, "OpenWaters_IslandsRow");
            _sliderRow.sizeDelta = new Vector2(400f, SliderRowHeight);

            TextMeshProUGUI rowLabel = CreateText(_sliderRow, "Label", "Amount", labelSource, Orange, 20f, TextAlignmentOptions.Left);
            SetAnchor(rowLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(LabelWidth, 26f));

            _valueLabel = CreateText(_sliderRow, "Value", IslandLabels[initial], titleSource, ValueColor, 20f, TextAlignmentOptions.Right);
            SetAnchor(_valueLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(ValueWidth, 26f));

            _slider = BuildSlider(startup, _sliderRow);
            if (_slider == null)
            {
                OpenWatersPlugin.Log.LogWarning("[UI] Could not find a World Modifiers slider to base on.");
                return;
            }

            _sliderRect = (RectTransform)_slider.transform;
            SetAnchor(_sliderRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(250f, 20f));
            _sliderRect.anchoredPosition = new Vector2(LabelWidth + Gap, 0f);

            _slider.minValue = 0f;
            _slider.maxValue = IslandLabels.Length - 1;
            _slider.wholeNumbers = true;
            _slider.value = initial;
            _slider.onValueChanged.AddListener(OnIslandsChanged);
            OnIslandsChanged(initial);
        }

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

        private static Button BuildButton(FejdStartup startup, Transform parent)
        {
            Button source = startup.m_newWorldDone;
            if (source == null)
            {
                return null;
            }

            Image backgroundSource = source.targetGraphic as Image;
            if (backgroundSource == null)
            {
                backgroundSource = source.GetComponent<Image>();
            }
            if (backgroundSource == null)
            {
                backgroundSource = source.GetComponentInChildren<Image>(true);
            }
            if (backgroundSource == null)
            {
                return null;
            }

            TMP_Text labelSource = source.GetComponentInChildren<TMP_Text>(true);

            GameObject rootObject = new GameObject("GeneratePreviewButton", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform root = (RectTransform)rootObject.transform;
            root.SetParent(parent, false);

            Image background = rootObject.GetComponent<Image>();
            background.sprite = backgroundSource.sprite != null ? backgroundSource.sprite : WhiteSprite;
            background.color = backgroundSource.color;
            background.pixelsPerUnitMultiplier = backgroundSource.pixelsPerUnitMultiplier;
            background.material = backgroundSource.material;
            background.type = background.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            background.raycastTarget = true;

            Button button = rootObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.transition = source.transition;
            button.colors = source.colors;
            button.spriteState = source.spriteState;
            button.animationTriggers = source.animationTriggers;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            Color labelColor = labelSource != null ? labelSource.color : ValueColor;
            TextMeshProUGUI label = CreateText(root, "Text", "Generate preview", labelSource, labelColor, 20f, TextAlignmentOptions.Center);
            SetAnchor(label.rectTransform, Center, Center, new Vector2(200f, 28f));

            OpenWatersPlugin.Log.LogInfo($"[UI] Built preview button from '{source.name}'.");
            return button;
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

            StartCoroutine(GeneratePreviewCoroutine(startup.m_newWorldSeed.text));
        }

        private IEnumerator GeneratePreviewCoroutine(string seed)
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
                World previewWorld = new World("OpenWatersPreview", seed);
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

            if (_previewTexture != null)
            {
                UnityEngine.Object.Destroy(_previewTexture);
            }
            _previewTexture = texture;
            if (_previewImage != null)
            {
                _previewImage.texture = _previewTexture;
                _previewImage.enabled = true;
            }
            if (_previewTitle != null)
            {
                _previewTitle.text = $"Preview - {islands} islands";
            }
            if (_previewHint != null)
            {
                _previewHint.gameObject.SetActive(false);
            }

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
        }

        private static Toggle BuildToggle(FejdStartup startup, Transform parent)
        {
            Toggle source = startup.m_crossplayServerToggle != null
                ? startup.m_crossplayServerToggle
                : startup.m_samePlatformOnlyToggleWorldPanel;
            if (source == null)
            {
                return null;
            }

            GameObject holder = new GameObject("OpenWaters_ToggleHolder", typeof(RectTransform));
            holder.SetActive(false);
            try
            {
                GameObject clone = UnityEngine.Object.Instantiate(source.gameObject, holder.transform, false);
                clone.name = "PreviewToggle";

                foreach (TMP_Text text in clone.GetComponentsInChildren<TMP_Text>(true))
                {
                    UnityEngine.Object.DestroyImmediate(text.gameObject);
                }

                Toggle toggle = clone.GetComponent<Toggle>();
                if (toggle == null)
                {
                    toggle = clone.GetComponentInChildren<Toggle>(true);
                }
                if (toggle == null)
                {
                    return null;
                }

                toggle.onValueChanged.RemoveAllListeners();
                toggle.interactable = true;
                Navigation navigation = toggle.navigation;
                navigation.mode = Navigation.Mode.None;
                toggle.navigation = navigation;

                clone.transform.SetParent(parent, false);
                clone.SetActive(true);
                OpenWatersPlugin.Log.LogInfo($"[UI] Cloned preview toggle from '{source.name}'.");
                return toggle;
            }
            finally
            {
                UnityEngine.Object.Destroy(holder);
            }
        }

        private void PositionAll(FejdStartup startup)
        {
            RectTransform parent = _section.parent as RectTransform;
            RectTransform seed = startup.m_newWorldSeed.GetComponent<RectTransform>();
            GetMetrics(parent, seed, out float x, out float seedBottom, out float scale, out float width);
            float z = parent.position.z;

            Place(_section, x, seedBottom - HeaderOffset * scale, width, 26f, z);

            float labelWidth = Mathf.Max(120f, _header.preferredWidth + 16f);
            _header.rectTransform.sizeDelta = new Vector2(labelWidth, 26f);
            float lineWidth = Mathf.Max(10f, (width - labelWidth) * 0.5f - 8f);
            SetLine(_lineLeft, -(labelWidth * 0.5f + 6f + lineWidth * 0.5f), lineWidth);
            SetLine(_lineRight, labelWidth * 0.5f + 6f + lineWidth * 0.5f, lineWidth);

            if (_sliderHeader != null)
            {
                Place(_sliderHeader.rectTransform, x, seedBottom - SubHeaderOffset * scale, width, 22f, z);
            }
            if (_sliderRow != null)
            {
                Place(_sliderRow, x, seedBottom - SliderOffset * scale, width, SliderRowHeight, z);
            }
            if (_sliderRect != null)
            {
                float sliderWidth = Mathf.Max(140f, width - LabelWidth - ValueWidth - Gap * 2f);
                _sliderRect.sizeDelta = new Vector2(sliderWidth, 20f);
            }
            if (_previewRow != null)
            {
                Place(_previewRow, x, seedBottom - PreviewOffset * scale, width, 30f, z);
            }

            if (_previewWindow != null)
            {
                Vector3[] panelCorners = new Vector3[4];
                parent.GetWorldCorners(panelCorners);
                float worldWidth = PreviewWindowWidth * scale;
                float worldHeight = PreviewWindowHeight * scale;
                float worldMargin = PreviewWindowMargin * scale;
                _previewWindow.sizeDelta = new Vector2(PreviewWindowWidth, PreviewWindowHeight);
                _previewWindow.position = new Vector3(
                    panelCorners[2].x + worldMargin + worldWidth * 0.5f,
                    panelCorners[1].y - worldHeight * 0.5f,
                    z);

                bool visible = OpenWatersSettings.PreviewEnabled;
                if (_previewWindow.gameObject.activeSelf != visible)
                {
                    _previewWindow.gameObject.SetActive(visible);
                }
            }

            AlignPreviewToggle();

            PositionButton(startup.m_newWorldDone.GetComponent<RectTransform>(), seed);
            Transform cancel = parent.Find("Cancel");
            if (cancel != null)
            {
                PositionButton(cancel as RectTransform, seed);
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

        private void ApplyPanelLayout(FejdStartup startup, RectTransform panel)        {
            if (panel == null || _layoutParent == panel)
            {
                return;
            }
            _layoutParent = panel;

            panel.sizeDelta += new Vector2(0f, ExtraPanelHeight);
            panel.anchoredPosition -= new Vector2(0f, ExtraPanelHeight * 0.5f);

            Vector2 offset = new Vector2(0f, ExtraPanelHeight * 0.5f);
            Offset(startup.m_newWorldName.GetComponent<RectTransform>(), offset);
            Offset(startup.m_newWorldSeed.GetComponent<RectTransform>(), offset);
        }

        private static void Offset(RectTransform rect, Vector2 offset)
        {
            if (rect != null)
            {
                rect.anchoredPosition += offset;
            }
        }

        private static void PositionButton(RectTransform button, RectTransform seed)
        {
            if (button == null || seed == null)
            {
                return;
            }

            Vector3[] corners = new Vector3[4];
            seed.GetWorldCorners(corners);
            float targetTop = corners[0].y - ButtonsOffset * WorldToUi(seed, corners);

            button.GetWorldCorners(corners);
            Vector3 position = button.position;
            button.position = new Vector3(position.x, position.y + targetTop - corners[1].y, position.z);
        }

        private static void GetMetrics(RectTransform parent, RectTransform seed, out float centerX, out float seedBottom, out float scale, out float width)
        {
            Vector3[] corners = new Vector3[4];
            parent.GetWorldCorners(corners);
            centerX = (corners[0].x + corners[2].x) * 0.5f;

            seed.GetWorldCorners(corners);
            seedBottom = corners[0].y;
            scale = WorldToUi(seed, corners);
            width = Mathf.Max(200f, parent.rect.width - PanelMargin);
        }

        private static Slider BuildSlider(FejdStartup startup, Transform parent)
        {
            Slider source = FindSourceSlider(startup);
            if (source == null)
            {
                return null;
            }

            Image fillSource = GetImage(source.fillRect);
            Image handleSource = GetImage(source.handleRect);
            Image backgroundSource = null;
            foreach (Image image in source.GetComponentsInChildren<Image>(true))
            {
                if (image == fillSource || image == handleSource)
                {
                    continue;
                }
                if (backgroundSource == null)
                {
                    backgroundSource = image;
                }
                if (image.name.ToLowerInvariant().Contains("background"))
                {
                    backgroundSource = image;
                    break;
                }
            }
            backgroundSource = backgroundSource != null ? backgroundSource : fillSource ?? handleSource;

            GameObject rootObject = new GameObject("IslandsSlider", typeof(RectTransform), typeof(Slider));
            RectTransform root = (RectTransform)rootObject.transform;
            root.SetParent(parent, false);

            Image background = CreateImage(root, "Background", backgroundSource);
            SetBarRect(background.rectTransform, BarHeight);

            RectTransform fillArea = CreateRect(root, "Fill Area");
            SetBarRect(fillArea, BarHeight);
            fillArea.sizeDelta = new Vector2(-SliderEndPadding, BarHeight);

            Image fill = CreateImage(fillArea, "Fill", fillSource);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            RectTransform handleArea = CreateRect(root, "Handle Slide Area");
            SetBarRect(handleArea, HandleSize);
            handleArea.sizeDelta = new Vector2(-SliderEndPadding, HandleSize);

            Image handle = CreateImage(handleArea, "Handle", handleSource);
            handle.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            handle.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            handle.rectTransform.pivot = Center;
            handle.rectTransform.sizeDelta = new Vector2(HandleSize, HandleSize);
            handle.rectTransform.anchoredPosition = Vector2.zero;

            Slider slider = rootObject.GetComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };

            OpenWatersPlugin.Log.LogInfo($"[UI] Built islands slider from '{source.name}'.");
            return slider;
        }

        private static Slider FindSourceSlider(FejdStartup startup)
        {
            if (startup.m_serverOptions != null)
            {
                foreach (KeySlider keySlider in startup.m_serverOptions.GetComponentsInChildren<KeySlider>(true))
                {
                    Slider slider = keySlider.GetComponentInParent<Slider>() ?? keySlider.GetComponentInChildren<Slider>(true);
                    if (slider != null)
                    {
                        return slider;
                    }
                }

                Slider[] sliders = startup.m_serverOptions.GetComponentsInChildren<Slider>(true);
                if (sliders.Length > 0)
                {
                    return sliders[0];
                }
            }

            foreach (Slider slider in Resources.FindObjectsOfTypeAll<Slider>())
            {
                if (slider != null && slider.gameObject != null && slider.gameObject.scene.IsValid() && slider.name != "IslandsSlider")
                {
                    return slider;
                }
            }
            return null;
        }

        private static Image GetImage(RectTransform rect)
        {
            if (rect == null)
            {
                return null;
            }
            Image image = rect.GetComponent<Image>();
            return image != null ? image : rect.GetComponentInChildren<Image>(true);
        }

        private void OnIslandsChanged(float value)
        {
            int index = Mathf.Clamp(Mathf.RoundToInt(value), 0, IslandLabels.Length - 1);
            OpenWatersSettings.IslandAmount = index;
            if (_valueLabel != null)
            {
                _valueLabel.text = IslandLabels[index];
            }
        }

        private static RectTransform CreateContainer(Transform parent, string name)
        {
            GameObject containerObject = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            RectTransform rect = (RectTransform)containerObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Center;
            containerObject.GetComponent<LayoutElement>().ignoreLayout = true;
            return rect;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            GameObject rectObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)rectObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform CreateLine(Transform parent, string name)
        {
            GameObject lineObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            RectTransform rect = (RectTransform)lineObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Center;

            RawImage image = lineObject.GetComponent<RawImage>();
            image.texture = Texture2D.whiteTexture;
            image.color = LineColor;
            image.raycastTarget = false;
            return rect;
        }

        private static Image CreateImage(Transform parent, string name, Image source)
        {
            RectTransform rect = CreateRect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            if (source != null)
            {
                image.sprite = source.sprite;
                image.color = source.color;
                image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                image.fillCenter = source.fillCenter;
                image.material = source.material;
            }
            if (image.sprite == null)
            {
                image.sprite = WhiteSprite;
            }
            image.type = image.sprite != null && image.sprite.border != Vector4.zero
                ? Image.Type.Sliced
                : Image.Type.Simple;
            image.raycastTarget = true;
            return image;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string text,
            TMP_Text source,
            Color color,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.SetActive(false);
            RectTransform rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Center;

            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            if (source != null)
            {
                label.font = source.font;
                label.fontMaterial = source.fontMaterial;
            }
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            textObject.SetActive(true);
            label.ForceMeshUpdate();
            return label;
        }

        private static void SetAnchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void SetBarRect(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = Center;
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void SetLine(RectTransform line, float x, float width)
        {
            line.anchoredPosition = new Vector2(x, 0f);
            line.sizeDelta = new Vector2(width, 2f);
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height, float z)
        {
            rect.sizeDelta = new Vector2(width, height);
            rect.position = new Vector3(x, y, z);
        }

        private static float WorldToUi(RectTransform rect, Vector3[] corners)
        {
            float worldWidth = Mathf.Abs(corners[2].x - corners[0].x);
            return worldWidth > 0.0001f ? worldWidth / Mathf.Max(0.0001f, rect.rect.width) : 1f;
        }

        private static TMP_Text FindText(FejdStartup startup, string text)
        {
            foreach (TMP_Text candidate in startup.m_createWorldPanel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (candidate.text == text)
                {
                    return candidate;
                }
            }
            return null;
        }

        private static TMP_Text FindSourceLabel(FejdStartup startup)
        {
            TMP_Text fallback = null;
            foreach (TMP_Text text in startup.m_createWorldPanel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font == null)
                {
                    continue;
                }
                if (text.text == "New World")
                {
                    return text;
                }
                if (fallback == null || text.fontSize > fallback.fontSize)
                {
                    fallback = text;
                }
            }
            return fallback;
        }

        private static Transform FindCommonAncestor(Transform first, Transform second)
        {
            for (Transform parent = first.parent; parent != null; parent = parent.parent)
            {
                if (second.IsChildOf(parent))
                {
                    return parent;
                }
            }
            return null;
        }

        private static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    _whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Center);
                }
                return _whiteSprite;
            }
        }
    }
}
