using UnityEngine;

namespace Gpm.Runtime
{
    internal static class VisualMaterials
    {
        internal static void StripSceneJunk(GameObject root)
        {
            if (root == null)
                return;
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (light != null)
                    UnityEngine.Object.DestroyImmediate(light.gameObject);
            }
            foreach (Camera cam in root.GetComponentsInChildren<Camera>(true))
            {
                if (cam != null)
                    UnityEngine.Object.DestroyImmediate(cam.gameObject);
            }
        }

        internal static void ApplyFbxLook(GameObject root)
        {
            if (root == null)
                return;
            StripSceneJunk(root);
            int n = 0;
            Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                Renderer r = rs[i];
                if (r == null || (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)))
                    continue;
                Material[] src = r.sharedMaterials;
                int slots = src != null && src.Length > 0 ? src.Length : 1;
                Material[] dst = new Material[slots];
                for (int m = 0; m < slots; m++)
                {
                    Material? old = src != null && m < src.Length ? src[m] : null;
                    string matName = GpmMaps.ResolveMatKey(old != null ? old.name : null, r.gameObject.name);
                    Material mat = VisualShader.Make(matName + "_gpm", cull: 0f);
                    Texture? albedo = GpmMaps.Albedo(matName) ?? PeekAlbedo(old);
                    bool albedoOwns = albedo != null;
                    if (albedo != null)
                        WriteAlbedo(mat, albedo);
                    else
                        ClearAlbedoMaps(mat);
                    Texture2D? nml = GpmMaps.Normal(matName);
                    if (nml != null)
                        ApplyDiskNormal(mat, matName, old);
                    else
                    {
                        CopyMap(old, mat, "_BumpMap", "_BumpMap");
                        CopyMap(old, mat, "_BumpMap", "_NormalMap");
                    }
                    KillEmission(mat);
                    GpmLook.ApplyFromBaked(mat, old, albedoOwns);
                    dst[m] = mat;
                    n++;
                }
                r.sharedMaterials = dst;
                r.enabled = true;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                r.receiveShadows = true;
            }
            GpmPlugin.ModLog?.LogInfo(
                $"VisualMaterials FBX-look '{root.name}' slots={n} cull=Off");
        }

        internal static void MatchHostDrawState(GameObject vis, GameObject host)
        {
            if (vis == null || host == null)
                return;
            int layer = host.layer;
            uint mask = 1u;
            Renderer? donor = null;
            Renderer[] hostRs = host.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < hostRs.Length; i++)
            {
                Renderer r = hostRs[i];
                if (r == null)
                    continue;
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer))
                    continue;
                if (PrefabFactoryIsVisual(r.transform))
                    continue;
                donor = r;
                layer = r.gameObject.layer;
                mask = r.renderingLayerMask;
                break;
            }

            Transform[] all = vis.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null)
                    all[i].gameObject.layer = layer;
            }
            Renderer[] visRs = vis.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < visRs.Length; i++)
            {
                Renderer r = visRs[i];
                if (r == null)
                    continue;
                r.renderingLayerMask = mask;
                if (donor == null)
                    continue;
                r.lightProbeUsage = donor.lightProbeUsage;
                r.reflectionProbeUsage = donor.reflectionProbeUsage;
            }
        }

        private static bool PrefabFactoryIsVisual(Transform t)
        {
            while (t != null)
            {
                if (t.name == GpmConstants.VisualRootName)
                    return true;
                t = t.parent;
            }
            return false;
        }

        private static void ApplyDiskNormal(Material mat, string matName, Material? baked)
        {
            Texture? bump = baked != null && baked.HasProperty("_BumpMap") ? baked.GetTexture("_BumpMap") : null;
            if (bump == null)
                bump = GpmMaps.Normal(matName);
            if (bump == null)
                return;
            if (mat.HasProperty("_BumpMap"))
                mat.SetTexture("_BumpMap", bump);
            if (mat.HasProperty("_NormalMap"))
                mat.SetTexture("_NormalMap", bump);
            if (mat.HasProperty("_BumpScale"))
                mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
        }

        private static void CopyMap(Material? src, Material dst, string srcProp, string dstProp)
        {
            if (src == null || dst == null || !src.HasProperty(srcProp) || !dst.HasProperty(dstProp))
                return;
            Texture t = src.GetTexture(srcProp);
            if (t != null)
                dst.SetTexture(dstProp, t);
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

        private static void KillEmission(Material mat)
        {
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", Color.black);
            if (mat.HasProperty("_EmissiveColor"))
                mat.SetColor("_EmissiveColor", Color.black);
            if (mat.HasProperty("_EmissionMap"))
                mat.SetTexture("_EmissionMap", null);
            mat.DisableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        private static Texture? PeekAlbedo(Material? mat)
        {
            if (mat == null)
                return null;
            if (mat.HasProperty("_BaseMap"))
            {
                Texture t = mat.GetTexture("_BaseMap");
                if (t != null)
                    return t;
            }
            if (mat.HasProperty("_MainTex"))
            {
                Texture t = mat.GetTexture("_MainTex");
                if (t != null)
                    return t;
            }
            return null;
        }

        private static void WriteAlbedo(Material mat, Texture tex)
        {
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
                VisualShader.ResetSt(mat, "_BaseMap");
            }
            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", tex);
                VisualShader.ResetSt(mat, "_MainTex");
            }
            if (mat.HasProperty("_BaseColorMap"))
            {
                mat.SetTexture("_BaseColorMap", tex);
                VisualShader.ResetSt(mat, "_BaseColorMap");
            }
        }

        private static void ClearAlbedoMaps(Material mat)
        {
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", null);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", null);
            if (mat.HasProperty("_BaseColorMap"))
                mat.SetTexture("_BaseColorMap", null);
        }
    }
}
