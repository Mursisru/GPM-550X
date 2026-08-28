# Changelog

All notable changes to this project are documented here.

## [1.0.0] - 2026-08-28

### Added

- **GPM-550X** quasi-ballistic strike missile for Nuclear Option (BepInEx 5 + Blueprinter).
- Custom encyclopedia entry, HUD/killfeed identity, and Blender-authored visual mesh (`GPM550X.nobp`).
- **AShM-300** (`AShM1*`) hardpoint injection — every vanilla quantity variant (x1, x2, x4, x6, …).
- **GPO-500-only** single-shot mounts on Vortex, Ifrit, and Revoker.
- Vanilla **OpticalSeeker** flight with INS/optical HUD label, cruise loft to 10 km, motor FX from Tusko-B donor.

### Specifications

| Parameter | Value |
|-----------|-------|
| Launch mass | 877.5 kg |
| Warhead | 550 kg HE |
| Cost | 2.8 |
| RCS | 0.3 m² |
| Body length | 3.9 m |
| Design range | 100 km (from rest, v=0) |
| Motor | 37 kN / 125 kg fuel / 27 s burn |
| Top speed cap | Mach 3.1 |
| Seeker G-limit | 5g |
| Pk | 0.65 |

### Fixed

- Hardpoint bootstrap crash when cloning AShM-300 mount templates (`Collection was modified` during encyclopedia iteration).
- Inject all AShM-300 quantity options per hardpoint set (e.g. both x2 and x4 variants).

[1.0.0]: https://github.com/Mursisru/GPM-550X/releases/tag/1.0.0
