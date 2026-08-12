using UnityEngine;

public class GlobalRhythmContextResolver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GlobalRhythmBarController barController;
    [SerializeField] private RhythmCombatController rhythmCombatController;
    [SerializeField] private GlobalRhythmThemeSO theme;

    [Header("Runtime State")]
    [SerializeField] private GlobalRhythmWeaponHint currentWeaponHint = GlobalRhythmWeaponHint.Default;
    [SerializeField] private GlobalRhythmPromptType currentPromptType = GlobalRhythmPromptType.None;

    private void Awake()
    {
        if (barController == null)
            barController = FindFirstObjectByType<GlobalRhythmBarController>();

        if (rhythmCombatController == null)
            rhythmCombatController = FindFirstObjectByType<RhythmCombatController>();
    }

    private void OnEnable()
    {
        if (rhythmCombatController != null)
            rhythmCombatController.onInputEvaluated.AddListener(OnInputEvaluated);
    }

    private void OnDisable()
    {
        if (rhythmCombatController != null)
            rhythmCombatController.onInputEvaluated.RemoveListener(OnInputEvaluated);
    }

    private void Start()
    {
        RefreshVisualState();
    }

    public void SetWeaponHint(GlobalRhythmWeaponHint weaponHint)
    {
        currentWeaponHint = weaponHint;
        RefreshVisualState();
    }

    public void SetPrompt(GlobalRhythmPromptType promptType)
    {
        currentPromptType = promptType;
        RefreshVisualState();
    }

    public void ClearPrompt()
    {
        currentPromptType = GlobalRhythmPromptType.None;
        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        if (barController == null || theme == null)
            return;

        GlobalRhythmVisualState state = theme.GetWeaponState(currentWeaponHint);

        if (currentPromptType != GlobalRhythmPromptType.None &&
            currentPromptType != GlobalRhythmPromptType.Neutral)
        {
            GlobalRhythmVisualState promptState = theme.GetPromptState(currentPromptType);

            // Mantener sprite de arma si el prompt no trae uno.
            if (promptState.centerSprite == null)
                promptState.centerSprite = state.centerSprite;

            barController.SetVisualState(promptState);
            return;
        }

        barController.SetVisualState(state);
    }

    private void OnInputEvaluated(RhythmInputResult result)
    {
        if (barController == null)
            return;

        barController.ShowInputFeedback(result.quality);
    }

    public GlobalRhythmBarController GetBarController()
    {
        return barController;
    }

    public void ShowJudgementInfo(string label, TimingJudgement judgement)
    {
        barController?.ShowJudgementInfo(label, judgement);
    }

    public void SetWindowRule(TimedSequenceActionRule rule)
    {
        barController?.SetWindowRule(rule);
    }

    public void SetPromptTextOverride(string text)
    {
        barController?.SetPromptTextOverride(text);
    }
}