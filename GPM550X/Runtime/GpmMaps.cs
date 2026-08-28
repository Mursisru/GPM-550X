using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace Gpm.Runtime
{
    internal static class GpmMaps
    {
        private const string MatMain = "MaterialGlossyPurple-Blue(Main)";
        private const string MatFc = "MaterialShrapnessMetal(FlightController)";
        private const string MatInside = "MaterialWhileNormalsAutomapping(InsideMain)";

        private static readonly Dictionary<string, string> AlbedoFile =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { MatMain, MatMain + " Color.png" },
                { MatFc, MatFc + " Color.png" },
                { MatInside, MatInside + " Color.png" }
            };

        private static readonly Dictionary<string, string> NormalFile =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { MatMain, MatMain + " Normal.png" },
                { MatFc, MatFc + " Normal.png" },
                { MatInside, MatInside + " Normal.png" }
            };

        private static readonly Dictionary<string, string> FoldToMat =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Texture2D> Cache =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        private static string? _dir;
        private static bool _dirTried;

        static GpmMaps()
        {
            Register(MatMain);
            Register(MatFc);
            Register(MatInside);
            FoldToMat["main"] = MatMain;
            FoldToMat["decal"] = MatMain;
            FoldToMat["decal1"] = MatMain;
            FoldToMat["decal2"] = MatMain;
            FoldToMat["decal3"] = MatMain;
            FoldToMat["decal4"] = MatMain;
            FoldToMat["decal5"] = MatMain;
            FoldToMat["decal6"] = MatMain;
            FoldToMat["flightcontroller"] = MatFc;
            FoldToMat["insidemain"] = MatInside;
            FoldToMat["aerodynamicstabilizator"] = MatMain;
        }

        internal static string ResolveMatKey(string? matName, string? goName)
        {
            string? fromMat = Match(matName);
            if (fromMat != null)
                return fromMat;
            string? fromGo = Match(goName);
            return fromGo ?? MatMain;
        }

        internal static Texture2D? Albedo(string blenderMatName) =>
            LoadNamed(blenderMatName, AlbedoFile, linear: false, suffix: "_albedo");

        internal static Texture2D? Normal(string blenderMatName)
        {
            Texture2D? tex = LoadNamed(blenderMatName, NormalFile, linear: true, suffix: "_nml");
            if (tex != null)
                PackNormalAg(tex);
            return tex;
        }

        private static void Register(string mat)
        {
            FoldToMat[Fold(mat)] = mat;
        }

        private static string? Match(string? raw)
        {
            if (string.IsNullOrEmpty(raw))
                return null;
            string key = StripMatSuffix(raw!);
            if (AlbedoFile.ContainsKey(key))
                return key;
            string fold = Fold(key);
            if (FoldToMat.TryGetValue(fold, out string? mat))
                return mat;
            foreach (KeyValuePair<string, string> kv in FoldToMat)
            {
                if (fold.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    return kv.Value;
            }
            return null;
        }

        private static Texture2D? LoadNamed(
            string blenderMatName,
            Dictionary<string, string> table,
            bool linear,
            string suffix)
        {
            string key = ResolveMatKey(blenderMatName, null);
            string cacheKey = key + suffix;
            if (Cache.TryGetValue(cacheKey, out Texture2D hit))
                return hit;
            if (!table.TryGetValue(key, out string? file) || string.IsNullOrEmpty(file))
                return null;
            Texture2D? tex = LoadFile(file, cacheKey, linear);
            if (tex != null)
                Cache[cacheKey] = tex;
            return tex;
        }

        private static Texture2D? LoadFile(string file, string cacheKey, bool linear)
        {
            if (Cache.TryGetValue(cacheKey, out Texture2D hit))
                return hit;
            byte[]? bytes = ReadBytes(file);
            if (bytes == null || bytes.Length < 16)
                return null;
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear);
                if (!ImageConversion.LoadImage(tex, bytes, markNonReadable: false))
                    return null;
                tex.name = cacheKey;
                tex.wrapMode = TextureWrapMode.Repeat;
                tex.filterMode = FilterMode.Bilinear;
                tex.anisoLevel = 4;
                Cache[cacheKey] = tex;
                GpmPlugin.ModLog?.LogInfo($"GpmMaps loaded '{file}' {tex.width}x{tex.height}");
                return tex;
            }
            catch (Exception ex)
            {
                GpmPlugin.ModLog?.LogWarning($"GpmMaps '{file}': {ex.Message}");
                return null;
            }
        }

        private static byte[]? ReadBytes(string file)
        {
            EnsureDir();
            var paths = new List<string>(8);
            if (!string.IsNullOrEmpty(_dir))
                paths.Add(Path.Combine(_dir, file));
            try
            {
                string plugins = Paths.PluginPath;
                if (!string.IsNullOrEmpty(plugins))
                {
                    paths.Add(Path.Combine(plugins, "GPM-550X", "Textures", "GPM550X", file));
                    paths.Add(Path.Combine(plugins, "GPM550X", "Textures", "GPM550X", file));
                }
            }
            catch
            {
                // Paths unavailable
            }
            for (int i = 0; i < paths.Count; i++)
            {
                if (File.Exists(paths[i]))
                    return File.ReadAllBytes(paths[i]);
            }
            return null;
        }

        private static string StripMatSuffix(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "mat";
            string n = name;
            int i = n.LastIndexOf("_gpm", StringComparison.OrdinalIgnoreCase);
            if (i > 0)
                n = n.Substring(0, i);
            i = n.LastIndexOf(" (Instance)", StringComparison.OrdinalIgnoreCase);
            if (i > 0)
                n = n.Substring(0, i);
            return n;
        }

        private static string Fold(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            char[] buf = new char[s.Length];
            int n = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsLetterOrDigit(c))
                    buf[n++] = char.ToLowerInvariant(c);
            }
            return new string(buf, 0, n);
        }

        private static void PackNormalAg(Texture2D tex)
        {
            if (tex == null)
                return;
            string packedKey = tex.name + "_ag";
            if (Cache.ContainsKey(packedKey))
                return;
            Color32[] px = tex.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                byte x = px[i].r;
                byte y = px[i].g;
                px[i] = new Color32(255, y, 255, x);
            }
            tex.SetPixels32(px);
            tex.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            Cache[packedKey] = tex;
        }

        private static void EnsureDir()
        {
            if (_dirTried)
                return;
            _dirTried = true;
            string? plugin = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(plugin))
            {
                string local = Path.Combine(plugin, "Textures", "GPM550X");
                if (Directory.Exists(local))
                    _dir = local;
            }
            if (!string.IsNullOrEmpty(_dir))
                return;
            try
            {
                string p = Path.Combine(Paths.PluginPath, "GPM-550X", "Textures", "GPM550X");
                if (Directory.Exists(p))
                    _dir = p;
            }
            catch
            {
                // ignore
            }
        }
    }
}
