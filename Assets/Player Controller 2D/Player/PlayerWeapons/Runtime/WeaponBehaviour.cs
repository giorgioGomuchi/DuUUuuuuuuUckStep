using UnityEngine;

[RequireComponent(typeof(Transform))]
public class WeaponBehaviour : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private WeaponDataSO weaponData;
    [SerializeField] private Transform firePoint;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer weaponVisual;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Vector2 currentAim = Vector2.right;
    private float nextFireTime;

    private float pendingDamageMultiplier = 1f;

    public WeaponDataSO WeaponData => weaponData;
    public float Cooldown => weaponData != null ? weaponData.cooldown : 0f;
    public bool UsesRhythmGate => weaponData != null && weaponData.useRhythmGate;

    public Transform FirePoint => firePoint;
    public Vector2 CurrentAim => currentAim;
    
    //only fire one boomerang 
    private bool attackLocked;

    private void Awake()
    {
        SetupVisual();
    }

    #region Setup

    private void SetupVisual()
    {
        if (weaponData == null)
        {
            Debug.LogError($"[{name}] WeaponData missing.", this);
            return;
        }

        if (weaponVisual == null)
            weaponVisual = GetComponentInChildren<SpriteRenderer>();

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

        if (weaponVisual.sprite == null && weaponData.weaponIcon != null)
            weaponVisual.sprite = weaponData.weaponIcon;
    }

    #endregion

    #region Public API

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
        if (weaponData == null || firePoint == null)
            return;

        if (Time.time < nextFireTime)
            return;

        if (IsAttackLocked)
        {
            if (debugLogs)
                Debug.Log("[WeaponBehaviour] Fire blocked: attack is locked (e.g., boomerang active).", this);
            return;
        }

        if (weaponData.attackModule == null)
        {
            Debug.LogError($"[{name}] WeaponData has no AttackModule assigned. weapon={weaponData.weaponName}", this);
            return;
        }

        //weaponData.attackModule.Execute(this, weaponData);

        bool didFire = weaponData.attackModule.Execute(this, weaponData);

        if (didFire)
        {
            nextFireTime = Time.time + weaponData.cooldown;
            ApplyCameraShake();
        }
        nextFireTime = Time.time + weaponData.cooldown;

        if (debugLogs)
            Debug.Log($"[WeaponBehaviour] Fired using module={weaponData.attackModule.name} weapon={weaponData.weaponName}", this);
    }

    public void SetNextAttackDamageMultiplier(float multiplier)
    {
        pendingDamageMultiplier = Mathf.Max(0.01f, multiplier);
    }

    public int ConsumeFinalDamage(int baseDamage)
    {
        int finalDamage = Mathf.RoundToInt(baseDamage * pendingDamageMultiplier);
        pendingDamageMultiplier = 1f;
        return finalDamage;
    }

    public void SetWeaponData(WeaponDataSO newData)
    {
        weaponData = newData;
        SetupVisual();
    }

    private void ApplyCameraShake()
    {
        if (weaponData == null) return;

        if (CameraShakeProvider.Instance != null)
        {
            CameraShakeProvider.Instance.Shake(
                weaponData.cameraShakeDuration,
                weaponData.cameraShakeStrength
            );

            if (debugLogs)
                Debug.Log($"[WeaponBehaviour] CameraShake applied duration={weaponData.cameraShakeDuration} strength={weaponData.cameraShakeStrength}", this);
        }
    }

    public void SetVisualVisible(bool visible)
    {
        if (weaponVisual != null)
            weaponVisual.enabled = visible;
    }

    public bool IsAttackLocked => attackLocked;

    public bool TryLockAttack()
    {
        if (attackLocked) return false;
        attackLocked = true;
        return true;
    }

    public void UnlockAttack()
    {
        attackLocked = false;
    }

    #endregion
}