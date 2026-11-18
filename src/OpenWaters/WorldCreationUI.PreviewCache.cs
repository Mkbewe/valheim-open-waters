using System.Collections.Generic;
using UnityEngine;

namespace OpenWaters
{
    internal sealed partial class WorldCreationUI
    {
        private sealed class PreviewCacheEntry
        {
            public string Key;
            public Texture2D Texture;
            public Texture2D BiomeTexture;
            public int Islands;
        }

        private readonly List<PreviewCacheEntry> _previewCache = new List<PreviewCacheEntry>();
        private string _lastPreviewKey;
        private void UpdatePreviewCacheState()
        {
            if (!OpenWatersSettings.Enabled || _previewGenerating || _previewImage == null || _previewTitle == null || _previewHint == null)
            {
                return;
            }

            FejdStartup startup = FejdStartup.instance;
            if (startup == null || startup.m_newWorldSeed == null)
            {
                return;
            }

            string key = GetPreviewKey(startup.m_newWorldSeed.text);
            if (key == _lastPreviewKey)
            {
                return;
            }
            _lastPreviewKey = key;

            if (TryGetCachedPreview(key, out PreviewCacheEntry entry))
            {
                ShowCachedPreview(entry);
            }
            else
            {
                ShowPreviewPlaceholder();
            }
        }

        private static string GetPreviewKey(string seed)
        {
            return $"{seed}|{(int)OpenWatersSettings.IslandAmount}|{OpenWatersSettings.WorldRadius}|" +
                   $"{OpenWatersSettings.BiomeLimit}|{OpenWatersSettings.StartBiomeLimit}|" +
                   $"{OpenWatersSettings.ExcludeMountains}";
        }

        private bool TryGetCachedPreview(string key, out PreviewCacheEntry entry)
        {
            for (int i = 0; i < _previewCache.Count; i++)
            {
                if (_previewCache[i].Key != key)
                {
                    continue;
                }
                if (_previewCache[i].Texture == null)
                {
                    _previewCache.RemoveAt(i);
                    break;
                }
                entry = _previewCache[i];
                _previewCache.RemoveAt(i);
                _previewCache.Add(entry);
                return true;
            }
            entry = null;
            return false;
        }

        private void ShowCachedPreview(PreviewCacheEntry entry)
        {
            if (entry == null || entry.Texture == null)
            {
                ShowPreviewPlaceholder();
                return;
            }

            bool showBiome = _biomeView && entry.BiomeTexture != null;
            Texture2D texture = showBiome ? entry.BiomeTexture : entry.Texture;
            if (texture == null)
            {
                ShowPreviewPlaceholder();
                return;
            }

            _currentPreviewEntry = entry;
            _previewImage.texture = texture;
            _previewImage.enabled = true;
            _previewTitle.text = showBiome
                ? $"Biome preview - {entry.Islands} islands"
                : $"Preview - {entry.Islands} islands";
            _previewHint.gameObject.SetActive(false);
            if (_biomeViewButton != null)
            {
                _biomeViewButton.interactable = OpenWatersSettings.Enabled && !_previewGenerating;
            }
        }

        private void ShowPreviewPlaceholder()
        {
            _currentPreviewEntry = null;
            _previewImage.enabled = false;
            _previewTitle.text = _biomeView ? "Biome preview" : "Map preview";
            _previewHint.gameObject.SetActive(true);
            _previewHint.text = "Preview will appear here";
            if (_biomeViewButton != null)
            {
                _biomeViewButton.interactable = false;
            }
        }

        private void StoreCachedPreview(PreviewCacheEntry entry)
        {
            _previewCache.Add(entry);
            while (_previewCache.Count > 8)
            {
                int evictIndex = -1;
                for (int i = 0; i < _previewCache.Count; i++)
                {
                    if (_previewCache[i] != _currentPreviewEntry)
                    {
                        evictIndex = i;
                        break;
                    }
                }
                if (evictIndex < 0)
                {
                    break;
                }

                PreviewCacheEntry oldest = _previewCache[evictIndex];
                _previewCache.RemoveAt(evictIndex);
                DestroyTextureIfUnused(oldest.Texture);
                DestroyTextureIfUnused(oldest.BiomeTexture);
            }
        }

        private void DestroyTextureIfUnused(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }
            if (_previewImage != null && _previewImage.texture == texture)
            {
                return;
            }
            if (_currentPreviewEntry != null &&
                (_currentPreviewEntry.Texture == texture ||
                 _currentPreviewEntry.BiomeTexture == texture))
            {
                return;
            }
            UnityEngine.Object.Destroy(texture);
        }

    }
}