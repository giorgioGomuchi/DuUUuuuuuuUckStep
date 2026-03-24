using UnityEngine;

[CreateAssetMenu(
    fileName = "BoomerangSequenceDefinition",
    menuName = "Game/Player/Boomerang Timed Sequence")]
public class BoomerangSequenceDefinitionSO : ScriptableObject
{
    [Header("Identificación")]
    [Tooltip("Id interno de la secuencia del boomerang.")]
    [SerializeField] private string sequenceId = "BoomerangSequence";

    [Header("Rules")]
    [Tooltip("Ventana visual/lógica para acertar el recall.")]
    [SerializeField] private TimedSequenceActionRule recallRule;

    [Tooltip("Ventana visual/lógica para acertar el reflect.")]
    [SerializeField] private TimedSequenceActionRule reflectRule;

    [Tooltip("Ventana visual/lógica para acertar el dash bonus.")]
    [SerializeField] private TimedSequenceActionRule dashRule;

    [Header("Main Timing")]
    [Min(0.05f)]
    [Tooltip("Duración total de la ventana de recall. Afecta al tiempo disponible para pulsar recall.")]
    [SerializeField] private float recallWindowDuration = 1f;

    [Min(0.05f)]
    [Tooltip("Tiempo que tarda la guitarra/boomerang en volver a la zona de reflect antes de poder devolverla.")]
    [SerializeField] private float returnToReflectDuration = 1f;

    [Min(0.05f)]
    [Tooltip("Duración total de la ventana de reflect. Afecta al tiempo disponible para golpear con melee.")]
    [SerializeField] private float reflectWindowDuration = 1f;

    [Header("UI / Transition")]
    [Min(0f)]
    [Tooltip("Pequeña pausa visual tras acertar recall o reflect para que la barra llegue al final antes de cambiar de fase.")]
    [SerializeField] private float uiPhaseTransitionHoldDuration = 0.08f;

    [Header("Reflect Activation During Return")]
    [Range(0.05f, 0.95f)]
    [Tooltip("Porcentaje de la vuelta tras el cual el boomerang ya puede empezar a armarse para reflect.")]
    [SerializeField] private float reflectActivationNormalized = 0.25f;

    [Header("Progress")]
    [Min(1)]
    [Tooltip("Número de ciclos recall + reflect necesarios para completar la secuencia base.")]
    [SerializeField] private int requiredSuccessfulCycles = 6;

    [Header("Dash Behaviour")]
    [Tooltip("Permite hacer dash durante la ventana de recall.")]
    [SerializeField] private bool allowDashDuringRecall = true;

    [Tooltip("Permite hacer dash durante la ventana de reflect.")]
    [SerializeField] private bool allowDashDuringReflect = true;

    [Tooltip("Si está activo, un dash fuera de timing hace fallar la secuencia.")]
    [SerializeField] private bool failOnBadDash = false;

    [Header("Cancel / Fail")]
    [Tooltip("Si está activo, cambiar de arma durante la secuencia la cancela.")]
    [SerializeField] private bool failOnSwitchWeaponInput = true;

    [Tooltip("Si falla la secuencia, limpia cualquier override temporal de arma.")]
    [SerializeField] private bool clearWeaponOverrideOnFail = true;

    [Header("UI")]
    [Tooltip("Offset en mundo para posicionar la barra sobre el player.")]
    [SerializeField] private Vector3 playerUIWorldOffset = new Vector3(0f, 1.4f, 0f);

    [Header("Advanced Orbit Reward")]
    [Tooltip("Si está activo, al completar la secuencia base el boomerang entra en fase de órbita en vez de dar un override de arma.")]
    [SerializeField] private bool useOrbitReward = true;

    [Min(0.05f)]
    [Tooltip("Duración de la órbita reward. Si Orbit Turns es mayor que 0, terminará al cumplirse cualquiera de las dos condiciones.")]
    [SerializeField] private float orbitDuration = 3.5f;

    [Min(0)]
    [Tooltip("Número de vueltas de órbita. 0 = ignorar y usar solo duración.")]
    [SerializeField] private int orbitTurns = 0;

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

    public bool IsValid()
    {
        return recallRule != null &&
               reflectRule != null &&
               dashRule != null &&
               recallWindowDuration > 0f &&
               returnToReflectDuration > 0f &&
               reflectWindowDuration > 0f &&
               requiredSuccessfulCycles > 0;
    }
}