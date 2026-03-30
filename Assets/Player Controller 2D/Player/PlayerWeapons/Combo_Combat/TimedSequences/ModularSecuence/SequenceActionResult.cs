using System;
using UnityEngine;

[Serializable]
public struct SequenceActionResult
{
    public bool accepted;
    public bool completedStep;
    public bool completedSequence;
    public bool perfect;
    public bool good;

    public int hits;
    public float damage;

    public static SequenceActionResult Rejected => new()
    {
        accepted = false,
        completedStep = false,
        completedSequence = false,
        perfect = false,
        good = false,
        hits = 0,
        damage = 0f
    };

    public static SequenceActionResult Success(bool perfect, bool good, bool completedStep)
    {
        return new SequenceActionResult
        {
            accepted = true,
            completedStep = completedStep,
            completedSequence = false,
            perfect = perfect,
            good = good,
            hits = 0,
            damage = 0f
        };
    }
}