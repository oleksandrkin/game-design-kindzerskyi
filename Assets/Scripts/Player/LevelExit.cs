using UnityEngine;
 
[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerController _)) return;
 
        // Увесь розрахунок «що далі» живе у Flow, а не тут
        Flow.NextLevel();
    }
}