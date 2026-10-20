using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float leftLimit = -8f;
    [SerializeField] private float rightLimit = 8f;

    private Rigidbody2D rb;
    private float moveInput;

    [Header("Jump")]
    [SerializeField, Min(0.1f)] private float jumpHeight = 2.5f;
    private Collider2D bodyCollider;
    private ContactFilter2D groundFilter;
    private readonly ContactPoint2D[] groundContacts = new ContactPoint2D[8];
    private bool jumpRequested;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private GameObject parryHitbox;
    [SerializeField] private float parryDuration = 0.15f;
    private bool isParrying = false;
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        groundFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        groundFilter.useTriggers = false;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        if (Input.GetKeyDown(KeyCode.W))
            jumpRequested = true;

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
        if (Input.GetKeyDown(KeyCode.Space) && !isParrying)
        {
            StartCoroutine(ParryWindow());
        }
        
    }

    private void FixedUpdate()
    {
        // Let the Rigidbody resolve contacts without also teleporting its Transform.
        Vector2 velocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        if (jumpRequested && IsGrounded())
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            velocity.y = Mathf.Sqrt(2f * gravity * jumpHeight);
        }

        // Consume each key press once, including presses made in midair.
        jumpRequested = false;
        rb.linearVelocity = velocity;
    }

    private bool IsGrounded()
    {
        if (bodyCollider == null || rb.linearVelocity.y > 0.1f)
            return false;

        int count = bodyCollider.GetContacts(groundFilter, groundContacts);
        for (int i = 0; i < count; i++)
        {
            ContactPoint2D contact = groundContacts[i];
            Collider2D ground = contact.collider == bodyCollider ? contact.otherCollider : contact.collider;
            // Side walls, triggers, and destroyed floor blocks cannot grant a jump.
            if (ground != null && ground.enabled && ground.gameObject.activeInHierarchy && contact.normal.y >= 0.65f)
                return true;
        }

        return false;
    }

    private void OnDisable()
    {
        jumpRequested = false;
    }

    private IEnumerator ParryWindow()
        {
            isParrying = true;
            animator.SetTrigger("Parry");
            parryHitbox.SetActive(true);

            yield return new WaitForSeconds(parryDuration);

            animator.ResetTrigger("Parry");
            parryHitbox.SetActive(false);
            isParrying = false;
        }
}
