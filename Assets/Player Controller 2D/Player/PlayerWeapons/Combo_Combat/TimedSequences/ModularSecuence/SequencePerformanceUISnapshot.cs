using System;

[Serializable]
public struct SequencePerformanceUISnapshot
{
    public int currentProgress;
    public int requiredProgress;

    public string metric1Label;
    public string metric1Value;

    public string metric2Label;
    public string metric2Value;

    public string metric3Label;
    public string metric3Value;

    public string metric4Label;
    public string metric4Value;

    public string rewardLabel;
    public bool rewardEligible;

    public string rewardStateText;
    public string rewardFormulaText;
    public string rewardResultText;

    public static SequencePerformanceUISnapshot Empty => new()
    {
        currentProgress = 0,
        requiredProgress = 0,

        metric1Label = string.Empty,
        metric1Value = string.Empty,

        metric2Label = string.Empty,
        metric2Value = string.Empty,

        metric3Label = string.Empty,
        metric3Value = string.Empty,

        metric4Label = string.Empty,
        metric4Value = string.Empty,

        rewardLabel = "Reward",
        rewardEligible = false,

        rewardStateText = string.Empty,
        rewardFormulaText = string.Empty,
        rewardResultText = string.Empty
    };
}