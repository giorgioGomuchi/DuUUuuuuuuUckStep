using System;
using UnityEngine;

[Serializable]
public class ComboEffectBinding
{
    [SerializeField] private PlayerComboRecipeSO recipe;
    [SerializeField] private ComboEffectSO effect;

    public PlayerComboRecipeSO Recipe => recipe;
    public ComboEffectSO Effect => effect;
}