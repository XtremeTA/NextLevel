using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float leftLimit = -8f;
    [SerializeField] private float rightLimit = 8f;

    private Rigidbody2D rb;
    private float moveInput;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        if (moveInput == 0)
        {
            animator.SetBool("isRunning", false);
        } else
        {
            animator.SetBool("isRunning", true);
            if (moveInput > 0)
            {
            spriteRenderer.flipX = false;
            }
            else if (moveInput < 0)
            {
            spriteRenderer.flipX = true;
            }
        }
        
    }

    private void FixedUpdate()
    {;
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        float clampedX = Mathf.Clamp(transform.position.x, leftLimit, rightLimit);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }
}
