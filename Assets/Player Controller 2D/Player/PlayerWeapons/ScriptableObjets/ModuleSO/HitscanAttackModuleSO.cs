using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/AttackModules/Hitscan")]
public class HitscanAttackModuleSO : AttackModuleSO
{
    [SerializeField] private bool debugLogs = false;

    public override bool Execute(WeaponBehaviour weapon, WeaponDataSO data)
    {
        if (weapon == null || data == null)
            return false;

        if (data is not HitscanWeaponDataSO hitscanData)
        {
            Debug.LogError("[HitscanAttackModuleSO] Wrong WeaponData type.", weapon);
            return false;
        }

        Vector2 origin = weapon.FirePoint.position;
        Vector2 direction = weapon.CurrentAim.sqrMagnitude > 0.0001f ? weapon.CurrentAim.normalized : Vector2.right;
        float range = Mathf.Max(0.1f, hitscanData.range);

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, range, hitscanData.targetLayer);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        int finalDamage = weapon.ConsumeFinalDamage(hitscanData.damage);

        int targetsDamaged = 0;
        Vector2 finalPoint = origin + direction * range;

        PlayerReferences playerRefs = weapon.GetComponentInParent<PlayerReferences>();
        WeaponSequenceControllerV2 sequenceController = playerRefs != null ? playerRefs.WeaponSequenceControllerV2 : null;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D other = hits[i].collider;
            if (other == null)
                continue;

            finalPoint = hits[i].point;

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(finalDamage);
                targetsDamaged++;

                sequenceController?.RegisterSequenceHit(other);

                if (debugLogs)
                    Debug.Log($"[HitscanAttackModuleSO] Hit -> {other.name} dmg={finalDamage}", weapon);

                if (targetsDamaged >= Mathf.Max(1, hitscanData.maxTargets))
                    break;
            }

            if (hitscanData.stopAtFirstCollider)
                break;
        }

        SpawnTracer(hitscanData, origin, finalPoint);

        if (debugLogs)
        {
            Debug.Log(
                $"[HitscanAttackModuleSO] Fired hitscan | weapon={hitscanData.weaponName} | range={range} | hits={targetsDamaged}",
                weapon);
        }

        return true;
    }

    private void SpawnTracer(HitscanWeaponDataSO data, Vector2 origin, Vector2 end)
    {
        if (data == null || data.tracerPrefab == null)
            return;

        GameObject tracerGO = Instantiate(data.tracerPrefab, Vector3.zero, Quaternion.identity);
        HitscanTracerVFX tracer = tracerGO.GetComponent<HitscanTracerVFX>();

        if (tracer == null)
        {
            Debug.LogWarning("[HitscanAttackModuleSO] tracerPrefab has no HitscanTracerVFX component.", tracerGO);
            Destroy(tracerGO);
            return;
        }

        float widthMultiplier = 1f;
        if (data is LaserWeaponDataSO laserData)
            widthMultiplier = Mathf.Max(0.1f, laserData.tracerWidthMultiplier);

        tracer.Play(origin, end, data.tracerDuration, widthMultiplier);
    }
}