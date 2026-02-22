using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy/Weapon Modifiers/Spread", fileName = "WM_Spread")]
public class SpreadModifierSO : WeaponModifierSO
{
    [Header("Spread (degrees)")]
    public float spreadAngle = 8f;

    public override void BeforeFire(ref WeaponFireContext ctx)
    {
        // Spread "sniper-style":
        // usando shotIndex/shotCount, distribuimos el ángulo dentro del spread total.
        // Si shotCount == 1, no alteramos.

        if (ctx.shotCount <= 1 || spreadAngle <= 0.001f)
            return;

        float t = (ctx.shotCount <= 1) ? 0.5f : (float)ctx.shotIndex / (ctx.shotCount - 1);
        float angleOffset = Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, t);

        Vector2 rotated = Rotate2D(ctx.baseDirection.normalized, angleOffset);

        ctx.ResetDirectionsToBase();
        ctx.directions[0] = rotated; // sustituimos la dirección base por la rotada
    }

    private static Vector2 Rotate2D(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);

        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }
}
