using UnityEngine;

public class WalkerAggressive : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float chaseSpeed = 3.5f; // Tốc độ khi đuổi theo sẽ nhanh hơn chút
    private bool movingRight = true;

    [Header("Detection Settings")]
    public Transform groundCheck;
    public Transform wallCheck;
    public float checkDistance = 0.5f;
    public LayerMask obstacleLayer;

    [Header("AI & Player Chase")]
    public LayerMask playerLayer;
    public float chaseRange = 5f;   // Khoảng cách phát hiện để đuổi theo
    public float escapeRange = 7f;  // Khoảng cách Player chạy thoát hẳn (nên lớn hơn chaseRange)

    private Rigidbody2D rb;
    private Transform playerTransform;
    private bool isChasing = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // Tự động tìm Player bằng Tag để không cần kéo tay trong Inspector
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private void FixedUpdate()
    {
        CheckForPlayer();

        if (isChasing && playerTransform != null)
        {
            ChasePlayerLogic();
        }
        else
        {
            PatrolLogic();
        }
    }

    // LỘC PHÁT HIỆN PLAYER
    void CheckForPlayer()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (!isChasing)
        {
            // Nếu chưa đuổi, check xem player có vào tầm kích hoạt không
            if (distanceToPlayer <= chaseRange)
            {
                isChasing = true;
                Debug.Log("Đã mục tiêu! Đang đuổi theo Player.");
            }
        }
        else
        {
            // Nếu đang đuổi, check xem player đã chạy thoát chưa
            if (distanceToPlayer > escapeRange)
            {
                isChasing = false;
            }
        }
    }

    // LOGIC ĐUỔI THEO PLAYER
    void ChasePlayerLogic()
    {
        // Xác định hướng của Player so với Quái (Trái hay Phải)
        float directionToPlayer = playerTransform.position.x - transform.position.x;

        if (directionToPlayer > 0 && !movingRight)
        {
            Flip();
        }
        else if (directionToPlayer < 0 && movingRight)
        {
            Flip();
        }

        // Di chuyển về phía Player với tốc độ chaseSpeed
        float moveDir = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDir * chaseSpeed, rb.linearVelocity.y);
    }

    // LOGIC TUẦN TRA BÌNH THƯỜNG 
    void PatrolLogic()
    {
        float moveDir = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDir * walkSpeed, rb.linearVelocity.y);

        if (!groundCheck || !wallCheck) return;

        bool isGroundedAhead = Physics2D.Raycast(groundCheck.position, Vector2.down, checkDistance, obstacleLayer);
        Vector2 forwardDirection = movingRight ? Vector2.right : Vector2.left;
        bool isWallAhead = Physics2D.Raycast(wallCheck.position, forwardDirection, checkDistance, obstacleLayer);

        if (!isGroundedAhead || isWallAhead)
        {
            Flip();
        }
    }

    // Hàm lật mặt quái
    void Flip()
    {
        movingRight = !movingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    // VẼ KHU VỰC PHÁT HIỆN ĐỂ DỄ CÂN BẰNG TRONG EDITOR
    private void OnDrawGizmos()
    {
        // Giữ nguyên Gizmos cũ của bạn
        if (groundCheck && wallCheck)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * checkDistance);
            Vector3 forward = movingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + forward * checkDistance);
        }

        // Vẽ thêm tầm phát hiện (Vòng tròn đỏ) và tầm thoát (Vòng tròn vàng)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, escapeRange);
    }
}