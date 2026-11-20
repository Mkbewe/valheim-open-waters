using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace OpenWaters
{
    internal sealed class PreviewService
    {
        private const float AnalysisWaterEdge = 500f;

        private WorldGenerator _generator;
        private World _generatorWorld;
        private string _generatorKey;

        public IEnumerator Generate(string seed, string key, PreviewResult result)
        {
            long started = DateTime.UtcNow.Ticks;
            bool initialized = false;
            WorldSettings settings = WorldSettingsStore.FromCurrentSettings(seed);
            string generatorKey = GetGeneratorKey(settings, seed);
            try
            {
                bool useCache = _generator != null &&
                                _generatorKey == generatorKey &&
                                SetGeneratorInstance(_generator);
                if (useCache)
                {
                    NoiseModifier.SetWorld(_generatorWorld, settings);
                    GenerationSession.Current.DeferIslandAnalysis = true;
                    try
                    {
                        NoiseModifier.Build(_generator);
                        initialized = true;
                    }
                    finally
                    {
                        GenerationSession.Current.DeferIslandAnalysis = false;
                    }
                }
                else
                {
                    _generator = null;
                    _generatorWorld = null;
                    _generatorKey = null;
                    GenerationSession.DeferNextSession();
                    try
                    {
                        World previewWorld = new World(OpenWatersSettings.PreviewWorldName, seed);
                        WorldGenerator.Initialize(previewWorld);
                        _generator = WorldGenerator.instance;
                        _generatorWorld = previewWorld;
                        _generatorKey = generatorKey;
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

                if (settings.ExcludeMountains || settings.StartBiomeLimit != 0)
                {
                    GenerationSession.Current.BeginAnalysis();
                    bool begun = false;
                    try
                    {
                        begun = IslandAnalysis.Begin(
                            WorldGenerator.instance,
                            settings.WorldRadius + AnalysisWaterEdge);
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

                IEnumerator render = PreviewRenderer.Render(result, settings);
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

            long elapsedMs = (DateTime.UtcNow.Ticks - started) / TimeSpan.TicksPerMillisecond;
            OpenWatersPlugin.Log.LogInfo(
                $"[Preview] Seed='{seed}', key='{key}', islands={result.Islands}, " +
                $"components={result.Components}, land={result.LandPercent:P1}, time={elapsedMs} ms.");
        }

        private static string GetGeneratorKey(WorldSettings settings, string seed)
        {
            StringBuilder key = new StringBuilder(seed);
            key.Append('|').Append(settings.IslandAmount);
            key.Append('|').Append(settings.WorldRadius);
            key.Append('|').Append(settings.StartBiomeLimit);
            key.Append('|').Append(settings.ExcludeMountains ? 1 : 0);
            AppendConfigValue(key, settings.FewLandFraction);
            AppendConfigValue(key, settings.FewFrequencyScale);
            AppendConfigValue(key, settings.FewPeakProtection);
            AppendConfigValue(key, settings.FewDetailDamping);
            AppendConfigValue(key, settings.FewMountainCompressionStart);
            AppendConfigValue(key, settings.FewMountainCompressionFactor);
            AppendConfigValue(key, settings.MediumLandFraction);
            AppendConfigValue(key, settings.MediumFrequencyScale);
            AppendConfigValue(key, settings.MediumPeakProtection);
            AppendConfigValue(key, settings.MediumDetailDamping);
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
    }
}
