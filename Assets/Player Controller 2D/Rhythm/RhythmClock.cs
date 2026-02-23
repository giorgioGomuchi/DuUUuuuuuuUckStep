using UnityEngine;

public class RhythmClock : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioSource musicSource;

    [Header("Tempo")]
    [SerializeField] private float bpm = 145f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private double startDspTime;
    private bool started;

    public float BPM => bpm;
    public float SecondsPerBeat => 60f / Mathf.Max(1f, bpm);

    private void Start()
    {
        StartClock();
    }

    private void Awake()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();
    }

    public void StartClock()
    {
        startDspTime = AudioSettings.dspTime;
        started = true;

        if (musicSource != null && !musicSource.isPlaying)
            musicSource.Play();

        if (debugLogs)
            Debug.Log($"[RhythmClock] Started at dsp={startDspTime:0.000}");
    }

    public float GetBeatPhase01()
    {
        if (!started) return 0f;

        double elapsed = AudioSettings.dspTime - startDspTime;
        double spb = SecondsPerBeat;

        if (spb <= 0.0001) return 0f;

        double phase = (elapsed % spb) / spb; // 0..1
        return (float)phase;
    }

    public float GetDistanceToNearestBeatSeconds()
    {
        if (!started) return float.MaxValue;

        float phase = GetBeatPhase01(); // 0..1
        float distPhase = Mathf.Min(phase, 1f - phase); // nearest edge (beat)
        return distPhase * SecondsPerBeat;
    }
}

/*
Unity setup:
- Put this on a GameObject like "RhythmSystem".
- Assign an AudioSource with your music.
- Call StartClock() once (e.g., on scene start).
*/