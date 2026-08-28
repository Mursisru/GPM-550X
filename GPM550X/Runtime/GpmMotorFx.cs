using System;
using System.Collections.Generic;
using System.Reflection;
using Gpm.Bootstrap;
using UnityEngine;

namespace Gpm.Runtime
{
    internal static class GpmMotorFx
    {
        private static readonly FieldInfo? MotorsField =
            typeof(Missile).GetField("motors", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Type? MotorType =
            typeof(Missile).GetNestedType("Motor", BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo? ParticlesField =
            MotorType?.GetField("particleSystems", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? TrailsField =
            MotorType?.GetField("trailEmitters", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? LightsField =
            MotorType?.GetField("lights", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? AudioField =
            MotorType?.GetField("audioSources", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly List<GameObject> PsTemplates = new List<GameObject>(4);
        private static readonly List<Vector3> PsLocalScale = new List<Vector3>(4);
        private static readonly List<GameObject> TrailTemplates = new List<GameObject>(2);
        private static readonly List<Vector3> TrailLocalScale = new List<Vector3>(2);
        private static readonly List<GameObject> AudioTemplates = new List<GameObject>(2);
        private static readonly List<GameObject> LightTemplates = new List<GameObject>(4);
        private static GameObject? _hold;

        internal static void Capture(Encyclopedia enc, MissileDefinition? tusko)
        {
            PsTemplates.Clear();
            PsLocalScale.Clear();
            TrailTemplates.Clear();
            TrailLocalScale.Clear();
            AudioTemplates.Clear();
            LightTemplates.Clear();
            if (MotorsField == null || ParticlesField == null)
                return;

            MissileDefinition? def = tusko ?? PrefabFactory.FindTuskoMissile(enc);
            if (def?.unitPrefab == null)
            {
                GpmPlugin.ModLog?.LogWarning("GpmMotorFx: no AShM3 for FX capture.");
                return;
            }
            Missile? mis = def.unitPrefab.GetComponent<Missile>() ??
                           def.unitPrefab.GetComponentInChildren<Missile>(true);
            if (mis == null)
                return;
            Array? motors = MotorsField.GetValue(mis) as Array;
            if (motors == null || motors.Length == 0)
                return;
            object? booster = motors.GetValue(0);
            if (booster == null)
                return;

            if (_hold == null)
            {
                _hold = new GameObject("Gpm_TuskoFxHold");
                UnityEngine.Object.DontDestroyOnLoad(_hold);
                _hold.SetActive(false);
            }

            if (ParticlesField.GetValue(booster) is Array psArr)
            {
                for (int i = 0; i < psArr.Length; i++)
                {
                    if (psArr.GetValue(i) is not ParticleSystem ps || ps == null)
                        continue;
                    GameObject go = UnityEngine.Object.Instantiate(ps.gameObject, _hold.transform);
                    go.name = "GpmTuskoPs";
                    go.SetActive(false);
                    PsTemplates.Add(go);
                    PsLocalScale.Add(ps.transform.lossyScale);
                }
            }
            if (TrailsField?.GetValue(booster) is Array trArr)
            {
                for (int i = 0; i < trArr.Length; i++)
                {
                    if (trArr.GetValue(i) is not TrailEmitter te || te == null)
                        continue;
                    GameObject go = UnityEngine.Object.Instantiate(te.gameObject, _hold.transform);
                    go.name = "GpmTuskoTrail";
                    go.SetActive(false);
                    TrailTemplates.Add(go);
                    TrailLocalScale.Add(te.transform.lossyScale);
                }
            }
            if (AudioField?.GetValue(booster) is Array auArr)
            {
                for (int i = 0; i < auArr.Length; i++)
                {
                    if (auArr.GetValue(i) is not AudioSource a || a == null || a.clip == null)
                        continue;
                    GameObject go = UnityEngine.Object.Instantiate(a.gameObject, _hold.transform);
                    go.name = "GpmTuskoAu";
                    go.SetActive(false);
                    AudioTemplates.Add(go);
                }
            }
            if (LightsField?.GetValue(booster) is Array ltArr)
            {
                for (int i = 0; i < ltArr.Length; i++)
                {
                    if (ltArr.GetValue(i) is not Light lit || lit == null)
                        continue;
                    GameObject go = UnityEngine.Object.Instantiate(lit.gameObject, _hold.transform);
                    go.name = "GpmTuskoLit";
                    go.SetActive(false);
                    LightTemplates.Add(go);
                }
            }
            GpmPlugin.ModLog?.LogInfo(
                $"GpmMotorFx capture from '{def.jsonKey}' ps={PsTemplates.Count} trails={TrailTemplates.Count} audio={AudioTemplates.Count} lights={LightTemplates.Count}");
        }

        internal static void Bind(Missile missile)
        {
            if (missile == null || MotorsField == null || MotorType == null)
                return;
            if (missile.transform.Find("GpmTuskoExhaust") != null ||
                HasChildNamed(missile.transform, "GpmTuskoExhaust"))
                return;

            Transform? vis = GpmVisualStamp.FindVisual(missile.transform);
            Transform? sock = vis != null
                ? TransformBinder.FindByAliases(vis, GpmConstants.EngineAliases)
                : null;
            if (sock == null)
                sock = TransformBinder.FindByAliases(missile.transform, GpmConstants.EngineAliases);
            if (sock == null && vis != null)
                sock = CreateAftSocket(vis);
            if (sock == null)
            {
                GpmPlugin.ModLog?.LogWarning("GPM: EngineEffectsSpawn missing.");
                return;
            }

            Array? motors = MotorsField.GetValue(missile) as Array;
            if (motors == null || motors.Length == 0 || motors.GetValue(0) is not object motor)
                return;

            WipeStockFx(missile, sock);
            InjectOnSocket(missile, motor, sock);
        }

        internal static void SilenceStock(Missile missile)
        {
            if (missile == null)
                return;
            Transform? vis = GpmVisualStamp.FindVisual(missile.transform);
            Transform? sock = vis != null
                ? TransformBinder.FindByAliases(vis, GpmConstants.EngineAliases)
                : null;
            WipeStockFx(missile, sock);
        }

        private static void InjectOnSocket(Missile missile, object motor, Transform socket)
        {
            var lights = new List<Light>(4);
            var psList = new List<ParticleSystem>(4);
            for (int i = 0; i < PsTemplates.Count; i++)
            {
                GameObject tpl = PsTemplates[i];
                if (tpl == null)
                    continue;
                GameObject go = UnityEngine.Object.Instantiate(tpl);
                go.name = "GpmTuskoExhaust";
                Vector3 scale = i < PsLocalScale.Count ? PsLocalScale[i] : Vector3.one;
                PlaceOnMissile(go.transform, socket, missile, scale);
                go.SetActive(true);
                ParticleSystem? root = go.GetComponent<ParticleSystem>() ??
                                       go.GetComponentInChildren<ParticleSystem>(true);
                if (root == null)
                {
                    UnityEngine.Object.Destroy(go);
                    continue;
                }
                LoopExhaust(root);
                HarvestLights(go, lights);
                psList.Add(root);
            }
            if (ParticlesField != null)
                ParticlesField.SetValue(motor, psList.ToArray());

            var trails = new List<TrailEmitter>(2);
            for (int i = 0; i < TrailTemplates.Count; i++)
            {
                GameObject tpl = TrailTemplates[i];
                if (tpl == null)
                    continue;
                GameObject go = UnityEngine.Object.Instantiate(tpl);
                go.name = "GpmTuskoTrail";
                Vector3 scale = i < TrailLocalScale.Count ? TrailLocalScale[i] : Vector3.one;
                PlaceOnMissile(go.transform, socket, missile, scale);
                go.SetActive(true);
                TrailEmitter? te = go.GetComponent<TrailEmitter>() ??
                                   go.GetComponentInChildren<TrailEmitter>(true);
                if (te == null)
                {
                    UnityEngine.Object.Destroy(go);
                    continue;
                }
                te.rb = missile.rb;
                trails.Add(te);
            }
            if (TrailsField != null)
                TrailsField.SetValue(motor, trails.ToArray());

            var audios = new List<AudioSource>(2);
            for (int i = 0; i < AudioTemplates.Count; i++)
            {
                GameObject tpl = AudioTemplates[i];
                if (tpl == null)
                    continue;
                GameObject go = UnityEngine.Object.Instantiate(tpl);
                go.name = "GpmTuskoAudio";
                PlaceOnMissile(go.transform, socket, missile, Vector3.one);
                go.SetActive(true);
                AudioSource? src = go.GetComponent<AudioSource>() ?? go.GetComponentInChildren<AudioSource>(true);
                if (src == null)
                {
                    UnityEngine.Object.Destroy(go);
                    continue;
                }
                src.playOnAwake = false;
                src.loop = true;
                src.spatialBlend = 1f;
                audios.Add(src);
            }
            if (AudioField != null)
                AudioField.SetValue(motor, audios.ToArray());

            for (int i = 0; i < LightTemplates.Count; i++)
            {
                GameObject tpl = LightTemplates[i];
                if (tpl == null)
                    continue;
                GameObject go = UnityEngine.Object.Instantiate(tpl);
                go.name = "GpmTuskoLight";
                PlaceOnMissile(go.transform, socket, missile, Vector3.one);
                go.SetActive(true);
                Light? lit = go.GetComponent<Light>() ?? go.GetComponentInChildren<Light>(true);
                if (lit == null)
                {
                    UnityEngine.Object.Destroy(go);
                    continue;
                }
                lit.enabled = true;
                lights.Add(lit);
            }
            if (lights.Count == 0)
            {
                GameObject go = new GameObject("GpmTuskoLight");
                PlaceOnMissile(go.transform, socket, missile, Vector3.one);
                Light lit = go.AddComponent<Light>();
                lit.type = LightType.Point;
                lit.color = new Color(1f, 0.55f, 0.2f);
                lit.intensity = 8f;
                lit.range = 25f;
                lit.enabled = true;
                lights.Add(lit);
            }
            if (LightsField != null)
                LightsField.SetValue(motor, lights.ToArray());
        }

        private static void HarvestLights(GameObject go, List<Light> dst)
        {
            Light[] found = go.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < found.Length; i++)
            {
                Light lit = found[i];
                if (lit == null)
                    continue;
                lit.enabled = true;
                dst.Add(lit);
            }
        }

        private static void LoopExhaust(ParticleSystem root)
        {
            ParticleSystem[] all = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < all.Length; i++)
            {
                ParticleSystem ps = all[i];
                if (ps == null)
                    continue;
                ParticleSystem.MainModule main = ps.main;
                main.loop = true;
                main.playOnAwake = false;
                ParticleSystem.EmissionModule em = ps.emission;
                em.enabled = true;
            }
        }

        private static Transform CreateAftSocket(Transform vis)
        {
            Transform sock = vis.Find("EngineEffectsSpawn");
            if (sock != null)
                return sock;
            GameObject go = new GameObject("EngineEffectsSpawn");
            sock = go.transform;
            sock.SetParent(vis, false);
            sock.localRotation = Quaternion.identity;
            sock.localScale = Vector3.one;
            Renderer[] rs = vis.GetComponentsInChildren<Renderer>(true);
            float minZ = 0f;
            bool any = false;
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null)
                    continue;
                Bounds b = rs[i].localBounds;
                Vector3 p = vis.InverseTransformPoint(rs[i].transform.TransformPoint(b.center - new Vector3(0f, 0f, b.extents.z)));
                if (!any || p.z < minZ)
                {
                    minZ = p.z;
                    any = true;
                }
            }
            sock.localPosition = any ? new Vector3(0f, 0f, minZ) : new Vector3(0f, 0f, -GpmConstants.LengthM * 0.5f);
            return sock;
        }

        private static bool HasChildNamed(Transform root, string name)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == name)
                    return true;
            }
            return false;
        }

