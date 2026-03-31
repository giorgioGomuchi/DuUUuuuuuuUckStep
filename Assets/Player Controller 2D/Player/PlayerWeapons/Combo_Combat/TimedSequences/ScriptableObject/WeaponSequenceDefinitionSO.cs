using UnityEngine;

[CreateAssetMenu(fileName = "WeaponSequenceDefinition", menuName = "Game/Player/Weapon Timed Sequence")]
public class WeaponSequenceDefinitionSO : SequenceDefinitionSOBase
{
    [Header("Generic Runtime Timing")]
    [Min(0f)]
    [SerializeField] private float startupDelay = 0f;

    [Min(0.05f)]
    [SerializeField] private float decisionWindowDuration = 0.5f;

    [Min(0f)]
    [SerializeField] private float completionDelay = 0f;

    [Header("Generic Runtime Fail Rules")]
    [SerializeField] private bool failOnTimeout = true;
    [SerializeField] private bool failOnWrongAction = true;
    [SerializeField] private bool failOnForbiddenInput = true;

    [Header("Sequence Weapon")]
    [SerializeField] private WeaponDataSO sequenceWeaponData;
    [SerializeField] private WeaponSlotType targetSlot = WeaponSlotType.Main;

    [Tooltip("If <= 0, uses Required Successful Shots.")]
    [SerializeField] private int overrideAmmoCount = 0;

    [Header("Action Rules")]
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

    [Header("Aim Guide")]
    [SerializeField] private bool showAimGuide = false;

    public override bool SupportsGenericRuntime => true;

    public override float StartupDelay => startupDelay;
    public override float DecisionWindowDuration => decisionWindowDuration;
    public override float CompletionDelay => completionDelay;

    public override bool FailOnTimeout => failOnTimeout;
    public override bool FailOnWrongAction => failOnWrongAction;
    public override bool FailOnForbiddenInput => failOnForbiddenInput;

    public WeaponDataSO SequenceWeaponData => sequenceWeaponData;
    public WeaponSlotType TargetSlot => targetSlot;
    public int OverrideAmmoCount => overrideAmmoCount;

    public bool UseDashResetLimitPerStep => useDashResetLimitPerStep;
    public int MaxDashResetsPerCurrentStep => maxDashResetsPerCurrentStep;
    public bool UseMaxSequenceDuration => useMaxSequenceDuration;
    public float MaxSequenceDurationSeconds => maxSequenceDurationSeconds;
    public int RequiredSuccessfulShots => RequiredSteps;
    public TimedSequenceActionRule ShootRule => shootRule;
    public TimedSequenceActionRule DashRule => dashRule;
    public bool FailOnSecondaryInput => failOnSecondaryInput;
    public bool FailOnSwitchWeaponInput => failOnSwitchWeaponInput;
    public bool ShowAimGuide => showAimGuide;

    public int ResolveInitialAmmo()
    {
        return overrideAmmoCount > 0
            ? overrideAmmoCount
            : Mathf.Max(1, RequiredSteps);
    }

    public override bool IsValid()
    {
        return RequiredSteps > 0 &&
               decisionWindowDuration > 0f &&
               sequenceWeaponData != null;
    }
}