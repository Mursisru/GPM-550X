using System.Reflection;
using Gpm.Bootstrap;
using UnityEngine;

namespace Gpm.Runtime
{
    /// <summary>Loft cruise altitude + cap steering G (Tusko seeker otherwise pulls 9g+ on climb).</summary>
    internal static class GpmCruiseLoft
    {
        private static readonly FieldInfo? AltField =
            typeof(OpticalSeekerCruiseMissile).GetField("altitudeTarget", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? GLimitField =
            typeof(Missile).GetField("gLimit", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void Apply(Missile missile)
        {
            if (missile == null || !GpmBootstrap.IsOurs(missile))
                return;

            OpticalSeekerCruiseMissile? seeker = missile.GetComponent<OpticalSeekerCruiseMissile>();
            if (seeker == null)
                seeker = missile.GetComponentInChildren<OpticalSeekerCruiseMissile>(true);
            if (seeker != null && AltField != null)
                AltField.SetValue(seeker, GpmConstants.CruiseAltitudeM);

            if (GLimitField != null)
                GLimitField.SetValue(missile, GpmConstants.SeekerGLimit);

            GpmUpright.Apply(missile);
        }
    }
}
