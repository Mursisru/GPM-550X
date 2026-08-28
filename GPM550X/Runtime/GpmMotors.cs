using System;
using System.Reflection;
using UnityEngine;

namespace Gpm.Runtime
{
    internal static class GpmMotors
    {
        private static readonly FieldInfo? MotorsField =
            typeof(Missile).GetField("motors", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? CurrentMotorField =
            typeof(Missile).GetField("motor", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? MassField =
            typeof(Missile).GetField("mass", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? BlastYieldField =
            typeof(Missile).GetField("blastYield", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? MotorStageField =
            typeof(Missile).GetField("motorStage", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Type? MotorType =
            typeof(Missile).GetNestedType("Motor", BindingFlags.NonPublic | BindingFlags.Public);

        internal static float AppliedThrustN { get; private set; }
        internal static float AppliedFuelKg { get; private set; }
        internal static float AppliedBurnS { get; private set; }
        internal static float AppliedTopSpeedMps { get; private set; }

        internal static void LoadProfile()
        {
            AppliedThrustN = GpmConstants.MotorThrustN;
            AppliedFuelKg = GpmConstants.MotorFuelKg;
            AppliedBurnS = GpmConstants.MotorBurnS;
            AppliedTopSpeedMps = GpmConstants.DesignTopSpeedMps;
            GpmPlugin.ModLog?.LogInfo(
                $"GPM motor profile F={AppliedThrustN:F0}N fuel={AppliedFuelKg:F1}kg burn={AppliedBurnS:F1}s top={AppliedTopSpeedMps:F0} ({GpmConstants.DesignTopSpeedMach:F1}M)");
        }

        internal static void Apply(Missile missile)
        {
            if (missile == null || MotorsField == null || MotorType == null)
                return;
            if (AppliedThrustN < 1f)
                LoadProfile();

            MassField?.SetValue(missile, GpmConstants.LaunchMassKg);
            BlastYieldField?.SetValue(missile, GpmConstants.BlastYieldKg);
            if (missile.rb != null)
                missile.rb.mass = GpmConstants.LaunchMassKg;

            Array? src = MotorsField.GetValue(missile) as Array;
            if (src == null || src.Length == 0)
                return;
            object? src0 = src.GetValue(0);
            if (src0 == null)
                return;

            Array dst = Array.CreateInstance(MotorType, 1);
            object motor = CloneMotor(src0);
            WriteFloat(motor, "thrust", AppliedThrustN);
            WriteFloat(motor, "fuelMass", AppliedFuelKg);
            WriteFloat(motor, "burnTime", AppliedBurnS);
            WriteFloat(motor, "topSpeed", AppliedTopSpeedMps);
            WritePrivateFloat(motor, "delayTimer", 0f);
            if (AppliedBurnS > 0.1f)
                WritePrivateFloat(motor, "burnRate", AppliedFuelKg / AppliedBurnS);
            dst.SetValue(motor, 0);
            MotorsField.SetValue(missile, dst);
            MotorStageField?.SetValue(missile, 0);
            CurrentMotorField?.SetValue(missile, motor);
        }

        private static object CloneMotor(object src)
        {
            object dst = Activator.CreateInstance(MotorType!)!;
            FieldInfo[] fields = MotorType!.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
                fields[i].SetValue(dst, fields[i].GetValue(src));
            WriteBool(dst, "activated", false);
            WritePrivateFloat(dst, "burnRate", 0f);
            FieldInfo? startup = MotorType.GetField("startupSource", BindingFlags.Instance | BindingFlags.NonPublic);
            startup?.SetValue(dst, null);
            EnsureEmptyArray(dst, "particleSystems");
            EnsureEmptyArray(dst, "trailEmitters");
            EnsureEmptyArray(dst, "audioSources");
            EnsureEmptyArray(dst, "lights");
            EnsureEmptyArray(dst, "destructEffects");
            return dst;
        }

        private static void EnsureEmptyArray(object motor, string name)
        {
            FieldInfo? f = MotorType?.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null || !f.FieldType.IsArray)
                return;
            f.SetValue(motor, Array.CreateInstance(f.FieldType.GetElementType()!, 0));
        }

        private static void WriteBool(object motor, string name, bool value)
        {
            FieldInfo? f = MotorType?.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(bool))
                f.SetValue(motor, value);
        }

        internal static float ReadFloat(object motor, string name, float fallback)
        {
            FieldInfo? f = MotorType?.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null)
                return fallback;
            object? v = f.GetValue(motor);
            return v is float fv ? fv : fallback;
        }

        internal static void WriteFloat(object motor, string name, float value)
        {
            FieldInfo? f = MotorType?.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(float))
                f.SetValue(motor, value);
        }

        private static void WritePrivateFloat(object motor, string name, float value)
        {
            FieldInfo? f = MotorType?.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(float))
                f.SetValue(motor, value);
        }
    }
}
