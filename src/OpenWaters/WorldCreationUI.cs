using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenWaters
{
    internal sealed partial class WorldCreationUI : MonoBehaviour
    {
        private const float PanelMargin = 36f;
        private const float HeaderOffset = 25f;
        private const float SubHeaderOffset = 55f;
        private const float SliderOffset = 86f;
        private const float ButtonsOffset = 137f;
        private const float ExtraPanelHeight = 75f;
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
        private RectTransform _section;
        private RectTransform _sliderRow;
        private RectTransform _sliderRect;
        private TextMeshProUGUI _sliderHeader;
        private TextMeshProUGUI _valueLabel;
        private Slider _slider;
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
                UpdatePreviewCacheState();
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
