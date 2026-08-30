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
                GpmPlugin.ModLog?.LogError("GPO-single mount has AShM3 jsonKey — skipping GPO inject only.");
                gpoSingle = null;
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

        internal static void EnsureRuntime(WeaponManager wm)
        {
            if (wm == null || !GpmBootstrap.IsReady)
                return;
            Aircraft? aircraft = wm.GetComponent<Aircraft>();
            if (aircraft?.definition?.unitPrefab == null)
                return;
            WeaponManager? template = aircraft.definition.unitPrefab.GetComponent<Aircraft>()?.weaponManager;
            if (template?.hardpointSets == null || wm.hardpointSets == null)
                return;

            int count = Math.Min(wm.hardpointSets.Length, template.hardpointSets.Length);
            for (int i = 0; i < count; i++)
            {
                HardpointSet? live = wm.hardpointSets[i];
                HardpointSet? def = template.hardpointSets[i];
                if (live == null || def?.weaponOptions == null)
                    continue;
                live.weaponOptions ??= new List<WeaponMount>();
                MergeOurMounts(live.weaponOptions, def.weaponOptions);
            }
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
                    InjectAshm300Clones(set.weaponOptions, slotClones, ref ashmSets);
                    if (HasAshm300SlotOption(set.weaponOptions))
                        continue;

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

        private static void InjectAshm300Clones(
            List<WeaponMount> options,
            Dictionary<string, WeaponMount> slotClones,
            ref int ashmSets)
        {
            for (int i = 0; i < options.Count; i++)
            {
                WeaponMount? o = options[i];
                if (o == null || string.IsNullOrEmpty(o.jsonKey))
                    continue;
                if (!PrefabFactory.IsAshm300SlotKey(o.jsonKey))
                    continue;
                if (!slotClones.TryGetValue(o.jsonKey, out WeaponMount? clone) || clone == null)
                    continue;
                if (ContainsRef(options, clone))
                    continue;
                options.Add(clone);
                ashmSets++;
            }
        }

        private static bool HasAshm300SlotOption(List<WeaponMount> options)
        {
            foreach (WeaponMount o in options)
            {
                if (o != null && PrefabFactory.IsAshm300SlotKey(o.jsonKey))
                    return true;
            }
            return false;
        }

        private static void MergeOurMounts(List<WeaponMount> live, List<WeaponMount> template)
        {
            for (int i = 0; i < template.Count; i++)
            {
                WeaponMount? m = template[i];
                if (m == null || !PrefabFactory.IsOurMountKey(m.jsonKey))
                    continue;
                if (!ContainsRef(live, m))
                    live.Add(m);
            }
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
