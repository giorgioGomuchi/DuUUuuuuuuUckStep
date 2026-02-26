using System;

public enum RhythmHitQuality
{
    Fail,
    Good,
    Perfect
}

public enum CombatAction
{
    Melee,
    Ranged,
    Dash,
    Special,

    BoomerangRebound
}

public enum MusicState
{
    Silence,
    Pulse,      // Beats
    Groove,     // Beats + Shakers
    Drive       // Beats + Shakers + Bass
}

[Serializable]
public class RhythmInputResult
{
    public CombatAction action;
    public RhythmHitQuality quality;

    // seconds
    public float distanceToBeat;

    // 0..1
    public float beatPhase01;

    // true if input happened in first half of beat
    public bool wasEarly;
}