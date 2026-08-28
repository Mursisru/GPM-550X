using Gpm.Bootstrap;
using UnityEngine;

namespace Gpm.Runtime
{
    internal static class GpmDockEject
    {
        internal static bool TryEject(Missile? missile, Transform? visual)
        {
            if (missile == null)
                return false;
            Transform vis = visual != null ? visual : PrefabFactory.FindVisual(missile.transform) ?? missile.transform;

            int killed = 0;
            for (int pass = 0; pass < 4; pass++)
            {
                Transform? dock = FindDockingPortMesh(vis);
                if (dock == null)
                    dock = FindDockingPortMesh(missile.transform);
                if (dock == null)
                    break;

                Renderer[] rs = dock.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < rs.Length; i++)
                {
                    if (rs[i] != null)
                        rs[i].enabled = false;
                }
                if (dock == vis)
                    break;
                dock.gameObject.SetActive(false);
                Object.DestroyImmediate(dock.gameObject);
                killed++;
            }

            if (killed > 0)
                GpmPlugin.ModLog?.LogInfo($"GPM DockingPort DestroyImmediate x{killed}");
            return killed > 0;
        }

        private static Transform? FindDockingPortMesh(Transform root)
        {
            if (root == null)
                return null;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            Transform? fallback = null;
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null)
                    continue;
                string n = t.name;
                if (n.IndexOf("DockingPlace", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (n == GpmConstants.VisualRootName)
                    continue;
                if (n.IndexOf("DockingPort", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (t.GetComponent<MeshFilter>() != null || t.GetComponent<MeshRenderer>() != null)
                    return t;
                fallback ??= t;
            }
            return fallback;
        }
    }
}
