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
        private const float ButtonsOffset = 107f;
        private const float ExtraPanelHeight = 45f;
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
        private static Sprite _whiteSprite;

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

            PositionButton(startup.m_newWorldDone.GetComponent<RectTransform>(), seed);
            Transform cancel = parent.Find("Cancel");
            if (cancel != null)
            {
                PositionButton(cancel as RectTransform, seed);
            }
        }

        private void ApplyPanelLayout(FejdStartup startup, RectTransform panel)
        {
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
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Center;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
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
