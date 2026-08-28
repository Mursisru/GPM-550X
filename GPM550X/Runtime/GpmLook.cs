using UnityEngine;

namespace Gpm.Runtime
{
    /// <summary>URP Lit scalars/maps 1:1 from baked nobp (FBX import), no overrides.</summary>
    internal static class GpmLook
    {
        internal static void ApplyFromBaked(Material mat, Material? baked, bool albedoOwnsColor)
        {
            if (mat == null)
                return;

            WriteTint(mat, albedoOwnsColor ? Color.white : PeekTint(baked));
            CopyGloss(baked, mat);
            CopyMap(baked, mat, "_MetallicGlossMap", "_MetallicGlossMap");
            CopyMap(baked, mat, "_MetallicGlossMap", "_MaskMap");
            CopyMap(baked, mat, "_OcclusionMap", "_OcclusionMap");
            if (baked != null && mat.HasProperty("_BumpScale") && baked.HasProperty("_BumpScale"))
                mat.SetFloat("_BumpScale", baked.GetFloat("_BumpScale"));
            if (baked != null && mat.HasProperty("_GlossMapScale") && baked.HasProperty("_GlossMapScale"))
                mat.SetFloat("_GlossMapScale", baked.GetFloat("_GlossMapScale"));
            SyncMetallicGlossKeyword(mat);
        }

        private static void CopyGloss(Material? src, Material dst)
        {
            if (src == null || dst == null)
                return;
            if (src.HasProperty("_Metallic") && dst.HasProperty("_Metallic"))
                dst.SetFloat("_Metallic", src.GetFloat("_Metallic"));
            if (src.HasProperty("_Glossiness") && dst.HasProperty("_Smoothness"))
                dst.SetFloat("_Smoothness", src.GetFloat("_Glossiness"));
            else if (src.HasProperty("_Smoothness") && dst.HasProperty("_Smoothness"))
                dst.SetFloat("_Smoothness", src.GetFloat("_Smoothness"));
            if (src.HasProperty("_Glossiness") && dst.HasProperty("_Glossiness"))
                dst.SetFloat("_Glossiness", src.GetFloat("_Glossiness"));
        }

        private static void CopyMap(Material? src, Material dst, string srcProp, string dstProp)
        {
            if (src == null || dst == null || !src.HasProperty(srcProp) || !dst.HasProperty(dstProp))
                return;
            Texture t = src.GetTexture(srcProp);
            if (t != null)
                dst.SetTexture(dstProp, t);
        }

        private static void SyncMetallicGlossKeyword(Material mat)
        {
            bool hasMap = mat.HasProperty("_MetallicGlossMap") && mat.GetTexture("_MetallicGlossMap") != null;
            if (!hasMap && mat.HasProperty("_MaskMap"))
                hasMap = mat.GetTexture("_MaskMap") != null;
            if (hasMap)
            {
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.EnableKeyword("_METALLICGLOSSMAP");
            }
            else
            {
                mat.DisableKeyword("_METALLICSPECGLOSSMAP");
                mat.DisableKeyword("_METALLICGLOSSMAP");
            }
        }

        private static Color PeekTint(Material? mat)
        {
            if (mat == null)
                return Color.white;
            if (mat.HasProperty("_BaseColor"))
                return mat.GetColor("_BaseColor");
            if (mat.HasProperty("_Color"))
                return mat.GetColor("_Color");
            return Color.white;
        }

        private static void WriteTint(Material mat, Color tint)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", tint);
        }
    }
}
