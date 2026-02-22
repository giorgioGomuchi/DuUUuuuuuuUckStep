using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    private Coroutine shakeRoutine;

    public void Shake(float duration, float strength)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, strength));
    }

    private void Update()
    {
        
    }

    private IEnumerator ShakeRoutine(float duration, float strength)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * strength;
            float offsetY = Random.Range(-1f, 1f) * strength;

            // 🔴 REPOSO SIEMPRE ES (0,0,0)
            transform.localPosition = new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 🔴 Volvemos exactamente al centro
        transform.localPosition = Vector3.zero;
        shakeRoutine = null;
    }
}
