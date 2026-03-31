using UnityEngine;

[CreateAssetMenu(fileName = "BoomerangSequence", menuName = "Game/Player/Boomerang Sequence")]
public class BoomerangSequenceDefinitionSO : SequenceDefinitionSOBase
{
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

    [Header("Dash Behaviour")]
    [SerializeField] private bool allowDashDuringRecall = true;
    [SerializeField] private bool allowDashDuringReflect = true;
    [SerializeField] private bool failOnBadDash = false;

    [Header("Cancel / Fail")]
    [SerializeField] private bool failOnSwitchWeaponInput = true;
    [SerializeField] private bool clearWeaponOverrideOnFail = true;

    [Header("Orbit Reward")]
    [SerializeField] private bool useOrbitReward = true;
    [Min(0.05f)]
    [SerializeField] private float orbitDuration = 3.5f;



    [Header("Projectile Cleanup")]
    [SerializeField] private bool destroyProjectileOnFail = true;
    [Min(0f)]
    [SerializeField] private float destroyProjectileOnFailDelay = 0f;

    public TimedSequenceActionRule RecallRule => recallRule;
    public TimedSequenceActionRule ReflectRule => reflectRule;
    public TimedSequenceActionRule DashRule => dashRule;

    public float RecallWindowDuration => recallWindowDuration;
    public float ReturnToReflectDuration => returnToReflectDuration;
    public float ReflectWindowDuration => reflectWindowDuration;
    public float UiPhaseTransitionHoldDuration => uiPhaseTransitionHoldDuration;
    public float ReflectActivationNormalized => reflectActivationNormalized;

    public int RequiredSuccessfulCycles => RequiredSteps;

    public bool AllowDashDuringRecall => allowDashDuringRecall;
    public bool AllowDashDuringReflect => allowDashDuringReflect;
    public bool FailOnBadDash => failOnBadDash;
    public bool FailOnSwitchWeaponInput => failOnSwitchWeaponInput;
    public bool ClearWeaponOverrideOnFail => clearWeaponOverrideOnFail;

    public bool UseOrbitReward => useOrbitReward;
    public float OrbitDuration => orbitDuration;
    public bool DestroyProjectileOnFail => destroyProjectileOnFail;
    public float DestroyProjectileOnFailDelay => Mathf.Max(0f, destroyProjectileOnFailDelay);

    public override bool IsValid()
    {
        return RequiredSteps > 0 &&
               recallWindowDuration > 0f &&
               returnToReflectDuration > 0f &&
               reflectWindowDuration > 0f &&
               recallRule != null &&
               reflectRule != null &&
               dashRule != null;
    }
}