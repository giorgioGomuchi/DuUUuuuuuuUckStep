using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animator Parameters")]
    [SerializeField] private string speedParameter = "Speed";

    private PlayerInputReader input;

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();

        if (animator == null)
        {
            Debug.LogError("[PlayerAnimationController] Animator reference is missing.");
            enabled = false;
            return;
        }

        Debug.Log("[PlayerAnimationController] Initialized");
    }

    private void Update()
    {
        float speed = input.MoveInput.magnitude;
        animator.SetFloat(speedParameter, speed);
    }
}
