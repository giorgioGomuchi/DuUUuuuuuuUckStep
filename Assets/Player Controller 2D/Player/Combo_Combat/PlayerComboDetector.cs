using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerComboDetector : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerActionRecorder actionRecorder;

    [Header("Recipes")]
    [SerializeField] private List<PlayerComboRecipeSO> recipes = new();

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    public event Action<PlayerComboRecipeSO> OnComboTriggered;

    private void Awake()
    {
        if (actionRecorder == null)
            actionRecorder = GetComponentInChildren<PlayerActionRecorder>();
    }

    private void OnEnable()
    {
        if (actionRecorder != null)
            actionRecorder.OnActionRecorded += HandleActionRecorded;
    }

    private void OnDisable()
    {
        if (actionRecorder != null)
            actionRecorder.OnActionRecorded -= HandleActionRecorded;
    }

    private void HandleActionRecorded(PlayerActionData actionData)
    {
        TryResolveCombo();
    }

    private void TryResolveCombo()
    {
        if (actionRecorder == null || recipes == null || recipes.Count == 0)
            return;

        PlayerComboRecipeSO bestMatch = null;
        int bestLength = -1;

        for (int i = 0; i < recipes.Count; i++)
        {
            PlayerComboRecipeSO recipe = recipes[i];
            if (recipe == null)
                continue;

            if (!recipe.MatchesTail(actionRecorder.Actions))
                continue;

            if (recipe.StepCount > bestLength)
            {
                bestMatch = recipe;
                bestLength = recipe.StepCount;
            }
        }

        if (bestMatch == null)
            return;

        if (debugLogs)
            Debug.Log($"[PlayerComboDetector] Combo triggered: {bestMatch.RecipeId}", this);

        OnComboTriggered?.Invoke(bestMatch);

        if (bestMatch.ConsumeMatchedActions)
            actionRecorder.RemoveLastActions(bestMatch.StepCount);
    }
}