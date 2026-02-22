using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animator Parameters")]
    [SerializeField] private string speedParameter = "Speed";


    private void Awake()
    {

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
        float speed = PlayerInputReader.MoveInput.magnitude;
        animator.SetFloat(speedParameter, speed);
    }
}
