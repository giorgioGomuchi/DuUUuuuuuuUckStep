using UnityEngine;

public class GlobalRhythmBarController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GlobalRhythmJudge judge;
    [SerializeField] private GlobalRhythmBarView view;
    [SerializeField] private GlobalRhythmWindowProfileSO profile;

    [Header("Visual State")]
    [SerializeField] private GlobalRhythmVisualState defaultVisualState = default;

    [Header("Beat Pulse")]
    [SerializeField] private float centerBeatScale = 1.08f;
    [SerializeField] private float centerReturnSpeed = 10f;

    [Header("Pulse Motion")]
    [SerializeField] private float nearCenterScale = 1.2f;
    [SerializeField] private float farScale = 0.7f;
    [SerializeField] private bool hidePulsesOutsideRail = true;

    private float lastBeatPhase;
    private bool initialized;

    private void Awake()
    {
        if (judge == null)
            judge = FindFirstObjectByType<GlobalRhythmJudge>();

        if (defaultVisualState.centerColor == default)
            defaultVisualState = GlobalRhythmVisualState.Default;
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (!initialized || judge == null || view == null || profile == null)
            return;

        UpdatePulseWaves();
        UpdateBeatFeedback();
    }

    public void Initialize()
    {
        if (judge == null || view == null || profile == null || !profile.IsValid())
            return;

        view.ApplyWindowRule(
            profile.Rule,
            defaultVisualState.goodColor,
            defaultVisualState.perfectColor);

        view.ApplyVisualState(defaultVisualState);
        view.EnsurePulsePool(profile.VisiblePulsePairs);

        initialized = true;
    }

    public void SetVisualState(GlobalRhythmVisualState state)
    {
        defaultVisualState = state;

        if (view == null)
            return;

        view.ApplyVisualState(defaultVisualState);
        view.ApplyWindowRule(
            profile != null ? profile.Rule : null,
            defaultVisualState.goodColor,
            defaultVisualState.perfectColor);
    }

    public void ShowInputFeedback(RhythmHitQuality quality)
    {
        view?.FlashFeedback(quality);
    }

    private void UpdatePulseWaves()
    {
        float phase = judge.GetBeatPhase01();
        int count = Mathf.Max(1, profile.VisiblePulsePairs);
        float travelBeats = Mathf.Max(0.25f, profile.TravelBeats);

        for (int i = 0; i < count; i++)
        {
            float beatOffset = i + phase;
            float t = beatOffset / travelBeats;

            bool visible = !hidePulsesOutsideRail || t <= 1f;

            float leftX = Mathf.Lerp(0f, 0.5f, Mathf.Clamp01(t));
            float rightX = Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(t));

            float scale = Mathf.Lerp(nearCenterScale, farScale, Mathf.Clamp01(t));

            view.SetPulseState(true, i, leftX, scale, visible);
            view.SetPulseState(false, i, rightX, scale, visible);
        }
    }

    private void UpdateBeatFeedback()
    {
        float phase = judge.GetBeatPhase01();

        if (phase < lastBeatPhase)
            view.PulseCenter(centerBeatScale);

        lastBeatPhase = phase;

        RectTransform centerRoot = view != null ? view.GetCenterRoot() : null;
        if (centerRoot != null)
        {
            centerRoot.localScale = Vector3.Lerp(
                centerRoot.localScale,
                Vector3.one,
                Time.deltaTime * centerReturnSpeed);
        }
    }
}