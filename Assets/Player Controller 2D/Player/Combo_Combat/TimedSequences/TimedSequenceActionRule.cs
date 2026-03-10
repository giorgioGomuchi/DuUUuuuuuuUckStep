using UnityEngine;

[System.Serializable]
public class TimedSequenceActionRule
{
    [SerializeField] private bool enabled = true;

    [Header("Windows")]
    [Range(0.01f, 0.49f)]
    [SerializeField] private float goodHalfWindowNormalized = 0.18f;

    [Range(0.01f, 0.49f)]
    [SerializeField] private float perfectHalfWindowNormalized = 0.06f;

    [SerializeField] private bool allowPerfect = true;

    public bool Enabled => enabled;
    public float GoodHalfWindowNormalized => goodHalfWindowNormalized;
    public float PerfectHalfWindowNormalized => perfectHalfWindowNormalized;
    public bool AllowPerfect => allowPerfect;

    public TimingJudgement Evaluate(float normalizedTime)
    {
        if (!enabled)
            return TimingJudgement.Fail;

        float distanceToCenter = Mathf.Abs(normalizedTime - 0.5f);

        if (allowPerfect && distanceToCenter <= perfectHalfWindowNormalized)
            return TimingJudgement.Perfect;

        if (distanceToCenter <= goodHalfWindowNormalized)
            return TimingJudgement.Good;

        return TimingJudgement.Fail;
    }
}