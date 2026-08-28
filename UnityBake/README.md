# GPM-550X Unity Bake

1. Open this folder in Unity **2022.3.62f3** (same as Nuclear Option).
2. Wait for FBX import (`Assets/MissilePack/GPM-550X.fbx`).
3. Menu: **GPM-550X → Build Nobp Bundle**.
4. Output: `UnityBake/Build/GPM550X.nobp` (copied to `GPM550X/Resources/` and game plugins).

The `.nobp` is a Unity AssetBundle loaded by **Blueprinter**. It must contain TextAsset `patch_manifest`.

Batch:

```
Unity.exe -batchmode -projectPath UnityBake -executeMethod BatchBuild.Build -quit
```
