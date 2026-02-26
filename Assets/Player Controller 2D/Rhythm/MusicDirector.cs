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

    // ✅ NUEVO: rebound notes + crescendo
    [Header("Boomerang Rebound Audio")]
    [SerializeField] private AudioSource reboundSfxSource;     // OneShot notes
    [SerializeField] private AudioClip[] reboundSfxClips;      // 1..N distintos

    [SerializeField] private AudioSource reboundCrescendoSource; // loop layer
    [SerializeField] private float reboundCrescendoFadeSpeed = 10f;
    [SerializeField, Range(0f, 1f)] private float reboundCrescendoMaxVolume = 0.9f;

    [SerializeField] private int dropThreshold = 5; // debe coincidir con combat (o lo serializas igual)

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 4f;

    private MusicState currentState = MusicState.Silence;
    private MusicState pendingState = MusicState.Silence;

    private bool dropActive;
    private double dropStartDSP;
    private double dropEndDSP;

    private float groove;
    private int lastProcessedBar = -1;

    // ✅ runtime rebound intensity 0..1
    private float reboundIntensity01;

    private void Awake()
    {
        if (clock == null)
            clock = FindFirstObjectByType<RhythmClock>();

        if (combat == null)
            combat = FindFirstObjectByType<RhythmCombatController>();

        if (combat != null)
        {
            combat.onComboTriggered.AddListener(OnComboTriggered);

            // ✅ NUEVO: escuchar rebotes
            combat.onBoomerangRebound.AddListener(OnBoomerangRebound);
            combat.onBoomerangReboundReset.AddListener(OnBoomerangReboundReset);
            combat.onBoomerangDropTriggered.AddListener(OnBoomerangDropTriggered);
        }
    }

    private void Start()
    {
        ScheduleInitialLoops();
        SetAllVolumes(0f);

        if (reboundCrescendoSource != null)
        {
            reboundCrescendoSource.volume = 0f;
            if (!reboundCrescendoSource.isPlaying)
                reboundCrescendoSource.Play(); // loop “siempre”, volumen controla presencia
        }
    }

    private void Update()
    {
        if (clock == null) return;

        UpdateGrooveAndState();
        HandleVariation();
        HandleDrop();
        UpdateVolumes();
        UpdateReboundCrescendo();
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

    // ✅ Drop por rebotes
    private void OnBoomerangDropTriggered()
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
    // VARIATION SYSTEM
    // =========================================================

    private void HandleVariation()
    {
        int currentBar = clock.GetCurrentBarIndex();

        if (currentBar == lastProcessedBar)
            return;

        TryReschedule(beats, beatsClips);
        TryReschedule(shakers, shakersClips);
        TryReschedule(bass, bassClips);
    }

    private void TryReschedule(AudioSource source, AudioClip[] clips)
    {
        if (source == null || clips == null || clips.Length <= 1)
            return;

        if (Random.value > groove)
            return;

        int index = Random.Range(0, clips.Length);

        double nextBar = clock.GetNextBarDSP();

        source.Stop();
        source.clip = clips[index];
        source.PlayScheduled(nextBar);
    }

    // =========================================================
    // BOOMERANG REBOUND AUDIO
    // =========================================================

    private void OnBoomerangRebound(int streak, RhythmHitQuality quality)
    {

        Debug.Log("[MusicDirector] Rebound audio triggered");
        // 1) Nota distinta por rebote
        PlayReboundNote(streak, quality);

        // 2) Crescendo (0..1)
        reboundIntensity01 = Mathf.Clamp01((float)streak / Mathf.Max(1, dropThreshold));
    }

    private void OnBoomerangReboundReset()
    {
        reboundIntensity01 = 0f;
    }

    private void PlayReboundNote(int streak, RhythmHitQuality quality)
    {
        if (reboundSfxSource == null || reboundSfxClips == null || reboundSfxClips.Length == 0)
            return;

        // índice: 1->0, 2->1, etc (clamp)
        int idx = Mathf.Clamp(streak - 1, 0, reboundSfxClips.Length - 1);
        AudioClip clip = reboundSfxClips[idx];
        if (clip == null) return;

        // leve boost si Perfect
        float vol = (quality == RhythmHitQuality.Perfect) ? 1f : 0.8f;

        // Si quieres cuantizar al siguiente beat exacto, puedes hacerlo con PlayScheduled.
        // Como ya estás dentro de Good/Perfect, el OneShot inmediato suele sonar bien.
        reboundSfxSource.PlayOneShot(clip, vol);
    }

    private void UpdateReboundCrescendo()
    {
        if (reboundCrescendoSource == null)
            return;

        float target = reboundIntensity01 * reboundCrescendoMaxVolume;
        reboundCrescendoSource.volume = Mathf.Lerp(
            reboundCrescendoSource.volume,
            target,
            Time.deltaTime * reboundCrescendoFadeSpeed
        );
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