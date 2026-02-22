using UnityEngine;

public class PlayerVisualController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Transform weaponPivot;

    [SerializeField] private Transform mainWeaponPivot;
    [SerializeField] private Transform secondaryWeaponPivot;

    public void SetAim(Vector2 aimDirection)
    {
        if (aimDirection == Vector2.zero)
            return;

        bodyRenderer.flipX = aimDirection.x < 0;

    }

    private void RotateWeapon(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void FlipBody(Vector2 dir)
    {
        bool lookingRight = dir.x >= 0f;

        bodyRenderer.flipX = !lookingRight;

        Vector3 scale = weaponPivot.localScale;
        scale.y = lookingRight ? 1f : -1f;
        weaponPivot.localScale = scale;
    }
}
