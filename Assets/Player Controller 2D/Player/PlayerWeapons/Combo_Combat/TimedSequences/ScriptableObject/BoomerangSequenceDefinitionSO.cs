using UnityEngine;

[CreateAssetMenu(fileName = "BoomerangSequence", menuName = "Game/Player/Boomerang Sequence")]
public class BoomerangSequenceDefinitionSO : ScriptableObject
{
    [Header("Identification")]
    [SerializeField] private string sequenceId = "BoomerangSequence";

    [Header("Rules")]
    [SerializeField] private TimedSequenceActionRule recallRule = new();
    [SerializeField] private TimedSequenceActionRule reflectRule = new();
    [SerializeField] private TimedSequenceActionRule dashRule = new();

    [Header("Main Timing")]
    [Min(0.05f)]
    [SerializeField] private float recallWindowDuration = 1f;

    [Min(0.05f)]
    [SerializeField] private float returnToReflectDuration = 0.3f;

    [Min(0.05f)]
    [SerializeField] private float reflectWindowDuration = 1f;

    [Header("UI / Transition")]
    [Min(0f)]
    [SerializeField] private float uiPhaseTransitionHoldDuration = 0.08f;

    [Header("Reflect Activation During Return")]
    [Range(0f, 1f)]
    [SerializeField] private float reflectActivationNormalized = 0.3f;

    [Header("Progress")]
    [Min(1)]
    [SerializeField] private int requiredSuccessfulCycles = 3;

    [Header("Dash Behaviour")]
    [SerializeField] private bool allowDashDuringRecall = true;
    [SerializeField] private bool allowDashDuringReflect = true;
    [SerializeField] private bool failOnBadDash = false;

    [Header("Cancel / Fail")]
    [SerializeField] private bool failOnSwitchWeaponInput = true;
    [SerializeField] private bool clearWeaponOverrideOnFail = true;

    [Header("UI")]
    [SerializeField] private Vector3 playerUIWorldOffset = new(0f, 1.4f, 0f);

    [Header("Orbit Reward")]
    [SerializeField] private bool useOrbitReward = true;

    [Min(0.05f)]
    [SerializeField] private float orbitDuration = 3.5f;

    [Min(0)]
    [SerializeField] private int orbitTurns = 0;

    [Header("Reward Gating")]
    [SerializeField] private bool requireDamageForReward = false;

    [Min(1)]
    [SerializeField] private int minUniqueEnemiesDamagedForReward = 1;

    [Header("Reward Scaling")]
    [SerializeField] private bool scaleOrbitDurationByUniqueEnemies = false;

    [Min(0f)]
    [SerializeField] private float extraOrbitDurationPerUniqueEnemy = 0.35f;

    [Min(0)]
    [SerializeField] private int maxEnemiesCountedForRewardDuration = 10;

    [SerializeField] private bool clampFinalOrbitDuration = true;

    [Min(0.05f)]
    [SerializeField] private float minFinalOrbitDuration = 0.5f;

    [Min(0.05f)]
    [SerializeField] private float maxFinalOrbitDuration = 6f;

    public string SequenceId => sequenceId;

    public TimedSequenceActionRule RecallRule => recallRule;
    public TimedSequenceActionRule ReflectRule => reflectRule;
    public TimedSequenceActionRule DashRule => dashRule;

    public float RecallWindowDuration => recallWindowDuration;
    public float ReturnToReflectDuration => returnToReflectDuration;
    public float ReflectWindowDuration => reflectWindowDuration;
    public float UiPhaseTransitionHoldDuration => uiPhaseTransitionHoldDuration;
    public float ReflectActivationNormalized => reflectActivationNormalized;

    public int RequiredSuccessfulCycles => requiredSuccessfulCycles;

    public bool AllowDashDuringRecall => allowDashDuringRecall;
    public bool AllowDashDuringReflect => allowDashDuringReflect;
    public bool FailOnBadDash => failOnBadDash;

    public bool FailOnSwitchWeaponInput => failOnSwitchWeaponInput;
    public bool ClearWeaponOverrideOnFail => clearWeaponOverrideOnFail;

    public Vector3 PlayerUIWorldOffset => playerUIWorldOffset;

    public bool UseOrbitReward => useOrbitReward;
    public float OrbitDuration => orbitDuration;
    public int OrbitTurns => orbitTurns;

    public bool RequireDamageForReward => requireDamageForReward;
    public int MinUniqueEnemiesDamagedForReward => Mathf.Max(0, minUniqueEnemiesDamagedForReward);

    public bool ScaleOrbitDurationByUniqueEnemies => scaleOrbitDurationByUniqueEnemies;
    public float ExtraOrbitDurationPerUniqueEnemy => Mathf.Max(0f, extraOrbitDurationPerUniqueEnemy);
    public int MaxEnemiesCountedForRewardDuration => Mathf.Max(0, maxEnemiesCountedForRewardDuration);

    public bool ClampFinalOrbitDuration => clampFinalOrbitDuration;
    public float MinFinalOrbitDuration => Mathf.Max(0.05f, minFinalOrbitDuration);
    public float MaxFinalOrbitDuration => Mathf.Max(MinFinalOrbitDuration, maxFinalOrbitDuration);

    public bool IsValid()
    {
        return recallWindowDuration > 0f &&
               returnToReflectDuration > 0f &&
               reflectWindowDuration > 0f &&
               requiredSuccessfulCycles > 0 &&
               recallRule != null &&
               reflectRule != null &&
               dashRule != null;
    }

    public bool CanActivateReward(BoomerangSequencePerformance performance)
    {
        if (!useOrbitReward)
            return false;

        if (!requireDamageForReward)
            return true;

        return performance != null &&
               performance.TotalUniqueEnemiesDamaged >= MinUniqueEnemiesDamagedForReward;
    }

    public float ResolveOrbitDuration(BoomerangSequencePerformance performance)
    {
        float result = orbitDuration;

        if (scaleOrbitDurationByUniqueEnemies && performance != null)
        {
            int countedEnemies = Mathf.Min(
                performance.TotalUniqueEnemiesDamaged,
                MaxEnemiesCountedForRewardDuration);

            result += countedEnemies * ExtraOrbitDurationPerUniqueEnemy;
        }

        if (clampFinalOrbitDuration)
        {
            result = Mathf.Clamp(
                result,
                MinFinalOrbitDuration,
                MaxFinalOrbitDuration);
        }
        else
        {
            result = Mathf.Max(0.05f, result);
        }

        return result;
    }
}