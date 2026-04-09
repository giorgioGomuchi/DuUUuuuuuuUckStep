using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimedSequenceUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Cursor UI")]
    [SerializeField] private RectTransform cursorRoot;
    [SerializeField] private Image cursorJudgementFlash;
    [SerializeField] private TimedSequenceCursorRingView cursorRingView;

    [Header("Player UI")]
    [SerializeField] private RectTransform playerRoot;
    [SerializeField] private Image playerJudgementFlash;
    [SerializeField] private TimedSequencePlayerBarView playerBarView;

    [Header("Text")]
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text phaseText;

    [Header("Performance Text")]
    [SerializeField] private TMP_Text hitsText;
    [SerializeField] private TMP_Text uniqueTargetsText;
    [SerializeField] private TMP_Text perfectShotsText;
    [SerializeField] private TMP_Text perfectHitText;
    [SerializeField] private TMP_Text rewardStateText;
    [SerializeField] private TMP_Text rewardFormulaText;
    [SerializeField] private TMP_Text rewardResultText;

    [Header("Phase Colors")]
    [SerializeField] private Color recallPhaseColor = new Color(0.55f, 0.8f, 1f, 1f);
    [SerializeField] private Color holdPhaseColor = new Color(0.85f, 0.85f, 0.95f, 1f);
    [SerializeField] private Color releasePhaseColor = new Color(0.5f, 1f, 0.6f, 1f);
    [SerializeField] private Color confirmPhaseColor = new Color(0.95f, 0.75f, 1f, 1f);
    [SerializeField] private Color reflectPhaseColor = new Color(1f, 0.92f, 0.35f, 1f);
    [SerializeField] private Color recoveryPhaseColor = new Color(1f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color orbitPhaseColor = new Color(0.3f, 1f, 1f, 1f);

    [Header("Behaviour")]
    [SerializeField] private bool hideWhenInactive = true;
    [SerializeField] private bool cursorUIOnlyForScreenAim = true;

    [Header("Flash")]
    [SerializeField] private float flashDuration = 0.08f;

    private PlayerReferences playerReferences;
    private WeaponSequenceDefinitionSO activeDefinition;
    private BoomerangSequenceDefinitionSO activeBoomerangDefinition;
    private Camera worldCamera;
    private bool visible;
    private float flashEndTime;
    private Vector3 activePlayerUIWorldOffset;

    private void Awake()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        visible = false;
        activeDefinition = null;
        activeBoomerangDefinition = null;
        playerReferences = null;
        activePlayerUIWorldOffset = Vector3.zero;

        ResetFlashVisuals();
        SetCanvasVisible(false);
    }

    private void Update()
    {
        if (flashEndTime > 0f && Time.time >= flashEndTime)
        {
            flashEndTime = 0f;
            ResetFlashVisuals();
        }
    }

    private void LateUpdate()
    {
        if (!visible || playerReferences == null)
            return;

        UpdateCursorPosition();
        UpdatePlayerBarPosition();
    }

    // ---------------------------------------------------------------------
    // SECUENCIAS ANTIGUAS / FRANCOTIRADOR
    // ---------------------------------------------------------------------

    public void Show(WeaponSequenceDefinitionSO definition, PlayerReferences references)
    {
        activeDefinition = definition;
        activeBoomerangDefinition = null;
        playerReferences = references;
        activePlayerUIWorldOffset = definition != null ? definition.PlayerUIWorldOffset : Vector3.zero;

        if (references != null && references.Aim != null)
            worldCamera = references.Aim.MainCamera;

        visible = true;
        SetCanvasVisible(true);
        ResetFlashVisuals();

        SetWindowProgress(
            normalizedTime: 0f,
            currentProgress: 0,
            requiredProgress: definition != null ? definition.RequiredSuccessfulShots : 0,
            definition: definition);

        SetPerformanceSnapshot(SequencePerformanceUISnapshot.Empty);
    }

    public void SetWindowProgress(
    float normalizedTime,
    int currentProgress,
    int requiredProgress,
    WeaponSequenceDefinitionSO definition)
    {
        normalizedTime = Mathf.Clamp01(normalizedTime);

        if (cursorRingView != null)
        {
            cursorRingView.SetDefinition(definition);
            cursorRingView.SetMarker(normalizedTime);
        }

        if (playerBarView != null)
        {
            playerBarView.SetNeutralMode(false);
            playerBarView.SetDefinition(definition);
            playerBarView.SetMarker(normalizedTime);
        }

        if (progressText != null)
            progressText.text = $"{currentProgress}/{requiredProgress}";

        if (phaseText != null)
            phaseText.text = "Sequence";
    }

    public void SetWaitingDashEnd(
    int currentProgress,
    int requiredProgress,
    WeaponSequenceDefinitionSO definition)
    {
        if (cursorRingView != null)
        {
            cursorRingView.SetDefinition(definition);
            cursorRingView.SetMarker(1f);
        }

        if (playerBarView != null)
        {
            playerBarView.SetNeutralMode(false);
            playerBarView.SetDefinition(definition);
            playerBarView.SetMarker(1f);
        }

        if (progressText != null)
            progressText.text = $"{currentProgress}/{requiredProgress}";

        if (phaseText != null)
            phaseText.text = "Dash";
    }

    // ---------------------------------------------------------------------
    // BOOMERANG
    // ---------------------------------------------------------------------

    public void ShowBoomerang(BoomerangSequenceDefinitionSO definition, PlayerReferences references)
    {
        activeDefinition = null;
        activeBoomerangDefinition = definition;
        playerReferences = references;
        activePlayerUIWorldOffset = definition != null ? definition.PlayerUIWorldOffset : Vector3.zero;

        if (references != null && references.Aim != null)
            worldCamera = references.Aim.MainCamera;

        visible = true;
        SetCanvasVisible(true);
        ResetFlashVisuals();

        SetBoomerangWindowProgress(
            normalizedTime: 0f,
            currentCycles: 0,
            requiredCycles: definition != null ? definition.RequiredSuccessfulCycles : 0,
            activeRule: definition != null ? definition.RecallRule : null,
            phaseLabel: "Recall",
            useNeutralBar: false);
    }

   

    public void SetBoomerangWindowProgress(
        float normalizedTime,
        int currentCycles,
        int requiredCycles,
        TimedSequenceActionRule activeRule,
        string phaseLabel,
        bool useNeutralBar)
    {
        normalizedTime = Mathf.Clamp01(normalizedTime);

        if (playerBarView != null)
        {
            playerBarView.SetNeutralMode(useNeutralBar);
            playerBarView.SetRule(activeRule);
            playerBarView.SetMarker(normalizedTime);
        }

        if (progressText != null)
            progressText.text = $"{currentCycles}/{requiredCycles}";

        if (phaseText != null)
            phaseText.text = phaseLabel;

        ApplyBoomerangPhaseStyle(phaseLabel);
        ApplyBoomerangBarStyle(phaseLabel, useNeutralBar);
    }


    private void ApplyBoomerangPhaseStyle(string phaseLabel)
    {
        Color color = phaseLabel switch
        {
            "Recall" => recallPhaseColor,
            "Hold" => holdPhaseColor,
            "Release" => releasePhaseColor,
            "Reflect" => reflectPhaseColor,
            "Recovery" => recoveryPhaseColor,
            "Orbit" => orbitPhaseColor,
            _ => Color.white
        };

        if (phaseText != null)
            phaseText.color = new Color(color.r, color.g, color.b, 0.85f);

        if (rewardStateText != null)
            rewardStateText.color = color;
    }

    private void ApplyBoomerangBarStyle(string phaseLabel, bool useNeutralBar)
    {
        if (playerBarView == null)
            return;

        if (useNeutralBar)
        {
            playerBarView.SetNeutralMode(true);
            playerBarView.SetMarkerColor(Color.white);
            playerBarView.SetBarTint(new Color(0.22f, 0.22f, 0.22f, 0.78f));
            playerBarView.SetDecisionLineVisible(false);
            return;
        }

        playerBarView.SetNeutralMode(false);

        Color markerColor = phaseLabel switch
        {
            "Release" => new Color(0.55f, 1f, 0.55f, 1f),
            "Reflect" => new Color(1f, 0.92f, 0.35f, 1f),
            "Recall" => new Color(0.6f, 0.85f, 1f, 1f),
            "Catch" => new Color(1f, 0.92f, 0.35f, 1f),
            _ => Color.white
        };

        Color barTint = phaseLabel switch
        {
            "Release" => new Color(0.16f, 0.28f, 0.16f, 0.78f),
            "Reflect" => new Color(0.3f, 0.24f, 0.08f, 0.82f),
            "Recall" => new Color(0.1f, 0.18f, 0.28f, 0.78f),
            "Catch" => new Color(0.24f, 0.2f, 0.08f, 0.78f),
            _ => new Color(0f, 0f, 0f, 0.55f)
        };

        playerBarView.SetMarkerColor(markerColor);
        playerBarView.SetBarTint(barTint);

        bool showDecisionLine = phaseLabel == "Release" || phaseLabel == "Reflect" || phaseLabel == "Catch";
        playerBarView.SetDecisionLineVisible(showDecisionLine);

        if (phaseLabel == "Release")
            playerBarView.SetDecisionLineColor(new Color(0.55f, 1f, 0.55f, 1f));
        else if (phaseLabel == "Reflect" || phaseLabel == "Catch")
            playerBarView.SetDecisionLineColor(new Color(1f, 0.92f, 0.35f, 1f));
        else
            playerBarView.SetDecisionLineColor(Color.white);
    }


    public void SetDecisionLineVisible(bool visible)
    {
        playerBarView?.SetDecisionLineVisible(visible);
    }

    public void SetDecisionLineColor(Color color)
    {
        playerBarView?.SetDecisionLineColor(color);
    }

    public void SetCatchPulseVisible(bool visible)
    {
        playerBarView?.SetCatchPulseVisible(visible);
    }

    public void FlashCatchCue(Color color)
    {
        if (playerJudgementFlash != null)
        {
            playerJudgementFlash.color = color;
            playerJudgementFlash.enabled = true;
        }

        playerBarView?.FlashCatchPulse(color);
        flashEndTime = Time.time + Mathf.Max(0.01f, flashDuration);
    }


    // ---------------------------------------------------------------------
    // SHOOTGUN
    // ---------------------------------------------------------------------

    public void ShowShotgun(ShotgunSequenceDefinitionSO definition, PlayerReferences references)
    {
        activeDefinition = null;
        activeBoomerangDefinition = null;
        playerReferences = references;
        activePlayerUIWorldOffset = definition != null ? definition.PlayerUIWorldOffset : Vector3.zero;

        if (references != null && references.Aim != null)
            worldCamera = references.Aim.MainCamera;

        visible = true;
        SetCanvasVisible(true);
        ResetFlashVisuals();

        SetShotgunWindowProgress(
            normalizedTime: 0f,
            currentProgress: 0,
            requiredProgress: definition != null ? definition.RequiredSuccessfulSteps : 0,
            definition: definition);

        SetPerformanceSnapshot(SequencePerformanceUISnapshot.Empty);
    }

    public void SetShotgunWindowProgress(
     float normalizedTime,
     int currentProgress,
     int requiredProgress,
     ShotgunSequenceDefinitionSO definition)
    {
        normalizedTime = Mathf.Clamp01(normalizedTime);

        if (playerBarView != null)
        {
            playerBarView.SetNeutralMode(false);
            playerBarView.SetRule(definition != null ? definition.ShootRule : null);
            playerBarView.SetMarker(normalizedTime);
        }

        if (progressText != null)
            progressText.text = $"{currentProgress}/{requiredProgress}";

        if (phaseText != null)
            phaseText.text = "Shotgun";
    }

    // ---------------------------------------------------------------------
    // COMÚN
    // ---------------------------------------------------------------------

    public void Hide()
    {
        visible = false;
        activeDefinition = null;
        activeBoomerangDefinition = null;
        playerReferences = null;
        activePlayerUIWorldOffset = Vector3.zero;
        flashEndTime = 0f;

        ResetFlashVisuals();
        SetCanvasVisible(!hideWhenInactive);
        ResetPerformanceTexts();


    }

    public void SetPerformanceSnapshot(SequencePerformanceUISnapshot snapshot)
    {
        if (hitsText != null)
            hitsText.text = FormatMetric(snapshot.metric1Label, snapshot.metric1Value);

        if (uniqueTargetsText != null)
            uniqueTargetsText.text = FormatMetric(snapshot.metric2Label, snapshot.metric2Value);

        if (perfectShotsText != null)
            perfectShotsText.text = FormatMetric(snapshot.metric3Label, snapshot.metric3Value);

        if (perfectHitText != null)
            perfectHitText.text = FormatMetric(snapshot.metric4Label, snapshot.metric4Value);

        if (rewardStateText != null)
        {
            rewardStateText.text = snapshot.rewardStateText ?? string.Empty;
            rewardStateText.color = Color.white;
        }

        if (rewardFormulaText != null)
        {
            rewardFormulaText.text = snapshot.rewardFormulaText ?? string.Empty;
            rewardFormulaText.color = new Color(0.82f, 0.82f, 0.82f, 1f);
        }

        if (rewardResultText != null)
        {
            rewardResultText.text = snapshot.rewardResultText ?? string.Empty;
            rewardResultText.color = snapshot.rewardEligible
                ? new Color(0.4f, 1f, 0.4f, 1f)
                : new Color(1f, 0.55f, 0.55f, 1f);
        }
    }

    private string FormatMetric(string label, string value)
    {
        if (string.IsNullOrWhiteSpace(label) && string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(label))
            return value ?? string.Empty;

        return $"{label}: {value}";
    }

    public void FlashJudgement(TimingJudgement judgement)
    {
        Color color = judgement switch
        {
            TimingJudgement.Perfect => new Color(1f, 0.95f, 0.2f, 0.9f),
            TimingJudgement.Good => new Color(0.9f, 1f, 0.9f, 0.85f),
            _ => new Color(1f, 0.3f, 0.3f, 0.9f)
        };

        if (cursorJudgementFlash != null)
        {
            cursorJudgementFlash.color = color;
            cursorJudgementFlash.enabled = true;
        }

        if (playerJudgementFlash != null)
        {
            playerJudgementFlash.color = color;
            playerJudgementFlash.enabled = true;
        }

        flashEndTime = Time.time + Mathf.Max(0.01f, flashDuration);
    }

    private void UpdateCursorPosition()
    {
        if (cursorRoot == null || playerReferences == null || playerReferences.Input == null)
            return;

        if (cursorUIOnlyForScreenAim && playerReferences.Input.AimScreen == Vector2.zero)
            return;

        cursorRoot.position = playerReferences.Input.AimScreen;
    }

    private void UpdatePlayerBarPosition()
    {
        if (playerRoot == null || playerReferences == null)
            return;

        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null)
            return;

        Vector3 worldPos = playerReferences.transform.position + activePlayerUIWorldOffset;
        playerRoot.position = cam.WorldToScreenPoint(worldPos);
    }

    private void ResetFlashVisuals()
    {
        if (cursorJudgementFlash != null)
            cursorJudgementFlash.enabled = false;

        if (playerJudgementFlash != null)
            playerJudgementFlash.enabled = false;
    }

    private void SetCanvasVisible(bool isVisible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }
        else if (rootCanvas != null)
        {
            rootCanvas.enabled = isVisible;
        }
    }

    private void ResetPerformanceTexts()
    {
        if (hitsText != null) hitsText.text = string.Empty;
        if (uniqueTargetsText != null) uniqueTargetsText.text = string.Empty;
        if (perfectShotsText != null) perfectShotsText.text = string.Empty;
        if (perfectHitText != null) perfectHitText.text = string.Empty;
        if (rewardStateText != null) rewardStateText.text = string.Empty;

        if (rewardFormulaText != null) rewardFormulaText.text = string.Empty;
        if (rewardResultText != null) rewardResultText.text = string.Empty;
    }
}