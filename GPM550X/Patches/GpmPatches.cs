using HarmonyLib;
using Gpm.Blueprinter;
using Gpm.Bootstrap;
using Gpm.Runtime;
using UnityEngine;

namespace Gpm.Patches
{
    [HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.InitializeWeaponManager))]
    internal static class GpmWeaponManagerInitPatch
    {
        private static void Prefix(WeaponManager __instance)
        {
            HardpointInjector.EnsureRuntime(__instance);
        }
    }

    [HarmonyPatch(typeof(Hardpoint), nameof(Hardpoint.SpawnMount))]
    internal static class GpmSpawnMountPatch
    {
        private static void Prefix(Aircraft aircraft, WeaponMount weaponMount)
        {
            if (aircraft?.weaponManager != null)
                HardpointInjector.EnsureRuntime(aircraft.weaponManager);
            if (!GpmBootstrap.IsOurMount(weaponMount) || weaponMount.prefab == null)
                return;
            WeaponInfo? shared = GpmBootstrap.Info ?? weaponMount.info;
            if (shared != null)
            {
                weaponMount.info = shared;
                weaponMount.sortWeapons = true;
                if (GpmBootstrap.Definition?.unitPrefab != null)
                    shared.weaponPrefab = GpmBootstrap.Definition.unitPrefab;
                foreach (MountedMissile mm in weaponMount.prefab.GetComponentsInChildren<MountedMissile>(true))
                {
                    if (mm != null)
                        mm.info = shared;
                }
            }
            PrefabFactory.FreezeTemplatePhysics(weaponMount.prefab);
            weaponMount.prefab.SetActive(true);
        }

        private static void Postfix(Hardpoint __instance, WeaponMount weaponMount, GameObject __result)
        {
            if (!GpmBootstrap.IsOurMount(weaponMount) || __result == null)
                return;
            if (weaponMount.prefab != null)
            {
                PrefabFactory.FreezeTemplatePhysics(weaponMount.prefab);
                weaponMount.prefab.SetActive(false);
            }
            bool bay = GpmBayUtil.ShouldTreatAsBay(__instance, weaponMount);
            PrefabFactory.ActivateMountedInstance(__result, bay);
            if (bay)
                GpmBayUtil.HideHardpointPylons(__instance);
        }
    }

    [HarmonyPatch(typeof(Spawner), nameof(Spawner.SpawnMissile), new[] { typeof(GameObject), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(Unit), typeof(Unit) })]
    internal static class GpmSpawnMissileGoPatch
    {
        private static void Prefix(GameObject missile, out bool __state)
        {
            if (GpmSpawnGate.IsOurFlyPrefab(missile) && GpmSpawnGate.Pending > 0)
                GpmSpawnGate.BeginPrefabStamp(missile);
            __state = GpmSpawnGate.TryBegin();
        }

        private static void Postfix(bool __state, GameObject missile, Unit target, Missile __result)
        {
            try
            {
                GpmSpawnGate.EndPrefabStamp();
                if (__result == null)
                    return;
                bool rescue = !__state && GpmSpawnGate.ShouldRescueClaim(missile);
                if (!__state && !rescue)
                    return;
                GpmSpawnGate.Claim(__result, target);
            }
            finally
            {
                GpmSpawnGate.EndPrefabStamp();
                if (__state)
                    GpmSpawnGate.End();
            }
        }
    }

    [HarmonyPatch(typeof(Spawner), nameof(Spawner.SpawnMissile), new[] { typeof(MissileDefinition), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(Unit), typeof(Unit) })]
    internal static class GpmSpawnMissileDefPatch
    {
        private static void Prefix(MissileDefinition missile, out bool __state)
        {
            if (missile == null)
            {
                __state = false;
                return;
            }
            __state = string.Equals(missile.jsonKey, GpmConstants.MissileJsonKey, System.StringComparison.Ordinal);
            if (!__state)
                return;
            GpmSpawnGate.InFlight = true;
            GpmSpawnGate.BeginPrefabStamp(missile.unitPrefab);
        }

        private static void Postfix(bool __state, Unit target, Missile __result)
        {
            try
            {
                GpmSpawnGate.EndPrefabStamp();
                if (!__state || __result == null)
                    return;
                GpmSpawnGate.Claim(__result, target);
            }
            finally
            {
                GpmSpawnGate.EndPrefabStamp();
                if (__state)
                    GpmSpawnGate.End();
            }
        }
    }

    [HarmonyPatch(typeof(Spawner), nameof(Spawner.SpawnMissileEncyclopedia))]
    internal static class GpmEncyclopediaSpawnPatch
    {
        private static void Prefix(MissileDefinition missile, out bool __state)
        {
            if (missile == null)
            {
                __state = false;
                return;
            }
            __state = string.Equals(missile.jsonKey, GpmConstants.MissileJsonKey, System.StringComparison.Ordinal);
            if (!__state)
                return;
            GpmSpawnGate.InFlight = true;
            GpmSpawnGate.BeginPrefabStamp(missile.unitPrefab);
        }

        private static void Postfix(bool __state, Missile __result)
        {
            try
            {
                GpmSpawnGate.EndPrefabStamp();
                if (!__state || __result == null)
                    return;
                NobpContent.TryLoad();
                GpmSpawnGate.Claim(__result, null);
            }
            finally
            {
                GpmSpawnGate.EndPrefabStamp();
                if (__state)
                    GpmSpawnGate.End();
            }
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.Awake))]
    internal static class GpmMissileAwakePatch
    {
        private static void Postfix(Missile __instance)
        {
            try
            {
                GpmSpawnGate.TryEarlyVisual(__instance);
            }
            catch (System.Exception ex)
            {
                GpmPlugin.ModLog?.LogError($"GpmMissileAwakePatch: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.OnEnable))]
    internal static class GpmMissileOnEnablePatch
    {
        private static void Postfix(Missile __instance)
        {
            try
            {
                GpmSpawnGate.TryEarlyVisual(__instance);
            }
            catch (System.Exception ex)
            {
                GpmPlugin.ModLog?.LogError($"GpmMissileOnEnablePatch: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(MountedMissile), nameof(MountedMissile.Fire))]
    internal static class GpmFirePatch
    {
        private static void Prefix(MountedMissile __instance, Unit target)
        {
            if (__instance?.info == null || !GpmBootstrap.IsOurInfo(__instance.info))
                return;
            GpmSpawnGate.SyncSharedInfo(__instance);
            GpmSpawnGate.NoteFire(__instance, target);
            PrefabFactory.RevealBayVisuals(__instance.gameObject);
        }
    }

    [HarmonyPatch(typeof(Missile), "StartMissile")]
    internal static class GpmStartMissilePatch
    {
        private static void Postfix(Missile __instance)
        {
            if (GpmBootstrap.IsOurs(__instance))
                GpmSpawnGate.Ensure(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), "LocalStart")]
    internal static class GpmLocalStartPatch
    {
        private static void Prefix(Missile __instance)
        {
            if (!GpmBootstrap.IsOurs(__instance))
                return;
            GpmTargetSync.Apply(__instance);
        }

        private static void Postfix(Missile __instance)
        {
            if (!GpmBootstrap.IsOurs(__instance))
                return;
            GpmSpawnGate.Ensure(__instance);
            GpmTargetSync.Apply(__instance);
            GpmCruiseLoft.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(OpticalSeekerCruiseMissile), nameof(OpticalSeekerCruiseMissile.Initialize))]
    internal static class GpmCruiseSeekerInitPatch
    {
        private static void Postfix(OpticalSeekerCruiseMissile __instance, Unit target)
        {
            Missile? missile = __instance.GetComponentInParent<Missile>();
            if (missile == null || !GpmBootstrap.IsOurs(missile))
                return;
            if (target != null && !target.disabled)
                missile.SetTarget(target);
            GpmTargetSync.Apply(missile);
            GpmCruiseLoft.Apply(missile);
        }
    }

    [HarmonyPatch(typeof(MountedMissile), nameof(MountedMissile.Rearm))]
    internal static class GpmRearmPatch
    {
        private static void Postfix(MountedMissile __instance)
        {
            if (__instance?.info == null || !GpmBootstrap.IsOurInfo(__instance.info))
                return;
            GpmMountVisual.Restore(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), "OnStartClient")]
    internal static class GpmOnStartClientPatch
    {
        private static void Postfix(Missile __instance)
        {
            if (GpmBootstrap.IsOurs(__instance))
                GpmSpawnGate.Ensure(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), "Steering")]
    internal static class GpmSteeringUprightPatch
    {
        private static void Postfix(Missile __instance)
        {
            if (GpmBootstrap.IsOurs(__instance))
                GpmUpright.Hold(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.GetSeekerType))]
    internal static class GpmGetSeekerTypePatch
    {
        private static bool Prefix(Missile __instance, ref string __result)
        {
            if (!GpmBootstrap.IsOurs(__instance))
                return true;
            __result = GpmConstants.SeekerTypeName;
            return false;
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.GetYield))]
    internal static class GpmGetYieldPatch
    {
        private static void Postfix(Missile __instance, ref float __result)
        {
            if (GpmBootstrap.IsOurs(__instance))
                __result = GpmConstants.BlastYieldKg;
        }
    }

    [HarmonyPatch(typeof(MissileDefinition), nameof(MissileDefinition.GetMass))]
    internal static class GpmDefMassPatch
    {
        private static void Postfix(MissileDefinition __instance, ref float __result)
        {
            if (__instance != null &&
                string.Equals(__instance.jsonKey, GpmConstants.MissileJsonKey, System.StringComparison.Ordinal))
                __result = GpmConstants.LaunchMassKg;
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.GetMass))]
    internal static class GpmGetMassPatch
    {
        private static void Postfix(Missile __instance, ref float __result)
        {
            if (GpmBootstrap.IsOurs(__instance))
                __result = GpmConstants.LaunchMassKg;
        }
    }

    [HarmonyPatch(typeof(AircraftSelectionMenu), nameof(AircraftSelectionMenu.DisplayInfo))]
    internal static class GpmDisplayInfoPatch
    {
        private static void Postfix(AircraftSelectionMenu __instance, WeaponInfo weaponInfo)
        {
            if (!GpmBootstrap.IsOurInfo(weaponInfo))
                return;
            weaponInfo.costPerRound = GpmConstants.Cost;
            weaponInfo.blastDamage = GpmConstants.BlastYieldKg;
            weaponInfo.massPerRound = GpmConstants.LaunchMassKg;
            AircraftSelectionDisplay.SetTmp(__instance, "weaponSeeker", GpmConstants.SeekerTypeName);
            AircraftSelectionDisplay.SetTmp(__instance, "weaponHE", "HE: " + UnitConverter.YieldReading(GpmConstants.BlastYieldKg));
            AircraftSelectionDisplay.SetTmp(__instance, "weaponCost", "C: " + UnitConverter.ValueReading(GpmConstants.Cost));
            AircraftSelectionDisplay.SetTmp(__instance, "weaponRCS", string.Format("RCS: {0}", GpmConstants.RadarSize));
        }
    }

    [HarmonyPatch(typeof(EncyclopediaBrowser), "DisplayUnitInfo")]
    internal static class GpmEncyclopediaDisplayPatch
    {
        private static void Postfix(EncyclopediaBrowser __instance, UnitDefinition definition)
        {
            if (definition == null ||
                !string.Equals(definition.jsonKey, GpmConstants.MissileJsonKey, System.StringComparison.Ordinal))
                return;
            definition.value = GpmConstants.Cost;
            definition.length = GpmConstants.LengthM;
            definition.width = GpmConstants.WidthM;
            definition.height = GpmConstants.HeightM;
            if (definition.spawnOffset.y < 0.05f)
                definition.spawnOffset = new Vector3(definition.spawnOffset.x, GpmConstants.HeightM * 0.5f, definition.spawnOffset.z);
            definition.radarSize = GpmConstants.RadarSize;
            AircraftSelectionDisplay.SetTmp(__instance, "guidance", GpmConstants.SeekerTypeName);
            AircraftSelectionDisplay.SetTmp(__instance, "yield", UnitConverter.YieldReading(GpmConstants.BlastYieldKg) + " TNT");
            AircraftSelectionDisplay.SetTmp(__instance, "mass", UnitConverter.WeightReading(GpmConstants.LaunchMassKg));
            AircraftSelectionDisplay.SetTmp(__instance, "cost", UnitConverter.ValueReading(GpmConstants.Cost));
            AircraftSelectionDisplay.SetTmp(__instance, "rcs", string.Format("{0}", GpmConstants.RadarSize));
            GpmEncyclopediaStats.ApplyMissilePanels(__instance);
        }
    }

    [HarmonyPatch(typeof(WeaponMount), nameof(WeaponMount.Initialize))]
    internal static class GpmMountInitPatch
    {
        private static void Postfix(WeaponMount __instance)
        {
            if (!GpmBootstrap.IsOurMount(__instance) || __instance.info == null)
                return;
            WeaponInfo info = GpmBootstrap.Info ?? __instance.info;
            __instance.info = info;
            __instance.sortWeapons = true;
            info.weaponName = GpmConstants.WeaponInfoName;
            info.shortName = GpmConstants.ShortName;
            info.massPerRound = GpmConstants.LaunchMassKg;
            info.blastDamage = GpmConstants.BlastYieldKg;
            info.costPerRound = GpmConstants.Cost;
            info.missile = true;
            info.bomb = false;
            info.glideBomb = false;
            info.overHorizon = true;
            Sprite? preview = GpmWeaponIcon.Get();
            if (preview != null)
                info.weaponIcon = preview;
            GpmEncyclopediaStats.ApplyTargetRequirements(info);
            if (GpmBootstrap.Definition?.unitPrefab != null)
                info.weaponPrefab = GpmBootstrap.Definition.unitPrefab;
            int ammo = __instance.ammo;
            if (__instance.prefab != null)
            {
                int counted = __instance.prefab.GetComponentsInChildren<Weapon>(true).Length;
                if (counted > 0)
                    ammo = counted;
                foreach (MountedMissile mm in __instance.prefab.GetComponentsInChildren<MountedMissile>(true))
                {
                    if (mm != null)
                        mm.info = info;
                }
            }
            __instance.mountName = ammo > 1
                ? string.Format("{0} x{1}", GpmConstants.MountDisplayName, ammo)
                : GpmConstants.MountDisplayName;
            __instance.mass = __instance.emptyMass + GpmConstants.LaunchMassKg * ammo;
        }
    }

    internal static class AircraftSelectionDisplay
    {
        internal static void SetTmp(object host, string field, string value)
        {
            System.Reflection.FieldInfo? f = host.GetType().GetField(field,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            object? tmp = f?.GetValue(host);
            if (tmp == null)
                return;
            System.Reflection.PropertyInfo? p = tmp.GetType().GetProperty("text");
            p?.SetValue(tmp, value);
        }
    }
}
