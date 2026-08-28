using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Gpm.UnityBake
{
    public static class NobpBundleBuilder
    {
        private const string PrefabName = "GpmVisual";
        private const string OutputName = "GPM550X.nobp";
        private const string FbxName = "GPM-550X.fbx";

        [MenuItem("GPM-550X/Build Nobp Bundle")]
        public static void Build()
        {
            string assetsRoot = "Assets/MissilePack";
            string buildDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build"));
            Directory.CreateDirectory(buildDir);

            EnsurePrefab(assetsRoot);
            EnsureManifest(assetsRoot);

            string prefabPath = $"{assetsRoot}/{PrefabName}.prefab";
            string manifestPath = $"{assetsRoot}/patch_manifest.txt";
            var assetNames = new List<string> { prefabPath, manifestPath };

            string matFolder = $"{assetsRoot}/Materials/GPM550X";
            if (AssetDatabase.IsValidFolder(matFolder))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { matFolder }))
                    assetNames.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            string texFolder = $"{assetsRoot}/Textures/GPM550X";
            if (AssetDatabase.IsValidFolder(texFolder))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { texFolder }))
                    assetNames.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            string fbx = $"{assetsRoot}/{FbxName}";
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), fbx.Replace('/', Path.DirectorySeparatorChar))))
                assetNames.Add(fbx);

            var build = new AssetBundleBuild
            {
                assetBundleName = OutputName,
                assetNames = assetNames.ToArray()
            };

            BuildPipeline.BuildAssetBundles(
                buildDir,
                new[] { build },
                BuildAssetBundleOptions.ForceRebuildAssetBundle,
                BuildTarget.StandaloneWindows64);

            string produced = Path.Combine(buildDir, OutputName);
            string alt = Path.Combine(buildDir, OutputName.ToLowerInvariant());
            string src = File.Exists(produced) ? produced : alt;

            string pluginRes = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "GPM550X", "Resources"));
            Directory.CreateDirectory(pluginRes);
            if (File.Exists(src))
            {
                File.Copy(src, Path.Combine(pluginRes, OutputName), true);
                string binRel = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "GPM550X", "bin", "Release"));
                Directory.CreateDirectory(binRel);
                File.Copy(src, Path.Combine(binRel, OutputName), true);
            }

            string deploy = @"C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option\BepInEx\plugins\GPM-550X";
            Directory.CreateDirectory(deploy);
            if (File.Exists(src))
            {
                File.Copy(src, Path.Combine(deploy, OutputName), true);
                File.Copy(src, Path.Combine(deploy, OutputName.ToLowerInvariant()), true);
            }

            string texAbs = Path.Combine(Application.dataPath, "MissilePack", "Textures", "GPM550X");
            if (Directory.Exists(texAbs))
            {
                string texDeploy = Path.Combine(deploy, "Textures", "GPM550X");
                Directory.CreateDirectory(texDeploy);
                foreach (string file in Directory.GetFiles(texAbs, "*.png"))
                {
                    string name = Path.GetFileName(file);
                    if (name.IndexOf("Displacement", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    if (name.IndexOf("without Bump", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    if (name.IndexOf("Preview", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    File.Copy(file, Path.Combine(texDeploy, name), true);
                }
            }

            Debug.Log($"GPM-550X: built {src}");
            AssetDatabase.Refresh();
        }

        private static void EnsureManifest(string assetsRoot)
        {
            string json =
@"{
  ""modName"": ""GPM550X"",
  ""schemaVersion"": 3,
  ""modVersion"": ""0.0.0"",
  ""Patches"": [],
  ""Ops"": [],
  ""Addressables"": []
}";
            string txtPath = Path.Combine(Application.dataPath, "MissilePack", "patch_manifest.txt");
            File.WriteAllText(txtPath, json);
            AssetDatabase.ImportAsset($"{assetsRoot}/patch_manifest.txt");
        }

        private static void EnsurePrefab(string assetsRoot)
        {
            string fbxPath = FindNamedFbx(assetsRoot, FbxName);
            if (string.IsNullOrEmpty(fbxPath))
            {
                Debug.LogWarning("GPM-550X: GPM-550X.fbx not found.");
                return;
            }

            ConfigureImporter(fbxPath);
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null)
            {
                Debug.LogError("GPM-550X: failed to load GPM-550X.fbx");
                return;
            }

            GameObject root = UnityEngine.Object.Instantiate(fbx);
            root.name = PrefabName;

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

            Shader lit = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");
            string matFolder = $"{assetsRoot}/Materials/GPM550X";
            if (!AssetDatabase.IsValidFolder($"{assetsRoot}/Materials"))
                AssetDatabase.CreateFolder(assetsRoot, "Materials");
            if (!AssetDatabase.IsValidFolder(matFolder))
                AssetDatabase.CreateFolder($"{assetsRoot}/Materials", "GPM550X");

            Dictionary<string, Material> bakedByBlender = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null)
                    continue;
                Material[] src = r.sharedMaterials;
                Material[] dst = new Material[Mathf.Max(1, src != null ? src.Length : 1)];
                for (int i = 0; i < dst.Length; i++)
                {
                    Material imported = src != null && i < src.Length ? src[i] : null;
                    string blenderName = imported != null && !string.IsNullOrEmpty(imported.name)
                        ? StripInstance(imported.name)
                        : r.gameObject.name + "_" + i;
                    if (bakedByBlender.TryGetValue(blenderName, out Material shared))
                    {
                        dst[i] = shared;
                        continue;
                    }

                    string matAssetPath = $"{matFolder}/{Sanitize(blenderName)}.mat";
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(matAssetPath);
                    if (mat == null)
                    {
                        mat = imported != null ? new Material(imported) : new Material(lit);
                        mat.name = blenderName;
                        AssetDatabase.CreateAsset(mat, matAssetPath);
                    }
                    else if (imported != null)
                        mat.CopyPropertiesFromMaterial(imported);

                    mat.name = blenderName;
                    if (mat.shader == null || mat.shader.name.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0)
                        mat.shader = lit;
                    ApplyDiskMaps(mat, blenderName, assetsRoot);
                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", Color.black);
                    mat.DisableKeyword("_EMISSION");
                    EditorUtility.SetDirty(mat);
                    bakedByBlender[blenderName] = mat;
                    dst[i] = mat;
                }
                r.sharedMaterials = dst;
            }

            AssetDatabase.SaveAssets();
            string prefabPath = $"{assetsRoot}/{PrefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"GPM-550X: GpmVisual from '{fbxPath}'");
        }

        private static void ApplyDiskMaps(Material mat, string blenderName, string assetsRoot)
        {
            string texRoot = $"{assetsRoot}/Textures/GPM550X";
            string colorPath = $"{texRoot}/{blenderName} Color.png";
            string normalPath = $"{texRoot}/{blenderName} Normal.png";
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
            if (color != null)
            {
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", color);
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", color);
            }
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normal != null)
            {
                ConfigureTextureImport(normalPath, asNormal: true);
                normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (mat.HasProperty("_BumpMap"))
                    mat.SetTexture("_BumpMap", normal);
                if (mat.HasProperty("_BumpScale"))
                    mat.SetFloat("_BumpScale", 1f);
                mat.EnableKeyword("_NORMALMAP");
            }
        }

        private static void ConfigureTextureImport(string assetPath, bool asNormal)
        {
            TextureImporter imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null)
                return;
            bool dirty = false;
            if (asNormal && imp.textureType != TextureImporterType.NormalMap)
            {
                imp.textureType = TextureImporterType.NormalMap;
                dirty = true;
            }
            if (imp.mipmapEnabled != true)
            {
                imp.mipmapEnabled = true;
                dirty = true;
            }
            if (dirty)
                imp.SaveAndReimport();
        }

        private static void ConfigureImporter(string fbxPath)
        {
            ModelImporter imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (imp == null)
            {
                AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
                return;
            }
            imp.weldVertices = false;
            imp.meshOptimizationFlags = (MeshOptimizationFlags)0;
            imp.importNormals = ModelImporterNormals.Import;
            imp.importTangents = ModelImporterTangents.CalculateMikk;
            imp.preserveHierarchy = true;
            imp.addCollider = false;
            imp.importLights = false;
            imp.importCameras = false;
            imp.animationType = ModelImporterAnimationType.None;
            imp.useFileScale = true;
            imp.globalScale = 1f;
            imp.SaveAndReimport();
        }

        private static string StripInstance(string name)
        {
            const string inst = " (Instance)";
            if (!string.IsNullOrEmpty(name) && name.EndsWith(inst, StringComparison.OrdinalIgnoreCase))
                return name.Substring(0, name.Length - inst.Length);
            return name;
        }

        private static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "mesh";
            char[] chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
                    chars[i] = '_';
            }
            return new string(chars);
        }

        private static string FindNamedFbx(string assetsRoot, string fileName)
        {
            string preferred = $"{assetsRoot}/{fileName}";
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), preferred.Replace('/', Path.DirectorySeparatorChar))))
                return preferred;
            string[] guids = AssetDatabase.FindAssets("GPM-550X t:Model");
            if (guids == null || guids.Length == 0)
                return null;
            return AssetDatabase.GUIDToAssetPath(guids[0]);
        }
    }
}
