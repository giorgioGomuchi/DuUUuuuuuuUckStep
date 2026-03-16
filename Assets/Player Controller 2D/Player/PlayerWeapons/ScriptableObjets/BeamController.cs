using System.Collections.Generic;
using UnityEngine;

public class BeamController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerReferences playerReferences;
    [SerializeField] private WeaponOverrideController weaponOverride;

    [Header("Runtime")]
    [SerializeField] private bool debugLogs = false;

    private BeamView activeBeamView;
    private WeaponBehaviour activeWeapon;
    private BeamWeaponDataSO activeBeamData;

    private bool beamActive;
    private Vector2 smoothedDirection = Vector2.right;
    private float lastDamageTickTime;

    private readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[64];
    private readonly HashSet<Collider2D> damagedThisTick = new();

    public bool IsBeamActive => beamActive;

    private void Awake()
    {
        if (playerReferences == null)
            playerReferences = GetComponentInParent<PlayerReferences>();

        if (weaponOverride == null && playerReferences != null)
            weaponOverride = playerReferences.WeaponOverride;
    }

    private void Update()
    {
        if (!TryResolveActiveBeamWeapon(out WeaponBehaviour weapon, out BeamWeaponDataSO beamData))
        {
            StopBeam();
            return;
        }

        activeWeapon = weapon;
        activeBeamData = beamData;

        bool shouldBeamBeActive =
            playerReferences != null &&
            playerReferences.Input != null &&
            playerReferences.Input.FirePrimaryHeld;

        if (!shouldBeamBeActive)
        {
            if (weaponOverride != null)
                weaponOverride.SetDurationConsumptionActive(false);

            StopBeam();
            return;
        }

        StartBeamIfNeeded();

        if (weaponOverride != null)
            weaponOverride.SetDurationConsumptionActive(true);

        TickBeam(Time.deltaTime);
    }

    private bool TryResolveActiveBeamWeapon(out WeaponBehaviour weapon, out BeamWeaponDataSO beamData)
    {
        weapon = null;
        beamData = null;

        if (weaponOverride == null || !weaponOverride.IsOverrideActive)
            return false;

        if (weaponOverride.ActiveOverrideData is not BeamWeaponDataSO currentBeamData)
            return false;

        if (playerReferences == null || playerReferences.WeaponSlots == null)
            return false;

        weapon = playerReferences.WeaponSlots.GetWeaponBySlot(weaponOverride.CurrentOverrideSlot);
        if (weapon == null || weapon.WeaponData != currentBeamData)
            return false;

        beamData = currentBeamData;
        return true;
    }

    private void StartBeamIfNeeded()
    {
        if (beamActive)
            return;

        beamActive = true;
        smoothedDirection = ResolveTargetAim();
        lastDamageTickTime = Time.time;

        if (activeBeamView == null && activeBeamData != null && activeBeamData.beamViewPrefab != null)
        {
            GameObject beamGO = Instantiate(activeBeamData.beamViewPrefab, transform);
            activeBeamView = beamGO.GetComponent<BeamView>();

            if (activeBeamView == null)
            {
                Debug.LogWarning("[BeamController] BeamView prefab has no BeamView component.", beamGO);
                Destroy(beamGO);
            }
        }

        if (activeBeamView != null)
        {
            activeBeamView.SetWidths(activeBeamData.beamWidth, activeBeamData.beamEndWidth);
            activeBeamView.SetVisible(true);
        }

        if (debugLogs)
            Debug.Log("[BeamController] Beam ON.", this);
    }

    private void TickBeam(float deltaTime)
    {
        if (!beamActive || activeWeapon == null || activeBeamData == null)
            return;

        Vector2 targetDirection = ResolveTargetAim();
        float lerpT = 1f - Mathf.Exp(-activeBeamData.aimSmoothSpeed * Mathf.Max(0.0001f, deltaTime));
        smoothedDirection = Vector2.Lerp(smoothedDirection, targetDirection, lerpT).normalized;

        Vector2 start = activeWeapon.FirePoint != null
            ? (Vector2)activeWeapon.FirePoint.position
            : (Vector2)activeWeapon.transform.position;

        Vector2 end = ResolveBeamEndPoint(start, smoothedDirection, activeBeamData);

        if (activeBeamView != null)
        {
            activeBeamView.SetBeam(
                start,
                end,
                activeBeamData.visualSegments,
                activeBeamData.visualWaveAmplitude,
                activeBeamData.visualWaveFrequency,
                activeBeamData.visualWaveScrollSpeed,
                Time.time);
        }

        if (Time.time >= lastDamageTickTime + activeBeamData.damageTickInterval)
        {
            ApplyBeamDamage(start, end, activeBeamData);
            lastDamageTickTime = Time.time;
        }
    }

    private Vector2 ResolveTargetAim()
    {
        if (playerReferences != null &&
            playerReferences.Aim != null &&
            playerReferences.Aim.CurrentAim.sqrMagnitude > 0.0001f)
        {
            return playerReferences.Aim.CurrentAim.normalized;
        }

        return Vector2.right;
    }

    private Vector2 ResolveBeamEndPoint(Vector2 start, Vector2 direction, BeamWeaponDataSO data)
    {
        float distance = Mathf.Max(1f, data.maxRange);

        RaycastHit2D hit = Physics2D.Raycast(start, direction, distance, data.blockingMask);
        if (hit.collider != null)
            return hit.point;

        return start + direction * distance;
    }

    private void ApplyBeamDamage(Vector2 start, Vector2 end, BeamWeaponDataSO data)
    {
        damagedThisTick.Clear();

        Vector2 direction = end - start;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
            return;

        direction /= distance;

        int count = Physics2D.RaycastNonAlloc(
            start,
            direction,
            hitBuffer,
            distance,
            data.targetLayer);

        if (count <= 0)
            return;

        int applied = 0;

        for (int i = 0; i < count; i++)
        {
            Collider2D col = hitBuffer[i].collider;
            if (col == null || damagedThisTick.Contains(col))
                continue;

            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null)
                continue;

            damageable.TakeDamage(data.damagePerTick);
            damagedThisTick.Add(col);
            applied++;

            if (applied >= Mathf.Max(1, data.maxTargetsPerTick))
                break;
        }
    }

    public void StopBeam()
    {
        if (!beamActive)
            return;

        beamActive = false;

        if (weaponOverride != null)
            weaponOverride.SetDurationConsumptionActive(false);

        if (activeBeamView != null)
            activeBeamView.SetVisible(false);

        activeWeapon = null;
        activeBeamData = null;

        if (debugLogs)
            Debug.Log("[BeamController] Beam OFF.", this);
    }
}