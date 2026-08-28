# GPM-550X

[![Version](https://img.shields.io/badge/version-1.0.0-blue)](https://github.com/Mursisru/GPM-550X/releases/tag/1.0.0)
[![BepInEx](https://img.shields.io/badge/BepInEx-5-green)](https://docs.bepinex.dev/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

BepInEx plugin that adds the **GPM-550X** quasi-ballistic strike missile to [Nuclear Option](https://store.steampowered.com/app/2654120/Nuclear_Option/).

> [!IMPORTANT]
> **Requires [Blueprinter](https://github.com/nikkorap/NOBlueprinter-Releases)** (`com.nikkorap.blueprinter`). Install `Blueprinter.dll` into `BepInEx/plugins/` before this mod.

> [!NOTE]
> Hangar slots: every **AShM-300** (`AShM1*`) pylon variant (same counts as vanilla — x1, x2, x4, x6, etc.) and **GPO-500-only** pylons at **1×** on Vortex / Ifrit / Revoker. GPO-only sets never mix with AShM-300 quantity layouts.

## Features

- Own encyclopedia entry (`missilepack_gpm550x`), killfeed / HUD name **GPM-550X**
- Vanilla **OpticalSeeker** loft (not cruise-missile terrain follow); HUD seeker string `INS / Opt.`
- 877.5 kg launch mass, 550 kg HE, cost 2.8, design range **100 km from rest** (`v=0`, `h=0`)
- Visual mesh from Blender via Blueprinter bundle `GPM550X.nobp`
- Engine flame captured from Tusko-B; `DockingPort` mesh removed after drop

> [!WARNING]
> Do not install a leftover `MissilePack.dll` next to this plugin. Fly/motor/FX donor is **Tusko-B (`AShM3*`)** only. Hangar slot templates come from **AShM-300 (`AShM1*`)**. GPO-500 is a hangar marker, not a fly template.

## Install

1. Install BepInEx 5 and Blueprinter.
2. Copy the `GPM-550X/` folder into `BepInEx/plugins/GPM-550X/`:
   - `GPM550X.dll`
   - `GPM550X.nobp`
   - `PreviewGpm.png`
   - `Textures/GPM550X/` (runtime Color + Normal maps)
3. Launch the game and select **GPM-550X** on AShM-300 pylons or single-shot GPO-500 pylons.

## Build

```powershell
dotnet build .\GPM550X\GPM550X.csproj -c Release
```

Release output auto-deploys to `BepInEx/plugins/GPM-550X/`.

Unity bake (mesh `.nobp`):

```text
Open UnityBake/ in Unity 2022.3.62f3 → GPM-550X → Build Nobp Bundle
```

Batch:

```text
Unity.exe -batchmode -nographics -projectPath UnityBake -executeMethod BatchBuild.Build -quit
```

## Model source

Blender export: `GPM-550X.fbx` + `GPM_Textures/`  
Unity import copy: `UnityBake/Assets/MissilePack/GPM-550X.fbx`

## License

MIT — see [LICENSE](LICENSE).
