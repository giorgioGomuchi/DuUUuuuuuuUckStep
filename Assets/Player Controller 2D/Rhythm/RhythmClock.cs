using UnityEngine;

public class RhythmClock : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioSource musicSource;

    [Header("Tempo")]
    [SerializeField] private float bpm = 145f;
    [SerializeField] private int beatsPerBar = 4;

    [Header("Metronome")]
    [SerializeField] private bool metronomeEnabled = false;
    [SerializeField] private bool accentFirstBeatOfBar = true;

    [SerializeField] private AudioSource metronomeSource;
    [SerializeField] private AudioClip[] metronomeClips;
    [SerializeField] private AudioClip[] accentMetronomeClips;

    [Min(1)]
    [SerializeField] private int metronomeSubdivision = 1;

    [Range(0f, 1f)]
    [SerializeField] private float metronomeVolume = 0.35f;

    [SerializeField] private double metronomeScheduleLeadTime = 0.1f;

    private double nextMetronomeDspTime;
    private int metronomeStepIndex;
    private AudioClip lastScheduledMetronomeClip;

    private double startDspTime;
    private bool started;

    public float BPM => bpm;
    public float SecondsPerBeat => 60f / bpm;
    public float SecondsPerBar => SecondsPerBeat * beatsPerBar;

    private void Start()
    {
        StartClock();
    }

    private void Update()
    {
        UpdateMetronome();
    }

    public void StartClock()
    {
        startDspTime = AudioSettings.dspTime;
        started = true;
        InitializeMetronome();

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

    private void InitializeMetronome()
    {
        if (!metronomeEnabled || metronomeSource == null || !HasAnyMetronomeClip())
            return;

        metronomeSource.playOnAwake = false;
        metronomeSource.loop = false;
        metronomeSource.volume = metronomeVolume;

        metronomeStepIndex = 0;
        nextMetronomeDspTime = startDspTime;
    }

    private void UpdateMetronome()
    {
        if (!started || !metronomeEnabled || metronomeSource == null || !HasAnyMetronomeClip())
            return;

        double dspNow = AudioSettings.dspTime;

        while (dspNow + metronomeScheduleLeadTime >= nextMetronomeDspTime)
        {
            AudioClip clipToPlay = PickMetronomeClipForCurrentStep();
            if (clipToPlay != null)
            {
                metronomeSource.clip = clipToPlay;
                metronomeSource.volume = metronomeVolume;
                metronomeSource.PlayScheduled(nextMetronomeDspTime);
                lastScheduledMetronomeClip = clipToPlay;
            }

            nextMetronomeDspTime += GetMetronomeStepSeconds();
            metronomeStepIndex++;
        }
    }

    private bool HasAnyMetronomeClip()
    {
        return (metronomeClips != null && metronomeClips.Length > 0) ||
               (accentMetronomeClips != null && accentMetronomeClips.Length > 0);
    }

    private double GetMetronomeStepSeconds()
    {
        int subdivision = Mathf.Max(1, metronomeSubdivision);
        return SecondsPerBeat / subdivision;
    }

    private AudioClip PickMetronomeClipForCurrentStep()
    {
        bool isMainBeat = (metronomeStepIndex % Mathf.Max(1, metronomeSubdivision)) == 0;
        bool isFirstBeatOfBar = (metronomeStepIndex % Mathf.Max(1, beatsPerBar * metronomeSubdivision)) == 0;

        if (accentFirstBeatOfBar && isMainBeat && isFirstBeatOfBar)
        {
            AudioClip accentClip = PickRandomClip(accentMetronomeClips, avoidLastClip: false);
            if (accentClip != null)
                return accentClip;
        }

        return PickRandomClip(metronomeClips, avoidLastClip: true);
    }

    private AudioClip PickRandomClip(AudioClip[] clips, bool avoidLastClip)
    {
        if (clips == null || clips.Length == 0)
            return null;

        if (clips.Length == 1)
            return clips[0];

        int index = Random.Range(0, clips.Length);

        if (avoidLastClip && clips[index] == lastScheduledMetronomeClip)
            index = (index + 1) % clips.Length;

        return clips[index];
    }
}