using Gpm.Bootstrap;
using UnityEngine;

namespace Gpm.Runtime
{
    internal static class GpmCalcProxy
    {
        private static Missile? _missile;

        internal static float EncyclopediaRangeM { get; private set; }
        internal static float EncyclopediaDeltaVMps { get; private set; }
        internal static float EncyclopediaBurnS { get; private set; }

        internal static void Init(Encyclopedia enc)
        {
            if (_missile != null || enc == null)
                return;

            MissileDefinition? tusko = PrefabFactory.FindTuskoMissile(enc);
            if (tusko?.unitPrefab == null)
            {
                GpmPlugin.ModLog?.LogWarning("GpmCalcProxy: no AShM3 donor.");
                return;
            }

            GameObject go = Object.Instantiate(tusko.unitPrefab);
            go.name = "GpmCalcProxy";
            go.SetActive(false);
            Object.DontDestroyOnLoad(go);
            NetworkPrefabPrep.PrepareTemplate(go);

            _missile = go.GetComponent<Missile>() ?? go.GetComponentInChildren<Missile>(true);
            if (_missile == null)
            {
                Object.Destroy(go);
                GpmPlugin.ModLog?.LogWarning("GpmCalcProxy: no Missile component.");
                return;
            }

            GpmMotors.LoadProfile();
            GpmMotors.Apply(_missile);
            CacheEncyclopediaStats();
            if (EncyclopediaRangeM < 90000f || EncyclopediaRangeM > 110000f)
            {
                GpmPlugin.ModLog?.LogWarning(
                    $"GpmCalcProxy: encyclopedia range {EncyclopediaRangeM:F0}m outside 90-110 km band — check motor constants.");
            }
            GpmPlugin.ModLog?.LogInfo(
                $"GpmCalcProxy restRange={EncyclopediaRangeM:F0}m burn={EncyclopediaBurnS:F1}s dV={EncyclopediaDeltaVMps:F0} thrust={GpmMotors.AppliedThrustN:F0} fuel={GpmMotors.AppliedFuelKg:F1}");
        }

        private static void CacheEncyclopediaStats()
        {
            if (_missile == null)
                return;
            EncyclopediaBurnS = _missile.GetTotalBurnTime();
            EncyclopediaDeltaVMps = _missile.CalcDeltaV();
            float nez;
            EncyclopediaRangeM = _missile.CalcRange(
                GpmConstants.CalcRestLaunchSpeedMps,
                GpmConstants.CalcRestLaunchAltM,
                GpmConstants.CalcRestTargetAltM,
                GpmConstants.CalcRestTargetDistM,
                0f,
                out nez);
        }

        internal static float CalcRange(
            float launchSpeed,
            float launchAltitude,
            float targetAltitude,
            float targetDist,
            float targetRelativeSpeed,
            out float noEscapeDistance)
        {
            if (_missile != null)
            {
                return _missile.CalcRange(
                    launchSpeed, launchAltitude, targetAltitude, targetDist, targetRelativeSpeed, out noEscapeDistance);
            }
            noEscapeDistance = GpmConstants.DesignRangeM * 0.65f;
            return GpmConstants.DesignRangeM;
        }
    }
}
