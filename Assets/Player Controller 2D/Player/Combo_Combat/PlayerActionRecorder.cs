using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActionRecorder : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxStoredActions = 12;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private readonly List<PlayerActionData> actions = new();

    public event Action<PlayerActionData> OnActionRecorded;

    public IReadOnlyList<PlayerActionData> Actions => actions;

    public void RecordAction(PlayerActionData actionData)
    {
        if (actionData.ActionType == PlayerActionType.None)
            return;

        actions.Add(actionData);

        while (actions.Count > maxStoredActions)
            actions.RemoveAt(0);

        if (debugLogs)
        {
            Debug.Log(
                $"[PlayerActionRecorder] Recorded {actionData.ActionType} | " +
                $"ActionDir={actionData.ActionDirection} | AimDir={actionData.AimDirection} | Source={actionData.SourceId}",
                this);
        }

        OnActionRecorded?.Invoke(actionData);
    }

    public void RemoveLastActions(int count)
    {
        if (count <= 0 || actions.Count == 0)
            return;

        count = Mathf.Min(count, actions.Count);
        actions.RemoveRange(actions.Count - count, count);
    }

    public void ClearAll()
    {
        actions.Clear();
    }
}