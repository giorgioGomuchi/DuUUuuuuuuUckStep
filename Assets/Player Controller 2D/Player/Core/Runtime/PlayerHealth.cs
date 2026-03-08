using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Config")]
    [SerializeField] private PlayerConfigSO config;

    [Header("Runtime")]
    [SerializeField] private int currentHealth;

    [Header("Events")]
    [SerializeField] private UnityEvent<int, int> onHealthChanged;
    [SerializeField] private UnityEvent onDied;

    private bool invulnerable;
    private bool isDead;

    public int MaxHealth => config != null ? config.maxHealth : 10;
    public int CurrentHealth => currentHealth;
    public bool IsInvulnerable => invulnerable;
    public bool IsDead => isDead;

    public void SetConfig(PlayerConfigSO playerConfig)
    {
        config = playerConfig;
    }

    public void SetInvulnerable(bool value)
    {
        invulnerable = value;
    }

    private void Awake()
    {
        currentHealth = MaxHealth;
        NotifyHealthChanged();
    }

    public void TakeDamage(int amount)
    {
        if (isDead)
            return;

        if (invulnerable)
            return;

        if (amount <= 0)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        NotifyHealthChanged();

        if (currentHealth == 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (isDead)
            return;

        if (amount <= 0)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, MaxHealth);

        NotifyHealthChanged();
    }

    public void RestoreFullHealth()
    {
        if (isDead)
            return;

        currentHealth = MaxHealth;
        NotifyHealthChanged();
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        onDied?.Invoke();

        // - cambiar a DeadState
        // - bloquear input
        // - lanzar animación
    }

    private void NotifyHealthChanged()
    {
        onHealthChanged?.Invoke(currentHealth, MaxHealth);
    }
}