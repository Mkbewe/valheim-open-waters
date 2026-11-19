using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenWaters
{
    internal sealed partial class WorldCreationUI
    {
        private static Sprite _whiteSprite;

        private static Button BuildButton(FejdStartup startup, Transform parent, string text = "Generate preview")
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
            TextMeshProUGUI label = CreateText(root, "Text", text, labelSource, labelColor, 20f, TextAlignmentOptions.Center);
            SetAnchor(label.rectTransform, Center, Center, new Vector2(200f, 28f));

            OpenWatersPlugin.Log.LogInfo($"[UI] Built preview button from '{source.name}'.");
            return button;
        }


        private static Toggle BuildToggle(FejdStartup startup, Transform parent, string objectName = "PreviewToggle")
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
                clone.name = objectName;

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
                OpenWatersPlugin.Log.LogInfo($"[UI] Cloned toggle '{objectName}' from '{source.name}'.");
                return toggle;
            }
            finally
            {
                UnityEngine.Object.Destroy(holder);
            }
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
