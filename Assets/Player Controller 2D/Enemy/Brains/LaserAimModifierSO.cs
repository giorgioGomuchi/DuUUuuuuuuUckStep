using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy/Weapon Modifiers/Laser Aim", fileName = "WM_LaserAim")]
public class LaserAimModifierSO : WeaponModifierSO
{
    [Header("Laser")]
    public float blinkInterval = 0.1f;

    private float blinkTimer;
    private bool visible;

    public override void BeforeFire(ref WeaponFireContext ctx)
    {
        // No afecta al disparo; el laser se actualiza desde el Brain (Aim state).
    }

    public void UpdateLaser(EnemyWeapon weapon, Transform target)
    {
        if (weapon == null || target == null) return;

        var lr = weapon.GetComponentInChildren<LineRenderer>();
        if (lr == null) return;

        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0f)
        {
            blinkTimer = Mathf.Max(0.01f, blinkInterval);
            visible = !visible;
            lr.enabled = visible;
        }

        if (weapon.FirePoint != null)
        {
            lr.SetPosition(0, weapon.FirePoint.position);
            lr.SetPosition(1, target.position);
        }
    }

    public void DisableLaser(EnemyWeapon weapon)
    {
        if (weapon == null) return;
        var lr = weapon.GetComponentInChildren<LineRenderer>();
        if (lr != null) lr.enabled = false;
    }
}
