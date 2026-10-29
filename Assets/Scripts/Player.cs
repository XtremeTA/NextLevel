using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Tooltip("Horizontal movement speed at the start of a run.")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 8f;
    [SerializeField, Min(0.1f)] private float maximumMoveSpeed = 15f;
    [SerializeField] private float leftLimit = -8f;
    [SerializeField] private float rightLimit = 8f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.5f;

    private bool isDashing;
    private float lastDashTime;
    private Coroutine dashRoutine;

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

    public float FacingDirection => spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
    public float MovementAmount => Mathf.Clamp01(Mathf.Abs(moveInput));
    private float CurrentMoveSpeed => Mathf.Lerp(moveSpeed, Mathf.Max(moveSpeed, maximumMoveSpeed),
        GameManager.Instance != null ? GameManager.Instance.SpeedRampProgress : 0f);
    public bool IsPerfectParry => isParrying && Time.time - parryStartedAt <= perfectParryWindow;
    public bool IsHitStunned => isHitStunned || isRecovering;

    public bool IsDashing => isDashing;

    [Header("Parry")]
    [SerializeField] private GameObject parryHitbox;
    [SerializeField] private float parryDuration = 0.15f;
    [SerializeField, Min(0f)] private float perfectParryWindow = 0.06f;
    private bool isParrying = false;
    private float parryStartedAt = float.NegativeInfinity;
    private Coroutine parryRoutine;

    [Header("Enemy Hits")]
    [Tooltip("How long knockback pushes the player. The hurt flash and stun last as long as the Hurt sound.")]
    [SerializeField, Min(0f)] private float hitStunDuration = 0.25f;
    [Tooltip("Extra invulnerable time after the hurt stun ends.")]
    [SerializeField, Min(0f)] private float hitInvulnerability = 1f;
    [SerializeField] private Vector2 enemyKnockback = new Vector2(4f, 3f);
    private bool isHitStunned;
    private float invulnerableUntil;
    private Coroutine enemyHitRoutine;
    private Coroutine fallRoutine;
    private static readonly int HurtState = Animator.StringToHash("Base Layer.PlayerHurt");

    private bool HurtAnimationActive => animator != null &&
        (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == HurtState ||
         (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).fullPathHash == HurtState));

    // The red/white flash runs for as long as the hurt sound is audible.
    private float HurtDuration => Mathf.Max(hitStunDuration,
        GameAudio.Instance != null ? GameAudio.Instance.GetAudibleLength(SoundCue.Hurt) : 0f);
    private static bool GameOver => GameManager.Instance != null && GameManager.Instance.IsGameOver;
    private bool ControlsLocked => isRecovering || isHitStunned || HurtAnimationActive || isDashing ||
        (GameManager.Instance != null && GameManager.Instance.IsGameOver);

    [Header("Fall Recovery")]
    [SerializeField, Min(0f)] private float catchPause = 0.15f;
    [SerializeField, Min(0.1f)] private float liftSpeed = 6f;
    [SerializeField, Min(0f)] private float hoverHeight = 1f;
    private Collider2D lastGround;
    private Vector2 lastGroundCenter;
    private float lastGroundTop;
    private Vector2 startPosition;
    private bool isRecovering;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        groundFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        groundFilter.useTriggers = false;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = rb.position;
        lastGroundCenter = startPosition;
        // No fall can be detected until the player has first touched ground.
        lastGroundTop = float.NegativeInfinity;
    }

    private void Update()
    {
        HandleInput(Input.GetAxisRaw("Horizontal"), Input.GetKeyDown(KeyCode.W), Input.GetKeyDown(KeyCode.Space), Input.GetKeyDown(KeyCode.LeftShift));
    }

    private void HandleInput(float horizontal, bool jumpPressed, bool parryPressed, bool dashPressed)
    {
        if (ControlsLocked)
        {
            moveInput = 0f;
            jumpRequested = false;
            return;
        }

        moveInput = horizontal;
        if (jumpPressed)
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
        if (parryPressed && !isParrying)
        {
            parryRoutine = StartCoroutine(ParryWindow());
        }
        if (dashPressed && !isDashing && Time.time >= lastDashTime + dashCooldown)
        {
            dashRoutine = StartCoroutine(Dash());
        }
    }

    private void LateUpdate()
    {
        if (!isHitStunned && !isRecovering)
            return;

        // Hurt owns the final displayed pose even if a movement/parry transition was queued this frame.
        moveInput = 0f;
        jumpRequested = false;
        animator.SetBool("isRunning", false);
        animator.ResetTrigger("Parry");
        animator.SetBool("isHurt", true);
        if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash != HurtState || animator.IsInTransition(0))
            animator.Play(HurtState, 0, 0f);
        animator.Update(0f);
    }

    private void PlayHurtImmediately()
    {
        animator.SetBool("isRunning", false);
        animator.ResetTrigger("Parry");
        animator.SetBool("isHurt", true);
        animator.Play(HurtState, 0, 0f);
        // A hit may arrive after the normal animation update; apply its sprite before rendering.
        animator.Update(0f);
    }

    private void FixedUpdate()
    {
        if (isDashing)
            return;
        if (isRecovering || (GameManager.Instance != null && GameManager.Instance.IsGameOver))
            return;

        Collider2D ground = FindGround();
        if (ground != null)
        {
            lastGround = ground;
            lastGroundCenter = ground.bounds.center;
            lastGroundTop = ground.bounds.max.y;
        }
        else if (bodyCollider.bounds.max.y < lastGroundTop)
        {
            // The whole body has dropped below the floor surface, so it fell through a gap.
            // A new fall replaces any hurt still in progress, so its timers can't end this one early.
            StopHurtRoutines();
            fallRoutine = StartCoroutine(RecoverFromFall());
            return;
        }

        if (isHitStunned || HurtAnimationActive)
        {
            moveInput = 0f;
            jumpRequested = false;
            return;
        }

        // Let the Rigidbody resolve contacts without also teleporting its Transform.
        Vector2 velocity = new Vector2(moveInput * CurrentMoveSpeed, rb.linearVelocity.y);
        if (jumpRequested && ground != null)
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            velocity.y = Mathf.Sqrt(2f * gravity * jumpHeight);
        }

        // Consume each key press once, including presses made in midair.
        jumpRequested = false;
        rb.linearVelocity = velocity;
    }

    private Collider2D FindGround()
    {
        if (bodyCollider == null || rb.linearVelocity.y > 0.1f)
            return null;

        int count = bodyCollider.GetContacts(groundFilter, groundContacts);
        for (int i = 0; i < count; i++)
        {
            ContactPoint2D contact = groundContacts[i];
            Collider2D ground = contact.collider == bodyCollider ? contact.otherCollider : contact.collider;
            // Side walls, triggers, and destroyed floor blocks cannot grant a jump.
            if (IsUsableGround(ground) && contact.normal.y >= 0.65f)
                return ground;
        }

        return null;
    }

    private static bool IsUsableGround(Collider2D ground)
    {
        return ground != null && ground.enabled && ground.gameObject.activeInHierarchy;
    }

    public bool TakeEnemyHit(Vector2 enemyPosition)
    {
        if (!isActiveAndEnabled || isRecovering || isHitStunned || HurtAnimationActive || Time.time < invulnerableUntil ||
            (GameManager.Instance != null && GameManager.Instance.IsGameOver))
            return false;

        float stunDuration = HurtDuration;
        invulnerableUntil = Time.time + stunDuration + hitInvulnerability;
        GameManager.Instance?.LoseLife();
        enemyHitRoutine = StartCoroutine(ReactToEnemyHit(enemyPosition, stunDuration));
        return true;
    }

    private IEnumerator ReactToEnemyHit(Vector2 enemyPosition, float stunDuration)
    {
        isHitStunned = true;
        moveInput = 0f;
        jumpRequested = false;
        CancelParry();
        CancelDash();
        PlayHurtImmediately();
        GameAudio.Instance?.Play(GameOver ? SoundCue.Death : SoundCue.Hurt);

        float away = rb.position.x >= enemyPosition.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(away * enemyKnockback.x, enemyKnockback.y);
        float knockbackEnd = Time.time + hitStunDuration;
        float stunEnd = Time.time + stunDuration;
        bool knockbackDone = false;
        while (Time.time < stunEnd)
        {
            yield return null;
            // Stop the sideways push so a long stun doesn't slide the player off the floor.
            if (!knockbackDone && Time.time >= knockbackEnd)
            {
                knockbackDone = true;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        EndHurt();
        enemyHitRoutine = null;
    }

    private void EndHurt()
    {
        isHitStunned = false;
        if (!isRecovering && !GameOver)
        {
            animator.SetBool("isHurt", false);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void StopHurtRoutines()
    {
        if (enemyHitRoutine != null)
        {
            StopCoroutine(enemyHitRoutine);
            enemyHitRoutine = null;
        }
        if (fallRoutine != null)
        {
            StopCoroutine(fallRoutine);
            fallRoutine = null;
        }
        isHitStunned = false;
    }

    private IEnumerator RecoverFromFall()
    {
        isRecovering = true;
        CancelParry();
        moveInput = 0f;
        jumpRequested = false;
        GameManager.Instance?.LoseLife();
        float hurtEnd = Time.time + HurtDuration;
        invulnerableUntil = hurtEnd + hitInvulnerability;
        PlayHurtImmediately();
        GameAudio.Instance?.Play(GameOver ? SoundCue.Death : SoundCue.Hurt);

        // Measure before disabling the collider, whose bounds collapse once it is off.
        float feetOffset = rb.position.y - bodyCollider.bounds.min.y;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        bodyCollider.enabled = false;

        yield return new WaitForSeconds(catchPause);

        // Rise straight back up through the gap, glide over a safe tile, then set down on it.
        // The tile is re-checked every step in case the ball breaks it mid-lift.
        Collider2D tile = null;
        Vector2 target = Vector2.zero;
        int stage = 0;
        while (stage < 3)
        {
            if (tile == null || !IsUsableGround(tile))
            {
                tile = FindSafeTile();
                target = tile != null
                    ? new Vector2(tile.bounds.center.x, tile.bounds.max.y + feetOffset)
                    : startPosition;
                stage = Mathf.Min(stage, 1);
            }

            float hoverY = target.y + hoverHeight;
            Vector2 waypoint =
                stage == 0 ? new Vector2(rb.position.x, Mathf.Max(rb.position.y, hoverY)) :
                stage == 1 ? new Vector2(target.x, hoverY) :
                target;

            rb.MovePosition(Vector2.MoveTowards(rb.position, waypoint, liftSpeed * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
            if (Vector2.Distance(rb.position, waypoint) < 0.001f)
                stage++;
        }

        if (tile != null)
        {
            lastGround = tile;
            lastGroundCenter = tile.bounds.center;
            lastGroundTop = tile.bounds.max.y;
        }

        // Back on solid ground, but keep flashing and stunned until the hurt sound finishes.
        isHitStunned = true;
        EndRecovery();
        while (Time.time < hurtEnd)
            yield return null;

        EndHurt();
        fallRoutine = null;
    }

    private Collider2D FindSafeTile()
    {
        if (IsUsableGround(lastGround))
            return lastGround;

        Collider2D nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (GroundBreak floorBlock in FindObjectsByType<GroundBreak>())
        {
            Collider2D tile = floorBlock.GetComponent<Collider2D>();
            if (!IsUsableGround(tile))
                continue;

            float distance = Vector2.Distance(tile.bounds.center, lastGroundCenter);
            if (distance < nearestDistance)
            {
                nearest = tile;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private void EndRecovery()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        bodyCollider.enabled = true;
        if (!isHitStunned && (GameManager.Instance == null || !GameManager.Instance.IsGameOver))
            animator.SetBool("isHurt", false);
        isRecovering = false;
    }

    private void OnDisable()
    {
        jumpRequested = false;
        moveInput = 0f;
        CancelParry();
        bool wasHurt = isHitStunned || isRecovering;
        StopHurtRoutines();
        if (isRecovering)
            EndRecovery();
        if (wasHurt)
            animator.SetBool("isHurt", false);
    }

    private IEnumerator ParryWindow()
    {
        if (ControlsLocked)
            yield break;

        isParrying = true;
        parryStartedAt = Time.time;
        animator.SetTrigger("Parry");
        parryHitbox.SetActive(true);

        yield return new WaitForSeconds(parryDuration);

        animator.ResetTrigger("Parry");
        parryHitbox.SetActive(false);
        isParrying = false;
        parryRoutine = null;
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        lastDashTime = Time.time;

        float direction = FacingDirection;

        rb.linearVelocity = new Vector2(
            direction * dashSpeed,
            0f
        );

        float endTime = Time.time + dashDuration;

        while (Time.time < endTime)
        {
            rb.linearVelocity = new Vector2(
                direction * dashSpeed,
                0f
            );

            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

        isDashing = false;
        dashRoutine = null;
    }

    private void CancelDash()
    {
        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        isDashing = false;
    }

    private void CancelParry()
    {
        if (parryRoutine != null)
        {
            StopCoroutine(parryRoutine);
            parryRoutine = null;
        }
        isParrying = false;
        parryStartedAt = float.NegativeInfinity;
        if (parryHitbox != null)
            parryHitbox.SetActive(false);
        if (animator != null)
            animator.ResetTrigger("Parry");
    }
}
