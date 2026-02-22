using UnityEngine;

[RequireComponent(typeof(Transform))]
public class WeaponBehaviour : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private WeaponDataSO weaponData;
    [SerializeField] private Transform firePoint;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer weaponVisual;

    private Vector2 currentAim = Vector2.right;
    private float nextFireTime;

    [Header("Rhythm Combo")]
    [SerializeField] private float firstSecondCooldown = 2f;
    [SerializeField] private float thirdCooldown = 3f;
    [SerializeField] private int maxComboSteps = 3;

    [Header("Rhythm Settings")]
    [SerializeField] private float rhythmTolerance = 0.3f;
    [SerializeField] private float perfectTolerance = 0.1f;
    [SerializeField] private float perfectDamageMultiplier = 1.75f;

    [SerializeField] private SpriteRenderer rhythmFeedbackRenderer;
    [SerializeField] private Color goodColor = Color.white;
    [SerializeField] private Color perfectColor = Color.yellow;
    [SerializeField] private Color failColor = Color.red;
    [SerializeField] private float feedbackFlashTime = 0.15f;

    private int currentComboStep = 0;
    private float nextAllowedAttackTime = 0f;
    public float Cooldown => weaponData.cooldown;

    private void Awake()
    {
        SetupVisual();

    }

    private void SetupVisual()
    {
        if (weaponData == null)
        {
            Debug.LogError($"[{name}] WeaponData missing.");
            return;
        }

        // Si no hay referencia al SpriteRenderer, lo buscamos
        if (weaponVisual == null)
        {
            weaponVisual = GetComponentInChildren<SpriteRenderer>();
        }

        // Si sigue sin existir, lo creamos
        if (weaponVisual == null)
        {
            GameObject visualGO = new GameObject("WeaponVisual");
            visualGO.transform.SetParent(transform);
            visualGO.transform.localPosition = Vector3.zero;
            visualGO.transform.localRotation = Quaternion.identity;

            weaponVisual = visualGO.AddComponent<SpriteRenderer>();
            weaponVisual.sortingLayerName = "Default";
            weaponVisual.sortingOrder = 11;
        }

        // Si no tiene sprite asignado, usamos el del WeaponData
        if (weaponVisual.sprite == null && weaponData.weaponIcon != null)
        {
            weaponVisual.sprite = weaponData.weaponIcon;
        }
    }

    public void SetAim(Vector2 direction)
    {
        if (direction == Vector2.zero)
            return;

        currentAim = direction.normalized;

        float angle = Mathf.Atan2(currentAim.y, currentAim.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void TryFire()
    {
        if (weaponData is MeleeAnimatedWeaponDataSO)
        {
            Fire();
            return;
        }

        // Solo armas ranged usan cooldown normal
        if (Time.time < nextFireTime)
            return;

        Fire();
        nextFireTime = Time.time + weaponData.cooldown;
    }

    private void Fire()
    {
        switch (weaponData)
        {
            case RangedWeaponDataSO ranged:
                FireRanged(ranged);
                break;

            case MeleeAnimatedWeaponDataSO melee:
                FireMelee(melee);
                break;
        }
    }

    private void FireRanged(RangedWeaponDataSO data)
    {
        GameObject proj = Instantiate(
            data.projectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        var projectile = proj.GetComponent<KinematicProjectile2D>();
        projectile.Initialize(
            currentAim,
            data.projectileSpeed,
            data.damage,
            data.targetLayer
        );

        if (data is ExplosiveWeaponDataSO explosiveData)
        {
            var explosive = proj.GetComponent<ExplosiveProjectile>();
            if (explosive != null)
            {
                explosive.ConfigureExplosion(
                    explosiveData.explosionRadius,
                    explosiveData.explosionDamage,
                    explosiveData.explosionForce,
                    explosiveData.explosionPrefab
                );
            }
        }
    }

    private void FireMelee(MeleeAnimatedWeaponDataSO data)
    {

        float timeUntilNext = nextAllowedAttackTime - Time.time;

        if (currentComboStep == 0)
        {
            ExecuteRhythmAttack(data, 1f);
            return;
        }

        float absTiming = Mathf.Abs(timeUntilNext);

        if (absTiming <= perfectTolerance)
        {
            // PERFECT
            ExecuteRhythmAttack(data, perfectDamageMultiplier);
            TriggerFeedback(perfectColor);
        }
        else if (absTiming <= rhythmTolerance)
        {
            // GOOD
            ExecuteRhythmAttack(data, 1f);
            TriggerFeedback(goodColor);
        }
        else
        {
            // FAIL
            TriggerFeedback(failColor);
            ResetCombo();
        }
        /*
        var controller = Instantiate(data.hitPrefab, firePoint.position, transform.rotation).GetComponent<MeleeHitController>();

        controller.Initialize(
            data.damage,
            data.targetLayer,
            data.hitLifetime,
            data.knockbackForce
        );
        */
    }


    private void ExecuteRhythmAttack(MeleeAnimatedWeaponDataSO data, float damageMultiplier)
    {
        currentComboStep++;

        if (currentComboStep > maxComboSteps)
            currentComboStep = 1;

        var controller = Instantiate(
            data.hitPrefab,
            firePoint.position,
            transform.rotation
        ).GetComponent<MeleeHitController>();

        int finalDamage = Mathf.RoundToInt(data.damage * damageMultiplier);

        controller.Initialize(
            finalDamage,
            data.targetLayer,
            data.hitLifetime,
            data.knockbackForce
        );

        float cooldown = GetCooldownForCurrentStep();
        nextAllowedAttackTime = Time.time + cooldown;

        Debug.Log($"[WeaponBehaviour] Step {currentComboStep} | x{damageMultiplier}");
    }
    private void TriggerFeedback(Color flashColor)
    {
        if (rhythmFeedbackRenderer == null)
            return;

        StopAllCoroutines();
        StartCoroutine(FlashFeedback(flashColor));
    }

    private System.Collections.IEnumerator FlashFeedback(Color flashColor)
    {
        Color original = rhythmFeedbackRenderer.color;

        rhythmFeedbackRenderer.color = flashColor;
        yield return new WaitForSeconds(feedbackFlashTime);

        rhythmFeedbackRenderer.color = original;
    }
    private float GetCooldownForCurrentStep()
    {
        return currentComboStep == maxComboSteps
        ? thirdCooldown
        : firstSecondCooldown;
    }

    private void ResetCombo()
    {
        currentComboStep = 0;
        Debug.Log("[WeaponBehaviour] Combo Reset");
    }
}
