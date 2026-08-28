using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Gpm.Runtime
{
    internal static class GpmEncyclopediaStats
    {
        private static readonly FieldInfo? RangeTextField =
            AccessTools.Field(typeof(EncyclopediaBrowser), "range");
        private static readonly FieldInfo? BurnTextField =
            AccessTools.Field(typeof(EncyclopediaBrowser), "burnTime");
        private static readonly FieldInfo? DeltaVTextField =
            AccessTools.Field(typeof(EncyclopediaBrowser), "deltaV");
        private static readonly FieldInfo? TopSpeedTextField =
            AccessTools.Field(typeof(EncyclopediaBrowser), "topSpeed");

        internal static void ApplyMissilePanels(EncyclopediaBrowser browser)
        {
            if (browser == null)
                return;
            float rangeM = GpmCalcProxy.EncyclopediaRangeM;
            float burnS = GpmCalcProxy.EncyclopediaBurnS;
            float deltaVMps = GpmCalcProxy.EncyclopediaDeltaVMps;
            if (rangeM < 1000f)
                rangeM = GpmConstants.DesignRangeM;
            if (burnS < 1f)
                burnS = GpmMotors.AppliedBurnS;
            SetText(RangeTextField, browser, UnitConverter.DistanceReading(rangeM));
            SetText(BurnTextField, browser, string.Format("{0:F0}s", burnS));
            SetText(DeltaVTextField, browser, UnitConverter.SpeedReading(deltaVMps));
            if (GpmMotors.AppliedTopSpeedMps > 1f)
                SetText(TopSpeedTextField, browser, UnitConverter.SpeedReading(GpmMotors.AppliedTopSpeedMps));
        }

        internal static void ApplyTargetRequirements(WeaponInfo info)
        {
            if (info == null)
                return;
            TargetRequirements tr = info.targetRequirements;
            tr.maxRange = GpmCalcProxy.EncyclopediaRangeM > 1000f
                ? GpmCalcProxy.EncyclopediaRangeM
                : GpmConstants.DesignRangeM;
            tr.minRange = GpmConstants.EncyclopediaMinRangeM;
            info.targetRequirements = tr;
        }

        private static void SetText(FieldInfo? field, EncyclopediaBrowser browser, string value)
        {
            object? tmp = field?.GetValue(browser);
            if (tmp == null)
                return;
            PropertyInfo? p = tmp.GetType().GetProperty("text");
            p?.SetValue(tmp, value);
        }
    }
}
