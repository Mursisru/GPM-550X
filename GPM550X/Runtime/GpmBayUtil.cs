using System;
using System.Reflection;
using HarmonyLib;
using Gpm.Bootstrap;

namespace Gpm.Runtime
{
    internal static class GpmBayUtil
    {
        internal static bool IsBayHardpoint(Hardpoint? hardpoint) =>
            hardpoint != null && hardpoint.bayDoors != null && hardpoint.bayDoors.Length > 0;

        internal static bool IsInternalBayMount(WeaponMount? mount)
        {
            if (mount == null)
                return false;
            if (mount.missileBay)
                return true;
            return PrefabFactory.IsInternalBayMountKey(mount.jsonKey);
        }

        internal static bool ShouldTreatAsBay(Hardpoint? hardpoint, WeaponMount? mount) =>
            IsBayHardpoint(hardpoint) || IsInternalBayMount(mount);

        internal static void ApplyInternalBayScalars(WeaponMount mount)
        {
            mount.RCS = GpmConstants.RadarSize;
            mount.emptyRCS = 0f;
            mount.emptyDrag = 0f;
            mount.missileBay = true;
        }

        internal static void HideHardpointPylons(Hardpoint hardpoint)
        {
            if (hardpoint == null)
                return;
            if (PylonOptionsField == null || PylonShowMethod == null)
                return;
            object? raw = PylonOptionsField.GetValue(hardpoint);
            if (raw is not Array pylons)
                return;
            foreach (object? pylon in pylons)
            {
                if (pylon == null)
                    continue;
                PylonShowMethod.Invoke(pylon, new object[] { false });
            }
            if (hardpoint.Plug != null)
                hardpoint.Plug.enabled = false;
        }

        private static readonly FieldInfo? PylonOptionsField =
            AccessTools.Field(typeof(Hardpoint), "pylonOptions");

        private static readonly MethodInfo? PylonShowMethod =
            AccessTools.Method(AccessTools.Inner(typeof(Hardpoint), "HardpointPylon"), "ShowPylon");
    }
}
