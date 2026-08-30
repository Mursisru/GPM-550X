using HarmonyLib;
using Gpm.Bootstrap;
using Gpm.Runtime;

namespace Gpm.Patches
{
    /// <summary>Internal weapon bays: no ETOS pylons, no aircraft RCS spike from mount emptyRCS.</summary>
    internal static class GpmBayRcs
    {
        private static readonly AccessTools.FieldRef<MountedMissile, Hardpoint> HardpointRef =
            AccessTools.FieldRefAccess<MountedMissile, Hardpoint>("hardpoint");

        private static readonly AccessTools.FieldRef<MountedMissile, WeaponMount> MountRef =
            AccessTools.FieldRefAccess<MountedMissile, WeaponMount>("mount");

        internal static bool TrySuppress(WeaponMount? mount, Hardpoint? hardpoint, out float savedRcs)
        {
            savedRcs = 0f;
            if (mount == null || !GpmBootstrap.IsOurMount(mount) || !GpmBayUtil.ShouldTreatAsBay(hardpoint, mount))
                return false;
            savedRcs = mount.RCS;
            mount.RCS = mount.emptyRCS;
            return true;
        }

        internal static void Restore(WeaponMount? mount, float savedRcs, bool active)
        {
            if (!active || mount == null)
                return;
            mount.RCS = savedRcs;
        }

        internal static WeaponMount? GetMount(MountedMissile? mm) =>
            mm == null ? null : MountRef(mm);

        internal static Hardpoint? GetHardpoint(MountedMissile? mm) =>
            mm == null ? null : HardpointRef(mm);
    }

    [HarmonyPatch(typeof(MountedMissile), nameof(MountedMissile.AttachToHardpoint))]
    internal static class GpmBayRcsAttachPatch
    {
        private static void Prefix(Hardpoint hardpoint, WeaponMount weaponMount, out float __state)
        {
            __state = float.NaN;
            if (GpmBayRcs.TrySuppress(weaponMount, hardpoint, out float saved))
                __state = saved;
        }

        private static void Finalizer(WeaponMount weaponMount, float __state)
        {
            GpmBayRcs.Restore(weaponMount, __state, !float.IsNaN(__state));
        }
    }

    [HarmonyPatch(typeof(MountedMissile), "RemoveFromHardpoint")]
    internal static class GpmBayRcsRemovePatch
    {
        private static void Prefix(MountedMissile __instance, out float __state)
        {
            __state = float.NaN;
            WeaponMount? mount = GpmBayRcs.GetMount(__instance);
            Hardpoint? hp = GpmBayRcs.GetHardpoint(__instance);
            if (GpmBayRcs.TrySuppress(mount, hp, out float saved))
                __state = saved;
        }

        private static void Finalizer(MountedMissile __instance, float __state)
        {
            GpmBayRcs.Restore(GpmBayRcs.GetMount(__instance), __state, !float.IsNaN(__state));
        }
    }

    [HarmonyPatch(typeof(MountedMissile), nameof(MountedMissile.Rearm))]
    internal static class GpmBayRcsRearmPatch
    {
        private static void Prefix(MountedMissile __instance, out float __state)
        {
            __state = float.NaN;
            WeaponMount? mount = GpmBayRcs.GetMount(__instance);
            Hardpoint? hp = GpmBayRcs.GetHardpoint(__instance);
            if (GpmBayRcs.TrySuppress(mount, hp, out float saved))
                __state = saved;
        }

        private static void Finalizer(MountedMissile __instance, float __state)
        {
            GpmBayRcs.Restore(GpmBayRcs.GetMount(__instance), __state, !float.IsNaN(__state));
        }
    }

    [HarmonyPatch(typeof(Hardpoint), nameof(Hardpoint.ShowPylon))]
    internal static class GpmBayShowPylonPatch
    {
        private static void Postfix(Hardpoint __instance, bool weaponLoaded)
        {
            if (!weaponLoaded)
                return;
            WeaponMount? mount = __instance.GetMount();
            if (!GpmBootstrap.IsOurMount(mount))
                return;
            if (!GpmBayUtil.ShouldTreatAsBay(__instance, mount))
                return;
            GpmBayUtil.HideHardpointPylons(__instance);
        }
    }
}
