using System;
using System.Collections.Generic;
using Mirage;
using Gpm.Runtime;
using UnityEngine;

namespace Gpm.Bootstrap
{
    internal static class PrefabFactory
    {
        internal static GameObject CloneAsPrefab(GameObject source, string name)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            GameObject clone = UnityEngine.Object.Instantiate(source);
            clone.name = name;
            NetworkPrefabPrep.PrepareTemplate(clone);
            UnityEngine.Object.DontDestroyOnLoad(clone);
            ResetPrefabTransform(clone);
            FreezeTemplatePhysics(clone);
            clone.SetActive(false);
            NetworkPrefabPrep.PrepareTemplate(clone);
            return clone;
        }

        internal static void ResetPrefabTransform(GameObject go)
        {
            if (go == null)
                return;
            go.hideFlags = HideFlags.None;
            go.transform.SetParent(null, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        internal static void FreezeTemplatePhysics(GameObject root)
        {
            if (root == null)
                return;
            foreach (Rigidbody rb in root.GetComponentsInChildren<Rigidbody>(true))
            {
                if (rb == null)
                    continue;
                rb.detectCollisions = false;
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            foreach (Camera cam in root.GetComponentsInChildren<Camera>(true))
            {
                if (cam != null)
                    cam.enabled = false;
            }
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (light != null)
                    light.enabled = false;
            }
        }

        internal static void ActivateMountedInstance(GameObject instance, bool internalBay)
        {
            if (instance == null)
                return;
            instance.hideFlags = HideFlags.None;
            instance.SetActive(true);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            WakeMountedSlots(instance);
            foreach (Rigidbody rb in instance.GetComponentsInChildren<Rigidbody>(true))
            {
                if (rb == null)
                    continue;
                rb.isKinematic = true;
                rb.detectCollisions = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            EnableGameplayBehaviours(instance);
            if (internalBay)
                HideBayVisuals(instance);
            else
                ShowPylonVisuals(instance);
        }

        internal static void RevealBayVisuals(GameObject host)
        {
            if (host == null)
                return;
            Transform? vis = FindVisual(host.transform);
            if (vis == null)
                return;
            vis.gameObject.SetActive(true);
            Renderer[] rs = vis.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] != null)
                    rs[i].enabled = true;
            }
        }

