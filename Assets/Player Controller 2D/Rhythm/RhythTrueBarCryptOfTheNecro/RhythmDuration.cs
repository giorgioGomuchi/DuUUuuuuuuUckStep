using UnityEngine;

[System.Serializable]
public struct RhythmDuration
{
    [SerializeField] private RhythmTimeUnit unit;
    [SerializeField] private float value;

    public RhythmTimeUnit Unit => unit;
    public float Value => value;

    public RhythmDuration(RhythmTimeUnit unit, float value)
    {
        this.unit = unit;
        this.value = Mathf.Max(0f, value);
    }

    public float ToSeconds(RhythmClock clock)
    {
        if (clock == null)
            return 0f;

        return unit switch
        {
            RhythmTimeUnit.Beats => value * clock.SecondsPerBeat,
            _ => value
        };
    }

    public float ToBeats(RhythmClock clock)
    {
        if (clock == null || clock.SecondsPerBeat <= 0.0001f)
            return 0f;

        return unit switch
        {
            RhythmTimeUnit.Seconds => value / clock.SecondsPerBeat,
            _ => value
        };
    }

    public static RhythmDuration FromSeconds(float seconds)
    {
        return new RhythmDuration(RhythmTimeUnit.Seconds, seconds);
    }

    public static RhythmDuration FromBeats(float beats)
    {
        return new RhythmDuration(RhythmTimeUnit.Beats, beats);
    }
}