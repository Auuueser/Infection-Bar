using System;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using GameNetcodeStuff;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IndependentCadaverInfectionBar;

internal sealed class InfectionBarController : MonoBehaviour
{
    private const string EladsHudPluginGuid = "me.eladnlg.customhud";
    private const string VanillaWarningRootPathSuffix = "IngamePlayerHUD/SpecialHUDGraphics/RadiationIncrease";

    private static Sprite pixelSprite;
    private static float nextTerminalLookupTime;
    private static float nextNativeHudElementLookupTime;
    private static float nextNativeHudParentFallbackLookupTime;
    private static float nextVanillaWarningRootLookupTime;
    private static HUDManager cachedHudManagerInstance;
    private static StartOfRound cachedStartOfRoundInstance;
    private static Terminal cachedTerminal;
    private static Transform cachedNativeHudElementTransform;
    private static RectTransform cachedNativeHudParentFallback;
    private static RectTransform cachedVanillaWarningRoot;
    private static FieldInfo hudPlayerInfoField;
    private static FieldInfo hudContainerField;
    private static FieldInfo hudElementsField;
    private static FieldInfo hudHudHiddenField;
    private static FieldInfo hudTerminalScriptField;
    private static FieldInfo hudRadiationGraphicAnimatorField;
    private static FieldInfo terminalInUseField;

    private ManualLogSource logger;
    private InfectionDataProvider dataProvider;
    private InfectionLayout layout;
    private LanguageHelper languageHelper;

    private CanvasGroup canvasGroup;
    private RectTransform infectionRoot;
    private Image panelBackground;
    private Image topLine;
    private Image infectionBackground;
    private Image infectionBar;
    private Text infectionText;
    private Image vanillaSprintMeterReference;
    private RectTransform vanillaWeightTextRoot;
    private CanvasGroup vanillaWeightTextCanvasGroup;
    private RectTransform vanillaInfectionTextRoot;
    private CanvasGroup vanillaInfectionTextCanvasGroup;
    private RectTransform vanillaInfectionValueTextRoot;
    private CanvasGroup vanillaInfectionValueTextCanvasGroup;
    private TextMeshProUGUI hiddenWeightCounter;
    private bool hiddenWeightCounterWasEnabled;
    private string lastVanillaWeightText = string.Empty;
    private string lastVanillaInfectionText = string.Empty;
    private string lastAppliedVanillaWeightText = string.Empty;
    private string lastAppliedVanillaInfectionText = string.Empty;
    private bool hasAppliedVanillaWeightText;
    private bool hasAppliedVanillaInfectionText;
    private bool hasClearedVanillaInfectionValueText;
    private VanillaArcTextSlot lastAppliedVanillaWeightSlot;
    private Vector2 lastAppliedVanillaWeightRootSize;
    private Vector2 lastAppliedVanillaInfectionRootSize;
    private bool hasVanillaLayoutSignature;
    private Vector2 lastVanillaAnchorMin;
    private Vector2 lastVanillaAnchorMax;
    private Vector2 lastVanillaPivot;
    private Vector2 lastVanillaSizeDelta;
    private Vector2 lastVanillaAnchoredPosition;
    private Quaternion lastVanillaLocalRotation;
    private Vector3 lastVanillaLocalScale;
    private float lastVanillaRingScale;
    private float lastVanillaRingOffsetX;
    private float lastVanillaRingOffsetY;
    private RectTransform shiftedVanillaWarningRoot;
    private Vector2 originalVanillaWarningAnchoredPosition;
    private bool hasOriginalVanillaWarningAnchoredPosition;
    private bool loggedMissingVanillaWarningRoot;

    private string cachedInfectionLabel = "Infection";
    private float nextInfectionLabelRefreshTime;
    private float lastRenderedInfectionFillAmount = -1f;
    private int lastRenderedInfectionPercent = -1;
    private string lastRenderedInfectionLabel = string.Empty;
    private bool lastVisibleState;
    private bool loggedMissingNativeHudParent;
    private bool loggedNativeHudParentFallback;
    private int lastTickFrame = -1;
    private float currentHudAlpha = 1f;
    private bool layoutDirty = true;
    private bool layoutConfigEventsSubscribed;
    private bool hasActiveHudStyle;
    private HudStyle activeHudStyle;
    private bool loggedMissingSprintMeter;
    private bool loggedVanillaSprintMeterDiagnostics;

    private enum HudStyle
    {
        Current,
        VanillaStaminaRing
    }

    private const string VanillaArcCharacterPrefix = "VanillaArcChar_";

    private enum VanillaArcTextSlot
    {
        WeightStaminaUpper,
        WeightInfectionInner,
        InfectionOuter
    }

    private enum VanillaArcRadiusBand
    {
        Inner,
        Middle,
        Outer
    }

    private const float SprintMeterSpriteSize = 326f;

    private static readonly float[] SprintMeterArcAngles =
    {
        -162.5f, -157.5f, -152.5f, -147.5f, -142.5f, -137.5f, -132.5f, -127.5f, -122.5f, -117.5f,
        -112.5f, -107.5f, -102.5f, -97.5f, -92.5f, -87.5f, -82.5f, -77.5f, -72.5f, -67.5f,
        -62.5f, -57.5f, -52.5f, -47.5f, -42.5f, -37.5f, -32.5f, -27.5f, -22.5f, -17.5f,
        -12.5f, -7.5f, -2.5f, 2.5f, 7.5f, 17.5f, 22.5f, 27.5f, 32.5f, 37.5f,
        42.5f, 47.5f, 52.5f
    };

    private static readonly float[] SprintMeterArcInnerRadii =
    {
        148.3f, 146.4f, 155.9f, 153.8f, 159.1f, 157.7f, 154.7f, 144.9f, 145.9f, 140.7f,
        134.0f, 125.9f, 122.8f, 118.0f, 115.8f, 108.8f, 106.8f, 104.0f, 97.1f, 100.4f,
        98.6f, 97.8f, 97.4f, 97.7f, 98.4f, 96.5f, 100.6f, 102.2f, 104.1f, 107.9f,
        111.2f, 114.8f, 119.9f, 125.5f, 129.0f, 144.0f, 150.1f, 155.9f, 158.0f, 165.0f,
        165.2f, 166.8f, 164.5f
    };

    private static readonly float[] SprintMeterArcMiddleRadii =
    {
        150.5f, 154.2f, 158.1f, 160.2f, 162.2f, 162.0f, 159.6f, 155.3f, 151.6f, 145.6f,
        139.5f, 133.8f, 128.2f, 123.1f, 120.7f, 113.6f, 111.0f, 108.3f, 105.6f, 104.3f,
        102.6f, 101.6f, 101.5f, 101.5f, 102.1f, 103.0f, 104.5f, 106.4f, 108.7f, 112.3f,
        116.1f, 120.3f, 125.5f, 130.5f, 134.2f, 149.4f, 155.7f, 161.2f, 165.2f, 168.4f,
        169.4f, 168.7f, 166.4f
    };

    private static readonly float[] SprintMeterArcOuterRadii =
    {
        152.1f, 158.5f, 161.1f, 165.2f, 165.5f, 165.5f, 164.1f, 160.9f, 163.2f, 150.6f,
        145.2f, 140.9f, 133.5f, 128.2f, 123.9f, 117.6f, 115.1f, 112.2f, 110.8f, 112.3f,
        106.7f, 105.3f, 105.1f, 105.4f, 106.2f, 109.9f, 108.1f, 110.3f, 113.0f, 116.4f,
        120.8f, 127.6f, 130.5f, 135.8f, 139.3f, 154.7f, 161.0f, 167.2f, 169.6f, 171.7f,
        173.9f, 171.2f, 168.6f
    };

    private readonly struct NativeHudState
    {
        internal NativeHudState(float alpha, bool isValid)
        {
            Alpha = alpha;
            IsValid = isValid;
        }

        internal float Alpha { get; }

        internal bool IsValid { get; }
    }

    internal void Initialize(ManualLogSource logger, InfectionDataProvider dataProvider, InfectionLayout layout, LanguageHelper languageHelper)
    {
        this.logger = logger;
        this.dataProvider = dataProvider;
        this.layout = layout;
        this.languageHelper = languageHelper;
        SubscribeLayoutConfigEvents();
        layoutDirty = true;
    }

    private void Update()
    {
        Tick();
    }

    internal void Tick()
    {
        if (lastTickFrame == Time.frameCount)
        {
            return;
        }

        lastTickFrame = Time.frameCount;

        ResetSceneCachesIfHudManagerChanged();
        PlayerControllerB player = dataProvider.GetLocalPlayer();
        EnsureInfectionUI(player);
        UpdateInfection(player);
    }

    internal void Shutdown()
    {
        DestroyInfectionUI();
        ClearCachedSceneReferences();
        cachedHudManagerInstance = null;
        cachedStartOfRoundInstance = null;
        UnsubscribeLayoutConfigEvents();
    }

    private void OnDestroy()
    {
        Shutdown();
    }

