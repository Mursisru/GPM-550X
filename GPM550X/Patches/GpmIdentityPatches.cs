using HarmonyLib;
using Gpm;

namespace Gpm.Patches
{
    [HarmonyPatch(typeof(UnitRegistry), nameof(UnitRegistry.RegisterUnit))]
    internal static class GpmPersistentIdentityPatch
    {
        private static void Postfix(Unit unit)
        {
            if (unit is not Missile missile || !GpmBootstrap.IsOurs(missile))
                return;
            GpmSpawnGate.ApplyDisplayIdentity(missile);
        }
    }
}
