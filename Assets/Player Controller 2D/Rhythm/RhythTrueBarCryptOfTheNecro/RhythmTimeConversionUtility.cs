using UnityEngine;

public static class RhythmTimeConversionUtility
{
    public static float SecondsToBeats(float seconds, RhythmClock clock)
    {
        if (clock == null || clock.SecondsPerBeat <= 0.0001f)
            return 0f;

        return seconds / clock.SecondsPerBeat;
    }

    public static float BeatsToSeconds(float beats, RhythmClock clock)
    {
        if (clock == null)
            return 0f;

        return beats * clock.SecondsPerBeat;
    }

    public static float NormalizedTimeToBeatOffset(float normalizedTime, float durationInBeats)
    {
        return Mathf.Clamp01(normalizedTime) * Mathf.Max(0f, durationInBeats);
    }

    public static float BeatOffsetToNormalizedTime(float beatOffset, float durationInBeats)
    {
        if (durationInBeats <= 0.0001f)
            return 1f;

        return Mathf.Clamp01(beatOffset / durationInBeats);
    }

    public static float GetCenteredBeatOffset(float durationInBeats)
    {
        return durationInBeats * 0.5f;
    }
}