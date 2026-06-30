using UnityEngine;
using System.Collections;

public class WalkerMeleeAggressive : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3.5f;
    private bool movingRight = true;

    [Header("Detection Settings")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float checkDistance = 0.5f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("AI Chase & Attack")]
    [SerializeField] private float chaseRange = 5f;
    [SerializeField] private float escapeRange = 7f;
    [SerializeField] private float attackRange = 1.2f;      // Tầm đánh (khoảng cách áp sát để vung đòn)
    [SerializeField] private float attackCooldown = 1.5f;   // Thời gian hồi đòn đánh (giây)
    [SerializeField] private int attackDamage = 15;         // Sát thương đòn đánh

    [Header("Attack Collision setup")]
    [SerializeField] private Transform attackPoint;         // Tâm của vùng đánh (đặt ở phía trước mặt quái)
    [SerializeField] private float attackRadius = 0.5f;     // Bán kính vùng gây sát thương đòn đánh
    [SerializeField] private LayerMask playerLayer;         // Chọn Layer là Player

    private Rigidbody2D rb;
    private Transform playerTransform;
    private Animator anim;

    private bool isChasing = false;
    private bool isAttacking = false;
    private bool canAttack = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private void FixedUpdate()
    {
        if (isAttacking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        CheckPlayerDistance();

        if (isChasing && playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= attackRange && canAttack)
            {
                StartCoroutine(AttackRoutine());
            }
            else
            {
                ChasePlayerLogic();
            }
        }
        else
        {
            PatrolLogic();
        }
    }

    void CheckPlayerDistance()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (!isChasing)
        {
            if (distanceToPlayer <= chaseRange)
            {
                isChasing = true;
            }
        }
        else
        {
            if (distanceToPlayer > escapeRange)
            {
                isChasing = false;
            }
        }
    }

    void ChasePlayerLogic()
    {
        float directionToPlayer = playerTransform.position.x - transform.position.x;

        if (directionToPlayer > 0 && !movingRight) Flip();
        else if (directionToPlayer < 0 && movingRight) Flip();

        float moveDir = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDir * chaseSpeed, rb.linearVelocity.y);

        if (anim != null) anim.SetFloat("Speed", chaseSpeed);
    }

    void PatrolLogic()
    {
        float moveDir = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDir * walkSpeed, rb.linearVelocity.y);

        if (anim != null) anim.SetFloat("Speed", walkSpeed);

        if (!groundCheck || !wallCheck) return;

        bool isGroundedAhead = Physics2D.Raycast(groundCheck.position, Vector2.down, checkDistance, obstacleLayer);
        Vector2 forwardDirection = movingRight ? Vector2.right : Vector2.left;
        bool isWallAhead = Physics2D.Raycast(wallCheck.position, forwardDirection, checkDistance, obstacleLayer);

        if (!isGroundedAhead || isWallAhead)
        {
            Flip();
        }
    }

    IEnumerator AttackRoutine()
    {
        canAttack = false;
        isAttacking = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Khóa đứng im để đánh

        Debug.Log("Quái bắt đầu vung tay tấn công!");
        if (anim != null)
        {
            anim.SetTrigger("Attack");
        }

        // Chờ 0.1 giây để quét sát thương
        yield return new WaitForSeconds(0.1f);

        PerformDamage();

        // Chờ thêm 0.1 giây nữa để kết thúc hành động đánh
        yield return new WaitForSeconds(0.1f);
        isAttacking = false;

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    void PerformDamage()
    {
        if (attackPoint == null) return;

        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);

        foreach (Collider2D player in hitPlayers)
        {
            Debug.Log("Đòn đánh của quái trúng: " + player.name);

            Health playerHealth = player.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage, transform.position);
            }
        }
    }

    void Flip()
    {
        movingRight = !movingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (attackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }

        if (groundCheck && wallCheck)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * checkDistance);
            Vector3 forward = movingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + forward * checkDistance);
        }
    }
}