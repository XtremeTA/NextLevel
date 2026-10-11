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

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private GameObject parryHitbox;
    [SerializeField] private float parryDuration = 0.15f;
    private bool isParrying = false;
    

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
        if (Input.GetKeyDown(KeyCode.Space) && !isParrying)
        {
            StartCoroutine(ParryWindow());
        }
        
    }

    private void FixedUpdate()
    {;
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        float clampedX = Mathf.Clamp(transform.position.x, leftLimit, rightLimit);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }

    private IEnumerator ParryWindow()
        {
            isParrying = true;
            parryHitbox.SetActive(true);

            yield return new WaitForSeconds(parryDuration);

            parryHitbox.SetActive(false);
            isParrying = false;
        }
}
