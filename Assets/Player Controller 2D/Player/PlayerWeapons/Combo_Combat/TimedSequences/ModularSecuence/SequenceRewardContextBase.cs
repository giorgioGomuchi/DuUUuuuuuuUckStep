using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SequenceRewardContextBase
{
    [Header("Core Outcome")]
    public bool sequenceCompleted;
    public int completedSteps;
    public int attemptedSteps;

    [Header("Timing Quality")]
    public int successfulActions;
    public int perfectCount;
    public int goodCount;

    [Header("Combat")]
    public int hitCount;
    public int uniqueTargetCount;
    public float totalDamage;

    [Header("Optional Metrics")]
    [SerializeField] private List<IntMetric> intMetrics = new();
    [SerializeField] private List<FloatMetric> floatMetrics = new();

    private readonly Dictionary<string, int> intLookup = new();
    private readonly Dictionary<string, float> floatLookup = new();

    [Serializable]
    public struct IntMetric
    {
        public string key;
        public int value;
    }

    [Serializable]
    public struct FloatMetric
    {
        public string key;
        public float value;
    }

    public void SetInt(string key, int value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        intLookup[key] = value;
        SyncIntList();
    }

    public void SetFloat(string key, float value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        floatLookup[key] = value;
        SyncFloatList();
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(key))
            return defaultValue;

        return intLookup.TryGetValue(key, out int value) ? value : defaultValue;
    }

    public float GetFloat(string key, float defaultValue = 0f)
    {
        if (string.IsNullOrWhiteSpace(key))
            return defaultValue;

        return floatLookup.TryGetValue(key, out float value) ? value : defaultValue;
    }

    public void RebuildLookups()
    {
        intLookup.Clear();
        floatLookup.Clear();

        for (int i = 0; i < intMetrics.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(intMetrics[i].key))
                intLookup[intMetrics[i].key] = intMetrics[i].value;
        }

        for (int i = 0; i < floatMetrics.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(floatMetrics[i].key))
                floatLookup[floatMetrics[i].key] = floatMetrics[i].value;
        }
    }

    private void SyncIntList()
    {
        intMetrics.Clear();
        foreach (var pair in intLookup)
        {
            intMetrics.Add(new IntMetric
            {
                key = pair.Key,
                value = pair.Value
            });
        }
    }

    private void SyncFloatList()
    {
        floatMetrics.Clear();
        foreach (var pair in floatLookup)
        {
            floatMetrics.Add(new FloatMetric
            {
                key = pair.Key,
                value = pair.Value
            });
        }
    }
}