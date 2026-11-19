using System;
using System.Collections;
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
        private readonly PreviewService _previewService = new PreviewService();

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

            PreviewResult result = new PreviewResult();
            IEnumerator generation = _previewService.Generate(seed, key, result);
            while (true)
            {
                bool moved = false;
                try
                {
                    moved = generation.MoveNext();
                }
                catch (Exception exception)
                {
                    OpenWatersPlugin.Log.LogError($"[Preview] Generation failed: {exception}");
                    break;
                }
                if (!moved)
                {
                    break;
                }
                yield return null;
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
