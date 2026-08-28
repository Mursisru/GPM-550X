using System.Reflection;

namespace Gpm.Runtime
{
    internal static class GpmDefinitionMass
    {
        private static readonly FieldInfo? NullableMass =
            typeof(MissileDefinition).GetField("mass",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        internal static void Apply(MissileDefinition? def, float kg)
        {
            if (def == null || kg <= 0f)
                return;
            NullableMass?.SetValue(def, kg);
        }
    }
}
