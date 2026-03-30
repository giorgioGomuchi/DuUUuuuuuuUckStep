using System;

[Serializable]
public struct SequenceRewardPreviewInfo
{
    public string stateText;
    public string formulaText;
    public string resultText;

    public static SequenceRewardPreviewInfo Empty => new()
    {
        stateText = string.Empty,
        formulaText = string.Empty,
        resultText = string.Empty
    };
}