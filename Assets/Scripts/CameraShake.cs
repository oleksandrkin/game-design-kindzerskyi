using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 basePosition;
    private Coroutine current;
 
    private void Awake() => basePosition = transform.localPosition;
 
    public void Shake(float duration = 0.12f, float strength = 0.15f)
    {
        // Якщо попередня тряска ще йде — зупиняємо її, інакше вони складуться
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(ShakeRoutine(duration, strength));
    }
 
    private IEnumerator ShakeRoutine(float duration, float strength)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Затухання: сильна на початку, згасає до нуля
            float damper = 1f - (elapsed / duration);
 
            transform.localPosition = basePosition +
                                      (Vector3)(Random.insideUnitCircle * strength * damper);
 
            elapsed += Time.unscaledDeltaTime;   // працює при timeScale = 0
            yield return null;
        }
 
        transform.localPosition = basePosition;  // ОБОВʼЯЗКОВО повернути
        current = null;
    }

}
