using UnityEngine;

[CreateAssetMenu(fileName = "BoomerangLoopWeaponData", menuName = "Game/Boomerang/Boomerang Loop Weapon Data")]
public class BoomerangLoopWeaponDataSO : BoomerangWeaponDataSO
{
    [Header("Loop Sequence")]
    public BoomerangLoopSequenceDefinitionSO loopSequenceDefinition;
}