        private static void WakeMountedSlots(GameObject instance)
        {
            MountedMissile[] slots = instance.GetComponentsInChildren<MountedMissile>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slots[i].gameObject.SetActive(true);
            }
        }

        private static void ShowPylonVisuals(GameObject instance)
        {
            EnsureVisualRenderers(instance);
            GpmStockVisual.Hide(instance);
            MountedMissile[] slots = instance.GetComponentsInChildren<MountedMissile>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;
                Transform? vis = FindVisual(slots[i].transform);
                if (vis != null)
                    VisualFit.Apply(vis);
            }
        }

        private static void HideBayVisuals(GameObject host)
        {
            HideStockRenderers(host);
            Transform? vis = FindVisual(host.transform);
            if (vis == null)
                return;
            Renderer[] rs = vis.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] != null)
                    rs[i].enabled = false;
            }
            MountedMissile[] slots = host.GetComponentsInChildren<MountedMissile>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;
                Transform? slotVis = FindVisual(slots[i].transform);
                if (slotVis == null)
                    continue;
                Renderer[] slotRs = slotVis.GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < slotRs.Length; r++)
                {
                    if (slotRs[r] != null)
                        slotRs[r].enabled = false;
                }
            }
        }

        private static void EnableGameplayBehaviours(GameObject root)
        {
            foreach (Behaviour b in root.GetComponentsInChildren<Behaviour>(true))
            {
                if (b == null)
                    continue;
                if (b is NetworkIdentity || b is NetworkBehaviour)
                    continue;
                string tn = b.GetType().Name;
                if (tn == "Camera" || tn == "AudioListener" || tn == "Flare" || tn == "Light" ||
                    tn == "ReflectionProbe" || tn == "Skybox")
                {
                    b.enabled = false;
                    continue;
                }
                if (tn == "Missile" || tn.EndsWith("Seeker", StringComparison.Ordinal))
                {
                    b.enabled = false;
                    continue;
                }
                b.enabled = true;
            }
            VisualMaterials.StripSceneJunk(root);
        }

        internal static Transform? FindVisual(Transform root)
        {
            if (root == null)
                return null;
            Transform direct = root.Find(GpmConstants.VisualRootName);
            if (direct != null)
                return direct;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == GpmConstants.VisualRootName)
                    return all[i];
            }
            return null;
        }

        internal static Transform ResolveVisualParent(GameObject host)
        {
            MountedMissile? mm = host.GetComponentInChildren<MountedMissile>(true);
            if (mm != null)
                return mm.transform;
            Missile? mis = host.GetComponentInChildren<Missile>(true);
            if (mis != null)
                return mis.transform;
            return host.transform;
        }

        private static void EnsureVisualRenderers(GameObject root)
        {
            Transform? vis = FindVisual(root.transform);
            if (vis == null)
                return;
            vis.gameObject.SetActive(true);
            foreach (Renderer r in vis.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null)
                    r.enabled = true;
            }
        }

        internal static void HideStockRenderers(GameObject root)
        {
            if (root == null)
                return;
            Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null)
                    continue;
                if (IsVisualRoot(rs[i].transform))
                    continue;
                rs[i].enabled = false;
            }
        }

        internal static bool IsVisualRoot(Transform t)
        {
            while (t != null)
            {
                if (t.name == GpmConstants.VisualRootName)
                    return true;
                t = t.parent;
            }
            return false;
        }

        internal static WeaponMount? FindMountByExactKey(Encyclopedia enc, string jsonKey)
        {
            if (string.IsNullOrEmpty(jsonKey))
                return null;
            if (Encyclopedia.WeaponLookup != null &&
                Encyclopedia.WeaponLookup.TryGetValue(jsonKey, out WeaponMount m) &&
                m != null)
                return m;
            if (enc?.weaponMounts == null)
                return null;
            foreach (WeaponMount w in enc.weaponMounts)
            {
                if (w != null && string.Equals(w.jsonKey, jsonKey, StringComparison.Ordinal))
                    return w;
            }
            return null;
        }

        internal static MissileDefinition? FindMissileByExactKey(Encyclopedia enc, string jsonKey)
        {
            if (string.IsNullOrEmpty(jsonKey))
                return null;
            if (Encyclopedia.Lookup != null &&
                Encyclopedia.Lookup.TryGetValue(jsonKey, out UnitDefinition u) &&
                u is MissileDefinition md)
                return md;
            if (enc?.missiles == null)
                return null;
            foreach (MissileDefinition missile in enc.missiles)
            {
                if (missile != null && string.Equals(missile.jsonKey, jsonKey, StringComparison.Ordinal))
                    return missile;
            }
            return null;
        }

        internal static MissileDefinition? FindTuskoMissile(Encyclopedia enc)
        {
            MissileDefinition? exact = FindMissileByExactKey(enc, GpmConstants.ShellMissileKey);
            if (exact?.unitPrefab != null)
                return exact;
            exact = FindMissileByExactKey(enc, GpmConstants.ShellMissileKeyAlt);
            if (exact?.unitPrefab != null)
                return exact;
            if (enc?.missiles == null)
                return null;
            foreach (MissileDefinition cand in enc.missiles)
            {
                if (cand?.unitPrefab == null || string.IsNullOrEmpty(cand.jsonKey))
                    continue;
                if (IsTuskoKey(cand.jsonKey))
                    return cand;
            }
            return null;
        }

        internal static WeaponMount? FindTuskoSingleMount(Encyclopedia enc)
        {
            WeaponMount? m = FindMountByExactKey(enc, GpmConstants.TuskoMountSingle);
            if (m?.prefab != null)
                return m;
            if (enc?.weaponMounts == null)
                return null;
            WeaponMount? fallback = null;
            foreach (WeaponMount w in enc.weaponMounts)
            {
                if (w?.prefab == null || !IsTuskoKey(w.jsonKey))
                    continue;
                fallback ??= w;
                int weapons = w.prefab.GetComponentsInChildren<Weapon>(true).Length;
                if (weapons <= 1)
                    return w;
            }
            return fallback;
        }

        internal static bool IsAshm300SlotKey(string? jsonKey)
        {
            if (string.IsNullOrEmpty(jsonKey) ||
                !jsonKey!.StartsWith(GpmConstants.Ashm300SlotPrefix, StringComparison.OrdinalIgnoreCase))
                return false;
            if (jsonKey.IndexOf("_RC", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            return true;
        }

        internal static bool IsInternalBayMountKey(string? jsonKey)
        {
            if (jsonKey is not { Length: > 0 } key || !IsOurMountKey(key))
                return false;
            string suffix = key.Substring(GpmConstants.MountKeyPrefix.Length);
            return suffix.StartsWith("internal", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsInternalBayDonor(string? donorKey, WeaponMount? donor)
        {
            if (donor != null && donor.missileBay)
                return true;
            if (donorKey is not { Length: > 0 } key)
                return false;
            return key.IndexOf("internal", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsTuskoKey(string? jsonKey)
        {
            return !string.IsNullOrEmpty(jsonKey) &&
                   jsonKey!.StartsWith(GpmConstants.TuskoPrefix, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsGpoKey(string? jsonKey)
        {
            return !string.IsNullOrEmpty(jsonKey) &&
                   jsonKey!.StartsWith(GpmConstants.GpoPrefix, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsGpoMount(WeaponMount? mount)
        {
            if (mount == null)
                return false;
            if (IsGpoKey(mount.jsonKey))
                return true;
            string? name = mount.info != null ? mount.info.weaponName : null;
            return !string.IsNullOrEmpty(name) &&
                   name!.IndexOf(GpmConstants.GpoWeaponName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsOurMountKey(string? jsonKey)
        {
            return !string.IsNullOrEmpty(jsonKey) &&
                   jsonKey!.StartsWith(GpmConstants.MountKeyPrefix, StringComparison.Ordinal);
        }

        internal static string MountKeyFromDonor(string? donorKey, string stripPrefix)
        {
            if (string.IsNullOrEmpty(donorKey))
                return GpmConstants.MountJsonKeySingle;
            string suffix = donorKey!;
            if (suffix.StartsWith(stripPrefix, StringComparison.OrdinalIgnoreCase))
                suffix = suffix.Substring(stripPrefix.Length);
            if (suffix.StartsWith("_", StringComparison.Ordinal))
                suffix = suffix.Substring(1);
            if (string.IsNullOrEmpty(suffix))
                suffix = "single";
            return GpmConstants.MountKeyPrefix + suffix;
        }

        internal static void AssertTuskoIntact(Encyclopedia enc) => AssertDonorsIntact(enc);

        internal static void AssertDonorsIntact(Encyclopedia enc)
        {
            if (enc == null)
                return;
            if (enc.missiles != null)
            {
                foreach (MissileDefinition m in enc.missiles)
                {
                    if (m == null || !IsTuskoKey(m.jsonKey))
                        continue;
                    if (!string.IsNullOrEmpty(m.unitName) &&
                        m.unitName.IndexOf("GPM", StringComparison.OrdinalIgnoreCase) >= 0)
                        GpmPlugin.ModLog?.LogError($"Tusko unitName mutated: '{m.unitName}' key='{m.jsonKey}'");
                }
            }
            if (enc.weaponMounts == null)
                return;
            foreach (WeaponMount m in enc.weaponMounts)
            {
                if (m == null || string.IsNullOrEmpty(m.jsonKey))
                    continue;
                bool tusko = IsTuskoKey(m.jsonKey);
                bool ashm300 = IsAshm300SlotKey(m.jsonKey);
                bool gpo = IsGpoKey(m.jsonKey);
                if (!tusko && !ashm300 && !gpo)
                    continue;
                if (m.mountName != null &&
                    m.mountName.IndexOf("GPM", StringComparison.OrdinalIgnoreCase) >= 0)
                    GpmPlugin.ModLog?.LogError($"Donor corrupted: mountName '{m.mountName}' on '{m.jsonKey}'");
                if (m.info != null &&
                    !string.IsNullOrEmpty(m.info.weaponName) &&
                    m.info.weaponName.IndexOf("GPM", StringComparison.OrdinalIgnoreCase) >= 0)
                    GpmPlugin.ModLog?.LogError($"Donor WeaponInfo mutated: '{m.info.weaponName}' on '{m.jsonKey}'");
            }
        }

        internal static void CopyMountScalars(WeaponMount src, WeaponMount dst)
        {
            dst.ammo = src.ammo;
            dst.turret = src.turret;
            dst.missileBay = src.missileBay;
            dst.radar = false;
            dst.tailHook = false;
            dst.slingloadHook = false;
            dst.countermeasure = false;
            dst.colorable = src.colorable;
            dst.Cargo = false;
            dst.Troops = false;
            dst.sortWeapons = true;
            dst.GearSafety = src.GearSafety;
            dst.GroundSafety = src.GroundSafety;
            dst.GunAmmo = false;
            dst.emptyCost = src.emptyCost;
            dst.emptyMass = src.emptyMass;
            dst.mass = src.mass;
            dst.drag = src.drag;
            dst.emptyDrag = src.emptyDrag;
            dst.RCS = src.RCS;
            dst.emptyRCS = src.emptyRCS;
            dst.dontAutomaticallyAddToEncyclopedia = false;
        }

        internal static void CopyUnitDefScalars(UnitDefinition src, UnitDefinition dst)
        {
            dst.visibleRange = src.visibleRange;
            dst.iconRange = src.iconRange;
            dst.iconSize = src.iconSize;
            dst.mapIconSize = src.mapIconSize;
            dst.captureStrength = 0f;
            dst.captureDefense = 0f;
            dst.manpower = 0f;
            dst.armorTier = src.armorTier;
            dst.damageTolerance = src.damageTolerance;
            dst.minEditorHeight = src.minEditorHeight;
            dst.maxEditorHeight = src.maxEditorHeight;
            dst.code = src.code;
            dst.spawnOffset = src.spawnOffset;
        }

        internal static void CopyMapIdentity(UnitDefinition src, UnitDefinition dst)
        {
            dst.mapIcon = src.mapIcon;
            dst.friendlyIcon = src.friendlyIcon;
            dst.hostileIcon = src.hostileIcon;
            dst.mapOrient = src.mapOrient;
            dst.mapIconSize = src.mapIconSize;
            dst.typeIdentity = src.typeIdentity;
            dst.roleIdentity = src.roleIdentity;
            dst.IsObstacle = false;
        }

        internal static bool ContainsNet(List<INetworkDefinition> list, INetworkDefinition item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item))
                    return true;
            }
            return false;
        }
    }
}
