using UnityEngine;

[System.Serializable]
public class AnalogTriggerGate
{
    [Header("Thresholds")]
    [SerializeField] private float pressThreshold = 0.6f;
    [SerializeField] private float releaseThreshold = 0.3f;

    public bool IsHeld { get; private set; }
    public bool PressedThisFrame { get; private set; }
    public bool ReleasedThisFrame { get; private set; }
    public float RawValue { get; private set; }

    public void UpdateValue(float rawValue)
    {
        RawValue = Mathf.Clamp01(rawValue);

        PressedThisFrame = false;
        ReleasedThisFrame = false;

        if (!IsHeld)
        {
            if (RawValue >= pressThreshold)
            {
                IsHeld = true;
                PressedThisFrame = true;
            }
        }
        else
        {
            if (RawValue <= releaseThreshold)
            {
                IsHeld = false;
                ReleasedThisFrame = true;
            }
        }
    }

    public void ForceRelease()
    {
        IsHeld = false;
        PressedThisFrame = false;
        ReleasedThisFrame = false;
        RawValue = 0f;
    }

    public void ClearFrameFlags()
    {
        PressedThisFrame = false;
        ReleasedThisFrame = false;
    }
}