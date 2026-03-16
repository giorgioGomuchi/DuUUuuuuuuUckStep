using UnityEngine;

[CreateAssetMenu(fileName = "StartWeaponSequenceComboEffect", menuName = "Game/Player/Combo Effects/Start Weapon Sequence")]
public class StartWeaponSequenceComboEffectSO : ComboEffectSO
{
    [SerializeField] private WeaponSequenceDefinitionSO sequenceDefinition;

    public override void Apply(ComboEffectContext context)
    {
        if (context == null)
        {
            Debug.LogError("[StartWeaponSequenceComboEffectSO] Context is null.");
            return;
        }

        if (context.PlayerReferences == null)
        {
            Debug.LogError("[StartWeaponSequenceComboEffectSO] PlayerReferences missing.");
            return;
        }

        if (sequenceDefinition == null)
        {
            Debug.LogError("[StartWeaponSequenceComboEffectSO] SequenceDefinition missing.");
            return;
        }

        WeaponSequenceController sequenceController = context.PlayerReferences.WeaponSequence;
        if (sequenceController == null)
        {
            Debug.LogError("[StartWeaponSequenceComboEffectSO] WeaponSequenceController missing.");
            return;
        }

        sequenceController.StartSequence(sequenceDefinition);
    }
}