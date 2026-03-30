using System;
using UnityEngine;

[Serializable]
public struct SequenceRewardResolution
{
    public bool shouldApply;
    public float duration;
    public int ammo;
    public float magnitude;

    public static SequenceRewardResolution None => new()
    {
        shouldApply = false,
        duration = 0f,
        ammo = 0,
        magnitude = 0f
    };

    public static SequenceRewardResolution CreateDuration(float value)
    {
        return new SequenceRewardResolution
        {
            shouldApply = true,
            duration = value,
            ammo = 0,
            magnitude = 0f
        };
    }

    public static SequenceRewardResolution CreateAmmo(int value)
    {
        return new SequenceRewardResolution
        {
            shouldApply = true,
            duration = 0f,
            ammo = value,
            magnitude = 0f
        };
    }

   
}