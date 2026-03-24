using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapons/Ranged/Boomerang")]
public class BoomerangWeaponDataSO : RangedWeaponDataSO
{
    [Header("Boomerang Sequence")]
    [Tooltip("Definición de la secuencia recall / return / reflect del boomerang.")]
    public BoomerangSequenceDefinitionSO sequenceDefinition;

    [Header("Outbound Flight")]
    [Tooltip("Distancia máxima que recorre al salir antes de perderse si no entra en secuencia.")]
    public float outboundDistance = 9.5f;

    [Header("Return Movement")]
    [Tooltip("Multiplicador base de velocidad durante la vuelta.")]
    public float returnSpeedMultiplier = 1.4f;

    [Tooltip("Qué tan agresivamente corrige la dirección para volver al target de presentación.")]
    public float returnSteering = 13f;

    [Tooltip("Distancia legacy a la que pasa a reflectable fuera del flujo timed.")]
    public float reflectableDistance = 1.15f;

    [Tooltip("Distancia de catch al owner en flujos legacy.")]
    public float catchDistance = 0.2f;

    [Tooltip("Qué rápido pierde velocidad cuando se queda en drift/lost.")]
    public float driftDeceleration = 18f;

    [Header("Timed Return Shape")]
    [Tooltip("Curvatura lateral durante la vuelta. Más alto = trayectoria más arqueada.")]
    public float timedReturnArcStrength = 0.08f;

    [Tooltip("Distancia delante del player a la que intenta colocarse antes del reflect.")]
    public float timedReturnPresentationDistance = 0.35f;

    [Header("Timed Return Speed")]
    [Tooltip("Multiplicador mínimo de velocidad durante la vuelta timed.")]
    public float timedReturnMinSpeedMultiplier = 1.15f;

    [Tooltip("Multiplicador máximo de velocidad durante la vuelta timed.")]
    public float timedReturnMaxSpeedMultiplier = 3.4f;

    [Tooltip("Qué rápido adapta su velocidad al tiempo restante.")]
    public float timedReturnSpeedSmoothing = 24f;

    [Header("Timed Return Assist")]
    [Tooltip("Radio alrededor del target donde puede quedarse presentado de forma estable.")]
    public float timedReturnHoldRadius = 1.1f;

    [Tooltip("Radio necesario para armar el estado reflectable.")]
    public float timedReturnReflectableRadius = 1.4f;

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
    
    [Header("Deflect Rules")]
    [Tooltip("Si está activo, solo se puede devolver/deflectar mientras está en fase de vuelta.")]
    public bool deflectOnlyWhileReturning = true;

    [Tooltip("Distancia que recorrerá tras un reflect/deflect antes de volver a cambiar de estado.")]
    public float outboundDistanceAfterDeflect = 4.5f;

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

    [Tooltip("Multiplicador global de velocidad de órbita. Útil para iterar rápido sin tocar la base.")]
    public float orbitSpeedMultiplier = 1f;

    [Tooltip("Sentido de giro de la órbita.")]
    public bool orbitClockwise = true;

    [Tooltip("Cooldown de daño por contacto mientras orbita.")]
    public float orbitContactDamageInterval = 0.2f;

}