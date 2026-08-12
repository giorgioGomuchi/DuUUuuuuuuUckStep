using UnityEngine;

public enum GlobalRhythmPromptType
{
    None = 0,
    Neutral = 1,
    Hold = 2,
    Release = 3,
    Tap = 4,
    Reflect = 5,
    Danger = 6
}

[System.Serializable]
public struct GlobalRhythmVisualState
{
    public Sprite centerSprite;
    public Color centerColor;
    public Color railColor;
    public Color pulseFillColor;
    public Color pulseOutlineColor;
    public Color goodColor;
    public Color perfectColor;
    public GlobalRhythmPromptType promptType;
    public bool emphasizeCenter;
    public bool showPromptText;

    public static GlobalRhythmVisualState Default => new GlobalRhythmVisualState
    {
        centerSprite = null,
        centerColor = Color.white,
        railColor = new Color(0f, 0f, 0f, 0.55f),
        pulseFillColor = new Color(0.85f, 0.85f, 0.95f, 0.9f),
        pulseOutlineColor = Color.black,
        goodColor = new Color(0.5f, 1f, 0.6f, 0.95f),
        perfectColor = new Color(1f, 0.95f, 0.2f, 1f),
        promptType = GlobalRhythmPromptType.Neutral,
        emphasizeCenter = false,
        showPromptText = false
    };
}