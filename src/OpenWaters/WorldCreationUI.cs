using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenWaters
{
    internal sealed partial class WorldCreationUI : MonoBehaviour
    {
        private const float PanelMargin = 36f;
        private const float HeaderOffset = 25f;
        private const float TitleOffset = 61f;
        private const float SubHeaderOffset = 97f;
        private const float SliderOffset = 128f;
        private const float StartingIslandHeaderOffset = 158f;
        private const float StartingIslandRowOffset = 252f;
        private const float RemoveMountainsOffset = 186f;
        private const float BiomeLimitOffset = 219f;
        private const float ButtonsOffset = 305f;
        private const float CompactButtonsOffset = 65f;
        private const float ExtraPanelHeight = 243f;
        private const float SliderRowHeight = 30f;
        private const float LabelWidth = 110f;
        private const float ValueWidth = 100f;
        private const float Gap = 8f;
        private const float BarHeight = 10f;
        private const float HandleSize = 18f;
        private const float SliderEndPadding = 20f;
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Color Orange = new Color(1f, 0.714f, 0.361f, 1f);
        private static readonly Color LineColor = new Color(1f, 0.714f, 0.361f, 0.85f);
        private static readonly Color ValueColor = new Color(0.9f, 0.86f, 0.78f, 1f);
        private static readonly string[] IslandLabels = { "Few", "Medium", "Vanilla" };
        private static readonly string[] BiomeLimitLabels = { "1", "2", "Vanilla" };
        private RectTransform _section;
        private RectTransform _sliderRow;
        private RectTransform _sliderRect;
        private TextMeshProUGUI _sliderHeader;
        private TextMeshProUGUI _valueLabel;
        private Slider _slider;
        private RectTransform _layoutParent;
        private RectTransform _layoutPanel;
        private Vector2 _basePanelSize;
        private Vector2 _basePanelPosition;
        private Vector2 _baseNamePosition;
        private Vector2 _baseSeedPosition;
        private RectTransform _seedRandomButton;
        private Vector2 _seedRandomOffset;
        private float _currentExtraHeight;
        private bool _layoutCaptured;
        private RectTransform _titleRow;
        private TextMeshProUGUI _header;
        private RectTransform _lineLeft;
        private RectTransform _lineRight;
        private TextMeshProUGUI _startingIslandHeader;
        private RectTransform _startingIslandRow;
        private RectTransform _mountainsRow;
        private Toggle _mountainsToggle;
        private TextMeshProUGUI _mountainsLabel;
        private RectTransform _biomeRow;
        private RectTransform _biomeSliderRect;
        private Slider _biomeSlider;
        private TextMeshProUGUI _biomeValueLabel;

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
                UpdatePreviewCacheState();
            }
        }

        private void CreateSection(FejdStartup startup)
        {
            RectTransform seed = startup.m_newWorldSeed.GetComponent<RectTransform>();
            Transform parent = FindCommonAncestor(seed.transform, startup.m_newWorldDone.transform) ?? startup.m_createWorldPanel.transform;
            ApplyPanelLayout(startup, parent as RectTransform);
            CreateSeedRandomButton(startup, parent);
            _baseSeedPosition = seed.anchoredPosition - new Vector2(0f, _currentExtraHeight * 0.5f);

            _section = CreateContainer(parent, "OpenWaters_Section");
            _section.sizeDelta = new Vector2(seed.rect.width, 30f);

            TMP_Text titleSource = FindSourceLabel(startup);
            _enabledToggle = BuildToggle(startup, _section, "ModToggle");
            if (_enabledToggle == null)
            {
                OpenWatersPlugin.Log.LogWarning("[UI] Could not create the Open Waters toggle.");
            }
            else
            {
                SetAnchor((RectTransform)_enabledToggle.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 30f));
                _enabledToggle.SetIsOnWithoutNotify(OpenWatersSettings.Enabled);
                _enabledToggle.onValueChanged.AddListener(OnEnabledChanged);
            }

            _enabledLabel = CreateText(_section, "Label", "Open Waters", titleSource, Orange, 16f, TextAlignmentOptions.Left);
            SetAnchor(_enabledLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(250f, 26f));
            _enabledLabel.rectTransform.anchoredPosition = new Vector2(38f, 0f);

            CreateTitleRow(startup, parent);

            CreateSliderRow(startup, parent);
            CreateStartingIslandSection(startup, parent);
            CreateMountainsRow(startup, parent);
            CreateBiomeLimitRow(startup, parent);
            CreatePreviewRow(startup, parent);
            CreatePreviewWindow(startup, parent);
            OpenWatersPlugin.Log.LogInfo($"[UI] Created Open Waters section under '{parent.name}'.");
        }

        private void CreateTitleRow(FejdStartup startup, Transform parent)
        {
            _titleRow = CreateContainer(parent, "OpenWaters_TitleRow");
            _titleRow.sizeDelta = new Vector2(400f, 26f);

            TMP_Text titleSource = FindSourceLabel(startup);
            _header = CreateText(_titleRow, "Label", "Open Waters", titleSource, Orange, 22f, TextAlignmentOptions.Center);
            _header.rectTransform.sizeDelta = new Vector2(160f, 26f);
            if (titleSource != null)
            {
                _header.fontStyle = titleSource.fontStyle;
                _header.outlineWidth = titleSource.outlineWidth;
                _header.outlineColor = titleSource.outlineColor;
            }

            _lineLeft = CreateLine(_titleRow, "LineLeft");
            _lineRight = CreateLine(_titleRow, "LineRight");
        }

        private void CreateStartingIslandSection(FejdStartup startup, Transform parent)
        {
            TMP_Text titleSource = FindSourceLabel(startup);
            _startingIslandHeader = CreateText(parent, "StartingIslandHeader", "Starting Island", titleSource, Orange, 20f, TextAlignmentOptions.Center);
            _startingIslandHeader.rectTransform.sizeDelta = new Vector2(400f, 22f);

            _startingIslandRow = CreateContainer(parent, "OpenWaters_StartingIslandRow");
            _startingIslandRow.sizeDelta = new Vector2(400f, 30f);
        }

        private void CreateMountainsRow(FejdStartup startup, Transform parent)
        {
            _mountainsRow = CreateContainer(parent, "OpenWaters_MountainsRow");
            _mountainsRow.sizeDelta = new Vector2(400f, 30f);

            _mountainsToggle = BuildToggle(startup, _mountainsRow, "MountainsToggle");
            if (_mountainsToggle == null)
            {
                OpenWatersPlugin.Log.LogWarning("[UI] Could not create the Remove mountains toggle.");
            }
            else
            {
                SetAnchor((RectTransform)_mountainsToggle.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 30f));
                _mountainsToggle.SetIsOnWithoutNotify(OpenWatersSettings.ExcludeMountains);
                _mountainsToggle.onValueChanged.AddListener(value =>
                {
                    OpenWatersSettings.ExcludeMountains = value;
                    OpenWatersPlugin.Log.LogInfo($"[UI] ExcludeMountains={value}");
                });
            }

            TMP_Text source = FindText(startup, "Seed") ?? FindSourceLabel(startup);
            TextMeshProUGUI label = CreateText(_mountainsRow, "Label", "Remove mountains", source, Orange, 16f, TextAlignmentOptions.Left);
            _mountainsLabel = label;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(250f, 26f);
            labelRect.anchoredPosition = new Vector2(38f, 0f);
        }

        private void CreateBiomeLimitRow(FejdStartup startup, Transform parent)
        {
            TMP_Text titleSource = FindSourceLabel(startup);
            TMP_Text labelSource = FindText(startup, "Seed") ?? titleSource;
            int initial = BiomeLimitToIndex(OpenWatersSettings.BiomeLimit);

            _biomeRow = CreateContainer(parent, "OpenWaters_BiomeRow");
            _biomeRow.sizeDelta = new Vector2(400f, SliderRowHeight);

            TextMeshProUGUI rowLabel = CreateText(_biomeRow, "Label", "Max biomes", labelSource, Orange, 16f, TextAlignmentOptions.Left);
            SetAnchor(rowLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(LabelWidth, 26f));

            _biomeValueLabel = CreateText(_biomeRow, "Value", BiomeLimitLabels[initial], titleSource, ValueColor, 20f, TextAlignmentOptions.Right);
            SetAnchor(_biomeValueLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(ValueWidth, 26f));

            _biomeSlider = BuildSlider(startup, _biomeRow);
            if (_biomeSlider == null)
            {
                OpenWatersPlugin.Log.LogWarning("[UI] Could not find a slider for Max biomes.");
                return;
            }

            _biomeSliderRect = (RectTransform)_biomeSlider.transform;
            SetAnchor(_biomeSliderRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(250f, 20f));
            _biomeSliderRect.anchoredPosition = new Vector2(LabelWidth + Gap, 0f);

            _biomeSlider.minValue = 0f;
            _biomeSlider.maxValue = BiomeLimitLabels.Length - 1;
            _biomeSlider.wholeNumbers = true;
            _biomeSlider.value = initial;
            _biomeSlider.onValueChanged.AddListener(OnBiomeLimitChanged);
            OnBiomeLimitChanged(initial);
        }

        private static int BiomeLimitToIndex(int biomeLimit)
        {
            if (biomeLimit == 1)
            {
                return 0;
            }
            if (biomeLimit == 2)
            {
                return 1;
            }
            return 2;
        }

        private void OnBiomeLimitChanged(float value)
        {
            int index = Mathf.Clamp(Mathf.RoundToInt(value), 0, BiomeLimitLabels.Length - 1);
            OpenWatersSettings.BiomeLimit = index == 2 ? 0 : index + 1;
            if (_biomeValueLabel != null)
            {
                _biomeValueLabel.text = BiomeLimitLabels[index];
            }
        }

        private void CreateSeedRandomButton(FejdStartup startup, Transform parent)
        {
            const string objectName = "OpenWaters_SeedRandom";
            if (parent.Find(objectName) != null)
            {
                return;
            }

            RectTransform seed = startup.m_newWorldSeed.GetComponent<RectTransform>();
            const float buttonWidth = 90f;
            const float gap = 8f;
            float originalWidth = seed.rect.width;
            float newWidth = Mathf.Max(200f, originalWidth - buttonWidth - gap);
            float shift = (originalWidth - newWidth) * 0.5f;
            seed.sizeDelta = new Vector2(newWidth, seed.sizeDelta.y);
            seed.anchoredPosition -= new Vector2(shift, 0f);

            Button button = BuildButton(startup, parent, "Random");
            if (button == null)
            {
                return;
            }

            RectTransform rect = (RectTransform)button.transform;
            rect.name = objectName;
            SetAnchor(rect, Center, Center, new Vector2(buttonWidth, seed.rect.height));
            rect.anchoredPosition = new Vector2(
                seed.anchoredPosition.x + newWidth * 0.5f + gap + buttonWidth * 0.5f,
                seed.anchoredPosition.y);
            _seedRandomButton = rect;
            _seedRandomOffset = rect.anchoredPosition - seed.anchoredPosition;

            button.onClick.AddListener(() =>
            {
                FejdStartup instance = FejdStartup.instance;
                if (instance != null && instance.m_newWorldSeed != null)
                {
                    instance.m_newWorldSeed.text = World.GenerateSeed();
                }
            });
        }

        private void CreateSliderRow(FejdStartup startup, Transform parent)        {
            TMP_Text titleSource = FindSourceLabel(startup);
            TMP_Text labelSource = FindText(startup, "Seed") ?? titleSource;
            int initial = Mathf.Clamp(OpenWatersSettings.IslandAmount, 0, IslandLabels.Length - 1);

            _sliderHeader = CreateText(parent, "Header", "Number of islands", titleSource, Orange, 20f, TextAlignmentOptions.Center);
            _sliderHeader.rectTransform.sizeDelta = new Vector2(400f, 22f);

            _sliderRow = CreateContainer(parent, "OpenWaters_IslandsRow");
            _sliderRow.sizeDelta = new Vector2(400f, SliderRowHeight);

            TextMeshProUGUI rowLabel = CreateText(_sliderRow, "Label", "Amount", labelSource, Orange, 16f, TextAlignmentOptions.Left);
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


        private void PositionAll(FejdStartup startup)
        {
            RectTransform parent = _section.parent as RectTransform;
            RectTransform seed = startup.m_newWorldSeed.GetComponent<RectTransform>();
            UpdatePanelLayout(startup, parent, OpenWatersSettings.Enabled);
            CenterPanelVertically(parent);
            GetMetrics(parent, seed, out float x, out float seedBottom, out float scale, out float width);
            float z = parent.position.z;

            Place(_section, x, seedBottom - HeaderOffset * scale, width, 30f, z);
            if (_titleRow != null)
            {
                Place(_titleRow, x, seedBottom - TitleOffset * scale, width, 26f, z);
                float labelWidth = Mathf.Max(120f, _header.preferredWidth + 16f);
                _header.rectTransform.sizeDelta = new Vector2(labelWidth, 26f);
                float lineWidth = Mathf.Max(10f, (width - labelWidth) * 0.5f - 8f);
                SetLine(_lineLeft, -(labelWidth * 0.5f + 6f + lineWidth * 0.5f), lineWidth);
                SetLine(_lineRight, labelWidth * 0.5f + 6f + lineWidth * 0.5f, lineWidth);
            }

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
            if (_startingIslandHeader != null)
            {
                Place(_startingIslandHeader.rectTransform, x, seedBottom - StartingIslandHeaderOffset * scale, width, 22f, z);
            }
            if (_startingIslandRow != null)
            {
                Place(_startingIslandRow, x, seedBottom - StartingIslandRowOffset * scale, width, 30f, z);
            }
            if (_mountainsRow != null)
            {
                Place(_mountainsRow, x, seedBottom - RemoveMountainsOffset * scale, width, 30f, z);
            }
            if (_biomeRow != null)
            {
                Place(_biomeRow, x, seedBottom - BiomeLimitOffset * scale, width, SliderRowHeight, z);
            }
            if (_biomeSliderRect != null)
            {
                float biomeSliderWidth = Mathf.Max(140f, width - LabelWidth - ValueWidth - Gap * 2f);
                _biomeSliderRect.sizeDelta = new Vector2(biomeSliderWidth, 20f);
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

                bool visible = OpenWatersSettings.Enabled && OpenWatersSettings.PreviewEnabled;
                if (_previewWindow.gameObject.activeSelf != visible)
                {
                    _previewWindow.gameObject.SetActive(visible);
                }
            }

            AlignPreviewToggle();
            UpdateEnabledState();

            PositionButton(startup.m_newWorldDone.GetComponent<RectTransform>(), seed,
                OpenWatersSettings.Enabled ? ButtonsOffset : CompactButtonsOffset);
            Transform cancel = parent.Find("Cancel");
            if (cancel != null)
            {
                PositionButton(cancel as RectTransform, seed,
                    OpenWatersSettings.Enabled ? ButtonsOffset : CompactButtonsOffset);
            }
        }


        private void ApplyPanelLayout(FejdStartup startup, RectTransform panel)
        {
            if (panel == null)
            {
                return;
            }

            if (!_layoutCaptured)
            {
                _layoutParent = panel;
                _layoutPanel = panel;
                _basePanelSize = panel.sizeDelta;
                _basePanelPosition = panel.anchoredPosition;
                _baseNamePosition = startup.m_newWorldName.GetComponent<RectTransform>().anchoredPosition;
                _baseSeedPosition = startup.m_newWorldSeed.GetComponent<RectTransform>().anchoredPosition;
                _layoutCaptured = true;
            }

            UpdatePanelLayout(startup, panel, OpenWatersSettings.Enabled);
        }

        private void UpdatePanelLayout(FejdStartup startup, RectTransform panel, bool enabled)
        {
            if (!_layoutCaptured || panel == null || panel != _layoutPanel ||
                startup.m_newWorldName == null || startup.m_newWorldSeed == null)
            {
                return;
            }

            float extraHeight = enabled ? ExtraPanelHeight : 0f;
            _currentExtraHeight = extraHeight;
            panel.sizeDelta = _basePanelSize + new Vector2(0f, extraHeight);
            panel.anchoredPosition = _basePanelPosition - new Vector2(0f, extraHeight * 0.5f);

            RectTransform name = startup.m_newWorldName.GetComponent<RectTransform>();
            RectTransform seed = startup.m_newWorldSeed.GetComponent<RectTransform>();
            name.anchoredPosition = _baseNamePosition + new Vector2(0f, extraHeight * 0.5f);
            seed.anchoredPosition = _baseSeedPosition + new Vector2(0f, extraHeight * 0.5f);

            if (_seedRandomButton != null)
            {
                _seedRandomButton.anchoredPosition = seed.anchoredPosition + _seedRandomOffset;
            }
        }

        private static void PositionButton(RectTransform button, RectTransform seed, float offset)
        {
            if (button == null || seed == null)
            {
                return;
            }

            Vector3[] corners = new Vector3[4];
            seed.GetWorldCorners(corners);
            float targetTop = corners[0].y - offset * WorldToUi(seed, corners);

            button.GetWorldCorners(corners);
            Vector3 position = button.position;
            button.position = new Vector3(position.x, position.y + targetTop - corners[1].y, position.z);
        }

        private static void CenterPanelVertically(RectTransform panel)
        {
            if (panel == null)
            {
                return;
            }

            Canvas canvas = panel.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null)
            {
                return;
            }

            Vector3[] canvasCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasCorners);
            float canvasCenterY = (canvasCorners[0].y + canvasCorners[2].y) * 0.5f;

            Vector3[] panelCorners = new Vector3[4];
            panel.GetWorldCorners(panelCorners);
            float panelCenterY = (panelCorners[0].y + panelCorners[2].y) * 0.5f;

            Vector3 position = panel.position;
            panel.position = new Vector3(position.x, position.y + canvasCenterY - panelCenterY, position.z);
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


        private void OnIslandsChanged(float value)
        {
            int index = Mathf.Clamp(Mathf.RoundToInt(value), 0, IslandLabels.Length - 1);
            OpenWatersSettings.IslandAmount = index;
            if (_valueLabel != null)
            {
                _valueLabel.text = IslandLabels[index];
            }
        }


        private static float WorldToUi(RectTransform rect, Vector3[] corners)
        {
            float worldWidth = Mathf.Abs(corners[2].x - corners[0].x);
            return worldWidth > 0.0001f ? worldWidth / Mathf.Max(0.0001f, rect.rect.width) : 1f;
        }

    }
}
