using System.Collections.Generic;
using System.Reflection;
using Gpm.Blueprinter;
using Gpm.Bootstrap;
using Gpm.Runtime;
using UnityEngine;

namespace Gpm
{
    internal sealed class GpmTag : MonoBehaviour
    {
        internal bool FlightReady;
        internal bool VisualReady;
    }

    internal static class GpmSpawnGate
    {
        private static readonly FieldInfo? InfoField =
            typeof(Missile).GetField("info", BindingFlags.Instance | BindingFlags.NonPublic);

        private const float PendingTtlS = 8f;
        internal static int Pending;
        internal static bool InFlight;
        private static float _until = -1f;
        private static Unit? _pendingTarget;
        private static readonly Queue<MountedMissile?> PendingMounts = new Queue<MountedMissile?>(8);

        internal static void NoteFire(MountedMissile? mount, Unit? target)
        {
            Expire();
            Pending++;
            _until = Time.realtimeSinceStartup + PendingTtlS;
            _pendingTarget = target;
            PendingMounts.Enqueue(mount);
            GpmMountVisual.HideFired(mount);
            SyncSharedInfo(mount);
        }

        internal static bool HasRecentFire() =>
            _until > 0f && Time.realtimeSinceStartup <= _until;

        internal static bool ShouldRescueClaim(GameObject? prefab)
        {
            if (!HasRecentFire())
                return false;
            GameObject? fly = GpmBootstrap.Definition?.unitPrefab;
            return fly != null && ReferenceEquals(prefab, fly);
        }

        internal static void SyncSharedInfo(MountedMissile? mount)
        {
            WeaponInfo? shared = GpmBootstrap.Info;
            GameObject? fly = GpmBootstrap.Definition?.unitPrefab;
            if (shared == null)
                return;
            if (fly != null)
                shared.weaponPrefab = fly;
            if (mount != null)
                mount.info = shared;
        }

        internal static bool TryBegin()
        {
            Expire();
            if (Pending <= 0)
                return false;
            Pending--;
            InFlight = true;
            return true;
        }

        internal static void End() => InFlight = false;

        private static void Expire()
        {
            if (Pending <= 0)
                return;
            if (_until < 0f || Time.realtimeSinceStartup <= _until)
                return;
            Pending = 0;
            _until = -1f;
            _pendingTarget = null;
            PendingMounts.Clear();
        }

        private static Missile? _stampMissile;
        private static UnitDefinition? _stampSavedDef;

        internal static bool BeginPrefabStamp(GameObject? prefab)
        {
            EndPrefabStamp();
            MissileDefinition? ours = GpmBootstrap.Definition;
            if (prefab == null || ours == null)
                return false;
            Missile? m = prefab.GetComponent<Missile>() ?? prefab.GetComponentInChildren<Missile>(true);
            if (m == null)
                return false;
            _stampMissile = m;
            _stampSavedDef = m.definition;
            m.definition = ours;
            return true;
        }

        internal static void EndPrefabStamp()
        {
            if (_stampMissile != null && _stampSavedDef != null)
                _stampMissile.definition = _stampSavedDef;
            _stampMissile = null;
            _stampSavedDef = null;
        }

        internal static void ApplyDisplayIdentity(Missile missile)
        {
            if (missile == null)
                return;
            MissileDefinition? def = GpmBootstrap.Definition;
            if (def != null)
                missile.definition = def;
            missile.NetworkunitName = GpmConstants.UnitName;
            missile.unitName = GpmConstants.UnitName;
            if (!UnitRegistry.TryGetPersistentUnit(missile.persistentID, out PersistentUnit pu) || pu == null)
                return;
            pu.unitName = GpmConstants.UnitName;
            if (def != null)
                pu.definition = def;
        }

        internal static bool IsSharedShell(GameObject? go)
        {
            if (go == null)
                return false;
            GameObject? fly = GpmBootstrap.Definition?.unitPrefab;
            return fly != null && ReferenceEquals(go, fly);
        }

