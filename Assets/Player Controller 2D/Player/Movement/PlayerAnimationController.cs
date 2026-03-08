using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement movement;

    [Header("Animator Parameters")]
    [SerializeField] private string speedParameter = "Speed";

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (movement == null) movement = GetComponentInParent<PlayerMovement>();

        if (animator == null || movement == null)
        {
            Debug.LogError("[PlayerAnimationController] Missing refs (Animator or PlayerMovement).");
            enabled = false;
        }
    }

    private void Update()
    {
        float speed = movement.CurrentVelocity.magnitude;
        animator.SetFloat(speedParameter, speed);
    }
}