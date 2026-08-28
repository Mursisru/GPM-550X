using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Gpm.Blueprinter;
using Gpm.Bootstrap;
using Gpm.Patches;
using Gpm.Runtime;
using UnityEngine;

namespace Gpm
{
    internal static class GpmBootstrap
    {
        private static bool _done;
        internal static MissileDefinition? Definition { get; private set; }
        internal static WeaponInfo? Info { get; private set; }
        internal static WeaponMount? GpoSingleMount { get; private set; }
        internal static readonly Dictionary<string, WeaponMount> SlotClones =
            new Dictionary<string, WeaponMount>(StringComparer.Ordinal);
        internal static float LengthM = GpmConstants.LengthM;
        internal static float WidthM = GpmConstants.WidthM;
        internal static float HeightM = GpmConstants.HeightM;

        private static readonly FieldInfo? UnitDisabled =
            typeof(UnitDefinition).GetField("disabled", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? MountDisabled =
            typeof(WeaponMount).GetField("disabled", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static IEnumerator Run(Encyclopedia enc)
        {
            if (_done || enc == null)
                yield break;

            yield return BlueprinterGate.WaitUntilReady();

            try
            {
                PrefabFactory.AssertDonorsIntact(enc);
                NobpContent.TryLoad();

                MissileDefinition? tusko = PrefabFactory.FindTuskoMissile(enc);
                if (tusko?.unitPrefab != null)
                    VisualShader.PrimeFrom(tusko.unitPrefab);

                GpmMotorFx.Capture(enc, tusko);
                LogTuskoMass(tusko);

                if (Encyclopedia.Lookup != null &&
                    Encyclopedia.Lookup.TryGetValue(GpmConstants.MissileJsonKey, out UnitDefinition existing) &&
                    existing is MissileDefinition md && md.unitPrefab != null)
                {
                    Definition = md;
                    GameObject? shellGo = GpmFlyFactory.BindSharedShell(tusko ?? md);
                    if (shellGo != null)
                        md.unitPrefab = shellGo;
                    ApplyMeasuredSize(md);
                }
                else
                    Definition = CreateDefinition(enc, tusko);

                GpmDefinitionMass.Apply(Definition, GpmConstants.LaunchMassKg);
                GpmCalcProxy.Init(enc);

            Info = CreateSharedInfo(enc, Definition);
            CreateSlotClones(enc, Definition, Info);
                GpoSingleMount = CreateGpoSingle(enc, Definition, Info);

                if (GpoSingleMount != null || SlotClones.Count > 0)
                    HardpointInjector.Inject(enc, SlotClones, GpoSingleMount);

                PrefabFactory.AssertTuskoIntact(enc);
                _done = Definition != null && Info != null && (SlotClones.Count > 0 || GpoSingleMount != null);
                GpmPlugin.ModLog?.LogInfo(_done
                    ? $"GPM-550X ready def={GpmConstants.MissileJsonKey} clones={SlotClones.Count} visual={(NobpContent.VisualPrefab != null)}"
                    : "GPM-550X bootstrap incomplete.");
            }
            catch (Exception ex)
            {
                GpmPlugin.ModLog?.LogError($"GpmBootstrap: {ex}");
            }
        }

        internal static bool IsOurs(Missile? missile)
        {
            if (missile == null)
                return false;
            if (missile.GetComponent<GpmTag>() != null)
                return true;
            WeaponInfo? wi = missile.GetWeaponInfo();
            if (IsOurInfo(wi))
                return true;
            return missile.definition != null &&
                   string.Equals(missile.definition.jsonKey, GpmConstants.MissileJsonKey, StringComparison.Ordinal);
        }

        internal static bool IsOurMount(WeaponMount? mount)
        {
            return mount != null && PrefabFactory.IsOurMountKey(mount.jsonKey);
        }

        internal static bool IsOurInfo(WeaponInfo? info)
        {
            if (info == null)
                return false;
            if (Info != null && ReferenceEquals(info, Info))
                return true;
            return string.Equals(info.weaponName, GpmConstants.WeaponInfoName, StringComparison.Ordinal) ||
                   string.Equals(info.shortName, GpmConstants.ShortName, StringComparison.Ordinal);
        }

        private static void LogTuskoMass(MissileDefinition? tusko)
        {
            if (tusko == null)
                return;
            float shown = tusko.mass;
            GpmPlugin.ModLog?.LogInfo(
                $"Tusko encyclopedia mass={shown:F1}kg (wiki 650). GPM LaunchMassKg={GpmConstants.LaunchMassKg:F1}.");
        }

        private static MissileDefinition? CreateDefinition(Encyclopedia enc, MissileDefinition? tusko)
        {
            if (tusko?.unitPrefab == null)
            {
                GpmPlugin.ModLog?.LogError("GPM-550X: no AShM3/Tusko-B unitPrefab.");
                return null;
            }

            MissileDefinition def = ScriptableObject.CreateInstance<MissileDefinition>();
            def.name = "MissilePack_GPM550X_Definition";
            def.jsonKey = GpmConstants.MissileJsonKey;
            PrefabFactory.CopyUnitDefScalars(tusko, def);
            PrefabFactory.CopyMapIdentity(tusko, def);
            def.unitName = GpmConstants.UnitName;
            def.bogeyName = GpmConstants.BogeyName;
            def.description = GpmConstants.Description;
            def.value = GpmConstants.Cost;
            def.mass = GpmConstants.LaunchMassKg;
            def.length = GpmConstants.LengthM;
            def.width = GpmConstants.WidthM;
            def.height = GpmConstants.HeightM;
            if (def.spawnOffset.y < 0.05f)
                def.spawnOffset = new Vector3(def.spawnOffset.x, GpmConstants.HeightM * 0.5f, def.spawnOffset.z);
            def.radarSize = GpmConstants.RadarSize;
            def.code = "MSL";
            def.IsObstacle = false;
            UnitDisabled?.SetValue(def, false);

            GameObject? fly = GpmFlyFactory.BindSharedShell(tusko);
            if (fly == null)
            {
                GpmPlugin.ModLog?.LogError("GPM-550X: fly prefab bind failed.");
                return null;
            }
            def.unitPrefab = fly;
            ApplyMeasuredSize(def);

            enc.missiles ??= new List<MissileDefinition>();
            if (!enc.missiles.Contains(def))
                enc.missiles.Add(def);
            Encyclopedia.Lookup ??= new Dictionary<string, UnitDefinition>(StringComparer.Ordinal);
            Encyclopedia.Lookup[def.jsonKey] = def;
            List<INetworkDefinition>? idx = enc.IndexLookup;
            if (idx != null && !PrefabFactory.ContainsNet(idx, def))
            {
                idx.Add(def);
                ((INetworkDefinition)def).LookupIndex = idx.Count - 1;
            }

            GpmDefinitionMass.Apply(def, GpmConstants.LaunchMassKg);
            GpmPlugin.ModLog?.LogInfo($"Created GPM-550X definition from shell '{tusko.jsonKey}'.");
            return def;
        }

        private static void ApplyMeasuredSize(MissileDefinition def)
        {
            LengthM = GpmConstants.LengthM;
            WidthM = GpmConstants.WidthM;
            HeightM = GpmConstants.HeightM;
            def.length = LengthM;
            def.width = WidthM;
            def.height = HeightM;
            if (def.spawnOffset.y < 0.05f)
                def.spawnOffset = new Vector3(def.spawnOffset.x, HeightM * 0.5f, def.spawnOffset.z);
        }

        private static WeaponInfo CreateSharedInfo(Encyclopedia enc, MissileDefinition? def)
        {
            WeaponInfo info = ScriptableObject.CreateInstance<WeaponInfo>();
            info.name = "MissilePack_GPM550X_Info";
            WeaponInfo? donor = FindTuskoWeaponInfo(enc);
            if (donor != null)
            {
                info.effectiveness = donor.effectiveness;
                info.targetRequirements = donor.targetRequirements;
                info.pK = donor.pK;
                info.fireInterval = donor.fireInterval;
                info.muzzleVelocity = donor.muzzleVelocity;
                info.maxSpeed = donor.maxSpeed;
                info.dragCoef = donor.dragCoef;
                info.gravMult = donor.gravMult;
                info.pierceDamage = donor.pierceDamage;
                info.armorTierEffectiveness = donor.armorTierEffectiveness;
                info.visibilityWhenFired = donor.visibilityWhenFired;
                info.useWeaponDoors = donor.useWeaponDoors;
                info.boresight = donor.boresight;
                info.rearmGround = donor.rearmGround;
                info.rearmShip = donor.rearmShip;
            }

            TargetRequirements tr = info.targetRequirements;
            tr.minAltitude = -200f;
            tr.maxAltitude = 80000f;
            tr.maxRange = GpmConstants.DesignRangeM;
            tr.minRange = GpmConstants.EncyclopediaMinRangeM;
            info.targetRequirements = tr;
            GpmEncyclopediaStats.ApplyTargetRequirements(info);

            Sprite? preview = GpmWeaponIcon.Get();
            if (preview != null)
                info.weaponIcon = preview;

            info.weaponName = GpmConstants.WeaponInfoName;
            info.shortName = GpmConstants.ShortName;
            info.description = GpmConstants.Description;
            info.massPerRound = GpmConstants.LaunchMassKg;
            info.costPerRound = GpmConstants.Cost;
            info.blastDamage = GpmConstants.BlastYieldKg;
            info.pK = GpmConstants.Pk;
            info.nuclear = false;
            info.strategic = false;
            info.bomb = false;
            info.glideBomb = false;
            info.missile = true;
            info.overHorizon = true;
            info.laserGuided = false;
            info.gun = false;
            info.energy = false;
            info.jammer = false;
            info.troops = false;
            info.hideInDisplay = false;
            info.cargo = false;
            info.sling = false;
            if (def?.unitPrefab != null)
                info.weaponPrefab = def.unitPrefab;
            return info;
        }

        private static WeaponInfo? FindTuskoWeaponInfo(Encyclopedia enc)
        {
            WeaponMount? single = PrefabFactory.FindTuskoSingleMount(enc);
            if (single?.info != null)
                return single.info;
            if (enc.weaponMounts == null)
                return null;
            foreach (WeaponMount w in enc.weaponMounts)
            {
                if (w?.info != null && PrefabFactory.IsTuskoKey(w.jsonKey))
                    return w.info;
            }
            return null;
        }

        private static void CreateSlotClones(Encyclopedia enc, MissileDefinition? def, WeaponInfo info)
        {
            if (enc.weaponMounts == null || def?.unitPrefab == null)
                return;
            foreach (WeaponMount w in enc.weaponMounts)
            {
                if (w == null || w.prefab == null || !PrefabFactory.IsAshm300SlotKey(w.jsonKey))
                    continue;
                RegisterClone(
                    enc,
                    w,
                    def,
                    info,
                    PrefabFactory.MountKeyFromDonor(w.jsonKey, GpmConstants.Ashm300SlotPrefix),
                    keepAll: true);
            }
        }

        private static WeaponMount? CreateGpoSingle(Encyclopedia enc, MissileDefinition? def, WeaponInfo info)
        {
            WeaponMount? donor = PrefabFactory.FindTuskoSingleMount(enc);
            if (donor?.prefab == null || def?.unitPrefab == null)
            {
                GpmPlugin.ModLog?.LogWarning("GPM-550X: no Tusko single mount for GPO-only slots.");
                return null;
            }
            return RegisterClone(enc, donor, def, info, GpmConstants.MountJsonKeyGpo, keepAll: false);
        }

        private static WeaponMount? RegisterClone(
            Encyclopedia enc,
            WeaponMount donor,
            MissileDefinition def,
            WeaponInfo info,
            string jsonKey,
            bool keepAll)
        {
            WeaponMount? existing = null;
            if (Encyclopedia.WeaponLookup != null &&
                Encyclopedia.WeaponLookup.TryGetValue(jsonKey, out existing) &&
                existing != null &&
                existing.prefab != null &&
                PrefabFactory.IsOurMountKey(existing.jsonKey))
            {
                RefreshMount(existing, def, info);
                if (keepAll && !string.IsNullOrEmpty(donor.jsonKey))
                    SlotClones[donor.jsonKey] = existing;
                return existing;
            }

            string donorKey = donor.jsonKey ?? "AShM3";
            WeaponMount mount = ScriptableObject.CreateInstance<WeaponMount>();
            mount.name = jsonKey;
            mount.jsonKey = jsonKey;
            mount.mountName = GpmConstants.MountDisplayName;
            PrefabFactory.CopyMountScalars(donor, mount);
            MountDisabled?.SetValue(mount, false);
            mount.info = info;
            mount.sortWeapons = true;

            GameObject mountGo = PrefabFactory.CloneAsPrefab(donor.prefab, jsonKey + "_Prefab");
            if (!keepAll)
                KeepSingle(mountGo);
            StampAllMounted(mountGo);
            mount.prefab = mountGo;
            BindMountedInfo(mount, info);

            int ammo = mountGo.GetComponentsInChildren<Weapon>(true).Length;
            if (!keepAll)
                ammo = 1;
            if (ammo < 1)
                ammo = 1;
            mount.ammo = ammo;
            mount.mass = mount.emptyMass + GpmConstants.LaunchMassKg * ammo;
            mount.RCS = GpmConstants.RadarSize;

            if (!string.Equals(donor.jsonKey, donorKey, StringComparison.Ordinal))
                GpmPlugin.ModLog?.LogError($"Tusko mount donor mutated: {donor.jsonKey}");

            enc.weaponMounts ??= new List<WeaponMount>();
            if (!enc.weaponMounts.Contains(mount))
                enc.weaponMounts.Add(mount);
            Encyclopedia.WeaponLookup ??= new Dictionary<string, WeaponMount>(StringComparer.Ordinal);
            Encyclopedia.WeaponLookup[mount.jsonKey] = mount;
            List<INetworkDefinition>? idx = enc.IndexLookup;
            if (idx != null && !PrefabFactory.ContainsNet(idx, mount))
            {
                idx.Add(mount);
                ((INetworkDefinition)mount).LookupIndex = idx.Count - 1;
            }

            try { mount.Initialize(); }
            catch (Exception ex) { GpmPlugin.ModLog?.LogWarning($"GPM Initialize '{jsonKey}': {ex.Message}"); }

            mount.jsonKey = jsonKey;
            mount.info = info;
            mount.sortWeapons = true;
            mount.mountName = ammo > 1
                ? string.Format("{0} x{1}", GpmConstants.MountDisplayName, ammo)
                : GpmConstants.MountDisplayName;
            BindMountedInfo(mount, info);

            if (keepAll)
                SlotClones[donorKey] = mount;
            GpmPlugin.ModLog?.LogInfo($"GPM mount '{jsonKey}' ammo={ammo} from '{donorKey}'.");
            return mount;
        }

        private static void RefreshMount(WeaponMount mount, MissileDefinition def, WeaponInfo info)
        {
            NobpContent.TryLoad();
            if (mount.prefab != null && NobpContent.VisualPrefab != null)
                StampAllMounted(mount.prefab);
            mount.info = info;
            mount.sortWeapons = true;
            if (def.unitPrefab != null)
                info.weaponPrefab = def.unitPrefab;
            BindMountedInfo(mount, info);
        }

        private static void StampAllMounted(GameObject mountGo)
        {
            NobpContent.TryLoad();
            if (NobpContent.VisualPrefab == null || mountGo == null)
                return;
            GpmVisualStamp.StampMountTemplate(mountGo, NobpContent.VisualPrefab);
        }

        private static void KeepSingle(GameObject mountGo)
        {
            MountedMissile[] mounted = mountGo.GetComponentsInChildren<MountedMissile>(true);
            for (int i = 1; i < mounted.Length; i++)
            {
                if (mounted[i] != null)
                    UnityEngine.Object.DestroyImmediate(mounted[i].gameObject);
            }
        }

        private static void BindMountedInfo(WeaponMount mount, WeaponInfo info)
        {
            if (mount.prefab == null)
                return;
            foreach (MountedMissile mm in mount.prefab.GetComponentsInChildren<MountedMissile>(true))
            {
                if (mm != null)
                    mm.info = info;
            }
        }
    }
}
