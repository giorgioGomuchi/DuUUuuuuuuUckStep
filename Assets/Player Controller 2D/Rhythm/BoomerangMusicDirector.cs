using UnityEngine;

public class BoomerangMusicDirector : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RhythmClock clock;

    [Header("Timing Windows")]
    [SerializeField] private float goodWindow = 0.1f;
    [SerializeField] private float perfectWindow = 0.05f;

    [Header("Rebound Settings")]
    [SerializeField] private int dropThreshold = 5;
    [SerializeField] private bool perfectCountsAsTwo = true;

    [Header("Audio")]
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioClip[] reboundClips;

    [SerializeField] private AudioSource crescendoLoop;
    [SerializeField] private float crescendoMaxVolume = 0.8f;
    [SerializeField] private float crescendoFadeSpeed = 8f;

    [SerializeField] private AudioSource dropSource;

    private int streak;
    private float crescendoTarget;

    private void Awake()
    {
        if (clock == null)
            clock = FindFirstObjectByType<RhythmClock>();

        if (crescendoLoop != null && !crescendoLoop.isPlaying)
        {
            crescendoLoop.volume = 0f;
            crescendoLoop.loop = true;
            crescendoLoop.Play();
        }
    }

    private void Update()
    {
        if (crescendoLoop != null)
        {
            crescendoLoop.volume = Mathf.Lerp(
                crescendoLoop.volume,
                crescendoTarget,
                Time.deltaTime * crescendoFadeSpeed
            );
        }
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    public bool TryRegisterRebound()
    {
        if (clock == null) return false;

        float dist = clock.GetDistanceToNearestBeatSeconds();

        bool perfect = dist <= perfectWindow;
        bool good = dist <= goodWindow;

        if (!perfect && !good)
        {
            ResetStreak();
            return false;
        }

        int add = 1;
        if (perfect && perfectCountsAsTwo)
            add = 2;

        streak += add;

        PlayReboundSound(perfect);

        UpdateCrescendo();

        if (streak >= dropThreshold)
        {
            TriggerDrop();
            ResetStreak();
        }

        return true;
    }

    private void PlayReboundSound(bool perfect)
    {
        if (oneShotSource == null || reboundClips == null || reboundClips.Length == 0)
            return;

        int index = Mathf.Clamp(streak - 1, 0, reboundClips.Length - 1);
        AudioClip clip = reboundClips[index];

        float volume = perfect ? 1f : 0.8f;

        oneShotSource.PlayOneShot(clip, volume);
    }

    private void UpdateCrescendo()
    {
        float t = Mathf.Clamp01((float)streak / dropThreshold);
        crescendoTarget = t * crescendoMaxVolume;
    }

    private void TriggerDrop()
    {
        if (dropSource == null || clock == null)
            return;

        double nextBar = clock.GetNextBarDSP();
        dropSource.PlayScheduled(nextBar);
    }

    private void ResetStreak()
    {
        streak = 0;
        crescendoTarget = 0f;
    }
}