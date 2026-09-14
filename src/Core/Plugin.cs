using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndependentCadaverInfectionBar;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(ChineseProjectIdentity.CurrentPluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(LethalConfigIntegration.PluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "InfectionBar";
    public const string PluginName = "InfectionBar";
    public const string PluginVersion = "1.2.0";

    private static GameObject controllerHost;
    private static InfectionBarController controller;
    private static ManualLogSource runtimeLogger;
    private static bool applicationQuitting;
    private static bool callbacksSubscribed;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        runtimeLogger = Logger;
        applicationQuitting = false;
        InfectionBarCompatibility.Initialize(Logger);
        bool useChineseConfig = ChineseConfigDetector.IsInstalled(Logger);
        ModConfig.Bind(Config, useChineseConfig);
        if (Chainloader.PluginInfos.ContainsKey(LethalConfigIntegration.PluginGuid))
        {
            LethalConfigIntegration.Register(Logger);
        }

        if (!callbacksSubscribed)
        {
            // Static engine events survive early destruction of the plugin and its host.
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting += OnApplicationQuitting;
            callbacksSubscribed = true;
        }
        EnsureController();
        Logger.LogInfo($"{PluginName} loaded.");
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (applicationQuitting) return;
        EnsureController();
    }

    private static void EnsureController()
    {
        if (applicationQuitting || runtimeLogger == null) return;
        if (controllerHost == null)
        {
            controllerHost = new GameObject("InfectionBarControllerHost");
            DontDestroyOnLoad(controllerHost);
        }
        if (!controllerHost.activeSelf) controllerHost.SetActive(true);
        controller = controllerHost.GetComponent<InfectionBarController>();
        if (controller == null)
        {
            controller = controllerHost.AddComponent<InfectionBarController>();
            controller.Initialize(runtimeLogger, new InfectionDataProvider(runtimeLogger), new InfectionLayout(), new LanguageHelper());
            runtimeLogger.LogInfo("HUD controller created/recovered by persistent runtime owner.");
        }
        if (!controller.enabled) controller.enabled = true;
    }

    private static void OnApplicationQuitting()
    {
        applicationQuitting = true;
        if (callbacksSubscribed)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.quitting -= OnApplicationQuitting;
            callbacksSubscribed = false;
        }
        InfectionBarCompatibility.Shutdown();
        if (controller != null) controller.Shutdown();
        controller = null;
        if (controllerHost != null) Destroy(controllerHost);
        controllerHost = null;
    }

    private void OnDestroy()
    {
        if (!applicationQuitting)
        {
            runtimeLogger?.LogWarning("Plugin destroyed before application quit; scene-load recovery remains subscribed.");
        }
    }
}
