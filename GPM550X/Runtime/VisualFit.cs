using UnityEngine;

namespace Gpm.Runtime
{
    internal static class VisualFit
    {
        internal static void Apply(Transform vis)
        {
            if (vis == null)
                return;
            vis.localPosition = Vector3.zero;
            vis.localRotation = Quaternion.identity;
            vis.localScale = Vector3.one;
            EnsureRenderersOn(vis);

            if (!TryEncapsulateLocal(vis, out Bounds local, includeDisabled: true))
                return;

            Vector3 size = local.size;
            int longAxis = 0;
            if (size.y >= size.x && size.y >= size.z)
                longAxis = 1;
            else if (size.z >= size.x && size.z >= size.y)
                longAxis = 2;
            vis.localRotation = AxisToForward(longAxis);
            FlipHeading(vis);

            float longest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            float want = GpmConstants.LengthM * GpmConstants.VisualScaleMult;
            float s = longest > 0.05f ? want / longest : 1f;
            s = Mathf.Clamp(s, 0.002f, 2f);
            vis.localScale = new Vector3(s, s, s);

            bool pylon = vis.parent != null && vis.parent.GetComponent<MountedMissile>() != null;
            if (pylon)
            {
                SnapAttach(vis);
                KissPylonPlate(vis);
            }
            else
                SnapCenter(vis);

            GpmPlugin.ModLog?.LogInfo(
                $"VisualFit scale={s:F4} longest={longest:F2} pylon={pylon} pos={vis.localPosition}");
        }

        private static void FlipHeading(Transform vis) =>
            vis.localRotation = Quaternion.Euler(0f, 180f, 0f) * vis.localRotation;

        private static void SnapAttach(Transform vis)
        {
            if (vis.parent == null)
                return;
            Transform? attach = TransformBinder.FindByAliases(vis, GpmConstants.AttachPylonAliases);
            if (attach == null)
                return;
            Vector3 attachInParent = vis.parent.InverseTransformPoint(attach.position);
            vis.localPosition -= attachInParent;
        }

        private static void KissPylonPlate(Transform vis)
        {
            if (vis.parent == null)
                return;
            float lift = 0f;
            if (TryStationRailParentY(vis, out float railY) && railY < -1e-4f)
                lift = -railY;
            lift += GpmConstants.PylonLiftExtraM;
            lift += GpmConstants.MountClearanceM;
            if (lift > 1e-4f)
                vis.localPosition += Vector3.up * lift;
        }

        private static bool TryStationRailParentY(Transform vis, out float maxY)
        {
            maxY = float.MinValue;
            Transform? dock = TransformBinder.FindByAliases(vis, GpmConstants.AttachPylonAliases);
            if (dock == null || vis.parent == null)
                return false;

            Vector3 dockParent = vis.parent.InverseTransformPoint(dock.position);
            float half = GpmConstants.RailStationHalfM;
            bool any = false;

            Renderer[] rs = vis.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                Renderer r = rs[i];
                if (r == null || !IsMainHull(r.gameObject.name))
                    continue;
                MeshFilter? mf = r.GetComponent<MeshFilter>();
                Mesh? mesh = mf != null ? mf.sharedMesh : null;
                if (mesh == null)
                    continue;
                Vector3[] verts = mesh.vertices;
                if (verts == null || verts.Length == 0)
                    continue;

                Matrix4x4 toParent = vis.parent.worldToLocalMatrix * r.localToWorldMatrix;
                for (int v = 0; v < verts.Length; v++)
                {
                    Vector3 p = toParent.MultiplyPoint3x4(verts[v]);
                    if (Mathf.Abs(p.z - dockParent.z) > half)
                        continue;
                    if (Mathf.Abs(p.x - dockParent.x) > half)
                        continue;
                    if (p.y > dockParent.y + 0.02f)
                        continue;
                    if (p.y > maxY)
                        maxY = p.y;
                    any = true;
                }
            }
            return any;
        }

        private static bool IsMainHull(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            return name.Equals("Main", System.StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Main.", System.StringComparison.OrdinalIgnoreCase);
        }

        private static void SnapCenter(Transform vis)
        {
            if (vis.parent == null)
                return;
            if (!TryEncapsulateLocal(vis, out Bounds local, includeDisabled: true))
                return;
            Vector3 centerWorld = vis.TransformPoint(local.center);
            Vector3 centerInParent = vis.parent.InverseTransformPoint(centerWorld);
            vis.localPosition -= centerInParent;
        }

        private static Quaternion AxisToForward(int axis)
        {
            switch (axis)
            {
                case 0:
                    return Quaternion.Euler(0f, 90f, 0f);
                case 1:
                    return Quaternion.Euler(90f, 0f, 0f);
                default:
                    return Quaternion.identity;
            }
        }

        private static void EnsureRenderersOn(Transform vis)
        {
            Renderer[] rs = vis.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] != null)
                    rs[i].enabled = true;
            }
        }

        private static bool TryEncapsulateLocal(Transform root, out Bounds bounds, bool includeDisabled)
        {
            bounds = default;
            bool any = false;
            Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                Renderer r = rs[i];
                if (r == null)
                    continue;
                if (!includeDisabled && !r.enabled)
                    continue;
                Bounds rb = r.localBounds;
                Vector3[] corners =
                {
                    new Vector3(rb.min.x, rb.min.y, rb.min.z),
                    new Vector3(rb.min.x, rb.min.y, rb.max.z),
                    new Vector3(rb.min.x, rb.max.y, rb.min.z),
                    new Vector3(rb.min.x, rb.max.y, rb.max.z),
                    new Vector3(rb.max.x, rb.min.y, rb.min.z),
                    new Vector3(rb.max.x, rb.min.y, rb.max.z),
                    new Vector3(rb.max.x, rb.max.y, rb.min.z),
                    new Vector3(rb.max.x, rb.max.y, rb.max.z)
                };
                Matrix4x4 toRoot = root.worldToLocalMatrix * r.localToWorldMatrix;
                for (int c = 0; c < corners.Length; c++)
                {
                    Vector3 p = toRoot.MultiplyPoint3x4(corners[c]);
                    if (!any)
                    {
                        bounds = new Bounds(p, Vector3.zero);
                        any = true;
                    }
                    else
                        bounds.Encapsulate(p);
                }
            }
            return any;
        }
    }
}
