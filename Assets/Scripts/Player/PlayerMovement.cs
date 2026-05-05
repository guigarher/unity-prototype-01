using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 movement;
    private Vector2 lastMoveDirection = Vector2.right;

    private InputSystem_Actions inputActions;
    private PlayerStats stats;

    public Vector2 LastMoveDirection => lastMoveDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();

        inputActions = new InputSystem_Actions();

        inputActions.Player.Move.performed += ctx => movement = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => movement = Vector2.zero;
    }

    void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Enable();
        }
    }

    void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }

    void FixedUpdate()
    {
        UpdateLastMoveDirection();

        float finalMoveSpeed = 5f;

        if (stats != null)
        {
            finalMoveSpeed = stats.moveSpeed;
        }

        rb.linearVelocity = movement * finalMoveSpeed;
    }

    void UpdateLastMoveDirection()
    {
        if (movement.sqrMagnitude <= 0.01f) return;

        if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
        {
            lastMoveDirection = movement.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            lastMoveDirection = movement.y > 0 ? Vector2.up : Vector2.down;
        }
    }
}