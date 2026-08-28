using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Gpm.Patches
{
    internal static class GpmHudRange
    {
        private static readonly FieldInfo? WeaponInfoField =
            AccessTools.Field(typeof(HUDMissileState), "weaponInfo");
        private static readonly FieldInfo? AircraftField =
            AccessTools.Field(typeof(HUDMissileState), "aircraft");
        private static readonly FieldInfo? FarTargetField =
            AccessTools.Field(typeof(HUDMissileState), "farTarget");
        private static readonly FieldInfo? CloseTargetField =
            AccessTools.Field(typeof(HUDMissileState), "closeTarget");
        private static readonly FieldInfo? MaxTargetDistField =
            AccessTools.Field(typeof(HUDMissileState), "maxTargetDist");
        private static readonly FieldInfo? MinTargetDistField =
            AccessTools.Field(typeof(HUDMissileState), "minTargetDist");
        private static readonly FieldInfo? MaxTargetSpeedField =
            AccessTools.Field(typeof(HUDMissileState), "maxTargetSpeed");
        private static readonly FieldInfo? MaxTargetAngleField =
            AccessTools.Field(typeof(HUDMissileState), "maxTargetAngle");
        private static readonly FieldInfo? KnownPosField =
            AccessTools.Field(typeof(HUDMissileState), "knownPos");
        private static readonly FieldInfo? MaxRangeField =
            AccessTools.Field(typeof(HUDMissileState), "maxRange");
        private static readonly FieldInfo? MinRangeField =
            AccessTools.Field(typeof(HUDMissileState), "minRange");
        private static readonly FieldInfo? NoEscapeField =
            AccessTools.Field(typeof(HUDMissileState), "noEscapeRange");
        private static readonly FieldInfo? LastCalcField =
            AccessTools.Field(typeof(HUDMissileState), "lastWeaponRangeCalc");
        private static readonly FieldInfo? TargetListField =
            AccessTools.Field(typeof(HUDMissileState), "targetList");
        private static readonly FieldInfo? StationField =
            AccessTools.Field(typeof(HUDMissileState), "weaponStation");
        private static readonly FieldInfo? RMinField =
            AccessTools.Field(typeof(HUDMissileState), "rMinTransform");
        private static readonly FieldInfo? RMaxField =
            AccessTools.Field(typeof(HUDMissileState), "rMaxTransform");
        private static readonly FieldInfo? RNeField =
            AccessTools.Field(typeof(HUDMissileState), "rNETransform");
        private static readonly FieldInfo? MaxDistTfField =
            AccessTools.Field(typeof(HUDMissileState), "maxTargetDistTransform");
        private static readonly FieldInfo? MinDistTfField =
            AccessTools.Field(typeof(HUDMissileState), "minTargetDistTransform");
        private static readonly FieldInfo? OutRangeTfField =
            AccessTools.Field(typeof(HUDMissileState), "outRangeTransform");
        private static readonly FieldInfo? DistSpanField =
            AccessTools.Field(typeof(HUDMissileState), "targetDistSpan");
        private static readonly FieldInfo? AvgDistTfField =
            AccessTools.Field(typeof(HUDMissileState), "avgDistTransform");

        internal static bool CalcWeaponRangePrefix(HUDMissileState __instance)
        {
            if (WeaponInfoField?.GetValue(__instance) is not WeaponInfo wi || !GpmBootstrap.IsOurInfo(wi))
                return true;
            if (TargetListField?.GetValue(__instance) is not IList list || list.Count == 0)
                return false;
            if (StationField?.GetValue(__instance) is WeaponStation st && st.Ammo == 0)
                return false;
            float last = LastCalcField?.GetValue(__instance) is float l ? l : 0f;
            if (last > 0f && Time.timeSinceLevelLoad - last < 1f)
                return false;
            if (AircraftField?.GetValue(__instance) is not Aircraft ac || ac == null)
                return true;

            ScanTargets(__instance, ac, list, out float tgtDist, out float maxSpd, out float tgtAlt);
            float nez;
            float range = Gpm.Runtime.GpmCalcProxy.CalcRange(
                ac.speed, ac.GlobalPosition().y, tgtAlt, tgtDist, maxSpd, out nez);
            MaxRangeField?.SetValue(__instance, range);
            NoEscapeField?.SetValue(__instance, nez);
            LastCalcField?.SetValue(__instance, Time.timeSinceLevelLoad);
            return false;
        }

        internal static void UpdateWeaponDisplayPostfix(HUDMissileState __instance, Aircraft aircraft, List<Unit> targetList)
        {
            if (WeaponInfoField?.GetValue(__instance) is not WeaponInfo wi || !GpmBootstrap.IsOurInfo(wi))
                return;
            if (targetList == null || targetList.Count == 0)
                return;
            if (FarTargetField?.GetValue(__instance) is not Unit far || far == null)
                return;
            GlobalPosition acPos = aircraft.GlobalPosition();
            if (!aircraft.NetworkHQ.TryGetKnownPosition(far, out GlobalPosition tgtPos))
                return;
            float maxDist = FastMath.Distance(tgtPos, acPos);
            MaxTargetDistField?.SetValue(__instance, maxDist);
            if (targetList.Count <= 1)
                MinTargetDistField?.SetValue(__instance, maxDist);
            else if (CloseTargetField?.GetValue(__instance) is Unit close && close != null &&
                     aircraft.NetworkHQ.TryGetKnownPosition(close, out GlobalPosition closePos))
                MinTargetDistField?.SetValue(__instance, FastMath.Distance(closePos, acPos));
            RefreshLadderMarkers(__instance);
        }

        private static void ScanTargets(
            HUDMissileState hud,
            Aircraft ac,
            IList list,
            out float tgtDist,
            out float maxSpd,
            out float tgtAlt)
        {
            tgtDist = 0f;
            maxSpd = 0f;
            tgtAlt = ac.GlobalPosition().y;
            GlobalPosition acPos = ac.GlobalPosition();
            float farSq = 0f;
            float closeSq = float.MaxValue;
            Unit? far = null;
            Unit? close = null;
            float maxAngle = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is not Unit unit || unit == null)
                    continue;
                if (!ac.NetworkHQ.TryGetKnownPosition(unit, out GlobalPosition gp))
                    continue;
                float sq = FastMath.SquareDistance(gp, acPos);
                if (sq > farSq)
                {
                    farSq = sq;
                    far = unit;
                }
                if (sq < closeSq)
                {
                    closeSq = sq;
                    close = unit;
                }
                maxAngle = Mathf.Max(maxAngle, Vector3.Angle(gp - acPos, ac.transform.forward));
                maxSpd = Mathf.Max(unit.speed, maxSpd);
            }
            FarTargetField?.SetValue(hud, far);
            CloseTargetField?.SetValue(hud, close);
            MaxTargetAngleField?.SetValue(hud, maxAngle);
            MaxTargetSpeedField?.SetValue(hud, maxSpd);
            tgtDist = farSq > 0f ? Mathf.Sqrt(farSq) : 0f;
            MaxTargetDistField?.SetValue(hud, tgtDist);
            if (list.Count <= 1)
                MinTargetDistField?.SetValue(hud, tgtDist);
            else if (closeSq < float.MaxValue)
                MinTargetDistField?.SetValue(hud, Mathf.Sqrt(closeSq));
            if (KnownPosField?.GetValue(hud) is GlobalPosition kp)
                tgtAlt = kp.y;
            else if (far != null && ac.NetworkHQ.TryGetKnownPosition(far, out GlobalPosition fp))
                tgtAlt = fp.y;
        }

        private static void RefreshLadderMarkers(HUDMissileState hud)
        {
            if (RMinField?.GetValue(hud) is not Transform rMin ||
                RMaxField?.GetValue(hud) is not Transform rMax ||
                MaxDistTfField?.GetValue(hud) is not Transform maxTf ||
                MinDistTfField?.GetValue(hud) is not Transform minTf ||
                OutRangeTfField?.GetValue(hud) is not Transform outTf ||
                RNeField?.GetValue(hud) is not Transform rNe ||
                DistSpanField?.GetValue(hud) is not Transform span ||
                AvgDistTfField?.GetValue(hud) is not Transform avgTf)
                return;
            float minRange = MinRangeField?.GetValue(hud) is float mn ? mn : 0f;
            float maxRange = MaxRangeField?.GetValue(hud) is float mx ? mx : 1f;
            float maxTargetDist = MaxTargetDistField?.GetValue(hud) is float md ? md : 0f;
            float minTargetDist = MinTargetDistField?.GetValue(hud) is float nd ? nd : 0f;
            float noEscape = NoEscapeField?.GetValue(hud) is float ne ? ne : maxRange;
            float denom = Mathf.Max(1f, maxRange - minRange);
            rNe.position = Vector3.Lerp(rMin.position, rMax.position, Mathf.Max((noEscape - minRange) / denom, 0.1f));
            if (maxTargetDist < maxRange)
                maxTf.position = Vector3.Lerp(rMin.position, rMax.position, (maxTargetDist - minRange) / denom);
            else
                maxTf.position = Vector3.Lerp(rMax.position, outTf.position, (maxTargetDist - maxRange) / Mathf.Max(1f, maxRange));
            if (minTargetDist < maxRange)
                minTf.position = Vector3.Lerp(rMin.position, rMax.position, (minTargetDist - minRange) / denom);
            else
                minTf.position = Vector3.Lerp(rMax.position, outTf.position, (minTargetDist - maxRange) / Mathf.Max(1f, maxRange));
            float uiScale = 1080f / Screen.height;
            span.localScale = new Vector3(span.localScale.x, uiScale * (maxTf.position.y - minTf.position.y), 1f);
            avgTf.position = Vector3.Lerp(minTf.position, maxTf.position, 0.5f);
        }
    }

    [HarmonyPatch(typeof(HUDMissileState), "CalcWeaponRange")]
    internal static class GpmHudRangeCalcPatch
    {
        private static bool Prefix(HUDMissileState __instance) =>
            GpmHudRange.CalcWeaponRangePrefix(__instance);
    }

    [HarmonyPatch(typeof(HUDMissileState), nameof(HUDMissileState.UpdateWeaponDisplay))]
    internal static class GpmHudRangeDisplayPatch
    {
        private static void Postfix(HUDMissileState __instance, Aircraft aircraft, List<Unit> targetList) =>
            GpmHudRange.UpdateWeaponDisplayPostfix(__instance, aircraft, targetList);
    }
}
