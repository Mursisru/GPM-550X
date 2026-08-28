using System.Reflection;
using Gpm.Bootstrap;
using UnityEngine;

namespace Gpm.Runtime
{
    /// <summary>Roll-level via vanilla uprightPreference; damp bank rate only.</summary>
    internal static class GpmUpright
    {
        private static readonly FieldInfo? UprightField =
            typeof(Missile).GetField("uprightPreference", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void Apply(Missile missile)
        {
            if (missile == null)
                return;
            UprightField?.SetValue(missile, GpmConstants.UprightPreference);
        }

        internal static void Hold(Missile missile)
        {
            if (missile?.rb == null || !GpmBootstrap.IsOurs(missile))
                return;

            Transform t = missile.transform;
            Vector3 local = t.InverseTransformVector(missile.rb.angularVelocity);
            local.z = Mathf.Lerp(local.z, 0f, GpmConstants.BankKillRate);
            missile.rb.angularVelocity = t.TransformVector(local);
        }
    }
}
