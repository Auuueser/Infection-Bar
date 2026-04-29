using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace IndependentCadaverInfectionBar;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "InfectionBar";
    public const string PluginName = "InfectionBar";
    public const string PluginVersion = "1.0.0";
    // Multiple tick sources keep the HUD alive across mod stacks that suppress one update path.
    internal static readonly bool EnableHudManagerTickFallback = true;

    private static GameObject controllerHost;
    private static Harmony harmony;
    private static ManualLogSource pluginLogger;
    private static bool loggedHudManagerStartPatch;
    private static bool loggedHudManagerUpdatePatch;
    private static bool loggedPlayerLateUpdatePatch;
    private static bool applicationQuitting;
    private InfectionBarController controller;
    private bool loggedFirstPluginUpdate;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        pluginLogger = Logger;

        ModConfig.Bind(Config);

        if (controllerHost == null)
        {
            controllerHost = new GameObject("InfectionBarControllerHost");
            DontDestroyOnLoad(controllerHost);
        }

        controller = controllerHost.GetComponent<InfectionBarController>();
        if (controller == null)
        {
            controller = controllerHost.AddComponent<InfectionBarController>();
        }

        controller.Initialize(Logger, new InfectionDataProvider(Logger), new InfectionLayout(), new LanguageHelper());
        ActiveController = controller;

        if (EnableHudManagerTickFallback && harmony == null)
        {
            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(Plugin).Assembly);
            if (ModConfig.DebugLogging.Value)
            {
                Logger.LogInfo("HUDManager.Update tick fallback enabled.");
            }
        }

        Logger.LogInfo($"{PluginName} loaded.");
    }

    private void Update()
    {
        if (ModConfig.DebugLogging.Value && !loggedFirstPluginUpdate)
        {
            Logger.LogInfo("Plugin Update tick fallback reached for the first time.");
            loggedFirstPluginUpdate = true;
        }

        controller?.Tick();
    }

    internal static void TickFromPatch(string source)
    {
        if (ModConfig.DebugLogging != null && ModConfig.DebugLogging.Value)
        {
            if (source == "HUDManager.Start" && !loggedHudManagerStartPatch)
            {
                pluginLogger?.LogInfo("HUDManager.Start tick fallback reached for the first time.");
                loggedHudManagerStartPatch = true;
            }
            else if (source == "HUDManager.Update" && !loggedHudManagerUpdatePatch)
            {
                pluginLogger?.LogInfo("HUDManager.Update tick fallback reached for the first time.");
                loggedHudManagerUpdatePatch = true;
            }
            else if (source == "PlayerControllerB.LateUpdate" && !loggedPlayerLateUpdatePatch)
            {
                pluginLogger?.LogInfo("PlayerControllerB.LateUpdate tick fallback reached for the first time.");
                loggedPlayerLateUpdatePatch = true;
            }
        }

        ActiveController?.Tick();
    }

    private void OnApplicationQuit()
    {
        applicationQuitting = true;
    }

    private void OnDestroy()
    {
        // Some mod stacks can destroy the BaseUnityPlugin object while the game session continues.
        // Keep the persistent controller alive in that case; full cleanup is only safe at process quit.
        if (!applicationQuitting)
        {
            if (ModConfig.DebugLogging != null && ModConfig.DebugLogging.Value)
            {
                Logger.LogWarning("Plugin OnDestroy reached before application quit; keeping controller and Harmony patches alive.");
            }

            return;
        }

        if (ActiveController == controller)
        {
            ActiveController = null;
        }

        controller?.Shutdown();
        controller = null;

        if (controllerHost != null)
        {
            Destroy(controllerHost);
            controllerHost = null;
        }

        if (harmony != null)
        {
            harmony.UnpatchSelf();
            harmony = null;
        }

        pluginLogger = null;
        loggedHudManagerStartPatch = false;
        loggedHudManagerUpdatePatch = false;
        loggedPlayerLateUpdatePatch = false;
        applicationQuitting = false;
    }

    internal static InfectionBarController ActiveController { get; private set; }
}

[HarmonyPatch(typeof(HUDManager), "Start")]
internal static class HudManagerStartPatch
{
    private static void Postfix()
    {
        Plugin.TickFromPatch("HUDManager.Start");
    }
}

[HarmonyPatch(typeof(HUDManager), "Update")]
internal static class HudManagerUpdatePatch
{
    private static void Postfix()
    {
        if (!Plugin.EnableHudManagerTickFallback)
        {
            return;
        }

        Plugin.TickFromPatch("HUDManager.Update");
    }
}

[HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
internal static class PlayerControllerLateUpdatePatch
{
    private static void Postfix()
    {
        Plugin.TickFromPatch("PlayerControllerB.LateUpdate");
    }
}
