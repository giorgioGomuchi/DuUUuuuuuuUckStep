using UnityEngine;

public class MusicDirector : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RhythmClock clock;
    [SerializeField] private RhythmCombatController combat;

    [Header("Layers")]
    [SerializeField] private AudioSource beats;
    [SerializeField] private AudioClip[] beatsClips;

    [SerializeField] private AudioSource shakers;
    [SerializeField] private AudioClip[] shakersClips;

    [SerializeField] private AudioSource bass;
    [SerializeField] private AudioClip[] bassClips;

    [SerializeField] private AudioSource synth;
    [SerializeField] private AudioClip[] synthClips;

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 4f;

    private MusicState currentState = MusicState.Silence;
    private MusicState pendingState = MusicState.Silence;

    private bool dropActive;
    private double dropStartDSP;
    private double dropEndDSP;

    private float groove;
    private int lastProcessedBar = -1;

    // =========================================================
    // INIT
    // =========================================================

    private void Awake()
    {
        if (clock == null)
            clock = FindFirstObjectByType<RhythmClock>();

        if (combat == null)
            combat = FindFirstObjectByType<RhythmCombatController>();

        if (combat != null)
            combat.onComboTriggered.AddListener(OnComboTriggered);
    }

    private void Start()
    {
        ScheduleInitialLoops();
        SetAllVolumes(0f);
    }

    private void Update()
    {
        if (clock == null) return;

        UpdateGrooveAndState();
        HandleVariation();       // 👈 AÑADIDO
        HandleDrop();
        UpdateVolumes();
    }

    // =========================================================
    // GROOVE + STATE
    // =========================================================

    private void UpdateGrooveAndState()
    {
        if (combat == null) return;

        float target = combat.GetRecentAccuracy01();
        groove = Mathf.Lerp(groove, target, Time.deltaTime * 3f);

        ResolveMusicState();

        int bar = clock.GetCurrentBarIndex();
        if (bar != lastProcessedBar)
        {
            lastProcessedBar = bar;
            currentState = pendingState;
        }
    }

    private void ResolveMusicState()
    {
        if (groove < 0.08f)
            pendingState = MusicState.Silence;
        else if (groove < 0.25f)
            pendingState = MusicState.Pulse;
        else if (groove < 0.55f)
            pendingState = MusicState.Groove;
        else
            pendingState = MusicState.Drive;
    }

    // =========================================================
    // DROP
    // =========================================================

    private void OnComboTriggered(RhythmComboRecipeSO recipe)
    {
        TriggerDrop();
    }

    public void TriggerDrop()
    {
        if (clock == null) return;

        double nextBar = clock.GetNextBarDSP();

        dropActive = true;
        dropStartDSP = nextBar;
        dropEndDSP = nextBar + (clock.SecondsPerBar * 2);
    }

    private void HandleDrop()
    {
        if (!dropActive) return;

        if (AudioSettings.dspTime >= dropEndDSP)
            dropActive = false;
    }

    // =========================================================
    // VARIATION SYSTEM (ARREGLO DEFINITIVO)
    // =========================================================

    private void HandleVariation()
    {
        int currentBar = clock.GetCurrentBarIndex();

        if (currentBar == lastProcessedBar)
            return;

        // Cambio de compás detectado
        TryReschedule(beats, beatsClips);
        TryReschedule(shakers, shakersClips);
        TryReschedule(bass, bassClips);
    }

    private void TryReschedule(AudioSource source, AudioClip[] clips)
    {
        if (source == null || clips == null || clips.Length <= 1)
            return;

        // Probabilidad depende del groove
        if (Random.value > groove)
            return;

        int index = Random.Range(0, clips.Length);

        double nextBar = clock.GetNextBarDSP();

        source.Stop();
        source.clip = clips[index];
        source.PlayScheduled(nextBar);
    }

    // =========================================================
    // AUDIO CORE
    // =========================================================

    private void ScheduleInitialLoops()
    {
        double dsp = AudioSettings.dspTime + 0.2;

        if (beatsClips.Length > 0) beats.clip = beatsClips[0];
        if (shakersClips.Length > 0) shakers.clip = shakersClips[0];
        if (bassClips.Length > 0) bass.clip = bassClips[0];
        if (synthClips.Length > 0) synth.clip = synthClips[0];

        beats.PlayScheduled(dsp);
        shakers.PlayScheduled(dsp);
        bass.PlayScheduled(dsp);
        synth.PlayScheduled(dsp);
    }

    private void UpdateVolumes()
    {
        float beatsTarget = 0;
        float shakersTarget = 0;
        float bassTarget = 0;
        float synthTarget = 0;

        switch (currentState)
        {
            case MusicState.Pulse:
                beatsTarget = 1;
                break;
            case MusicState.Groove:
                beatsTarget = 1;
                shakersTarget = 1;
                break;
            case MusicState.Drive:
                beatsTarget = 1;
                shakersTarget = 1;
                bassTarget = 1;
                break;
        }

        if (dropActive && AudioSettings.dspTime >= dropStartDSP)
            synthTarget = 1f;

        beats.volume = Mathf.Lerp(beats.volume, beatsTarget, Time.deltaTime * fadeSpeed);
        shakers.volume = Mathf.Lerp(shakers.volume, shakersTarget, Time.deltaTime * fadeSpeed);
        bass.volume = Mathf.Lerp(bass.volume, bassTarget, Time.deltaTime * fadeSpeed);
        synth.volume = Mathf.Lerp(synth.volume, synthTarget, Time.deltaTime * fadeSpeed);
    }

    private void SetAllVolumes(float v)
    {
        beats.volume = v;
        shakers.volume = v;
        bass.volume = v;
        synth.volume = v;
    }
}