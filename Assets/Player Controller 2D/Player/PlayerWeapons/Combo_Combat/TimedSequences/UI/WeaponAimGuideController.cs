using UnityEngine;

public class WeaponAimGuideController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerReferences playerReferences;
    [SerializeField] private AimGuideView aimGuideView;

    [Header("Guide")]
    [SerializeField] private bool guideActive;
    [SerializeField] private LayerMask blockingMask;
    [SerializeField] private float maxDistance = 40f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    public bool IsGuideActive => guideActive;

    private void Awake()
    {
        if (playerReferences == null)
            playerReferences = GetComponentInParent<PlayerReferences>();

        if (aimGuideView == null)
            aimGuideView = GetComponentInChildren<AimGuideView>(true);

        HideGuide();
    }

    private void LateUpdate()
    {
        if (!guideActive || playerReferences == null || aimGuideView == null)
            return;

        Transform firePoint = ResolveFirePoint();
        if (firePoint == null)
            return;

        Vector2 start = firePoint.position;
        Vector2 direction = ResolveAimDirection();
        Vector2 end = ResolveEndPoint(start, direction);

        aimGuideView.SetLine(start, end);
    }

    public void ShowGuide()
    {
        guideActive = true;
        aimGuideView?.Show();

        if (debugLogs)
            Debug.Log("[WeaponAimGuideController] Aim guide ON.", this);
    }

    public void HideGuide()
    {
        guideActive = false;
        aimGuideView?.Hide();

        if (debugLogs)
            Debug.Log("[WeaponAimGuideController] Aim guide OFF.", this);
    }

    public void FlashShot()
    {
        if (!guideActive)
            return;

        aimGuideView?.FlashShot();
    }

    private Transform ResolveFirePoint()
    {
        if (playerReferences == null || playerReferences.WeaponSlots == null)
            return null;

        WeaponBehaviour mainWeapon = playerReferences.WeaponSlots.MainWeapon;
        if (mainWeapon == null || mainWeapon.FirePoint == null)
            return null;

        return mainWeapon.FirePoint;
    }

    private Vector2 ResolveAimDirection()
    {
        if (playerReferences != null && playerReferences.Aim != null && playerReferences.Aim.CurrentAim.sqrMagnitude > 0.0001f)
            return playerReferences.Aim.CurrentAim.normalized;

        return Vector2.right;
    }

    private Vector2 ResolveEndPoint(Vector2 start, Vector2 direction)
    {
        float distance = Mathf.Max(0.1f, maxDistance);

        RaycastHit2D hit = Physics2D.Raycast(start, direction, distance, blockingMask);
        if (hit.collider != null)
            return hit.point;

        return start + direction * distance;
    }
}