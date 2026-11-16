using UnityEngine;
 
public class PlayerHealth : Health
{
    [SerializeField] private PlayerRespawn respawn;   // з лабораторної 4
 
    [Header("Канали подій")]
    [SerializeField] private IntEventSO healthChanged;
    [SerializeField] private IntEventSO maxHealthChanged;
 
    // Start, а НЕ Awake: усі OnEnable у сцені вже відпрацювали,
    // тобто HUD уже підписався і стартові числа до нього дійдуть
    private void Start()
    {
        maxHealthChanged.Raise(Max);
        healthChanged.Raise(Current);
    }
 
    protected override void OnHealthChanged() => healthChanged.Raise(Current);
    
    protected override void Die()
    {
        // Гравець не знищується — повертається на контрольну точку
        respawn.Respawn();
        ResetHealth();
    }
}