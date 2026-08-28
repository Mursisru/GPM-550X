using Gpm.Bootstrap;
using UnityEngine;

namespace Gpm.Runtime
{
    /// <summary>
    /// Hide Tusko hull without DestroyImmediate. Destroying MeshRenderer/PS during
    /// Missile.Awake/OnEnable re-enters OnEnable and NRE's Motor.particleSystems.
    /// Clearing sharedMesh keeps ghosts gone if the game re-enables the renderer.
    /// </summary>
    internal static class GpmStockVisual
    {
        internal static void Hide(GameObject? root)
        {
            if (root == null)
                return;

            Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                Renderer r = rs[i];
                if (r == null || PrefabFactory.IsVisualRoot(r.transform))
                    continue;
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer)
                    continue;
                if (r.GetComponent<ParticleSystem>() != null)
                    continue;

                r.enabled = false;
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                {
                    MeshFilter? mf = r.GetComponent<MeshFilter>();
                    if (mf != null)
                        mf.sharedMesh = null;
                    if (r is SkinnedMeshRenderer skin)
                        skin.sharedMesh = null;
                }
            }
        }
    }
}