        private static void PlaceOnMissile(Transform t, Transform socket, Missile missile, Vector3 donorWorldScale)
        {
            Vector3 scale = donorWorldScale;
            if (scale.x < 1e-4f)
                scale = Vector3.one;
            scale *= GpmConstants.FxWorldScaleM;
            t.SetParent(missile.transform, false);
            t.localScale = scale;
            Vector3 aft = -missile.transform.forward;
            if (aft.sqrMagnitude < 1e-4f)
                aft = -socket.forward;
            t.rotation = Quaternion.LookRotation(aft, missile.transform.up);
            t.position = socket.position + aft * GpmConstants.FxAftNudgeM;
        }

        private static void WipeStockFx(Missile missile, Transform? sock)
        {
            var kill = new List<GameObject>(8);
            TrailEmitter[] trails = missile.GetComponentsInChildren<TrailEmitter>(true);
            for (int i = 0; i < trails.Length; i++)
            {
                TrailEmitter te = trails[i];
                if (te == null)
                    continue;
                if (IsUnder(te.transform, sock) || IsOursFx(te.transform))
                    continue;
                te.StopTrail();
                te.enabled = false;
                kill.Add(te.gameObject);
            }
            ParticleSystem[] all = missile.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < all.Length; i++)
            {
                ParticleSystem ps = all[i];
                if (ps == null)
                    continue;
                if (IsUnder(ps.transform, sock) || IsOursFx(ps.transform))
                    continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                kill.Add(ps.gameObject);
            }
            for (int i = 0; i < kill.Count; i++)
            {
                GameObject go = kill[i];
                if (go == null)
                    continue;
                go.SetActive(false);
                UnityEngine.Object.Destroy(go);
            }
        }

        private static bool IsOursFx(Transform t)
        {
            while (t != null)
            {
                string n = t.name;
                if (n == "GpmTuskoExhaust" || n == "GpmTuskoTrail" || n == "GpmTuskoAudio" || n == "GpmTuskoLight")
                    return true;
                t = t.parent;
            }
            return false;
        }

        private static bool IsUnder(Transform t, Transform? sock)
        {
            if (sock == null || t == null)
                return false;
            return t == sock || t.IsChildOf(sock);
        }
    }
}
