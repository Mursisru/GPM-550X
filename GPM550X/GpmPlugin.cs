using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Gpm.Patches;

namespace Gpm
{
    [BepInPlugin(AppVersion.Guid, AppVersion.Name, AppVersion.Version)]
    [BepInDependency("com.nikkorap.blueprinter", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class GpmPlugin : BaseUnityPlugin
    {
        internal static GpmPlugin? Instance { get; private set; }
        internal static ManualLogSource? ModLog { get; private set; }

        private HarmonyLib.Harmony? _harmony;

        private void Awake()
        {
            Instance = this;
            ModLog = Logger;
            try
            {
                _harmony = new HarmonyLib.Harmony(AppVersion.Guid);
                _harmony.PatchAll(typeof(EncyclopediaAfterLoadPatch).Assembly);
                ModLog.LogInfo($"GPM-550X {AppVersion.Version} loaded (requires Blueprinter).");
            }
            catch (Exception ex)
            {
                ModLog.LogError($"Awake failed: {ex}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                _harmony?.UnpatchSelf();
            }
            catch
            {
                // ignore
            }
            Instance = null;
        }

        internal void StartBootstrap(Encyclopedia enc)
        {
            if (enc == null)
                return;
            StartCoroutine(GpmBootstrap.Run(enc));
        }
    }
}
