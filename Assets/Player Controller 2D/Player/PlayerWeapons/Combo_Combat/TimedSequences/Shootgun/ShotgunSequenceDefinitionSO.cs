using UnityEngine;

[CreateAssetMenu(
    fileName = "ShotgunSequenceDefinition",
    menuName = "Game/Player/Shotgun Timed Sequence")]
public class ShotgunSequenceDefinitionSO : SequenceDefinitionSOBase
{
    [Header("Generic Runtime Timing")]
    [Min(0f)]
    [SerializeField] private float startupDelay = 0f;

    [Min(0.05f)]
    [SerializeField] private float decisionWindowDuration = 0.5f;

    [Min(0f)]
    [SerializeField] private float completionDelay = 0.2f;

    [Header("Generic Runtime Fail Rules")]
    [SerializeField] private bool failOnTimeout = true;
    [SerializeField] private bool failOnWrongAction = true;
    [SerializeField] private bool failOnForbiddenInput = true;

    [Header("Sequence Weapon")]
    [SerializeField] private ShotgunWeaponDataSO sequenceWeaponData;
    [SerializeField] private WeaponSlotType targetSlot = WeaponSlotType.Main;

    [Header("Activation Rules")]
    [SerializeField] private TimedSequenceActionRule shootRule = new();
    [SerializeField] private TimedSequenceActionRule dashRule = new();

    [Header("Dash Reset Limits")]
    [SerializeField] private bool useDashResetLimitPerStep = true;

    [Min(0)]
    [SerializeField] private int maxDashResetsPerCurrentStep = 1;

    [Header("Sequence Lifetime")]
    [SerializeField] private bool useMaxSequenceDuration = false;

    [Min(0.1f)]
    [SerializeField] private float maxSequenceDurationSeconds = 8f;

    [Header("Fail Rules (Weapon Specific)")]
    [SerializeField] private bool failOnSecondaryInput = true;
    [SerializeField] private bool failOnSwitchWeaponInput = true;

    public override bool SupportsGenericRuntime => true;

    public override float StartupDelay => startupDelay;
    public override float DecisionWindowDuration => decisionWindowDuration;
    public override float CompletionDelay => completionDelay;

    public override bool FailOnTimeout => failOnTimeout;
    public override bool FailOnWrongAction => failOnWrongAction;
    public override bool FailOnForbiddenInput => failOnForbiddenInput;

    public ShotgunWeaponDataSO SequenceWeaponData => sequenceWeaponData;
    public WeaponSlotType TargetSlot => targetSlot;

    public TimedSequenceActionRule ShootRule => shootRule;
    public TimedSequenceActionRule DashRule => dashRule;

    public bool UseDashResetLimitPerStep => useDashResetLimitPerStep;
    public int MaxDashResetsPerCurrentStep => Mathf.Max(0, maxDashResetsPerCurrentStep);

    public bool UseMaxSequenceDuration => useMaxSequenceDuration;
    public float MaxSequenceDurationSeconds => Mathf.Max(0.1f, maxSequenceDurationSeconds);

    public bool FailOnSecondaryInput => failOnSecondaryInput;
    public bool FailOnSwitchWeaponInput => failOnSwitchWeaponInput;

    public int RequiredSuccessfulSteps => RequiredSteps;

    public override bool IsValid()
    {
        return RequiredSteps > 0 &&
               decisionWindowDuration > 0f &&
               sequenceWeaponData != null &&
               shootRule != null &&
               shootRule.Enabled;
    }
}