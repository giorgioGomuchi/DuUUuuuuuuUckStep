using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BeatRingUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RhythmClock rhythmClock;
    [SerializeField] private RhythmCombatController rhythmCombat;

    [Header("UI")]
    [SerializeField] private Image ringImage;                 // Filled Radial 360
    [SerializeField] private RectTransform shrinkingIndicator; // scale pulse

    [Header("Shrink Settings")]
    [SerializeField] private float maxScale = 1.0f;
    [SerializeField] private float minScale = 0.25f;

    [Header("Feedback")]
    [SerializeField] private Color goodColor = Color.white;
    [SerializeField] private Color perfectColor = Color.yellow;
    [SerializeField] private Color failColor = Color.red;
    [SerializeField] private float flashSeconds = 0.12f;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip goodClip;
    [SerializeField] private AudioClip perfectClip;
    [SerializeField] private AudioClip failClip;

    private Coroutine flashRoutine;
    private Color originalColor;

    private void Awake()
    {
        if (rhythmClock == null)
            rhythmClock = FindFirstObjectByType<RhythmClock>();

        if (rhythmCombat == null)
            rhythmCombat = FindFirstObjectByType<RhythmCombatController>();

        if (ringImage != null)
            originalColor = ringImage.color;
    }

    private void OnEnable()
    {
        if (rhythmCombat != null)
            rhythmCombat.onQualityEvaluated.AddListener(OnQuality);
    }

    private void OnDisable()
    {
        if (rhythmCombat != null)
            rhythmCombat.onQualityEvaluated.RemoveListener(OnQuality);
    }

    private void Update()
    {
        if (rhythmClock == null) return;

        float phase = rhythmClock.GetBeatPhase01(); // 0..1
        if (ringImage != null)
            ringImage.fillAmount = phase;

        if (shrinkingIndicator != null)
        {
            float scale = Mathf.Lerp(maxScale, minScale, phase);
            shrinkingIndicator.localScale = Vector3.one * scale;
        }
    }

    private void OnQuality(RhythmHitQuality quality)
    {
        switch (quality)
        {
            case RhythmHitQuality.Perfect:
                Flash(perfectColor);
                PlayClip(perfectClip);
                break;
            case RhythmHitQuality.Good:
                Flash(goodColor);
                PlayClip(goodClip);
                break;
            default:
                Flash(failColor);
                PlayClip(failClip);
                break;
        }
    }

    private void Flash(Color c)
    {
        if (ringImage == null) return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(c));
    }

    private IEnumerator FlashRoutine(Color c)
    {
        ringImage.color = c;
        yield return new WaitForSeconds(flashSeconds);
        ringImage.color = originalColor;
    }

    private void PlayClip(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
}

/*
Unity setup:
- Create a Canvas -> add an Image for the ring:
  - Image Type: Filled
  - Fill Method: Radial 360
- Add a child (Image/RectTransform) as shrinkingIndicator.
- Add this script to the UI root, link refs.
- Assign SFX clips.
*/