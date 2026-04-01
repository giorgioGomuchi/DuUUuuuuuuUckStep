using UnityEngine;

[CreateAssetMenu(
    fileName = "StartShotgunSequenceComboEffect",
    menuName = "Game/Player/Combo Effects/Start Shotgun Sequence")]
public class StartShotgunSequenceComboEffectSO : ComboEffectSO
{
    [SerializeField] private ShotgunSequenceDefinitionSO sequenceDefinition;

    public override void Apply(ComboEffectContext context)
    {
        if (context == null)
        {
            Debug.LogError("[StartShotgunSequenceComboEffectSO] Context is null.");
            return;
        }

        if (context.PlayerReferences == null)
        {
            Debug.LogError("[StartShotgunSequenceComboEffectSO] PlayerReferences missing.");
            return;
        }

        if (sequenceDefinition == null)
        {
            Debug.LogError("[StartShotgunSequenceComboEffectSO] SequenceDefinition missing.");
            return;
        }

        ShotgunSequenceController shotgunController =
            context.PlayerReferences.GetComponentInChildren<ShotgunSequenceController>(true);

        if (shotgunController == null)
        {
            Debug.LogError("[StartShotgunSequenceComboEffectSO] ShotgunSequenceController missing.");
            return;
        }

        if (shotgunController.IsSequenceActive)
        {
            Debug.Log("[StartShotgunSequenceComboEffectSO] Sequence already active -> combo relaunch ignored.");
            return;
        }

        bool started = shotgunController.StartSequence(sequenceDefinition);

        if (!started)
            Debug.LogWarning("[StartShotgunSequenceComboEffectSO] Shotgun sequence failed to start.");
    }
}