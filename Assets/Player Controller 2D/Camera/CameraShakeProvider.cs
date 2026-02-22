using UnityEngine;

public class CameraShakeProvider : MonoBehaviour
{
    public static CameraShakeProvider Instance { get; private set; }

    [SerializeField] private CameraShake cameraShake;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Shake(float duration, float strength)
    {
        if (cameraShake == null) return;
        if (duration <= 0f || strength <= 0f) return;

        cameraShake.Shake(duration, strength);
    }
}
