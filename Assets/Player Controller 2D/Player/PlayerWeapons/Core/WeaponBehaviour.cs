using System;
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
    private bool attackLocked;

    public event Action<WeaponBehaviour, WeaponDataSO> OnWeaponDataChanged;

    public WeaponDataSO WeaponData => weaponData;
    public string WeaponName => weaponData != null ? weaponData.weaponName : "None";
    public Transform FirePoint => firePoint;
    public Vector2 CurrentAim => currentAim;
    public bool IsAttackLocked => attackLocked;

    private void Awake()
    {
        EnsureVisualExists();
        RefreshVisualFromData();
    }

    private void EnsureVisualExists()
    {
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
    }

    private void RefreshVisualFromData()
    {
        if (weaponData == null)
        {
            Debug.LogError($"[{name}] WeaponData missing.", this);
            return;
        }

        EnsureVisualExists();
        weaponVisual.sprite = weaponData.weaponIcon;

        if (debugLogs)
            Debug.Log($"[WeaponBehaviour] Visual refreshed -> {weaponData.weaponName}", this);
    }

    public void SetAim(Vector2 direction)
    {
        if (direction == Vector2.zero)
            return;

        currentAim = direction.normalized;

        float angle = Mathf.Atan2(currentAim.y, currentAim.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public bool TryFire()
    {
        if (weaponData == null || firePoint == null)
            return false;

        if (attackLocked)
            return false;

        if (!CanFireByCadence())
            return false;

        if (weaponData.attackModule == null)
        {
            Debug.LogError($"[{name}] WeaponData has no AttackModule assigned. weapon={weaponData.weaponName}", this);
            return false;
        }

        bool didFire = weaponData.attackModule.Execute(this, weaponData);
        if (!didFire)
            return false;

        CommitCadenceAfterFire();
        ApplyCameraShake();
        return true;
    }

    private bool CanFireByCadence()
    {
        if (weaponData == null)
            return false;

        switch (weaponData.cadenceMode)
        {
            case WeaponCadenceMode.Continuous:
                return true;

            case WeaponCadenceMode.ExternalCadence:
                return true;

            case WeaponCadenceMode.InternalCooldown:
            default:
                return Time.time >= nextFireTime;
        }
    }

    private void CommitCadenceAfterFire()
    {
        if (weaponData == null)
            return;

        if (weaponData.cadenceMode == WeaponCadenceMode.InternalCooldown)
            nextFireTime = Time.time + weaponData.cooldown;
    }

    public void ResetInternalCooldown()
    {
        nextFireTime = 0f;
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
        if (newData == null)
        {
            Debug.LogWarning($"[{name}] Tried to assign null WeaponData.", this);
            return;
        }

        weaponData = newData;
        ResetInternalCooldown();
        RefreshVisualFromData();
        OnWeaponDataChanged?.Invoke(this, weaponData);
    }

    private void ApplyCameraShake()
    {
        if (weaponData == null)
            return;

        if (CameraShakeProvider.Instance != null)
        {
            CameraShakeProvider.Instance.Shake(
                weaponData.cameraShakeDuration,
                weaponData.cameraShakeStrength
            );
        }
    }

    public void SetVisualVisible(bool visible)
    {
        if (weaponVisual != null)
            weaponVisual.enabled = visible;
    }

    public bool TryLockAttack()
    {
        if (attackLocked)
            return false;

        attackLocked = true;
        return true;
    }

    public void UnlockAttack()
    {
        attackLocked = false;
    }

    public void CancelAttack()
    {
        attackLocked = false;

        if (weaponData != null && weaponData.attackModule is ICancelableAttackModule cancelable)
            cancelable.Cancel(this, weaponData);
    }
}