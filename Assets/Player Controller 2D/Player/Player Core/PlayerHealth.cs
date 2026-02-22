using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 10;

    [SerializeField] private int currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        //Debug.Log($"[Player] Health initialized: {currentHealth}");
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"[Player] Took {amount} damage. HP = {currentHealth}");

        if (currentHealth <= 0f)
            Die();
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
