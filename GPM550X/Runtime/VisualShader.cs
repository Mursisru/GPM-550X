using UnityEngine;

namespace Gpm.Runtime
{
    internal static class VisualShader
    {
        private static Shader? _lit;
        private static Material? _template;

        internal static Shader Lit => _lit != null ? _lit : Resolve();
        internal static Material? Template => _template;

        internal static void PrimeFrom(GameObject? sampleRoot)
        {
            if (sampleRoot == null)
                return;
            MeshRenderer[] rs = sampleRoot.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                MeshRenderer r = rs[i];
                if (r == null)
                    continue;
                Material? mat = r.sharedMaterial;
                if (mat?.shader == null || !IsUrpMeshLit(mat.shader.name))
                    continue;
                _template = mat;
                _lit = mat.shader;
                GpmPlugin.ModLog?.LogInfo($"VisualShader: primed '{_lit.name}' from '{sampleRoot.name}/{r.name}'");
                return;
            }
        }

        internal static Material Make(string name, float cull)
        {
            Material mat = _template != null ? new Material(_template) : new Material(Resolve());
            mat.name = name;
            StripInheritedMaps(mat);
            ForceOpaqueLit(mat, cull);
            return mat;
        }

        internal static void StripInheritedMaps(Material mat)
        {
            if (mat == null)
                return;
            string[] maps =
            {
                "_BaseMap", "_MainTex", "_BaseColorMap",
                "_BumpMap", "_NormalMap", "_BentNormalMap",
                "_MetallicGlossMap", "_MaskMap",
                "_OcclusionMap", "_DetailAlbedoMap", "_DetailNormalMap",
                "_DetailMask", "_EmissionMap", "_EmissiveColorMap",
                "_ParallaxMap", "_HeightMap", "_SpecGlossMap"
            };
            for (int i = 0; i < maps.Length; i++)
            {
                if (mat.HasProperty(maps[i]))
                    mat.SetTexture(maps[i], null);
            }
        }

        private static Shader Resolve()
        {
            if (_lit != null && IsUrpMeshLit(_lit.name))
                return _lit;

            MeshRenderer[] scene = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
            for (int i = 0; i < scene.Length; i++)
            {
                MeshRenderer r = scene[i];
                if (r == null)
                    continue;
                Material? mat = r.sharedMaterial;
                if (mat?.shader == null || !IsUrpMeshLit(mat.shader.name))
                    continue;
                _template = mat;
                _lit = mat.shader;
                return _lit;
            }

            Shader? found = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Universal Render Pipeline/Simple Lit") ??
                            Shader.Find("Lit");
            if (found != null && IsUrpMeshLit(found.name))
            {
                _lit = found;
                return _lit;
            }

            _lit = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse") ?? Shader.Find("Unlit/Texture");
            if (_lit == null)
                throw new System.InvalidOperationException("VisualShader: no usable shader");
            return _lit;
        }

        internal static bool IsUrpMeshLit(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            string n = name!;
            if (n.IndexOf("Error", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (n.IndexOf("Hidden", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (n.IndexOf("UI", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (n.IndexOf("Sprite", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (n.IndexOf("Particle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (n.IndexOf("Universal Render Pipeline/Lit", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (n.IndexOf("Universal Render Pipeline/Simple Lit", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return n.Equals("Lit", System.StringComparison.OrdinalIgnoreCase);
        }

        internal static void ForceOpaqueLit(Material mat, float cull)
        {
            if (mat == null)
                return;
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 0f);
            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_AlphaClip"))
                mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_SrcBlend"))
                mat.SetFloat("_SrcBlend", 1f);
            if (mat.HasProperty("_DstBlend"))
                mat.SetFloat("_DstBlend", 0f);
            if (mat.HasProperty("_ZWrite"))
                mat.SetFloat("_ZWrite", 1f);
            if (mat.HasProperty("_Cull"))
                mat.SetFloat("_Cull", cull);
            if (mat.HasProperty("_CullMode"))
                mat.SetFloat("_CullMode", cull);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = 2000;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
            ResetSt(mat, "_BaseMap");
            ResetSt(mat, "_MainTex");
            ResetSt(mat, "_BaseColorMap");
        }

        internal static void ResetSt(Material mat, string prop)
        {
            if (mat == null || !mat.HasProperty(prop))
                return;
            mat.SetTextureScale(prop, Vector2.one);
            mat.SetTextureOffset(prop, Vector2.zero);
        }
    }
}
