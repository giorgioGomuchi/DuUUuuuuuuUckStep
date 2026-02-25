using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BeatRingUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RhythmClock rhythmClock;
    [SerializeField] private RhythmCombatController rhythmCombat;
    [SerializeField] private MusicDirector musicDirector;

    [Header("Ring")]
    [SerializeField] private Image ringImage;
    [SerializeField] private RectTransform shrinkingIndicator;

    [Header("Hit Pulse")]
    [SerializeField] private Image hitPulseImage;
    [SerializeField] private float pulseDuration = 0.25f;
    [SerializeField] private float maxPulseScale = 1.8f;

    [Header("Text")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float textDuration = 0.5f;

    [Header("Colors")]
    [SerializeField] private Color perfectColor = new Color(1f, 0.9f, 0.2f);
    [SerializeField] private Color goodColor = Color.white;
    [SerializeField] private Color failColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private Color comboColor = Color.cyan;
    [SerializeField] private Color baseRingColor = Color.white;

    private Color currentTargetColor;

    [Header("BuildUp Visual")]
    [SerializeField] private Color buildUpColor = Color.magenta;
    [SerializeField] private float buildUpPulseMultiplier = 2f;

    private float pulseTimer;
    private float textTimer;

    private void Awake()
    {
        if (rhythmClock == null)
            rhythmClock = FindFirstObjectByType<RhythmClock>();

        if (rhythmCombat == null)
            rhythmCombat = FindFirstObjectByType<RhythmCombatController>();

        if (hitPulseImage != null)
            hitPulseImage.color = Color.clear;
        currentTargetColor = baseRingColor;

        if (ringImage != null)
            ringImage.color = baseRingColor;
    }

    private void OnEnable()
    {
        rhythmCombat.onInputEvaluated.AddListener(OnInput);
        rhythmCombat.onComboTriggered.AddListener(OnCombo);
    }

    private void OnDisable()
    {
        rhythmCombat.onInputEvaluated.RemoveListener(OnInput);
        rhythmCombat.onComboTriggered.RemoveListener(OnCombo);
    }

    private void Update()
    {
        UpdateRing();
        UpdatePulse();
        UpdateText();
    }

    private void UpdateRing()
    {
        if (rhythmClock == null) return;

        float phase = rhythmClock.GetBeatPhase01();

        //bool isBuildUp = musicDirector != null && musicDirector.IsBuildUpActive();

        // Determinar color objetivo
        //currentTargetColor = isBuildUp ? buildUpColor : baseRingColor;

        // Interpolación suave
        if (ringImage != null)
        {
            ringImage.color = Color.Lerp(
                ringImage.color,
                currentTargetColor,
                Time.deltaTime * 6f
            );

            ringImage.fillAmount = phase;
        }

        if (shrinkingIndicator != null)
        {
            float scale = Mathf.Lerp(1f, 0.3f, phase);
            shrinkingIndicator.localScale = Vector3.one * scale;
        }
    }

    private void UpdatePulse()
    {
        if (pulseTimer > 0f)
        {
            pulseTimer -= Time.deltaTime;

            float t = 1f - (pulseTimer / pulseDuration);

            float scale = Mathf.Lerp(0.5f, maxPulseScale, t);
            hitPulseImage.rectTransform.localScale = Vector3.one * scale;

            Color c = hitPulseImage.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            hitPulseImage.color = c;
        }
    }

    private void UpdateText()
    {
        if (textTimer > 0f)
        {
            textTimer -= Time.deltaTime;
        }
        else if (feedbackText != null)
        {
            feedbackText.text = "";
        }
    }

    private void OnInput(RhythmInputResult result)
    {
        Color c = result.quality switch
        {
            RhythmHitQuality.Perfect => perfectColor,
            RhythmHitQuality.Good => goodColor,
            _ => failColor
        };

        TriggerPulse(c);

        if (feedbackText != null)
        {
            string timing = "";

            if (result.quality != RhythmHitQuality.Fail)
                timing = result.wasEarly ? "EARLY" : "LATE";
            else
                timing = result.wasEarly ? "EARLY" : "LATE";

            feedbackText.text =
                $"{result.action.ToString().ToUpper()} - " +
                $"{result.quality.ToString().ToUpper()} " +
                $"({timing})";

            feedbackText.color = c;

            feedbackText.fontSize =
                result.quality == RhythmHitQuality.Perfect ? 42 :
                result.quality == RhythmHitQuality.Good ? 34 : 28;
        }

        textTimer = textDuration;
    }

    private void OnCombo(RhythmComboRecipeSO recipe)
    {
        if (feedbackText == null || recipe == null)
            return;

        feedbackText.text = $"COMBO: {recipe.RecipeId}";
        feedbackText.color = comboColor;
        feedbackText.fontSize = 48;

        textTimer = 0.8f;

        TriggerPulse(comboColor);
    }

    private void TriggerPulse(Color c)
    {
        if (hitPulseImage == null)
            return;

        hitPulseImage.color = c;
        hitPulseImage.rectTransform.localScale = Vector3.one * 0.5f;
        pulseTimer = pulseDuration;
    }
}