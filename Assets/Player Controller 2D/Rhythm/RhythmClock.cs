using UnityEngine;

public class RhythmClock : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioSource musicSource;

    [Header("Tempo")]
    [SerializeField] private float bpm = 145f;
    [SerializeField] private int beatsPerBar = 4;

    private double startDspTime;
    private bool started;

    public float BPM => bpm;
    public float SecondsPerBeat => 60f / bpm;
    public float SecondsPerBar => SecondsPerBeat * beatsPerBar;

    private void Start()
    {
        StartClock();
    }

    public void StartClock()
    {
        startDspTime = AudioSettings.dspTime;
        started = true;

        if (musicSource != null && !musicSource.isPlaying)
            musicSource.Play();
    }

    public float GetBeatPhase01()
    {
        if (!started) return 0f;

        double elapsed = AudioSettings.dspTime - startDspTime;
        double phase = (elapsed % SecondsPerBeat) / SecondsPerBeat;
        return (float)phase;
    }

    public float GetDistanceToNearestBeatSeconds()
    {
        float phase = GetBeatPhase01();
        float dist = Mathf.Min(phase, 1f - phase);
        return dist * SecondsPerBeat;
    }

    public bool IsBarStart()
    {
        if (!started) return false;

        double elapsed = AudioSettings.dspTime - startDspTime;
        double barTime = elapsed % SecondsPerBar;

        return barTime < 0.02; // ventana pequeña
    }

    public double GetSongElapsedDSP()
    {
        if (!started) return 0;
        return AudioSettings.dspTime - startDspTime;
    }

    public int GetCurrentBarIndex()
    {
        return Mathf.FloorToInt((float)(GetSongElapsedDSP() / SecondsPerBar));
    }

    public double GetNextBarDSP()
    {
        double elapsed = GetSongElapsedDSP();
        double bars = System.Math.Ceiling(elapsed / SecondsPerBar);
        return startDspTime + (bars * SecondsPerBar);
    }

    public double GetNextSubdivisionDSP(int division)
    {
        double subdivisionLength = SecondsPerBeat / (division / 4.0);
        double elapsed = GetSongElapsedDSP();
        double next = System.Math.Ceiling(elapsed / subdivisionLength);
        return startDspTime + (next * subdivisionLength);
    }
}