using UnityEngine;

public class DashVfxController : MonoBehaviour
{
    private TrailRenderer[] trails;

    private void Awake()
    {
        trails = GetComponentsInChildren<TrailRenderer>(true);

        foreach (var t in trails)
        {
            t.emitting = false;
            t.Clear();
        }
    }

    public void Play()
    {
        foreach (var t in trails)
        {
            t.Clear();        // Limpia buffer viejo
            t.emitting = false;
            t.transform.position = transform.position;
            t.emitting = true;
        }
    }

    public void Stop()
    {
        foreach (var t in trails)
        {
            t.emitting = false;
        }
    }

    public void ClearImmediately()
    {
        foreach (var t in trails)
        {
            t.Clear();
        }
    }
}