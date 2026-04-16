using UnityEngine;

public class GlobalRhythmJudge : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RhythmClock rhythmClock;
    [SerializeField] private GlobalRhythmWindowProfileSO defaultProfile;

    public RhythmClock Clock => rhythmClock;
    public GlobalRhythmWindowProfileSO DefaultProfile => defaultProfile;

    private void Awake()
    {
        if (rhythmClock == null)
            rhythmClock = FindFirstObjectByType<RhythmClock>();
    }

    public RhythmHitQuality EvaluateNow()
    {
        return EvaluateNow(defaultProfile);
    }

    public RhythmHitQuality EvaluateNow(GlobalRhythmWindowProfileSO profile)
    {
        if (rhythmClock == null || profile == null || !profile.IsValid())
            return RhythmHitQuality.Fail;

        float normalized = GetNormalizedDistanceToCenter(profile);
        TimingJudgement judgement = profile.Rule.Evaluate(normalized);

        return judgement switch
        {
            TimingJudgement.Perfect => RhythmHitQuality.Perfect,
            TimingJudgement.Good => RhythmHitQuality.Good,
            _ => RhythmHitQuality.Fail
        };
    }

    public float GetBeatPhase01()
    {
        if (rhythmClock == null)
            return 0f;

        return rhythmClock.GetBeatPhase01();
    }

    public float GetDistanceToNearestBeatSeconds()
    {
        if (rhythmClock == null)
            return 0f;

        return rhythmClock.GetDistanceToNearestBeatSeconds();
    }

    public float GetNormalizedDistanceToCenter(GlobalRhythmWindowProfileSO profile)
    {
        if (rhythmClock == null || profile == null || profile.Rule == null)
            return 0f;

        float phase = rhythmClock.GetBeatPhase01();

        // Mapeamos la cercanía al beat a una “barra centrada en 0.5”.
        // Beat exacto => 0.5
        // Más lejos del beat => más hacia 0 o 1
        float distance01 = Mathf.Min(phase, 1f - phase) / 0.5f;
        float centered = 0.5f + ((phase <= 0.5f ? -1f : 1f) * distance01 * 0.5f);

        return Mathf.Clamp01(centered);
    }

    public float GetSecondsPerBeat()
    {
        return rhythmClock != null ? rhythmClock.SecondsPerBeat : 0f;
    }

    public float GetBpm()
    {
        return rhythmClock != null ? rhythmClock.BPM : 0f;
    }
}