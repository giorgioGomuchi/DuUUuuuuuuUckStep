using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class RhythmInputEvent : UnityEvent<RhythmInputResult> { }

[Serializable]
public class ComboTriggeredEvent : UnityEvent<RhythmComboRecipeSO> { }

public class RhythmCombatController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RhythmClock rhythmClock;

    [Header("Timing Windows")]
    [SerializeField] private float goodWindowSeconds = 0.075f;
    [SerializeField] private float perfectWindowSeconds = 0.03f;

    [Header("Combo Buffer")]
    [SerializeField] private float bufferResetSeconds = 0.55f;
    [SerializeField] private int maxBufferSize = 8;

    [Header("Perfect Tracking")]
    [SerializeField] private int perfectHistorySize = 16;

    [Header("Combo Recipes")]
    [SerializeField] private List<RhythmComboRecipeSO> recipes = new();

    [Header("Events")]
    public RhythmInputEvent onInputEvaluated;
    public ComboTriggeredEvent onComboTriggered;

    [SerializeField] private int accuracyHistorySize = 16;
    private readonly Queue<float> accuracyHistory = new();

    private readonly List<CombatAction> actionBuffer = new();
    private readonly List<RhythmHitQuality> qualityBuffer = new();
    private readonly Queue<RhythmHitQuality> perfectHistory = new();

    private float lastInputTime;

    private void Awake()
    {
        if (rhythmClock == null)
            rhythmClock = FindFirstObjectByType<RhythmClock>();
    }

    public RhythmInputResult RegisterAttack(CombatAction action)
    {
        float dist = rhythmClock != null ? rhythmClock.GetDistanceToNearestBeatSeconds() : 0f;
        float phase = rhythmClock != null ? rhythmClock.GetBeatPhase01() : 0f;

        RhythmHitQuality quality = EvaluateQuality(dist);
        bool wasEarly = phase < 0.5f;

        var result = new RhythmInputResult
        {
            action = action,
            quality = quality,
            distanceToBeat = dist,
            beatPhase01 = phase,
            wasEarly = wasEarly
        };

        onInputEvaluated?.Invoke(result);

        TrackPerfectHistory(quality);
        TrackAccuracyHistory(quality);

        if (Time.time - lastInputTime > bufferResetSeconds || quality == RhythmHitQuality.Fail)
        {
            actionBuffer.Clear();
            qualityBuffer.Clear();
        }

        lastInputTime = Time.time;

        actionBuffer.Add(action);
        qualityBuffer.Add(quality);

        TrimBuffers();
        TryResolveRecipes();

        return result;
    }

    private RhythmHitQuality EvaluateQuality(float dist)
    {
        if (dist <= perfectWindowSeconds) return RhythmHitQuality.Perfect;
        if (dist <= goodWindowSeconds) return RhythmHitQuality.Good;
        return RhythmHitQuality.Fail;
    }

    private void TrackPerfectHistory(RhythmHitQuality quality)
    {
        perfectHistory.Enqueue(quality);

        while (perfectHistory.Count > perfectHistorySize)
            perfectHistory.Dequeue();
    }

    public float GetRecentPerfectRatio()
    {
        if (perfectHistory.Count == 0)
            return 0f;

        int perfectCount = 0;

        foreach (var q in perfectHistory)
            if (q == RhythmHitQuality.Perfect)
                perfectCount++;

        return (float)perfectCount / perfectHistory.Count;
    }

    private void TrimBuffers()
    {
        while (actionBuffer.Count > maxBufferSize) actionBuffer.RemoveAt(0);
        while (qualityBuffer.Count > maxBufferSize) qualityBuffer.RemoveAt(0);
    }

    private void TryResolveRecipes()
    {
        if (recipes == null || recipes.Count == 0) return;

        foreach (var recipe in recipes)
        {
            if (recipe == null || recipe.Length == 0) continue;
            if (!EndsWithSequence(recipe.Sequence)) continue;
            if (recipe.RequireGoodOrPerfect && !LastWasPerfect()) continue;

            onComboTriggered?.Invoke(recipe);

            actionBuffer.Clear();
            qualityBuffer.Clear();
            return;
        }
    }

    private bool EndsWithSequence(CombatAction[] sequence)
    {
        if (sequence == null) return false;
        if (actionBuffer.Count < sequence.Length) return false;

        int start = actionBuffer.Count - sequence.Length;

        for (int i = 0; i < sequence.Length; i++)
            if (actionBuffer[start + i] != sequence[i])
                return false;

        return true;
    }

    private bool LastWasPerfect()
    {
        if (qualityBuffer.Count == 0) return false;
        return qualityBuffer[^1] == RhythmHitQuality.Perfect;
    }


    private void TrackAccuracyHistory(RhythmHitQuality quality)
    {
        float score = quality switch
        {
            RhythmHitQuality.Perfect => 1f,
            RhythmHitQuality.Good => 0.6f,
            _ => 0f
        };

        accuracyHistory.Enqueue(score);

        while (accuracyHistory.Count > accuracyHistorySize)
            accuracyHistory.Dequeue();
    }

    public float GetRecentAccuracy01()
    {
        if (accuracyHistory.Count == 0)
            return 0f;

        float sum = 0f;
        foreach (var s in accuracyHistory)
            sum += s;

        return Mathf.Clamp01(sum / accuracyHistory.Count);
    }
}