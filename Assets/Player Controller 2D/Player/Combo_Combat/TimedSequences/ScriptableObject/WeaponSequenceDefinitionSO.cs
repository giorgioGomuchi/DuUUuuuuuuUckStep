using UnityEngine;

[CreateAssetMenu(fileName = "WeaponSequenceDefinition", menuName = "Game/Player/Weapon Timed Sequence")]
public class WeaponSequenceDefinitionSO : ScriptableObject
{
    [Header("Id")]
    [SerializeField] private string sequenceId = "SniperSequence";

    [Header("Sequence Weapon")]
    [SerializeField] private WeaponDataSO sequenceWeaponData;
    [SerializeField] private WeaponSlotType targetSlot = WeaponSlotType.Main;

    [Tooltip("If <= 0, uses Required Successful Shots.")]
    [SerializeField] private int overrideAmmoCount = 0;

    [Header("Progress")]
    [Min(1)]
    [SerializeField] private int requiredSuccessfulShots = 6;

    [Header("Timing")]
    [Min(0.05f)]
    [SerializeField] private float decisionWindowDuration = 0.9f;

    [Min(0f)]
    [SerializeField] private float startupDelay = 0.08f;

    [SerializeField] private TimedSequenceActionRule shootRule = new();
    [SerializeField] private TimedSequenceActionRule dashRule = new();

    [Header("Fail Rules")]
    [SerializeField] private bool failOnTimeout = true;
    [SerializeField] private bool failOnSecondaryInput = true;
    [SerializeField] private bool failOnSwitchWeaponInput = true;

    [Header("Aim Guide")]
    [SerializeField] private bool showAimGuide = false;

    [Header("UI")]
    [SerializeField] private Vector3 playerUIWorldOffset = new(0f, 1.5f, 0f);

    [Header("Reward")]
    [SerializeField] private SequenceRewardSO completionReward;

    public string SequenceId => sequenceId;
    public WeaponDataSO SequenceWeaponData => sequenceWeaponData;
    public WeaponSlotType TargetSlot => targetSlot;
    public int OverrideAmmoCount => overrideAmmoCount;
    public int RequiredSuccessfulShots => requiredSuccessfulShots;
    public float DecisionWindowDuration => decisionWindowDuration;
    public float StartupDelay => startupDelay;
    public TimedSequenceActionRule ShootRule => shootRule;
    public TimedSequenceActionRule DashRule => dashRule;
    public bool FailOnTimeout => failOnTimeout;
    public bool FailOnSecondaryInput => failOnSecondaryInput;
    public bool FailOnSwitchWeaponInput => failOnSwitchWeaponInput;
    public bool ShowAimGuide => showAimGuide;
    public Vector3 PlayerUIWorldOffset => playerUIWorldOffset;
    public SequenceRewardSO CompletionReward => completionReward;

    public int ResolveInitialAmmo()
    {
        return overrideAmmoCount > 0
            ? overrideAmmoCount
            : Mathf.Max(1, requiredSuccessfulShots);
    }

    public bool IsValid()
    {
        return sequenceWeaponData != null &&
               requiredSuccessfulShots > 0 &&
               decisionWindowDuration > 0f;
    }
}