    private void EnsureInfectionUI(PlayerControllerB player)
    {
        HudStyle desiredHudStyle = ResolveHudStyle();
        if (infectionRoot != null)
        {
            if (!hasActiveHudStyle || activeHudStyle != desiredHudStyle)
            {
                DestroyInfectionUI();
            }
            else
            {
                RefreshLayoutIfNeeded(player);
                return;
            }
        }

        if (desiredHudStyle == HudStyle.VanillaStaminaRing)
        {
            TryCreateVanillaStaminaRingUI(player);
            return;
        }

        RectTransform nativeHudParent = GetNativeHudParentTransform();
        if (nativeHudParent == null)
        {
            if (ShouldLogDiagnostics() && !loggedMissingNativeHudParent)
            {
                logger.LogWarning("Waiting for native HUD parent before creating infection bar UI.");
                loggedMissingNativeHudParent = true;
            }

            return;
        }

        loggedMissingNativeHudParent = false;
        GameObject rootObject = new GameObject("IndependentCadaverInfectionBarRoot", typeof(RectTransform), typeof(CanvasGroup));
        rootObject.transform.SetParent(nativeHudParent, false);

        NativeHudState nativeHudState = GetNativeHudState();
        lastVisibleState = false;
        lastRenderedInfectionFillAmount = -1f;
        lastRenderedInfectionPercent = -1;
        lastRenderedInfectionLabel = string.Empty;
        currentHudAlpha = nativeHudState.IsValid ? nativeHudState.Alpha : 1f;
        canvasGroup = rootObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = currentHudAlpha;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Vector2 rootSize = GetEffectiveRootSize();
        Vector2 barSize = GetEffectiveBarSize();

        infectionRoot = rootObject.GetComponent<RectTransform>();
        infectionRoot.anchorMin = layout.GetAnchorMin();
        infectionRoot.anchorMax = layout.GetAnchorMax();
        infectionRoot.pivot = layout.GetPivot();
        infectionRoot.sizeDelta = rootSize;
        infectionRoot.anchoredPosition = AlignVector2(layout.GetAnchoredPosition());
        ApplyNativeHudElementRotation();

        RectTransform panelRect = CreateRect("Panel", infectionRoot, rootSize, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        panelBackground = panelRect.gameObject.AddComponent<Image>();
        panelBackground.sprite = GetPixelSprite();
        panelBackground.color = new Color(0.10f, 0.02f, 0.02f, Mathf.Clamp01(ModConfig.PanelBackgroundAlpha.Value));
        panelBackground.enabled = ModConfig.ShowPanelBackground.Value;

        RectTransform topLineRect = CreateRect("TopLine", infectionRoot, new Vector2(barSize.x, barSize.y), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        topLineRect.anchoredPosition = AlignVector2(new Vector2(8f, -6f));
        topLine = topLineRect.gameObject.AddComponent<Image>();
        topLine.sprite = GetPixelSprite();
        topLine.color = new Color(0.84f, 0.18f, 0.16f, 0.72f);

        RectTransform backgroundRect = CreateRect("Background", infectionRoot, new Vector2(barSize.x, barSize.y), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        backgroundRect.anchoredPosition = AlignVector2(new Vector2(8f, -6f));
        infectionBackground = backgroundRect.gameObject.AddComponent<Image>();
        infectionBackground.sprite = GetPixelSprite();
        infectionBackground.type = Image.Type.Sliced;
        infectionBackground.color = new Color(0.24f, 0.05f, 0.05f, 0.42f);

        RectTransform fillRect = CreateRect("Fill", backgroundRect, barSize, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        infectionBar = fillRect.gameObject.AddComponent<Image>();
        infectionBar.sprite = GetPixelSprite();
        infectionBar.type = Image.Type.Filled;
        infectionBar.fillMethod = Image.FillMethod.Horizontal;
        infectionBar.fillOrigin = 0;
        infectionBar.fillAmount = 0f;
        infectionBar.color = new Color(0.91f, 0.23f, 0.19f, 0.98f);

        RectTransform textRect = CreateRect("Text", infectionRoot, new Vector2(rootSize.x - 16f, 18f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        textRect.anchoredPosition = AlignVector2(new Vector2(ModConfig.TextOffsetX.Value, ModConfig.TextOffsetY.Value));
        textRect.localRotation = Quaternion.identity;

        infectionText = textRect.gameObject.AddComponent<Text>();
        infectionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        infectionText.fontSize = ModConfig.TextFontSize.Value;
        infectionText.alignment = TextAnchor.MiddleLeft;
        infectionText.horizontalOverflow = HorizontalWrapMode.Overflow;
        infectionText.verticalOverflow = VerticalWrapMode.Overflow;
        infectionText.color = new Color(0.97f, 0.95f, 0.92f, 0.94f);

        cachedInfectionLabel = DetermineInfectionLabel();
        nextInfectionLabelRefreshTime = Time.unscaledTime + 5f;
        infectionText.text = cachedInfectionLabel + " 0%";
        infectionRoot.gameObject.SetActive(false);

        if (ShouldLogDiagnostics())
        {
            logger.LogInfo("Standalone infection bar UI created. style=Current");
        }

        activeHudStyle = HudStyle.Current;
        hasActiveHudStyle = true;
    }

    private static HudStyle ResolveHudStyle()
    {
        string mode = (ModConfig.HudStyleMode?.Value ?? "Auto").Trim();
        if (string.Equals(mode, "Current", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, "CurrentStyle", StringComparison.OrdinalIgnoreCase))
        {
            return HudStyle.Current;
        }

        if (string.Equals(mode, "Vanilla", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, "VanillaStamina", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, "VanillaStaminaRing", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, "VanillaStaminaRingStyle", StringComparison.OrdinalIgnoreCase))
        {
            return HudStyle.VanillaStaminaRing;
        }

        return IsEladsHudInstalled() ? HudStyle.Current : HudStyle.VanillaStaminaRing;
    }

    private static bool IsEladsHudInstalled()
    {
        if (Chainloader.PluginInfos.ContainsKey(EladsHudPluginGuid))
        {
            return true;
        }

        foreach (var pluginInfo in Chainloader.PluginInfos.Values)
        {
            string guid = pluginInfo?.Metadata?.GUID ?? string.Empty;
            string name = pluginInfo?.Metadata?.Name ?? string.Empty;
            string combined = guid + " " + name;
            if (combined.IndexOf("Elads", StringComparison.OrdinalIgnoreCase) >= 0
                && combined.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryCreateVanillaStaminaRingUI(PlayerControllerB player)
    {
        if (!TryGetSprintMeter(player, out Image sprintMeterImage, out RectTransform sprintMeterRect))
        {
            if (ShouldLogDiagnostics() && !loggedMissingSprintMeter)
            {
                logger.LogWarning("Waiting for original sprint meter UI before creating vanilla infection ring.");
                loggedMissingSprintMeter = true;
            }

            return false;
        }

        RectTransform parentRect = sprintMeterRect.parent as RectTransform;
        if (parentRect == null)
        {
            if (ShouldLogDiagnostics() && !loggedMissingSprintMeter)
            {
                logger.LogWarning("Original sprint meter UI has no RectTransform parent.");
                loggedMissingSprintMeter = true;
            }

            return false;
        }

        loggedMissingSprintMeter = false;
        LogVanillaSprintMeterDiagnostics(sprintMeterImage, sprintMeterRect);

        GameObject rootObject = new GameObject("IndependentCadaverInfectionBarVanillaStaminaRingRoot", typeof(RectTransform), typeof(CanvasGroup));
        rootObject.transform.SetParent(parentRect, false);

        NativeHudState nativeHudState = GetNativeHudState();
        lastVisibleState = false;
        lastRenderedInfectionFillAmount = -1f;
        lastRenderedInfectionPercent = -1;
        lastRenderedInfectionLabel = string.Empty;
        currentHudAlpha = nativeHudState.IsValid ? nativeHudState.Alpha : 1f;

        canvasGroup = rootObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = currentHudAlpha;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        infectionRoot = rootObject.GetComponent<RectTransform>();
        vanillaSprintMeterReference = sprintMeterImage;
        ApplyVanillaStaminaRingTransform(sprintMeterRect);

        RectTransform backgroundRect = CreateRect("VanillaRingBackground", infectionRoot, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        StretchToParent(backgroundRect);
        infectionBackground = backgroundRect.gameObject.AddComponent<Image>();
        CopySprintMeterImageStyle(sprintMeterImage, infectionBackground, new Color(0.34f, 0.05f, 0.04f, 0.42f));
        ApplyVanillaRingFillAmount(infectionBackground, 1f);

        RectTransform fillRect = CreateRect("VanillaRingFill", infectionRoot, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        StretchToParent(fillRect);
        infectionBar = fillRect.gameObject.AddComponent<Image>();
        CopySprintMeterImageStyle(sprintMeterImage, infectionBar, new Color(0.91f, 0.18f, 0.13f, 0.95f));
        ApplyVanillaRingFillAmount(infectionBar, 0f);

        CreateVanillaArcTexts(sprintMeterRect);

        cachedInfectionLabel = DetermineInfectionLabel();
        nextInfectionLabelRefreshTime = Time.unscaledTime + 5f;
        SetInfectionText(FormatInfectionText(cachedInfectionLabel, 0));
        infectionRoot.gameObject.SetActive(false);
        if (vanillaWeightTextRoot != null)
        {
            vanillaWeightTextRoot.gameObject.SetActive(false);
        }

        if (vanillaInfectionTextRoot != null)
        {
            vanillaInfectionTextRoot.gameObject.SetActive(false);
        }

        if (vanillaInfectionValueTextRoot != null)
        {
            vanillaInfectionValueTextRoot.gameObject.SetActive(false);
        }

        activeHudStyle = HudStyle.VanillaStaminaRing;
        hasActiveHudStyle = true;
        layoutDirty = false;

        if (ShouldLogDiagnostics())
        {
            logger.LogInfo("Standalone infection bar UI created. style=VanillaStaminaRing");
        }

        return true;
    }

    private static bool TryGetSprintMeter(PlayerControllerB player, out Image sprintMeterImage, out RectTransform sprintMeterRect)
    {
        sprintMeterImage = player?.sprintMeterUI;
        sprintMeterRect = sprintMeterImage != null ? sprintMeterImage.rectTransform : null;
        return sprintMeterImage != null && sprintMeterRect != null;
    }

    private void RefreshVanillaStaminaRingLayout(PlayerControllerB player)
    {
        if (!TryGetSprintMeter(player, out Image sprintMeterImage, out RectTransform sprintMeterRect))
        {
            return;
        }

        RectTransform parentRect = sprintMeterRect.parent as RectTransform;
        bool parentChanged = parentRect != null && infectionRoot.parent != parentRect;
        if (parentChanged)
        {
            infectionRoot.SetParent(parentRect, false);
        }

        vanillaSprintMeterReference = sprintMeterImage;
        bool forceLiveRefresh = ShouldForceVanillaHudLiveLayoutRefresh();
        bool layoutChanged = parentChanged || forceLiveRefresh || layoutDirty || HasVanillaSprintMeterLayoutChanged(sprintMeterRect);
        if (layoutChanged)
        {
            ApplyVanillaStaminaRingTransform(sprintMeterRect);
            RefreshVanillaArcTextLayout(sprintMeterRect, forceTextRefresh: true);
            CopySprintMeterImageStyle(sprintMeterImage, infectionBackground, new Color(0.34f, 0.05f, 0.04f, 0.42f));
            CopySprintMeterImageStyle(sprintMeterImage, infectionBar, new Color(0.91f, 0.18f, 0.13f, 0.95f));
            ApplyVanillaRingFillAmount(infectionBackground, 1f);
            ApplyVanillaRingFillAmount(infectionBar, lastRenderedInfectionFillAmount >= 0f ? VanillaRingFillMapping.MapInfectionToVisibleFill(lastRenderedInfectionFillAmount) : infectionBar.fillAmount);
            CaptureVanillaSprintMeterLayoutSignature(sprintMeterRect);
            layoutDirty = false;
        }

        SetOriginalWeightCounterHidden(ModConfig.InfectionBarEnabled.Value);
        UpdateVanillaWeightText(forceLiveRefresh || layoutChanged);
        ApplyVanillaInfectionTextSegments(forceLiveRefresh || layoutChanged);
    }

    private void CreateVanillaArcTexts(RectTransform sprintMeterRect)
    {
        RectTransform textParent = sprintMeterRect.parent as RectTransform;
        if (textParent == null)
        {
            return;
        }

        CreateVanillaArcTextRoot("IndependentCadaverInfectionBarVanillaWeightText", textParent, out vanillaWeightTextRoot, out vanillaWeightTextCanvasGroup);
        CreateVanillaArcTextRoot("IndependentCadaverInfectionBarVanillaInfectionText", textParent, out vanillaInfectionTextRoot, out vanillaInfectionTextCanvasGroup);
        CreateVanillaArcTextRoot("IndependentCadaverInfectionBarVanillaInfectionValueText", textParent, out vanillaInfectionValueTextRoot, out vanillaInfectionValueTextCanvasGroup);
        UpdateVanillaWeightText(force: true);
        RefreshVanillaArcTextLayout(sprintMeterRect, forceTextRefresh: true);
    }

    private void CreateVanillaArcTextRoot(string name, RectTransform parent, out RectTransform root, out CanvasGroup group)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        textObject.transform.SetParent(parent, false);
        root = textObject.GetComponent<RectTransform>();
        group = textObject.GetComponent<CanvasGroup>();
        group.alpha = currentHudAlpha;
        group.interactable = false;
        group.blocksRaycasts = false;
        root.gameObject.SetActive(false);
    }

    private void RefreshVanillaArcTextLayout(RectTransform sprintMeterRect, bool forceTextRefresh)
    {
        if (vanillaWeightTextRoot == null || vanillaInfectionTextRoot == null || vanillaInfectionValueTextRoot == null)
        {
            return;
        }

        RectTransform targetParent = sprintMeterRect.parent as RectTransform;
        if (targetParent != null && vanillaWeightTextRoot.parent != targetParent)
        {
            vanillaWeightTextRoot.SetParent(targetParent, false);
            vanillaInfectionTextRoot.SetParent(targetParent, false);
            vanillaInfectionValueTextRoot.SetParent(targetParent, false);
        }

        Vector2 ringPosition = sprintMeterRect.anchoredPosition
            + GetVanillaOuterRingOffset(sprintMeterRect)
            + new Vector2(ModConfig.VanillaRingOffsetX.Value, ModConfig.VanillaRingOffsetY.Value);
        Vector2 referenceSize = GetRectReferenceSize(sprintMeterRect);
        float scale = Mathf.Max(0.1f, ModConfig.VanillaRingScale.Value);
        Vector2 ringSize = new Vector2(Mathf.Abs(referenceSize.x) * scale, Mathf.Abs(referenceSize.y) * scale);
        bool infectionVisible = lastVisibleState;
        Vector2 weightPosition = infectionVisible ? ringPosition : sprintMeterRect.anchoredPosition;
        Vector2 weightSize = infectionVisible ? ringSize : new Vector2(Mathf.Abs(referenceSize.x), Mathf.Abs(referenceSize.y));
        VanillaArcTextSlot weightSlot = infectionVisible ? VanillaArcTextSlot.WeightInfectionInner : VanillaArcTextSlot.WeightStaminaUpper;

        ApplyVanillaTextRect(vanillaWeightTextRoot, sprintMeterRect.anchorMin, sprintMeterRect.anchorMax, new Vector2(0.5f, 0.5f), weightSize, weightPosition, sprintMeterRect.localRotation, sprintMeterRect.localScale);
        ApplyVanillaTextRect(vanillaInfectionTextRoot, sprintMeterRect.anchorMin, sprintMeterRect.anchorMax, new Vector2(0.5f, 0.5f), ringSize, ringPosition, sprintMeterRect.localRotation, sprintMeterRect.localScale);
        ApplyVanillaTextRect(vanillaInfectionValueTextRoot, sprintMeterRect.anchorMin, sprintMeterRect.anchorMax, new Vector2(0.5f, 0.5f), ringSize, ringPosition, sprintMeterRect.localRotation, sprintMeterRect.localScale);
        UpdateVanillaWeightText(forceTextRefresh);
        ApplyVanillaInfectionTextSegments(forceTextRefresh);
    }

    private bool HasVanillaSprintMeterLayoutChanged(RectTransform sprintMeterRect)
    {
        if (!hasVanillaLayoutSignature)
        {
            return true;
        }

        return (lastVanillaAnchorMin - sprintMeterRect.anchorMin).sqrMagnitude > 0.0001f
            || (lastVanillaAnchorMax - sprintMeterRect.anchorMax).sqrMagnitude > 0.0001f
            || (lastVanillaPivot - sprintMeterRect.pivot).sqrMagnitude > 0.0001f
            || (lastVanillaSizeDelta - sprintMeterRect.sizeDelta).sqrMagnitude > 0.0001f
            || (lastVanillaAnchoredPosition - sprintMeterRect.anchoredPosition).sqrMagnitude > 0.0001f
            || Quaternion.Angle(lastVanillaLocalRotation, sprintMeterRect.localRotation) > 0.01f
            || (lastVanillaLocalScale - sprintMeterRect.localScale).sqrMagnitude > 0.0001f
            || Mathf.Abs(lastVanillaRingScale - ModConfig.VanillaRingScale.Value) > 0.0001f
            || Mathf.Abs(lastVanillaRingOffsetX - ModConfig.VanillaRingOffsetX.Value) > 0.0001f
            || Mathf.Abs(lastVanillaRingOffsetY - ModConfig.VanillaRingOffsetY.Value) > 0.0001f;
    }

    private void CaptureVanillaSprintMeterLayoutSignature(RectTransform sprintMeterRect)
    {
        hasVanillaLayoutSignature = true;
        lastVanillaAnchorMin = sprintMeterRect.anchorMin;
        lastVanillaAnchorMax = sprintMeterRect.anchorMax;
        lastVanillaPivot = sprintMeterRect.pivot;
        lastVanillaSizeDelta = sprintMeterRect.sizeDelta;
        lastVanillaAnchoredPosition = sprintMeterRect.anchoredPosition;
        lastVanillaLocalRotation = sprintMeterRect.localRotation;
        lastVanillaLocalScale = sprintMeterRect.localScale;
        lastVanillaRingScale = ModConfig.VanillaRingScale.Value;
        lastVanillaRingOffsetX = ModConfig.VanillaRingOffsetX.Value;
        lastVanillaRingOffsetY = ModConfig.VanillaRingOffsetY.Value;
    }

    private static bool ShouldForceVanillaHudLiveLayoutRefresh()
    {
        return ModConfig.DebugVanillaHudLiveLayoutRefresh?.Value == true;
    }

    private void ApplyOrRestoreVanillaWarningTextOffset(bool infectionRingVisible)
    {
        bool shouldOffset = activeHudStyle == HudStyle.VanillaStaminaRing
            && infectionRingVisible
            && ModConfig.InfectionBarEnabled.Value
            && ModConfig.VanillaWarningTextOffsetEnabled.Value;
        if (!shouldOffset)
        {
            RestoreVanillaWarningTextOffset();
            return;
        }

        RectTransform warningRoot = ResolveVanillaWarningRoot();
        if (warningRoot == null)
        {
            if (ShouldLogDiagnostics() && !loggedMissingVanillaWarningRoot)
            {
                logger.LogWarning("Waiting for original warning text root before applying vanilla HUD warning text offset.");
                loggedMissingVanillaWarningRoot = true;
            }

            return;
        }

        loggedMissingVanillaWarningRoot = false;
        if (shiftedVanillaWarningRoot != warningRoot)
        {
            RestoreVanillaWarningTextOffset();
            shiftedVanillaWarningRoot = warningRoot;
            originalVanillaWarningAnchoredPosition = warningRoot.anchoredPosition;
            hasOriginalVanillaWarningAnchoredPosition = true;
        }

        if (!hasOriginalVanillaWarningAnchoredPosition)
        {
            originalVanillaWarningAnchoredPosition = warningRoot.anchoredPosition;
            hasOriginalVanillaWarningAnchoredPosition = true;
        }

        VanillaWarningTextOffsetCalculator.CalculateShiftedPosition(
            true,
            originalVanillaWarningAnchoredPosition.x,
            originalVanillaWarningAnchoredPosition.y,
            ModConfig.VanillaWarningTextOffsetX.Value,
            ModConfig.VanillaWarningTextOffsetY.Value,
            out float shiftedX,
            out float shiftedY);
        Vector2 shiftedPosition = new Vector2(shiftedX, shiftedY);
        if ((warningRoot.anchoredPosition - shiftedPosition).sqrMagnitude > 0.0001f)
        {
            warningRoot.anchoredPosition = shiftedPosition;
        }
    }

    private void RestoreVanillaWarningTextOffset()
    {
        if (shiftedVanillaWarningRoot != null && hasOriginalVanillaWarningAnchoredPosition)
        {
            shiftedVanillaWarningRoot.anchoredPosition = originalVanillaWarningAnchoredPosition;
        }

        shiftedVanillaWarningRoot = null;
        originalVanillaWarningAnchoredPosition = Vector2.zero;
        hasOriginalVanillaWarningAnchoredPosition = false;
        loggedMissingVanillaWarningRoot = false;
    }

    private static RectTransform ResolveVanillaWarningRoot()
    {
        if (cachedVanillaWarningRoot != null)
        {
            return cachedVanillaWarningRoot;
        }

        if (Time.unscaledTime < nextVanillaWarningRootLookupTime)
        {
            return null;
        }

        nextVanillaWarningRootLookupTime = Time.unscaledTime + 1f;
        HUDManager hudManager = HUDManager.Instance;
        if (hudManager == null)
        {
            return null;
        }

        if (hudManager.statusEffectText != null)
        {
            cachedVanillaWarningRoot = hudManager.statusEffectText.rectTransform;
            return cachedVanillaWarningRoot;
        }

        if (hudRadiationGraphicAnimatorField == null)
        {
            hudRadiationGraphicAnimatorField = typeof(HUDManager).GetField("radiationGraphicAnimator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        if (hudRadiationGraphicAnimatorField?.GetValue(hudManager) is Component radiationGraphicAnimator
            && radiationGraphicAnimator.transform is RectTransform directRoot)
        {
            cachedVanillaWarningRoot = directRoot;
            return cachedVanillaWarningRoot;
        }

        Transform[] transforms = hudManager.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform is RectTransform rectTransform && IsVanillaWarningRoot(transform))
            {
                cachedVanillaWarningRoot = rectTransform;
                return cachedVanillaWarningRoot;
            }
        }

        return null;
    }

    private static bool IsVanillaWarningRoot(Transform transform)
    {
        return transform != null
            && string.Equals(transform.name, "RadiationIncrease", StringComparison.OrdinalIgnoreCase)
            && GetHierarchyPath(transform).EndsWith(VanillaWarningRootPathSuffix, StringComparison.OrdinalIgnoreCase);
    }

    private static void GetVanillaArcParameters(VanillaArcTextSlot slot, int textLength, out float startAngle, out float endAngle, out VanillaArcRadiusBand radiusBand, out float radiusOffsetPixels)
    {
        float centerAngle;
        float maxSpan;
        float characterStep;

        switch (slot)
        {
            case VanillaArcTextSlot.WeightInfectionInner:
                centerAngle = 24f;
                maxSpan = 16f;
                characterStep = 4.8f;
                radiusBand = VanillaArcRadiusBand.Outer;
                radiusOffsetPixels = 86f;
                break;
            case VanillaArcTextSlot.InfectionOuter:
                centerAngle = 42f;
                maxSpan = 22f;
                characterStep = 4.4f;
                radiusBand = VanillaArcRadiusBand.Outer;
                radiusOffsetPixels = 150f;
                break;
            default:
                centerAngle = 43f;
                maxSpan = 16f;
                characterStep = 4.8f;
                radiusBand = VanillaArcRadiusBand.Outer;
                radiusOffsetPixels = 92f;
                break;
        }

        float span = textLength <= 1 ? 0f : Mathf.Min(maxSpan, characterStep * (textLength - 1));
        startAngle = centerAngle + span * 0.5f;
        endAngle = centerAngle - span * 0.5f;
    }

    private static float GetVanillaArcCharacterRotation(float angle, VanillaArcTextSlot slot)
    {
        switch (slot)
        {
            case VanillaArcTextSlot.WeightInfectionInner:
                return Mathf.Clamp(angle - 56f, -42f, -10f);
            case VanillaArcTextSlot.InfectionOuter:
                return Mathf.Clamp(angle - 58f, -48f, -10f);
            default:
                return Mathf.Clamp(angle - 54f, -36f, -8f);
        }
    }

    private static float GetVanillaArcFontScale(VanillaArcTextSlot slot)
    {
        switch (slot)
        {
            case VanillaArcTextSlot.InfectionOuter:
                return 0.62f;
            case VanillaArcTextSlot.WeightInfectionInner:
                return 0.60f;
            default:
                return 0.66f;
        }
    }

    private static float GetVanillaArcCharacterClearancePixels(VanillaArcTextSlot slot)
    {
        switch (slot)
        {
            case VanillaArcTextSlot.WeightInfectionInner:
                return 78f;
            case VanillaArcTextSlot.InfectionOuter:
                return 118f;
            default:
                return 70f;
        }
    }

    private static float GetSprintMeterArcRadius(float angle, VanillaArcRadiusBand band)
    {
        float[] radii = band == VanillaArcRadiusBand.Inner
            ? SprintMeterArcInnerRadii
            : band == VanillaArcRadiusBand.Outer
                ? SprintMeterArcOuterRadii
                : SprintMeterArcMiddleRadii;

        if (angle <= SprintMeterArcAngles[0])
        {
            return radii[0];
        }

        int lastIndex = SprintMeterArcAngles.Length - 1;
        if (angle >= SprintMeterArcAngles[lastIndex])
        {
            return radii[lastIndex];
        }

        for (int i = 0; i < lastIndex; i++)
        {
            float startAngle = SprintMeterArcAngles[i];
            float endAngle = SprintMeterArcAngles[i + 1];
            if (angle < startAngle || angle > endAngle)
            {
                continue;
            }

            float t = Mathf.InverseLerp(startAngle, endAngle, angle);
            return Mathf.Lerp(radii[i], radii[i + 1], t);
        }

        return radii[lastIndex];
    }

    private static float ApplyVanillaArcTextClearance(float angle, float radius, VanillaArcTextSlot slot, VanillaArcRadiusBand band)
    {
        float clearance = GetVanillaArcCharacterClearancePixels(slot);
        float innerRadius = GetSprintMeterArcRadius(angle, VanillaArcRadiusBand.Inner);
        float outerRadius = GetSprintMeterArcRadius(angle, VanillaArcRadiusBand.Outer);

        switch (slot)
        {
            case VanillaArcTextSlot.WeightInfectionInner:
                return Mathf.Max(radius, outerRadius + clearance);
            case VanillaArcTextSlot.InfectionOuter:
                return Mathf.Max(radius, outerRadius + clearance);
            default:
                return Mathf.Max(radius, outerRadius + clearance);
        }
    }

    private static void ApplyVanillaTextRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position, Quaternion rotation, Vector3 scale)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localRotation = rotation;
        rect.localScale = scale;
    }

    private static void ApplyVanillaArcText(RectTransform root, string text, Color color, VanillaArcTextSlot slot)
    {
        if (root == null)
        {
            return;
        }

        string textValue = text ?? string.Empty;
        DisableRootText(root);
        SetUnusedVanillaArcCharactersInactive(root, textValue.Length);
        if (textValue.Length == 0)
        {
            return;
        }

        Vector2 rootSize = root.sizeDelta;
        if (rootSize.sqrMagnitude < 0.0001f)
        {
            rootSize = new Vector2(80f, 80f);
        }

        VanillaArcGlyphLayout[] glyphLayouts = VanillaArcTextLayout.Build(textValue, GetVanillaArcTextTrack(slot), Mathf.Abs(rootSize.x), Mathf.Abs(rootSize.y));

        for (int i = 0; i < glyphLayouts.Length; i++)
        {
            VanillaArcGlyphLayout glyphLayout = glyphLayouts[i];
            RectTransform characterRect = GetOrCreateVanillaArcCharacter(root, i);
            TextMeshProUGUI characterText = characterRect.GetComponent<TextMeshProUGUI>();

            characterRect.gameObject.SetActive(true);
            characterRect.anchoredPosition = new Vector2(glyphLayout.LocalX, glyphLayout.LocalY);
            characterRect.localRotation = Quaternion.Euler(0f, 0f, glyphLayout.RotationZ);
            characterRect.localScale = Vector3.one;

            string characterValue = glyphLayout.Character.ToString();
            if (!string.Equals(characterText.text, characterValue, StringComparison.Ordinal))
            {
                characterText.text = characterValue;
            }

            CopyWeightCounterTextStyle(characterText, color);
            characterText.fontSize *= glyphLayout.FontScale;
            characterText.alignment = TextAlignmentOptions.Center;
            characterText.raycastTarget = false;
            characterText.enableWordWrapping = false;
            characterText.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private static VanillaArcTextTrack GetVanillaArcTextTrack(VanillaArcTextSlot slot)
    {
        switch (slot)
        {
            case VanillaArcTextSlot.WeightInfectionInner:
                return VanillaArcTextTrack.InfectionWeightInner;
            case VanillaArcTextSlot.InfectionOuter:
                return VanillaArcTextTrack.InfectionLabelOuter;
            default:
                return VanillaArcTextTrack.StaminaWeightUpper;
        }
    }

    private static void DisableRootText(RectTransform root)
    {
        TextMeshProUGUI rootText = root.GetComponent<TextMeshProUGUI>();
        if (rootText == null)
        {
            return;
        }

        rootText.enabled = false;
        rootText.text = string.Empty;
    }

    private static RectTransform GetOrCreateVanillaArcCharacter(RectTransform root, int index)
    {
        string childName = VanillaArcCharacterPrefix + index.ToString("00");
        Transform existingChild = root.Find(childName);
        if (existingChild != null)
        {
            return existingChild as RectTransform;
        }

        GameObject characterObject = new GameObject(childName, typeof(RectTransform), typeof(TextMeshProUGUI));
        characterObject.transform.SetParent(root, false);
        RectTransform characterRect = characterObject.GetComponent<RectTransform>();
        characterRect.anchorMin = new Vector2(0.5f, 0.5f);
        characterRect.anchorMax = new Vector2(0.5f, 0.5f);
        characterRect.pivot = new Vector2(0.5f, 0.5f);
        characterRect.sizeDelta = new Vector2(36f, 36f);

        TextMeshProUGUI characterText = characterObject.GetComponent<TextMeshProUGUI>();
        characterText.raycastTarget = false;
        characterText.enableWordWrapping = false;
        characterText.overflowMode = TextOverflowModes.Overflow;
        characterText.alignment = TextAlignmentOptions.Center;
        return characterRect;
    }

    private static void SetUnusedVanillaArcCharactersInactive(RectTransform root, int activeCount)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (!child.name.StartsWith(VanillaArcCharacterPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (!int.TryParse(child.name.Substring(VanillaArcCharacterPrefix.Length), out int childIndex))
            {
                continue;
            }

            child.gameObject.SetActive(childIndex < activeCount);
        }
    }

    private static void CopyWeightCounterTextStyle(TextMeshProUGUI target, Color color)
    {
        if (target == null)
        {
            return;
        }

        TextMeshProUGUI weightCounter = HUDManager.Instance?.weightCounter;
        if (weightCounter != null)
        {
            target.font = weightCounter.font;
            target.fontSharedMaterial = weightCounter.fontSharedMaterial;
            target.fontSize = weightCounter.fontSize;
            target.fontStyle = weightCounter.fontStyle;
            target.alignment = weightCounter.alignment;
            target.characterSpacing = weightCounter.characterSpacing;
            target.wordSpacing = weightCounter.wordSpacing;
            target.lineSpacing = weightCounter.lineSpacing;
        }
        else
        {
            target.fontSize = 16f;
            target.alignment = TextAlignmentOptions.Center;
        }

        target.color = color;
    }

    private void UpdateVanillaWeightText(bool force = false)
    {
        TextMeshProUGUI weightCounter = HUDManager.Instance?.weightCounter;
        string currentWeightText = weightCounter != null ? weightCounter.text : string.Empty;
        VanillaArcTextSlot weightSlot = lastVisibleState ? VanillaArcTextSlot.WeightInfectionInner : VanillaArcTextSlot.WeightStaminaUpper;
        Vector2 rootSize = vanillaWeightTextRoot != null ? vanillaWeightTextRoot.sizeDelta : Vector2.zero;
        bool rootSizeChanged = (lastAppliedVanillaWeightRootSize - rootSize).sqrMagnitude > 0.0001f;
        lastVanillaWeightText = currentWeightText;

        if (!force
            && hasAppliedVanillaWeightText
            && lastAppliedVanillaWeightSlot == weightSlot
            && !rootSizeChanged
            && string.Equals(lastAppliedVanillaWeightText, currentWeightText, StringComparison.Ordinal))
        {
            return;
        }

        ApplyVanillaArcText(vanillaWeightTextRoot, currentWeightText, new Color(0.95f, 0.40f, 0.04f, 0.96f), weightSlot);
        lastAppliedVanillaWeightText = currentWeightText;
        lastAppliedVanillaWeightSlot = weightSlot;
        lastAppliedVanillaWeightRootSize = rootSize;
        hasAppliedVanillaWeightText = true;
    }

    private void ApplyVanillaInfectionTextSegments(bool force = false)
    {
        Vector2 rootSize = vanillaInfectionTextRoot != null ? vanillaInfectionTextRoot.sizeDelta : Vector2.zero;
        bool rootSizeChanged = (lastAppliedVanillaInfectionRootSize - rootSize).sqrMagnitude > 0.0001f;
        if (force
            || !hasAppliedVanillaInfectionText
            || rootSizeChanged
            || !string.Equals(lastAppliedVanillaInfectionText, lastVanillaInfectionText, StringComparison.Ordinal))
        {
            ApplyVanillaArcText(vanillaInfectionTextRoot, lastVanillaInfectionText, new Color(0.95f, 0.26f, 0.18f, 0.95f), VanillaArcTextSlot.InfectionOuter);
            lastAppliedVanillaInfectionText = lastVanillaInfectionText;
            lastAppliedVanillaInfectionRootSize = rootSize;
            hasAppliedVanillaInfectionText = true;
        }

        if (force || !hasClearedVanillaInfectionValueText)
        {
            ApplyVanillaArcText(vanillaInfectionValueTextRoot, string.Empty, new Color(0.95f, 0.26f, 0.18f, 0.95f), VanillaArcTextSlot.InfectionOuter);
            hasClearedVanillaInfectionValueText = true;
        }
    }

    private void SetOriginalWeightCounterHidden(bool hidden)
    {
        TextMeshProUGUI weightCounter = HUDManager.Instance?.weightCounter;
        if (!hidden)
        {
            RestoreOriginalWeightCounter();
            return;
        }

        if (weightCounter == null)
        {
            return;
        }

        if (hiddenWeightCounter != weightCounter)
        {
            RestoreOriginalWeightCounter();
            hiddenWeightCounter = weightCounter;
            hiddenWeightCounterWasEnabled = weightCounter.enabled;
        }

        weightCounter.enabled = false;
    }

    private void RestoreOriginalWeightCounter()
    {
        if (hiddenWeightCounter != null)
        {
            hiddenWeightCounter.enabled = hiddenWeightCounterWasEnabled;
        }

        hiddenWeightCounter = null;
        hiddenWeightCounterWasEnabled = false;
    }

    private static Vector2 GetRectReferenceSize(RectTransform rectTransform)
    {
        Vector2 referenceSize = rectTransform.rect.size;
        if (referenceSize.sqrMagnitude < 0.0001f)
        {
            referenceSize = rectTransform.sizeDelta;
        }

        return referenceSize;
    }

    private void ApplyVanillaStaminaRingTransform(RectTransform sprintMeterRect)
    {
        float scale = Mathf.Max(0.1f, ModConfig.VanillaRingScale.Value);
        Vector2 outerRingOffset = GetVanillaOuterRingOffset(sprintMeterRect);
        infectionRoot.anchorMin = sprintMeterRect.anchorMin;
        infectionRoot.anchorMax = sprintMeterRect.anchorMax;
        infectionRoot.pivot = sprintMeterRect.pivot;
        infectionRoot.sizeDelta = sprintMeterRect.sizeDelta * scale;
        infectionRoot.anchoredPosition = sprintMeterRect.anchoredPosition
            + outerRingOffset
            + new Vector2(ModConfig.VanillaRingOffsetX.Value, ModConfig.VanillaRingOffsetY.Value);
        infectionRoot.localRotation = sprintMeterRect.localRotation;
        infectionRoot.localScale = sprintMeterRect.localScale;
    }

    private static Vector2 GetVanillaOuterRingOffset(RectTransform sprintMeterRect)
    {
        Vector2 referenceSize = GetRectReferenceSize(sprintMeterRect);
        float offsetX = Mathf.Max(28f, Mathf.Abs(referenceSize.x) * 0.08f);
        float offsetY = -Mathf.Max(18f, Mathf.Abs(referenceSize.y) * 0.10f);
        return new Vector2(offsetX, offsetY);
    }

    private static void CopySprintMeterImageStyle(Image source, Image target, Color color)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.sprite = source.sprite;
        target.overrideSprite = source.overrideSprite;
        target.material = source.material;
        target.type = source.type;
        target.fillMethod = source.fillMethod;
        target.fillOrigin = source.fillOrigin;
        target.fillClockwise = source.fillClockwise;
        target.fillCenter = source.fillCenter;
        target.preserveAspect = source.preserveAspect;
        target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
        target.color = color;
        target.raycastTarget = false;
    }

    private static void ApplyVanillaRingFillAmount(Image target, float fillAmount)
    {
        if (target == null)
        {
            return;
        }

        target.type = Image.Type.Filled;
        target.fillAmount = Mathf.Clamp01(fillAmount);
        target.raycastTarget = false;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void LogVanillaSprintMeterDiagnostics(Image sprintMeterImage, RectTransform sprintMeterRect)
    {
        if (!ShouldLogDiagnostics() || loggedVanillaSprintMeterDiagnostics)
        {
            return;
        }

        loggedVanillaSprintMeterDiagnostics = true;
        string spriteName = sprintMeterImage.sprite != null ? sprintMeterImage.sprite.name : "<null>";
        string overrideSpriteName = sprintMeterImage.overrideSprite != null ? sprintMeterImage.overrideSprite.name : "<null>";
        string materialName = sprintMeterImage.material != null ? sprintMeterImage.material.name : "<null>";
        logger.LogInfo("Original sprint meter UI found: path=" + GetHierarchyPath(sprintMeterRect)
            + ", sprite=" + spriteName
            + ", overrideSprite=" + overrideSpriteName
            + ", material=" + materialName
            + ", type=" + sprintMeterImage.type
            + ", fillMethod=" + sprintMeterImage.fillMethod
            + ", fillOrigin=" + sprintMeterImage.fillOrigin
            + ", fillClockwise=" + sprintMeterImage.fillClockwise
            + ", fillAmount=" + sprintMeterImage.fillAmount
            + ", anchorMin=" + sprintMeterRect.anchorMin
            + ", anchorMax=" + sprintMeterRect.anchorMax
            + ", pivot=" + sprintMeterRect.pivot
            + ", sizeDelta=" + sprintMeterRect.sizeDelta
            + ", anchoredPosition=" + sprintMeterRect.anchoredPosition
            + ", localRotation=" + sprintMeterRect.localEulerAngles);
    }

    private void ResetSceneCachesIfHudManagerChanged()
    {
        HUDManager hudManager = HUDManager.Instance;
        StartOfRound startOfRound = StartOfRound.Instance;
        if (cachedHudManagerInstance == hudManager && cachedStartOfRoundInstance == startOfRound)
        {
            return;
        }

        cachedHudManagerInstance = hudManager;
        cachedStartOfRoundInstance = startOfRound;
        ClearCachedSceneReferences();
        dataProvider.ResetCadaverGrowthCache();
        DestroyInfectionUI();
    }

    private static void ClearCachedSceneReferences()
    {
        cachedTerminal = null;
        cachedNativeHudElementTransform = null;
        cachedNativeHudParentFallback = null;
        cachedVanillaWarningRoot = null;
        nextTerminalLookupTime = 0f;
        nextNativeHudElementLookupTime = 0f;
        nextNativeHudParentFallbackLookupTime = 0f;
        nextVanillaWarningRootLookupTime = 0f;
    }

    private void DestroyInfectionUI()
    {
        RestoreVanillaWarningTextOffset();

        if (infectionRoot != null)
        {
            Destroy(infectionRoot.gameObject);
        }

        canvasGroup = null;
        infectionRoot = null;
        panelBackground = null;
        topLine = null;
        infectionBackground = null;
        infectionBar = null;
        infectionText = null;
        vanillaSprintMeterReference = null;
        RestoreOriginalWeightCounter();
        if (vanillaWeightTextRoot != null)
        {
            Destroy(vanillaWeightTextRoot.gameObject);
        }

        if (vanillaInfectionTextRoot != null)
        {
            Destroy(vanillaInfectionTextRoot.gameObject);
        }

        if (vanillaInfectionValueTextRoot != null)
        {
            Destroy(vanillaInfectionValueTextRoot.gameObject);
        }

        vanillaWeightTextRoot = null;
        vanillaWeightTextCanvasGroup = null;
        vanillaInfectionTextRoot = null;
        vanillaInfectionTextCanvasGroup = null;
        vanillaInfectionValueTextRoot = null;
        vanillaInfectionValueTextCanvasGroup = null;
        lastVanillaWeightText = string.Empty;
        lastVanillaInfectionText = string.Empty;
        lastAppliedVanillaWeightText = string.Empty;
        lastAppliedVanillaInfectionText = string.Empty;
        hasAppliedVanillaWeightText = false;
        hasAppliedVanillaInfectionText = false;
        hasClearedVanillaInfectionValueText = false;
        lastAppliedVanillaWeightSlot = VanillaArcTextSlot.WeightStaminaUpper;
        lastAppliedVanillaWeightRootSize = Vector2.zero;
        lastAppliedVanillaInfectionRootSize = Vector2.zero;
        hasVanillaLayoutSignature = false;
        loggedMissingVanillaWarningRoot = false;
        hasActiveHudStyle = false;
        lastVisibleState = false;
        lastRenderedInfectionFillAmount = -1f;
        lastRenderedInfectionPercent = -1;
        lastRenderedInfectionLabel = string.Empty;
        currentHudAlpha = 1f;
        layoutDirty = true;
        loggedMissingNativeHudParent = false;
        loggedNativeHudParentFallback = false;
        loggedMissingSprintMeter = false;
        loggedVanillaSprintMeterDiagnostics = false;
    }

    private void RefreshLayoutIfNeeded(PlayerControllerB player)
    {
        NativeHudState nativeHudState = GetNativeHudState();
        float targetHudAlpha = nativeHudState.IsValid ? nativeHudState.Alpha : 1f;
        currentHudAlpha = Mathf.MoveTowards(currentHudAlpha, targetHudAlpha, Time.unscaledDeltaTime * 12f);
        canvasGroup.alpha = currentHudAlpha;
        if (vanillaWeightTextCanvasGroup != null)
        {
            vanillaWeightTextCanvasGroup.alpha = currentHudAlpha;
        }

        if (vanillaInfectionTextCanvasGroup != null)
        {
            vanillaInfectionTextCanvasGroup.alpha = currentHudAlpha;
        }

        if (vanillaInfectionValueTextCanvasGroup != null)
        {
            vanillaInfectionValueTextCanvasGroup.alpha = currentHudAlpha;
        }

        if (activeHudStyle == HudStyle.VanillaStaminaRing)
        {
            RefreshVanillaStaminaRingLayout(player);
            return;
        }

        ApplyNativeHudElementRotation();
        if (!layoutDirty)
        {
            return;
        }

        layoutDirty = false;
        Vector2 rootSize = GetEffectiveRootSize();
        if ((infectionRoot.sizeDelta - rootSize).sqrMagnitude > 0.0001f)
        {
            infectionRoot.sizeDelta = rootSize;
            panelBackground.rectTransform.sizeDelta = rootSize;
        }

        Vector2 anchorMin = layout.GetAnchorMin();
        if ((infectionRoot.anchorMin - anchorMin).sqrMagnitude > 0.0001f)
        {
            infectionRoot.anchorMin = anchorMin;
        }

        Vector2 anchorMax = layout.GetAnchorMax();
        if ((infectionRoot.anchorMax - anchorMax).sqrMagnitude > 0.0001f)
        {
            infectionRoot.anchorMax = anchorMax;
        }

        Vector2 pivot = layout.GetPivot();
        if ((infectionRoot.pivot - pivot).sqrMagnitude > 0.0001f)
        {
            infectionRoot.pivot = pivot;
        }

        panelBackground.enabled = ModConfig.ShowPanelBackground.Value;
        panelBackground.color = new Color(0.10f, 0.02f, 0.02f, Mathf.Clamp01(ModConfig.PanelBackgroundAlpha.Value));

        Vector2 alignedAnchoredPosition = AlignVector2(layout.GetAnchoredPosition());
        if ((infectionRoot.anchoredPosition - alignedAnchoredPosition).sqrMagnitude > 0.0001f)
        {
            infectionRoot.anchoredPosition = alignedAnchoredPosition;
        }

        Vector2 barSize = GetEffectiveBarSize();
        if ((infectionBackground.rectTransform.sizeDelta - barSize).sqrMagnitude > 0.0001f)
        {
            infectionBackground.rectTransform.sizeDelta = barSize;
            topLine.rectTransform.sizeDelta = barSize;
        }

        RectTransform fillRect = infectionBar.rectTransform;
        if ((fillRect.sizeDelta - Vector2.zero).sqrMagnitude > 0.0001f)
        {
            fillRect.sizeDelta = Vector2.zero;
        }

        if ((fillRect.offsetMin - Vector2.zero).sqrMagnitude > 0.0001f)
        {
            fillRect.offsetMin = Vector2.zero;
        }

        if ((fillRect.offsetMax - Vector2.zero).sqrMagnitude > 0.0001f)
        {
            fillRect.offsetMax = Vector2.zero;
        }

        RectTransform textRect = infectionText.rectTransform;
        Vector2 desiredTextPosition = AlignVector2(new Vector2(ModConfig.TextOffsetX.Value, ModConfig.TextOffsetY.Value));
        if ((textRect.anchoredPosition - desiredTextPosition).sqrMagnitude > 0.0001f)
        {
            textRect.anchoredPosition = desiredTextPosition;
        }

        if (infectionText.fontSize != ModConfig.TextFontSize.Value)
        {
            infectionText.fontSize = ModConfig.TextFontSize.Value;
        }

        Vector2 desiredTextSize = new Vector2(rootSize.x - 16f, 18f);
        if ((textRect.sizeDelta - desiredTextSize).sqrMagnitude > 0.0001f)
        {
            textRect.sizeDelta = desiredTextSize;
        }
    }

    private void SubscribeLayoutConfigEvents()
    {
        if (layoutConfigEventsSubscribed || ModConfig.UiWidth == null)
        {
            return;
        }

        ModConfig.HudStyleMode.SettingChanged += OnLayoutConfigChanged;
        ModConfig.UiWidth.SettingChanged += OnLayoutConfigChanged;
        ModConfig.UiHeight.SettingChanged += OnLayoutConfigChanged;
        ModConfig.AnchorPreset.SettingChanged += OnLayoutConfigChanged;
        ModConfig.PositionOffsetX.SettingChanged += OnLayoutConfigChanged;
        ModConfig.PositionOffsetY.SettingChanged += OnLayoutConfigChanged;
        ModConfig.TextOffsetX.SettingChanged += OnLayoutConfigChanged;
        ModConfig.TextOffsetY.SettingChanged += OnLayoutConfigChanged;
        ModConfig.TextFontSize.SettingChanged += OnLayoutConfigChanged;
        ModConfig.ShowPanelBackground.SettingChanged += OnLayoutConfigChanged;
        ModConfig.PanelBackgroundAlpha.SettingChanged += OnLayoutConfigChanged;
        ModConfig.ReduceAliasing.SettingChanged += OnLayoutConfigChanged;
        ModConfig.VanillaRingScale.SettingChanged += OnLayoutConfigChanged;
        ModConfig.VanillaRingOffsetX.SettingChanged += OnLayoutConfigChanged;
        ModConfig.VanillaRingOffsetY.SettingChanged += OnLayoutConfigChanged;
        ModConfig.VanillaWarningTextOffsetEnabled.SettingChanged += OnLayoutConfigChanged;
        ModConfig.VanillaWarningTextOffsetX.SettingChanged += OnLayoutConfigChanged;
        ModConfig.VanillaWarningTextOffsetY.SettingChanged += OnLayoutConfigChanged;
        ModConfig.DebugVanillaHudLiveLayoutRefresh.SettingChanged += OnLayoutConfigChanged;
        layoutConfigEventsSubscribed = true;
    }

    private void UnsubscribeLayoutConfigEvents()
    {
        if (!layoutConfigEventsSubscribed || ModConfig.UiWidth == null)
        {
            return;
        }

        ModConfig.HudStyleMode.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.UiWidth.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.UiHeight.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.AnchorPreset.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.PositionOffsetX.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.PositionOffsetY.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.TextOffsetX.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.TextOffsetY.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.TextFontSize.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.ShowPanelBackground.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.PanelBackgroundAlpha.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.ReduceAliasing.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.VanillaRingScale.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.VanillaRingOffsetX.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.VanillaRingOffsetY.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.VanillaWarningTextOffsetEnabled.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.VanillaWarningTextOffsetX.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.VanillaWarningTextOffsetY.SettingChanged -= OnLayoutConfigChanged;
        ModConfig.DebugVanillaHudLiveLayoutRefresh.SettingChanged -= OnLayoutConfigChanged;
        layoutConfigEventsSubscribed = false;
    }

    private void OnLayoutConfigChanged(object sender, EventArgs args)
    {
        layoutDirty = true;
    }

    private void UpdateInfection(PlayerControllerB player)
    {
        if (!ModConfig.InfectionBarEnabled.Value)
        {
            SetInfectionVisible(false);
            ApplyOrRestoreVanillaWarningTextOffset(false);
            return;
        }

        if (infectionRoot == null || infectionBar == null || (infectionText == null && vanillaInfectionTextRoot == null))
        {
            return;
        }

        float infectionNormalized = GetInfectionNormalized(player);
        bool shouldShow = ShouldShowInfectionBar(player, infectionNormalized);

        SetInfectionVisible(shouldShow);
        ApplyOrRestoreVanillaWarningTextOffset(shouldShow);
        if (!shouldShow)
        {
            return;
        }

        ApplyInfectionFillAmount(infectionNormalized);

        string infectionLabel = GetInfectionLabel();
        int infectionPercent = Mathf.RoundToInt(infectionNormalized * 100f);
        string infectionTextValue = FormatInfectionText(infectionLabel, infectionPercent);
        if (lastRenderedInfectionPercent != infectionPercent || !string.Equals(lastRenderedInfectionLabel, infectionTextValue, StringComparison.Ordinal))
        {
            SetInfectionText(infectionTextValue);
            lastRenderedInfectionPercent = infectionPercent;
            lastRenderedInfectionLabel = infectionTextValue;
        }

        if (activeHudStyle == HudStyle.VanillaStaminaRing)
        {
            UpdateVanillaWeightText();
        }
    }

    private string FormatInfectionText(string infectionLabel, int infectionPercent)
    {
        return infectionLabel + " " + infectionPercent.ToString() + "%";
    }

    private void ApplyInfectionFillAmount(float infectionNormalized)
    {
        float clampedInfection = Mathf.Clamp01(infectionNormalized);
        float renderedFillAmount = activeHudStyle == HudStyle.VanillaStaminaRing
            ? VanillaRingFillMapping.MapInfectionToVisibleFill(clampedInfection)
            : clampedInfection;
        if (Mathf.Abs(lastRenderedInfectionFillAmount - clampedInfection) <= 0.0001f
            && Mathf.Abs(infectionBar.fillAmount - renderedFillAmount) <= 0.0001f)
        {
            return;
        }

        if (activeHudStyle == HudStyle.VanillaStaminaRing)
        {
            ApplyVanillaRingFillAmount(infectionBar, renderedFillAmount);
        }
        else
        {
            infectionBar.fillAmount = renderedFillAmount;
        }

        lastRenderedInfectionFillAmount = clampedInfection;
    }

    private void SetInfectionText(string text)
    {
        if (infectionText != null)
        {
            infectionText.text = text;
        }

        lastVanillaInfectionText = text;
        ApplyVanillaInfectionTextSegments();
    }

    private bool ShouldShowInfectionBar(PlayerControllerB player, float infectionNormalized)
    {
        if (!ModConfig.InfectionBarEnabled.Value)
        {
            return false;
        }

        if (player == null || player.isPlayerDead || !player.isPlayerControlled)
        {
            return false;
        }

        if (ModConfig.InfectionBarAlwaysVisible.Value)
        {
            return true;
        }

        return infectionNormalized > 0f;
    }

    private static NativeHudState GetNativeHudState()
    {
        HUDManager hudManager = HUDManager.Instance;
        if (hudManager == null)
        {
            return default;
        }

        if (hudHudHiddenField == null)
        {
            hudHudHiddenField = typeof(HUDManager).GetField("hudHidden", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        bool hudHidden = hudHudHiddenField != null
            && hudHudHiddenField.GetValue(hudManager) is bool hiddenValue
            && hiddenValue;
        if (hudHidden)
        {
            return new NativeHudState(0f, isValid: true);
        }

        if (IsTerminalInUse(hudManager))
        {
            return new NativeHudState(Mathf.Clamp01(ModConfig.TerminalFadeAlpha.Value), isValid: true);
        }

        return new NativeHudState(1f, isValid: true);
    }

    private static bool IsTerminalInUse(HUDManager hudManager)
    {
        Terminal terminal = ResolveTerminal(hudManager);

        if (terminal == null)
        {
            return false;
        }

        if (terminalInUseField == null)
        {
            terminalInUseField = typeof(Terminal).GetField("terminalInUse", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        return terminalInUseField != null
            && terminalInUseField.GetValue(terminal) is bool terminalInUse
            && terminalInUse;
    }

    private static Terminal ResolveTerminal(HUDManager hudManager)
    {
        if (hudTerminalScriptField == null)
        {
            hudTerminalScriptField = typeof(HUDManager).GetField("terminalScript", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        Terminal terminal = hudTerminalScriptField?.GetValue(hudManager) as Terminal;
        if (terminal != null)
        {
            cachedTerminal = terminal;
            return terminal;
        }

        if (cachedTerminal != null)
        {
            return cachedTerminal;
        }

        if (Time.unscaledTime < nextTerminalLookupTime)
        {
            return null;
        }

        nextTerminalLookupTime = Time.unscaledTime + 1f;
        cachedTerminal = FindObjectOfType<Terminal>();
        return cachedTerminal;
    }

    private static HUDElement GetHudElementFieldValue(HUDManager hudManager, ref FieldInfo cache, string fieldName)
    {
        if (cache == null)
        {
            cache = typeof(HUDManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        return cache?.GetValue(hudManager) as HUDElement;
    }

    private RectTransform GetNativeHudParentTransform()
    {
        HUDManager hudManager = HUDManager.Instance;
        if (hudManager == null)
        {
            return null;
        }

        Transform nativeHudElementTransform = ResolveNativeHudElementTransform();
        RectTransform nativeHudElementParent = nativeHudElementTransform?.parent as RectTransform;
        if (nativeHudElementParent != null)
        {
            return nativeHudElementParent;
        }

        if (hudContainerField == null)
        {
            hudContainerField = typeof(HUDManager).GetField("HUDContainer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        if (hudContainerField?.GetValue(hudManager) is GameObject hudContainer)
        {
            RectTransform hudContainerRect = hudContainer.GetComponent<RectTransform>();
            if (hudContainerRect != null)
            {
                return hudContainerRect;
            }
        }

        HUDElement playerInfoElement = GetHudElementFieldValue(hudManager, ref hudPlayerInfoField, "PlayerInfo");
        if (playerInfoElement?.canvasGroup != null)
        {
            RectTransform parentRect = playerInfoElement.canvasGroup.transform.parent as RectTransform;
            if (parentRect != null)
            {
                return parentRect;
            }
        }

        RectTransform fallbackParent = ResolveNativeHudParentFallback();
        if (fallbackParent != null && ShouldLogDiagnostics() && !loggedNativeHudParentFallback)
        {
            logger.LogWarning("Using scene fallback native HUD parent for infection bar: " + GetHierarchyPath(fallbackParent));
            loggedNativeHudParentFallback = true;
        }

        return fallbackParent;
    }

    private static RectTransform ResolveNativeHudParentFallback()
    {
        if (cachedNativeHudParentFallback != null)
        {
            return cachedNativeHudParentFallback;
        }

        if (Time.unscaledTime < nextNativeHudParentFallbackLookupTime)
        {
            return null;
        }

        nextNativeHudParentFallbackLookupTime = Time.unscaledTime + 1f;

        GameObject inGamePlayerHud = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD");
        if (inGamePlayerHud != null && inGamePlayerHud.TryGetComponent(out RectTransform hudRect))
        {
            cachedNativeHudParentFallback = hudRect;
            return cachedNativeHudParentFallback;
        }

        Canvas[] canvases = FindObjectsOfType<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            Transform child = canvases[i].transform.Find("IngamePlayerHUD");
            if (child is RectTransform childRect)
            {
                cachedNativeHudParentFallback = childRect;
                return cachedNativeHudParentFallback;
            }
        }

        return null;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
        {
            return "<null>";
        }

        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private static bool ShouldLogDiagnostics()
    {
        return ModConfig.DebugLogging?.Value == true;
    }

    private void ApplyNativeHudElementRotation()
    {
        if (infectionRoot == null)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.identity;
        if (TryGetNativeHudElementRotation(out Quaternion nativeHudRotation))
        {
            desiredRotation = nativeHudRotation;
        }

        if (infectionRoot.localRotation != desiredRotation)
        {
            infectionRoot.localRotation = desiredRotation;
        }
    }

    private bool TryGetNativeHudElementRotation(out Quaternion rotation)
    {
        rotation = Quaternion.identity;
        Transform nativeHudElementTransform = ResolveNativeHudElementTransform();
        if (nativeHudElementTransform == null)
        {
            return false;
        }

        Transform parentTransform = infectionRoot.parent;
        if (parentTransform != null && nativeHudElementTransform.parent != parentTransform)
        {
            rotation = Quaternion.Inverse(parentTransform.rotation) * nativeHudElementTransform.rotation;
        }
        else
        {
            rotation = nativeHudElementTransform.localRotation;
        }

        return true;
    }

    private static Transform ResolveNativeHudElementTransform()
    {
        if (cachedNativeHudElementTransform != null)
        {
            return cachedNativeHudElementTransform;
        }

        if (Time.unscaledTime < nextNativeHudElementLookupTime)
        {
            return null;
        }

        nextNativeHudElementLookupTime = Time.unscaledTime + 1f;

        HUDManager hudManager = HUDManager.Instance;
        if (hudManager == null)
        {
            return null;
        }

        if (hudElementsField == null)
        {
            hudElementsField = typeof(HUDManager).GetField("HUDElements", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        HUDElement[] hudElements = hudElementsField?.GetValue(hudManager) as HUDElement[];
        if (hudElements != null && hudElements.Length > 2 && hudElements[2]?.canvasGroup != null)
        {
            cachedNativeHudElementTransform = hudElements[2].canvasGroup.transform;
            return cachedNativeHudElementTransform;
        }

        HUDElement playerInfoElement = GetHudElementFieldValue(hudManager, ref hudPlayerInfoField, "PlayerInfo");
        if (playerInfoElement?.canvasGroup != null)
        {
            cachedNativeHudElementTransform = playerInfoElement.canvasGroup.transform;
        }

        return cachedNativeHudElementTransform;
    }

    private string GetInfectionLabel()
    {
        if (Time.unscaledTime >= nextInfectionLabelRefreshTime)
        {
            string infectionLabel = DetermineInfectionLabel();
            if (!string.Equals(cachedInfectionLabel, infectionLabel, StringComparison.Ordinal))
            {
                cachedInfectionLabel = infectionLabel;
            }

            nextInfectionLabelRefreshTime = Time.unscaledTime + 5f;
        }

        return cachedInfectionLabel;
    }

    private string DetermineInfectionLabel()
    {
        return languageHelper.DetermineInfectionLabel();
    }

    private float GetInfectionNormalized(PlayerControllerB player)
    {
        if (dataProvider.TryGetInfectionNormalized(player, out float infectionNormalized))
        {
            return infectionNormalized;
        }

        return 0f;
    }

    private void SetInfectionVisible(bool shouldShow)
    {
        if (infectionRoot == null)
        {
            return;
        }

        bool vanillaWeightVisible = activeHudStyle == HudStyle.VanillaStaminaRing && ModConfig.InfectionBarEnabled.Value;
        if (lastVisibleState == shouldShow
            && (vanillaWeightTextRoot == null || vanillaWeightTextRoot.gameObject.activeSelf == vanillaWeightVisible)
            && (vanillaInfectionTextRoot == null || vanillaInfectionTextRoot.gameObject.activeSelf == shouldShow))
        {
            return;
        }

        infectionRoot.gameObject.SetActive(shouldShow);
        if (activeHudStyle == HudStyle.VanillaStaminaRing)
        {
            SetOriginalWeightCounterHidden(vanillaWeightVisible);
        }

        if (vanillaWeightTextRoot != null)
        {
            vanillaWeightTextRoot.gameObject.SetActive(vanillaWeightVisible);
        }

        if (vanillaInfectionTextRoot != null)
        {
            vanillaInfectionTextRoot.gameObject.SetActive(shouldShow);
        }

        if (vanillaInfectionValueTextRoot != null)
        {
            vanillaInfectionValueTextRoot.gameObject.SetActive(false);
        }

        lastVisibleState = shouldShow;
        if (activeHudStyle == HudStyle.VanillaStaminaRing)
        {
            layoutDirty = true;
            hasAppliedVanillaWeightText = false;
        }

        if (!shouldShow)
        {
            lastRenderedInfectionFillAmount = -1f;
            lastRenderedInfectionPercent = -1;
        }
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        return rect;
    }

    private Vector2 GetEffectiveRootSize()
    {
        Vector2 rootSize = layout.GetRootSize();
        if (ModConfig.ReduceAliasing.Value)
        {
            rootSize.x = Mathf.Round(rootSize.x);
            rootSize.y = Mathf.Round(rootSize.y);
        }

        return rootSize;
    }

    private Vector2 GetEffectiveBarSize()
    {
        Vector2 barSize = layout.GetBarSize();
        if (ModConfig.ReduceAliasing.Value)
        {
            barSize.x = Mathf.Round(barSize.x);
            barSize.y = Mathf.Max(10f, Mathf.Round(barSize.y));
        }

        return barSize;
    }

    private static Vector2 AlignVector2(Vector2 value)
    {
        if (!ModConfig.ReduceAliasing.Value)
        {
            return value;
        }

        return new Vector2(Mathf.Round(value.x), Mathf.Round(value.y));
    }

    private static Sprite GetPixelSprite()
    {
        if (pixelSprite != null)
        {
            return pixelSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, mipChain: false);
        texture.name = "IndependentCadaverInfectionBarPixel";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        pixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        pixelSprite.hideFlags = HideFlags.HideAndDontSave;
        return pixelSprite;
    }
}
