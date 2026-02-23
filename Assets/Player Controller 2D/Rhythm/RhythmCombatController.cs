using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum RhythmHitQuality
{
    Fail,
    Good,
    Perfect
}

[Serializable]
public class RhythmQualityEvent : UnityEvent<RhythmHitQuality> { }

[Serializable]
public class ComboTriggeredEvent : UnityEvent<RhythmComboRecipeSO> { }

public class RhythmCombatController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RhythmClock rhythmClock;

    [Header("Timing Windows (seconds)")]
    [SerializeField] private float goodWindowSeconds = 0.06f;
    [SerializeField] private float perfectWindowSeconds = 0.025f;
    [SerializeField] private float bufferResetSeconds = 0.5f;
    [SerializeField] private int maxBufferSize = 8;

    [Header("Combo Recipes")]
    [SerializeField] private List<RhythmComboRecipeSO> recipes = new();

    [Header("Events")]
    public RhythmQualityEvent onQualityEvaluated;
    public ComboTriggeredEvent onComboTriggered;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly List<RhythmHitQuality> qualityBuffer = new();
    private readonly List<CombatAction> buffer = new();
    private float lastInputTime;

    private void Awake()
    {
        if (rhythmClock == null)
            rhythmClock = FindFirstObjectByType<RhythmClock>();
    }

    public RhythmHitQuality RegisterAttack(CombatAction action)
    {
        var quality = EvaluateQuality();

        if (debugLogs)
            Debug.Log($"[RhythmCombat] Input={action} Quality={quality}");

        onQualityEvaluated?.Invoke(quality);

        // Reset buffer if too late or fail
        if (Time.time - lastInputTime > bufferResetSeconds || quality == RhythmHitQuality.Fail)
        {
            buffer.Clear();
            qualityBuffer.Clear();
        }

        lastInputTime = Time.time;

        buffer.Add(action);
        qualityBuffer.Add(quality);
        TrimQualityBuffer();
        TrimBuffer();

        TryResolveRecipes(quality);

        return quality;
    }

    private RhythmHitQuality EvaluateQuality()
    {
        if (rhythmClock == null)
            return RhythmHitQuality.Good; // fallback

        float dist = rhythmClock.GetDistanceToNearestBeatSeconds();

        if (dist <= perfectWindowSeconds) return RhythmHitQuality.Perfect;
        if (dist <= goodWindowSeconds) return RhythmHitQuality.Good;
        return RhythmHitQuality.Fail;
    }

    private void TrimBuffer()
    {
        while (buffer.Count > maxBufferSize)
            buffer.RemoveAt(0);
    }

    private void TrimQualityBuffer()
    {
        while (qualityBuffer.Count > maxBufferSize)
            qualityBuffer.RemoveAt(0);
    }

    //private void TryResolveRecipes(RhythmHitQuality lastQuality)
    //{
    //    if (recipes == null || recipes.Count == 0) return;

    //    foreach (var recipe in recipes)
    //    {
    //        if (recipe == null || recipe.Length == 0) continue;

    //        if (recipe.RequireGoodOrPerfect && lastQuality == RhythmHitQuality.Fail)
    //            continue;

    //        if (EndsWithSequence(recipe.Sequence))
    //        {
    //            if (debugLogs)
    //                Debug.Log($"[RhythmCombat] Combo triggered: {recipe.RecipeId}");

    //            onComboTriggered?.Invoke(recipe);
    //            buffer.Clear();
    //            return;
    //        }
    //    }
    //}

    private void TryResolveRecipes(RhythmHitQuality lastQuality)
    {
        if (recipes == null || recipes.Count == 0) return;

        foreach (var recipe in recipes)
        {
            if (recipe == null || recipe.Length == 0) continue;

            if (!EndsWithSequence(recipe.Sequence))
                continue;

            if (!SequenceQualityIsValid(recipe))
                continue;

            if (debugLogs)
                Debug.Log($"[RhythmCombat] Combo triggered: {recipe.RecipeId}");

            onComboTriggered?.Invoke(recipe);

            buffer.Clear();
            qualityBuffer.Clear();
            return;
        }
    }

    private bool EndsWithSequence(CombatAction[] sequence)
    {
        if (sequence == null) return false;
        if (buffer.Count < sequence.Length) return false;

        int start = buffer.Count - sequence.Length;

        for (int i = 0; i < sequence.Length; i++)
        {
            if (buffer[start + i] != sequence[i])
                return false;
        }
        return true;
    }

    private bool SequenceQualityIsValid(RhythmComboRecipeSO recipe)
    {
        if (qualityBuffer.Count < recipe.Length)
            return false;

        int start = qualityBuffer.Count - recipe.Length;

        for (int i = 0; i < recipe.Length; i++)
        {
            var q = qualityBuffer[start + i];

            // Último input debe ser PERFECT
            if (i == recipe.Length - 1)
            {
                if (q != RhythmHitQuality.Perfect)
                    return false;
            }
            else
            {
                // Los anteriores no pueden ser FAIL
                if (q == RhythmHitQuality.Fail)
                    return false;
            }
        }

        return true;
    }
}

/*
Unity setup:
- Put this on the same "RhythmSystem" GameObject.
- Assign RhythmClock.
- Add recipes list (SO_RhythmComboRecipe assets).
- Weapon system should call RegisterAttack(Melee/Ranged) when an attack is requested.
*/