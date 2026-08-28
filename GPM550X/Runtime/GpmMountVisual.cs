using System.Reflection;
using Gpm.Bootstrap;
using UnityEngine;

namespace Gpm.Runtime
{
    /// <summary>Rail-slot visual + stock FX — hide immediately on Fire so rail ghosts do not trail backward.</summary>
    internal static class GpmMountVisual
    {
        private static readonly FieldInfo? DeployPsField =
            typeof(MountedMissile).GetField("deployParticles", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void HideFired(MountedMissile? mount)
        {
            if (mount == null)
                return;
            StopDeployFx(mount);
            SilenceSlotFx(mount.gameObject);
            SetVis(mount.gameObject, visible: false);
        }

        internal static void Restore(MountedMissile? mount)
        {
            if (mount == null)
                return;
            SetVis(mount.gameObject, visible: true);
            Transform? vis = PrefabFactory.FindVisual(mount.transform);
            if (vis != null)
                VisualFit.Apply(vis);
        }

        private static void StopDeployFx(MountedMissile mount)
        {
            if (DeployPsField?.GetValue(mount) is not ParticleSystem ps || ps == null)
                return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);
        }

        private static void SilenceSlotFx(GameObject host)
        {
            if (host == null)
                return;
            TrailEmitter[] trails = host.GetComponentsInChildren<TrailEmitter>(true);
            for (int i = 0; i < trails.Length; i++)
            {
                TrailEmitter te = trails[i];
                if (te == null)
                    continue;
                te.StopTrail();
                te.enabled = false;
            }
            ParticleSystem[] psArr = host.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < psArr.Length; i++)
            {
                ParticleSystem ps = psArr[i];
                if (ps == null)
                    continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }
        }

        private static void SetVis(GameObject host, bool visible)
        {
            if (host == null)
                return;
            Transform? vis = PrefabFactory.FindVisual(host.transform);
            if (vis != null)
            {
                vis.gameObject.SetActive(visible);
                Renderer[] rs = vis.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < rs.Length; i++)
                {
                    if (rs[i] != null)
                        rs[i].enabled = visible;
                }
            }
            if (!visible)
                GpmStockVisual.Hide(host);
        }
    }
}
