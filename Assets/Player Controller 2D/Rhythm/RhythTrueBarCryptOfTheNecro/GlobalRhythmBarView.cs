using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GlobalRhythmBarView : MonoBehaviour
{
    [Header("Rail")]
    [SerializeField] private RectTransform railArea;
    [SerializeField] private Image railBackground;

    [Header("Zones")]
    [SerializeField] private RectTransform goodLeftZone;
    [SerializeField] private RectTransform perfectZone;
    [SerializeField] private RectTransform goodRightZone;

    [Header("Center Core")]
    [SerializeField] private RectTransform centerRoot;
    [SerializeField] private Image centerFrameImage;
    [SerializeField] private Image centerIconImage;

    [Header("Prompt")]
    [SerializeField] private TMP_Text promptText;

    [Header("Pulse Pool")]
    [SerializeField] private RectTransform leftPulseContainer;
    [SerializeField] private RectTransform rightPulseContainer;
    [SerializeField] private GlobalRhythmPulseView pulsePrefab;

    [Header("Feedback")]
    [SerializeField] private Image feedbackFlash;
    [SerializeField] private float feedbackDuration = 0.08f;

    private readonly List<GlobalRhythmPulseView> leftPulses = new();
    private readonly List<GlobalRhythmPulseView> rightPulses = new();

    private float feedbackEndTime;

    public RectTransform GetCenterRoot() => centerRoot;

    public float RailWidth => railArea != null ? railArea.rect.width : 0f;

    private void Update()
    {
        if (feedbackFlash != null && feedbackFlash.enabled && Time.time >= feedbackEndTime)
            feedbackFlash.enabled = false;
    }

    public void EnsurePulsePool(int countPerSide)
    {
        int targetCount = Mathf.Max(1, countPerSide);

        BuildPool(leftPulseContainer, leftPulses, targetCount);
        BuildPool(rightPulseContainer, rightPulses, targetCount);
    }

    public void ApplyWindowRule(TimedSequenceActionRule rule, Color goodColor, Color perfectColor)
    {
        if (railArea == null)
            return;

        if (rule == null)
        {
            ClearZones();
            return;
        }

        float width = railArea.rect.width;

        SetZone(goodLeftZone, width, rule.GoodMin, rule.PerfectMin);
        SetZone(perfectZone, width, rule.PerfectMin, rule.PerfectMax);
        SetZone(goodRightZone, width, rule.PerfectMax, rule.GoodMax);

        SetZoneColor(goodLeftZone, goodColor);
        SetZoneColor(perfectZone, perfectColor);
        SetZoneColor(goodRightZone, goodColor);
    }

    public void ApplyVisualState(GlobalRhythmVisualState state)
    {
        if (railBackground != null)
            railBackground.color = state.railColor;

        if (centerFrameImage != null)
            centerFrameImage.color = state.centerColor;

        if (centerIconImage != null)
        {
            centerIconImage.sprite = state.centerSprite;
            centerIconImage.enabled = state.centerSprite != null;
            centerIconImage.color = state.centerColor;
        }

        if (promptText != null)
        {
            promptText.enabled = state.showPromptText;
            promptText.text = BuildPromptText(state.promptType);
            promptText.color = state.centerColor;
        }

        SetPulseColor(state.pulseColor);

        if (centerRoot != null)
            centerRoot.localScale = state.emphasizeCenter ? Vector3.one * 1.08f : Vector3.one;
    }

    public void SetPulseColor(Color color)
    {
        for (int i = 0; i < leftPulses.Count; i++)
            leftPulses[i].SetColor(color);

        for (int i = 0; i < rightPulses.Count; i++)
            rightPulses[i].SetColor(color);
    }

    public void SetPulseState(bool isLeft, int index, float normalizedX, float scale, bool visible)
    {
        List<GlobalRhythmPulseView> list = isLeft ? leftPulses : rightPulses;
        if (index < 0 || index >= list.Count || railArea == null)
            return;

        GlobalRhythmPulseView pulse = list[index];
        pulse.SetVisible(visible);

        if (!visible)
            return;

        float width = railArea.rect.width;
        float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, Mathf.Clamp01(normalizedX));

        pulse.SetAnchoredPosition(x, 0f);
        pulse.SetScale(scale);
    }

    public void HideAllPulses()
    {
        for (int i = 0; i < leftPulses.Count; i++)
            leftPulses[i].SetVisible(false);

        for (int i = 0; i < rightPulses.Count; i++)
            rightPulses[i].SetVisible(false);
    }

    public void FlashFeedback(RhythmHitQuality quality)
    {
        if (feedbackFlash == null)
            return;

        Color color = quality switch
        {
            RhythmHitQuality.Perfect => new Color(1f, 0.95f, 0.2f, 0.9f),
            RhythmHitQuality.Good => new Color(0.5f, 1f, 0.6f, 0.85f),
            _ => new Color(1f, 0.3f, 0.3f, 0.8f)
        };

        feedbackFlash.color = color;
        feedbackFlash.enabled = true;
        feedbackEndTime = Time.time + Mathf.Max(0.01f, feedbackDuration);
    }

    public void PulseCenter(float scaleMultiplier)
    {
        if (centerRoot == null)
            return;

        centerRoot.localScale = Vector3.one * Mathf.Max(1f, scaleMultiplier);
    }

    private void BuildPool(
        RectTransform container,
        List<GlobalRhythmPulseView> list,
        int targetCount)
    {
        if (container == null || pulsePrefab == null)
            return;

        while (list.Count < targetCount)
        {
            GlobalRhythmPulseView instance = Instantiate(pulsePrefab, container);
            instance.name = $"{pulsePrefab.name}_{list.Count}";
            instance.SetVisible(false);
            list.Add(instance);
        }

        for (int i = 0; i < list.Count; i++)
            list[i].SetVisible(i < targetCount);
    }

    private string BuildPromptText(GlobalRhythmPromptType promptType)
    {
        return promptType switch
        {
            GlobalRhythmPromptType.Hold => "HOLD",
            GlobalRhythmPromptType.Release => "RELEASE",
            GlobalRhythmPromptType.Tap => "TAP",
            GlobalRhythmPromptType.Reflect => "REFLECT",
            GlobalRhythmPromptType.Danger => "DANGER",
            _ => string.Empty
        };
    }

    private void ClearZones()
    {
        SetZone(goodLeftZone, 0f, 0f, 0f);
        SetZone(perfectZone, 0f, 0f, 0f);
        SetZone(goodRightZone, 0f, 0f, 0f);
    }

    private void SetZone(RectTransform zone, float totalWidth, float min, float max)
    {
        if (zone == null)
            return;

        float xMin = Mathf.Lerp(-totalWidth * 0.5f, totalWidth * 0.5f, min);
        float xMax = Mathf.Lerp(-totalWidth * 0.5f, totalWidth * 0.5f, max);
        float zoneWidth = Mathf.Max(0f, xMax - xMin);

        zone.anchorMin = new Vector2(0.5f, zone.anchorMin.y);
        zone.anchorMax = new Vector2(0.5f, zone.anchorMax.y);
        zone.pivot = new Vector2(0f, 0.5f);
        zone.anchoredPosition = new Vector2(xMin, zone.anchoredPosition.y);
        zone.sizeDelta = new Vector2(zoneWidth, zone.sizeDelta.y);
    }

    private void SetZoneColor(RectTransform zone, Color color)
    {
        if (zone == null)
            return;

        Image image = zone.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }
}