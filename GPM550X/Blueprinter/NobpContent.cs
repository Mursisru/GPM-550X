using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Gpm.Blueprinter
{
    internal static class NobpContent
    {
        private static AssetBundle? _bundle;
        private static GameObject? _visualPrefab;
        private static bool _tried;

        internal static GameObject? VisualPrefab => _visualPrefab;

        internal static void TryLoad()
        {
            if (_tried)
                return;
            _tried = true;
            try
            {
                _bundle = FindLoaded() ?? LoadFromDisk();
                if (_bundle == null)
                {
                    GpmPlugin.ModLog?.LogWarning("GPM550X.nobp missing — visual stamp skipped.");
                    return;
                }

                _visualPrefab = _bundle.LoadAsset<GameObject>(GpmConstants.MeshPrefabAsset);
                if (_visualPrefab == null)
                {
                    GameObject[] all = _bundle.LoadAllAssets<GameObject>();
                    if (all != null)
                    {
                        for (int i = 0; i < all.Length; i++)
                        {
                            GameObject go = all[i];
                            if (go == null)
                                continue;
                            if (go.name.IndexOf("Gpm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                go.name.IndexOf("GPM", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                _visualPrefab = go;
                                break;
                            }
                        }
                    }
                }

                if (_visualPrefab != null)
                    GpmPlugin.ModLog?.LogInfo($"GPM visual ready: '{_visualPrefab.name}'");
                else
                    GpmPlugin.ModLog?.LogWarning("nobp loaded but GpmVisual not found.");
            }
            catch (Exception ex)
            {
                GpmPlugin.ModLog?.LogError($"NobpContent: {ex}");
            }
        }

        private static AssetBundle? FindLoaded()
        {
            foreach (AssetBundle b in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (b == null)
                    continue;
                try
                {
                    if (b.Contains(GpmConstants.MeshPrefabAsset))
                        return b;
                }
                catch
                {
                    // ignore
                }
            }
            return null;
        }

        private static AssetBundle? LoadFromDisk()
        {
            string? path = FindNobpPath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            AssetBundle? fromFile = AssetBundle.LoadFromFile(path);
            if (fromFile != null)
            {
                GpmPlugin.ModLog?.LogInfo($"Loaded .nobp from file: {path}");
                return fromFile;
            }
            GpmPlugin.ModLog?.LogWarning($"LoadFromFile returned null: {path}");
            return null;
        }

        private static string? FindNobpPath()
        {
            string? pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(pluginDir))
                return null;
            string direct = Path.Combine(pluginDir, GpmConstants.NobpFileName);
            if (File.Exists(direct))
                return direct;
            string lower = Path.Combine(pluginDir, GpmConstants.NobpFileName.ToLowerInvariant());
            return File.Exists(lower) ? lower : null;
        }
    }
}
