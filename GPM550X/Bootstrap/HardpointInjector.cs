using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gpm.Bootstrap
{
    internal static class HardpointInjector
    {
        internal static void Inject(Encyclopedia enc, Dictionary<string, WeaponMount> slotClones, WeaponMount? gpoSingle)
        {
            if (enc?.aircraft == null)
                return;
            if (gpoSingle != null && PrefabFactory.IsTuskoKey(gpoSingle.jsonKey))
            {
                GpmPlugin.ModLog?.LogError("Refusing inject: GPO-single mount still has AShM3 jsonKey.");
                return;
            }

            int ashmSets = 0;
            int gpoSets = 0;
            int gpoSkipped = 0;
            foreach (AircraftDefinition ad in enc.aircraft)
            {
                if (ad?.unitPrefab == null)
                    continue;
                InjectOnPrefab(ad, slotClones, gpoSingle, ref ashmSets, ref gpoSets, ref gpoSkipped);
            }
            GpmPlugin.ModLog?.LogInfo(
                $"HardpointInjector: AShM-300-qty sets={ashmSets} GPO-only 1x sets={gpoSets} GPO-skipped={gpoSkipped}.");
        }

        private static void InjectOnPrefab(
            AircraftDefinition ad,
            Dictionary<string, WeaponMount> slotClones,
            WeaponMount? gpoSingle,
            ref int ashmSets,
            ref int gpoSets,
            ref int gpoSkipped)
        {
            bool allowGpo = GpmAircraftFilter.AllowsGpoMount(ad);
            WeaponManager[] managers = ad.unitPrefab.GetComponentsInChildren<WeaponManager>(true);
            foreach (WeaponManager wm in managers)
            {
                if (wm?.hardpointSets == null)
                    continue;
                foreach (HardpointSet set in wm.hardpointSets)
                {
                    if (set == null)
                        continue;
                    set.weaponOptions ??= new List<WeaponMount>();
                    WeaponMount? ashm = FindAshm300Option(set.weaponOptions);
                    if (ashm != null)
                    {
                        if (string.IsNullOrEmpty(ashm.jsonKey) ||
                            !slotClones.TryGetValue(ashm.jsonKey, out WeaponMount clone) || clone == null)
                            continue;
                        if (ContainsRef(set.weaponOptions, clone))
                            continue;
                        set.weaponOptions.Add(clone);
                        ashmSets++;
                        continue;
                    }

                    if (!HasGpoOption(set.weaponOptions) || gpoSingle == null)
                        continue;
                    if (!allowGpo)
                    {
                        gpoSkipped++;
                        continue;
                    }
                    if (ContainsRef(set.weaponOptions, gpoSingle))
                        continue;
                    set.weaponOptions.Add(gpoSingle);
                    gpoSets++;
                }
            }
        }

        private static WeaponMount? FindAshm300Option(List<WeaponMount> options)
        {
            foreach (WeaponMount o in options)
            {
                if (o != null && PrefabFactory.IsAshm300SlotKey(o.jsonKey))
                    return o;
            }
            return null;
        }

        private static bool HasGpoOption(List<WeaponMount> options)
        {
            foreach (WeaponMount o in options)
            {
                if (PrefabFactory.IsGpoMount(o))
                    return true;
            }
            return false;
        }

        private static bool ContainsRef(List<WeaponMount> options, WeaponMount mount)
        {
            if (mount == null || string.IsNullOrEmpty(mount.jsonKey))
                return false;
            for (int i = 0; i < options.Count; i++)
            {
                if (ReferenceEquals(options[i], mount))
                    return true;
                if (options[i] != null &&
                    string.Equals(options[i].jsonKey, mount.jsonKey, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
