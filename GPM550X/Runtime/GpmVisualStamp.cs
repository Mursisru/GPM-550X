using Mirage;
using Gpm.Bootstrap;
using Gpm.Runtime;
using UnityEngine;

namespace Gpm
{
    internal static class GpmVisualStamp
    {
        internal static Transform? FindVisual(Transform root) => PrefabFactory.FindVisual(root);

        /// <summary>Stamp every rail slot; park only the mount template root (never deactivate MountedMissile).</summary>
        internal static void StampMountTemplate(GameObject mountGo, GameObject? visualPrefab)
        {
            if (mountGo == null || visualPrefab == null)
                return;

            MountedMissile[] mms = mountGo.GetComponentsInChildren<MountedMissile>(true);
            if (mms.Length == 0)
            {
                Stamp(mountGo, visualPrefab);
                mountGo.SetActive(false);
                NetworkPrefabPrep.PrepareTemplate(mountGo);
                return;
            }

            int n = 0;
            for (int i = 0; i < mms.Length; i++)
            {
                if (mms[i] != null && Stamp(mms[i].gameObject, visualPrefab))
                    n++;
            }
            if (n > 0)
                GpmStockVisual.Hide(mountGo);
            mountGo.SetActive(false);
            NetworkPrefabPrep.PrepareTemplate(mountGo);
        }

        internal static bool Stamp(GameObject host, GameObject? visualPrefab)
        {
            if (host == null || visualPrefab == null)
                return false;
            if (GpmSpawnGate.IsSharedShell(host))
                return false;
            if (FindVisual(host.transform) != null)
            {
                GpmStockVisual.Hide(host);
                return true;
            }

            Transform parent = PrefabFactory.ResolveVisualParent(host);
            GameObject vis = Object.Instantiate(visualPrefab, parent, false);
            vis.name = GpmConstants.VisualRootName;
            vis.hideFlags = HideFlags.None;
            vis.SetActive(true);

            VisualMaterials.StripSceneJunk(vis);
            StripVisualPhysics(vis);
            VisualMaterials.MatchHostDrawState(vis, host);

            int visOn = 0;
            foreach (Renderer r in vis.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null)
                    continue;
                r.enabled = true;
                visOn++;
            }
            if (visOn > 0)
                GpmStockVisual.Hide(host);

            VisualFit.Apply(vis.transform);
            VisualMaterials.ApplyFbxLook(vis);
            return visOn > 0;
        }

        internal static bool TryMeasurePrefab(GameObject visualPrefab, out Vector3 size)
        {
            size = new Vector3(GpmConstants.LengthM, GpmConstants.HeightM, GpmConstants.WidthM);
            if (visualPrefab == null)
                return false;
            GameObject tmp = Object.Instantiate(visualPrefab);
            tmp.name = "GpmMeasureTmp";
            tmp.SetActive(true);
            Renderer[] rs = tmp.GetComponentsInChildren<Renderer>(true);
            Bounds? b = null;
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null)
                    continue;
                if (b == null)
                    b = rs[i].bounds;
                else
                {
                    Bounds nb = b.Value;
                    nb.Encapsulate(rs[i].bounds);
                    b = nb;
                }
            }
            Object.DestroyImmediate(tmp);
            if (!b.HasValue)
                return false;
            size = b.Value.size;
            return true;
        }

        private static void StripVisualPhysics(GameObject vis)
        {
            Collider[] cols = vis.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null)
                    cols[i].enabled = false;
            }
            NetworkIdentity[] ids = vis.GetComponentsInChildren<NetworkIdentity>(true);
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] != null)
                    ids[i].enabled = false;
            }
        }
    }
}
