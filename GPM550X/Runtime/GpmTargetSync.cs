using System.Reflection;
using Gpm.Bootstrap;
using UnityEngine;

namespace Gpm.Runtime
{
    /// <summary>Resolve missile.target + seeker.targetUnit for HUD and cruise INS climb.</summary>
    internal static class GpmTargetSync
    {
        private static readonly FieldInfo? TargetField =
            typeof(Missile).GetField("target", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? SeekerTargetField =
            typeof(MissileSeeker).GetField("targetUnit", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void Apply(Missile missile)
        {
            if (missile == null || !GpmBootstrap.IsOurs(missile))
                return;
            if (!missile.targetID.IsValid)
                return;
            if (!UnitRegistry.TryGetUnit(new PersistentID?(missile.targetID), out Unit unit) ||
                unit == null || unit.disabled)
                return;

            TargetField?.SetValue(missile, unit);
            MissileSeeker? seeker = missile.GetComponent<MissileSeeker>();
            if (seeker != null)
                SeekerTargetField?.SetValue(seeker, unit);
            missile.SetTarget(unit);
        }
    }
}
