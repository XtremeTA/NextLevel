using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float leftLimit = -8f;
    [SerializeField] private float rightLimit = 8f;

    private Rigidbody2D rb;
    private float moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
    }

    private void FixedUpdate()
    {
        float nextX = Mathf.Clamp(rb.position.x + moveInput * moveSpeed * Time.fixedDeltaTime,
                                  leftLimit, rightLimit);
        rb.MovePosition(new Vector2(nextX, rb.position.y));
    }
}
