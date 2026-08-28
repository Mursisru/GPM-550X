using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Gpm.Runtime
{
    internal static class GpmWeaponIcon
    {
        private static Sprite? _sprite;
        private static bool _tried;

        internal static Sprite? Get()
        {
            if (_sprite != null)
                return _sprite;
            if (_tried)
                return null;
            _tried = true;
            byte[]? bytes = ReadBytes();
            if (bytes == null || bytes.Length == 0)
                return null;
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear: false);
                tex.name = "PreviewGpm";
                tex.filterMode = FilterMode.Bilinear;
                if (!ImageConversion.LoadImage(tex, bytes, markNonReadable: false))
                {
                    UnityEngine.Object.Destroy(tex);
                    return null;
                }
                ShadeToAlpha(tex);
                tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
                _sprite = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);
                _sprite.name = "PreviewGpm";
                return _sprite;
            }
            catch (Exception ex)
            {
                GpmPlugin.ModLog?.LogWarning($"GPM preview icon: {ex.Message}");
                return null;
            }
        }

        private static void ShadeToAlpha(Texture2D tex)
        {
            int baseA = GpmConstants.PreviewIconAlphaBase;
            int darkA = baseA / 2;
            int darkLuma = GpmConstants.PreviewIconDarkLuma;
            Color32[] px = tex.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                Color32 c = px[i];
                if (c.a == 0)
                {
                    px[i] = new Color32(255, 255, 255, 0);
                    continue;
                }
                int luma = (c.r * 299 + c.g * 587 + c.b * 114) / 1000;
                if (luma < 12)
                {
                    px[i] = new Color32(255, 255, 255, 0);
                    continue;
                }
                int a = luma < darkLuma ? darkA : baseA;
                a = a * c.a / 255;
                px[i] = new Color32(255, 255, 255, (byte)a);
            }
            tex.SetPixels32(px);
        }

        private static byte[]? ReadBytes()
        {
            string? pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(pluginDir))
            {
                string path = Path.Combine(pluginDir, GpmConstants.PreviewIconFileName);
                if (File.Exists(path))
                    return File.ReadAllBytes(path);
            }
            Assembly asm = Assembly.GetExecutingAssembly();
            using Stream? s = asm.GetManifestResourceStream(GpmConstants.PreviewIconResource);
            if (s == null)
                return null;
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
