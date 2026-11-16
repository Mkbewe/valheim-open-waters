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
                   $"{OpenWatersSettings.BiomeLimit}|{OpenWatersSettings.ExcludeMountains}";
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
            if (entry.Texture == null)
            {
                ShowPreviewPlaceholder();
                return;
            }
            _previewImage.texture = entry.Texture;
            _previewImage.enabled = true;
            _previewTitle.text = $"Preview - {entry.Islands} islands";
            _previewHint.gameObject.SetActive(false);
        }

        private void ShowPreviewPlaceholder()
        {
            _previewImage.enabled = false;
            _previewTitle.text = "Map preview";
            _previewHint.gameObject.SetActive(true);
            _previewHint.text = "Preview will appear here";
        }

        private void StoreCachedPreview(PreviewCacheEntry entry)
        {
            _previewCache.Add(entry);
            while (_previewCache.Count > 8)
            {
                PreviewCacheEntry oldest = _previewCache[0];
                _previewCache.RemoveAt(0);
                if (_previewImage == null || _previewImage.texture != oldest.Texture)
                {
                    UnityEngine.Object.Destroy(oldest.Texture);
                }
            }
        }

    }
}