using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SequenceRewardSO : ScriptableObject
{
    public abstract void Apply(WeaponSequenceRewardContext context);

}
