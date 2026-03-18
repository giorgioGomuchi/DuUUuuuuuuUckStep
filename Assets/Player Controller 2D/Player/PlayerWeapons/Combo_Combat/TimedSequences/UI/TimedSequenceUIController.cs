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
        SetWindowProgress(0f, 0, definition != null ? definition.RequiredSuccessfulShots : 0, definition);
    }

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
            phaseLabel: "Recall");
    }

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
    }

    public void SetWindowProgress(float normalizedTime, int currentShots, int requiredShots, WeaponSequenceDefinitionSO definition)
    {
        normalizedTime = Mathf.Clamp01(normalizedTime);

        if (cursorRingView != null)
        {
            cursorRingView.SetDefinition(definition);
            cursorRingView.SetMarker(normalizedTime);
        }

        if (playerBarView != null)
        {
            playerBarView.SetDefinition(definition);
            playerBarView.SetMarker(normalizedTime);
        }

        if (progressText != null)
            progressText.text = $"{currentShots}/{requiredShots}";

        if (phaseText != null)
            phaseText.text = "Sequence";
    }

    public void SetBoomerangWindowProgress(
        float normalizedTime,
        int currentCycles,
        int requiredCycles,
        TimedSequenceActionRule activeRule,
        string phaseLabel)
    {
        normalizedTime = Mathf.Clamp01(normalizedTime);

        if (playerBarView != null)
        {
            playerBarView.SetRule(activeRule);
            playerBarView.SetMarker(normalizedTime);
        }

        if (progressText != null)
            progressText.text = $"{currentCycles}/{requiredCycles}";

        if (phaseText != null)
            phaseText.text = phaseLabel;
    }

    public void SetWaitingDashEnd(int currentShots, int requiredShots, WeaponSequenceDefinitionSO definition)
    {
        if (cursorRingView != null)
        {
            cursorRingView.SetDefinition(definition);
            cursorRingView.SetMarker(1f);
        }

        if (playerBarView != null)
        {
            playerBarView.SetDefinition(definition);
            playerBarView.SetMarker(1f);
        }

        if (progressText != null)
            progressText.text = $"{currentShots}/{requiredShots}";

        if (phaseText != null)
            phaseText.text = "Dash";
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
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        playerRoot.position = screenPos;
    }

    private void ResetFlashVisuals()
    {
        if (cursorJudgementFlash != null)
        {
            cursorJudgementFlash.enabled = false;
            Color c = cursorJudgementFlash.color;
            c.a = 0f;
            cursorJudgementFlash.color = c;
        }

        if (playerJudgementFlash != null)
        {
            playerJudgementFlash.enabled = false;
            Color c = playerJudgementFlash.color;
            c.a = 0f;
            playerJudgementFlash.color = c;
        }
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
}