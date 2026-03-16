using UnityEngine;

public class AimReticleView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerAim aim;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Follow")]
    [SerializeField] private bool smoothFollow = true;
    [SerializeField] private float smoothTime = 0.03f;

    private Vector3 velocity;

    private void Reset()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (aim == null)
            return;

        Vector3 targetPosition = aim.GetAimWorldPosition();

        if (smoothFollow)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime
            );
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    public void SetAim(PlayerAim playerAim)
    {
        aim = playerAim;
    }
}