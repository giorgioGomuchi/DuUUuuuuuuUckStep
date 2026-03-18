using UnityEngine;

[CreateAssetMenu(
    fileName = "BoomerangSequenceDefinition",
    menuName = "Game/Player/Boomerang Timed Sequence")]
public class BoomerangSequenceDefinitionSO : ScriptableObject
{
    [Header("Id")]
    [SerializeField] private string sequenceId = "BoomerangSequence";

    [Header("Rules")]
    [SerializeField] private TimedSequenceActionRule recallRule;
    [SerializeField] private TimedSequenceActionRule reflectRule;
    [SerializeField] private TimedSequenceActionRule dashRule;

    [Header("Timing")]
    [Min(0.05f)]
    [SerializeField] private float recallWindowDuration = 0.45f;

    [Min(0.05f)]
    [SerializeField] private float reflectWindowDuration = 0.45f;

    [Header("Reflect Timing")]
    [Range(0.05f, 0.95f)]
    [SerializeField] private float reflectActivationNormalized = 0.55f;

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
    [SerializeField] private Vector3 playerUIWorldOffset = new Vector3(0f, 1.4f, 0f);

    [Header("Reward")]
    [SerializeField] private WeaponDataSO completionRewardWeaponData;
    [SerializeField] private WeaponSlotType completionRewardSlot = WeaponSlotType.Main;
    [SerializeField] private int completionRewardAmmo = 1;

    public string SequenceId => sequenceId;
    public TimedSequenceActionRule RecallRule => recallRule;
    public TimedSequenceActionRule ReflectRule => reflectRule;
    public TimedSequenceActionRule DashRule => dashRule;
    public float RecallWindowDuration => recallWindowDuration;
    public float ReflectWindowDuration => reflectWindowDuration;
    public float ReflectActivationNormalized => reflectActivationNormalized;
    public int RequiredSuccessfulCycles => requiredSuccessfulCycles;
    public bool AllowDashDuringRecall => allowDashDuringRecall;
    public bool AllowDashDuringReflect => allowDashDuringReflect;
    public bool FailOnBadDash => failOnBadDash;
    public bool FailOnSwitchWeaponInput => failOnSwitchWeaponInput;
    public bool ClearWeaponOverrideOnFail => clearWeaponOverrideOnFail;
    public Vector3 PlayerUIWorldOffset => playerUIWorldOffset;
    public WeaponDataSO CompletionRewardWeaponData => completionRewardWeaponData;
    public WeaponSlotType CompletionRewardSlot => completionRewardSlot;
    public int CompletionRewardAmmo => Mathf.Max(1, completionRewardAmmo);

    public bool IsValid()
    {
        return recallRule != null &&
               reflectRule != null &&
               dashRule != null &&
               recallWindowDuration > 0f &&
               reflectWindowDuration > 0f &&
               requiredSuccessfulCycles > 0;
    }
}