        internal static void TryEarlyVisual(Missile? missile)
        {
            if (missile == null)
                return;
            try
            {
                if (IsSharedShell(missile.gameObject))
                    return;
                if (HasForeignOwnerTag(missile))
                    return;
                if (!GpmBootstrap.IsOurs(missile))
                    return;

                GpmStockVisual.Hide(missile.gameObject);
                GpmTag? tag = missile.GetComponent<GpmTag>();
                if (tag != null && tag.VisualReady && GpmVisualStamp.FindVisual(missile.transform) != null)
                    return;

                NobpContent.TryLoad();
                if (GpmVisualStamp.FindVisual(missile.transform) == null && NobpContent.VisualPrefab != null)
                    GpmVisualStamp.Stamp(missile.gameObject, NobpContent.VisualPrefab);
                else
                    GpmStockVisual.Hide(missile.gameObject);

                if (tag == null)
                    tag = missile.gameObject.AddComponent<GpmTag>();
                tag.VisualReady = GpmVisualStamp.FindVisual(missile.transform) != null;
            }
            catch (System.Exception ex)
            {
                GpmPlugin.ModLog?.LogError($"TryEarlyVisual: {ex}");
            }
        }

        internal static void Claim(Missile missile, Unit? fireTarget)
        {
            if (missile == null)
                return;
            if (IsSharedShell(missile.gameObject))
                return;
            if (HasForeignOwnerTag(missile))
                return;

            ApplyDisplayIdentity(missile);
            if (GpmBootstrap.Info != null)
                InfoField?.SetValue(missile, GpmBootstrap.Info);
            if (missile.GetComponent<GpmTag>() == null)
                missile.gameObject.AddComponent<GpmTag>();

            Unit? t = fireTarget != null ? fireTarget : _pendingTarget;
            if (t != null && !t.disabled)
                missile.SetTarget(t);
            _pendingTarget = null;

            MountedMissile? firedMount = PendingMounts.Count > 0 ? PendingMounts.Dequeue() : null;
            GpmMountVisual.HideFired(firedMount);

            GpmMotorFx.SilenceStock(missile);
            missile.RCS = GpmConstants.RadarSize;

            NobpContent.TryLoad();
            if (GpmVisualStamp.FindVisual(missile.transform) == null && NobpContent.VisualPrefab != null)
                GpmVisualStamp.Stamp(missile.gameObject, NobpContent.VisualPrefab);

            Transform? vis = GpmVisualStamp.FindVisual(missile.transform);
            FinishFlight(missile, vis);
            GpmStockVisual.Hide(missile.gameObject);
            GpmTag? ready = missile.GetComponent<GpmTag>();
            if (ready != null)
                ready.VisualReady = vis != null || GpmVisualStamp.FindVisual(missile.transform) != null;
        }

        internal static void FinishFlight(Missile missile, Transform? vis = null)
        {
            if (missile == null)
                return;
            missile.RCS = GpmConstants.RadarSize;
            GpmTargetSync.Apply(missile);
            GpmCruiseLoft.Apply(missile);

            GpmTag? tag = missile.GetComponent<GpmTag>();
            if (tag != null && tag.FlightReady)
                return;

            GpmMotors.Apply(missile);
            GpmMotorFx.SilenceStock(missile);
            GpmMotorFx.Bind(missile);
            if (vis == null)
                vis = GpmVisualStamp.FindVisual(missile.transform);
            GpmDockEject.TryEject(missile, vis);
            if (tag != null)
                tag.FlightReady = true;
        }

        internal static void Ensure(Missile missile)
        {
            if (missile == null || !GpmBootstrap.IsOurs(missile))
                return;
            if (missile.GetComponent<GpmTag>() == null)
                Claim(missile, _pendingTarget);
            else
            {
                ApplyDisplayIdentity(missile);
                FinishFlight(missile);
            }
        }

        internal static bool IsOurFlyPrefab(GameObject? go)
        {
            if (go == null)
                return false;
            GameObject? fly = GpmBootstrap.Definition?.unitPrefab;
            if (fly != null && ReferenceEquals(go, fly))
                return true;
            return go.GetComponent<GpmTag>() != null || go.GetComponentInChildren<GpmTag>(true) != null;
        }

        private static bool HasForeignOwnerTag(Missile missile)
        {
            if (missile == null)
                return false;
            MonoBehaviour[] comps = missile.GetComponents<MonoBehaviour>();
            for (int i = 0; i < comps.Length; i++)
            {
                MonoBehaviour? c = comps[i];
                if (c == null)
                    continue;
                string n = c.GetType().Name;
                for (int t = 0; t < GpmConstants.ForeignOwnerTags.Length; t++)
                {
                    if (n.IndexOf(GpmConstants.ForeignOwnerTags[t], System.StringComparison.Ordinal) >= 0)
                        return true;
                }
            }
            return false;
        }
    }
}
