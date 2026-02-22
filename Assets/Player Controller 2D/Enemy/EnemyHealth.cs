using System;
using UnityEngine;

[RequireComponent(typeof(EnemyController))]
public class EnemyHealth : MonoBehaviour, IDamageable, IKnockbackable
{
    public event Action<EnemyHealth> OnEnemyDied;



    [Header("Stats")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;



    [Header("Death")]
    [SerializeField] private float destroyDelay = 0.35f;



    private bool isDead;

    private EnemyController controller;
    private Rigidbody2D rb;

    private void Awake()
    {
        controller = GetComponent<EnemyController>();
        rb = GetComponent<Rigidbody2D>();

        // Si tienes data, la usamos como fuente
        if (controller != null && controller.data != null)
        {
            if (controller.data.maxHealth > 0) maxHealth = (int)controller.data.maxHealth;
        }

        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        controller?.PlayHit();

        controller.LockMovement(0.15f);

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    public void ApplyKnockback(Vector2 direction, float force)
    {

        Debug.Log($"[Knockback] Called. Dir={direction} Force={force}", this);

        if (isDead) return;

        if (controller == null)
            controller = GetComponent<EnemyController>();

        if (controller == null) return;

        controller.ApplyKnockback(direction, force, 0.15f);

    }



    private void Die()
    {
        isDead = true;

        controller?.PlayDie();

        // OJO: si quieres que el collider deje de molestar:
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;


        //NOTIFICAR AL LEVEL SYSTEM
        OnEnemyDied?.Invoke(this);


        Destroy(gameObject, destroyDelay);
    }



}
