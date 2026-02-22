using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    private Rigidbody2D rb;
    private PlayerInputReader input;

    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInputReader>();
    }

    private void OnEnable()
    {
        input.MoveEvent += SetMove;
    }

    private void OnDisable()
    {
        input.MoveEvent -= SetMove;
    }

    private void SetMove(Vector2 dir)
    {
        moveInput = dir;
    }

    private void FixedUpdate()
    {
        rb.velocity = moveInput * speed;
    }
}
