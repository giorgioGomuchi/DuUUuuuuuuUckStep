using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int currentHealth = 10;

    private bool invulnerable;

    public void SetInvulnerable(bool value) => invulnerable = value;

    private void Awake()
    {
        currentHealth = maxHealth;
        //Debug.Log($"[Player] Health initialized: {currentHealth}");
    }

    public void TakeDamage(int amount)
    {
        if (invulnerable) return;

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            // TODO: muerte
        }
    }


    private void Die()
    {
        //Debug.Log("[Player] Died");
        // aquí luego:
        // - animación
        // - restart
        // - baksdjaklsdja`s
    }
}
