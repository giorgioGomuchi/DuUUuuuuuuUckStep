using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapons/Ranged/Boomerang")]
public class BoomerangWeaponDataSO : RangedWeaponDataSO
{
    [Header("Boomerang Sequence")]
    [Tooltip("Definición de la secuencia recall / return / reflect del boomerang.")]
    public BoomerangSequenceDefinitionSO sequenceDefinition;

    [Header("Outbound Flight")]
    [Tooltip("Distancia máxima que recorre al salir antes de perderse si no entra en secuencia.")]
    public float outboundDistance = 12f;

    [Header("Return Mode")]
    [Tooltip("Si está activo, la vuelta hasta reflect tarda SIEMPRE exactamente Return To Reflect Duration. Si está apagado, usa la lógica física/realista.")]
    public bool useFixedReturnToReflect = true;

    [Header("Return Movement")]
    [Tooltip("Multiplicador base de velocidad durante la vuelta física/realista.")]
    public float returnSpeedMultiplier = 1.4f;

    [Tooltip("Qué tan agresivamente corrige la dirección en modos físicos/realistas.")]
    public float returnSteering = 13f;

    [Tooltip("Distancia legacy a la que pasa a reflectable fuera del flujo timed.")]
    public float reflectableDistance = 1.15f;

    [Tooltip("Distancia de catch al owner en flujos legacy.")]
    public float catchDistance = 0.3f;

    [Tooltip("Qué rápido pierde velocidad cuando se queda en drift/lost.")]
    public float driftDeceleration = 18f;

    [Header("Timed Return Shape")]
    [Tooltip("Curvatura lateral durante la vuelta. Más alto = trayectoria más arqueada.")]
    public float timedReturnArcStrength = 0.08f;

    [Tooltip("Distancia delante del player a la que intenta colocarse antes del reflect.")]
    public float timedReturnPresentationDistance = 0.45f;

    [Header("Timed Return Speed")]
    [Tooltip("Solo relevante en modo físico/realista.")]
    public float timedReturnMinSpeedMultiplier = 1.15f;

    [Tooltip("Solo relevante en modo físico/realista.")]
    public float timedReturnMaxSpeedMultiplier = 3.4f;

    [Tooltip("Solo relevante en modo físico/realista.")]
    public float timedReturnSpeedSmoothing = 24f;

    [Header("Timed Return Assist")]
    [Tooltip("Radio alrededor del target donde puede quedarse presentado de forma estable.")]
    public float timedReturnHoldRadius = 1.0f;

    [Tooltip("Radio necesario para armar el estado reflectable en modo físico/realista.")]
    public float timedReturnReflectableRadius = 1.35f;

    [Header("Reflect Hold")]
    [Tooltip("Si está activo, cuando el boomerang entra en reflectable se queda en el centro del player para poder devolverlo desde cualquier dirección.")]
    public bool holdReflectAtOwnerCenter = true;

    [Header("Deflect Rules")]
    [Tooltip("Si está activo, solo se puede devolver/deflectar mientras está en fase de vuelta.")]
    public bool deflectOnlyWhileReturning = true;

    [Tooltip("Distancia que recorrerá tras un reflect/deflect antes de volver a cambiar de estado.")]
    public float outboundDistanceAfterDeflect = 4.5f;

    [Header("Projectile Interaction")]
    [Tooltip("Layers de proyectiles enemigos que el boomerang destruye al tocarlos.")]
    public LayerMask destroyEnemyProjectileMask;

    [Header("Spin")]
    [Tooltip("Velocidad visual de giro del sprite del boomerang.")]
    public float spinDegPerSec = 720f;

    [Header("Reflectable Feedback")]
    [Tooltip("Color de feedback cuando el boomerang ya puede ser devuelto.")]
    public Color reflectableColor = new Color(1f, 0.9f, 0.2f, 1f);

    [Tooltip("Duración del flash visual al entrar en reflectable.")]
    public float reflectableFlashDuration = 0.12f;

    [Header("Dash Bonuses")]
    [Tooltip("Bonus temporal al multiplicador de velocidad de vuelta si aciertas dash en recall.")]
    public float dashReturnSpeedMultiplierBonus = 0.35f;

    [Tooltip("Bonus temporal al steering si aciertas dash en recall.")]
    public float dashReturnSteeringBonus = 5f;

    [Tooltip("Bonus al reflect si aciertas dash en ventana de reflect.")]
    public float dashReflectSpeedMultiplierBonus = 1.35f;

    [Header("Advanced Orbit Reward")]
    [Tooltip("Radio inicial de la órbita al empezar el reward.")]
    public float orbitStartRadius = 0.8f;

    [Tooltip("Cuánto crece el radio de la órbita por segundo.")]
    public float orbitRadiusGrowthPerSecond = 1f;

    [Tooltip("Radio máximo que puede alcanzar la órbita.")]
    public float orbitMaxRadius = 3.5f;

    [Tooltip("Velocidad angular base de la órbita en grados por segundo.")]
    public float orbitAngularSpeedDegPerSec = 360f;

    [Tooltip("Multiplicador global de velocidad de órbita.")]
    public float orbitSpeedMultiplier = 1f;

    [Tooltip("Sentido de giro de la órbita.")]
    public bool orbitClockwise = true;

    [Tooltip("Cooldown de daño por contacto mientras orbita.")]
    public float orbitContactDamageInterval = 0.2f;

    [Header("Orbit Start Feedback")]
    [Tooltip("Color de flash al empezar la órbita.")]
    public Color orbitStartFlashColor = new Color(0.3f, 1f, 1f, 1f);

    [Tooltip("Duración del flash de entrada a órbita.")]
    public float orbitStartFlashDuration = 0.18f;

    [Tooltip("Escala máxima temporal al empezar la órbita.")]
    public float orbitStartPulseScaleMultiplier = 1.35f;

    [Tooltip("Duración del pulso de escala al empezar la órbita.")]
    public float orbitStartPulseDuration = 0.2f;
}