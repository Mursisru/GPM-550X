using Gpm.Bootstrap;
using UnityEngine;

namespace Gpm.Runtime
{
    internal static class GpmFlyFactory
    {
        internal static GameObject? BindSharedShell(MissileDefinition? tuskoDef)
        {
            if (tuskoDef?.unitPrefab == null)
            {
                GpmPlugin.ModLog?.LogError("GPM-550X: no AShM3 unitPrefab to share.");
                return null;
            }

            Missile? mis = tuskoDef.unitPrefab.GetComponent<Missile>() ??
                           tuskoDef.unitPrefab.GetComponentInChildren<Missile>(true);
            GpmMotors.LoadProfile();
            GpmPlugin.ModLog?.LogInfo(
                $"GPM uses stock unitPrefab '{tuskoDef.unitPrefab.name}' jsonKey={tuskoDef.jsonKey}.");
            return tuskoDef.unitPrefab;
        }
    }
}
