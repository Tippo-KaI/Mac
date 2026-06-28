using UnityEngine;

public class FlyingAggressive : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] waypoints;      // Danh sách các điểm mốc tuần tra (kéo từ Hierarchy vào)
    public float patrolSpeed = 2f;
    private int currentWaypointIndex = 0;
    private float waypointRadius = 0.2f; // Khoảng cách để tính là đã chạm điểm mốc

    [Header("Chase Settings")]
    public float chaseSpeed = 3.5f;
    public float chaseRange = 5f;
    public float escapeRange = 7f;

    private Rigidbody2D rb;
    private Transform playerTransform;
    private bool isChasing = false;
    private bool movingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 0f; // Bắt buộc tắt trọng lực để quái bay được
        }

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

    void CheckForPlayer()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (!isChasing)
        {
            if (distanceToPlayer <= chaseRange)
            {
                isChasing = true;
                Debug.Log("Quái bay đã phát hiện mục tiêu!");
            }
        }
        else
        {
            if (distanceToPlayer > escapeRange)
            {
                isChasing = false;
                Debug.Log("Player đã thoát khỏi tầm quái bay.");
            }
        }
    }

    // LOGIC ĐUỔI THEO PLAYER (Bay tự do theo mọi hướng X, Y)
    void ChasePlayerLogic()
    {
        // Tính toán vector hướng bay thẳng từ Quái tới Player
        Vector2 direction = (playerTransform.position - transform.position).normalized;

        // Di chuyển Rigidbody về phía Player
        rb.linearVelocity = direction * chaseSpeed;

        // Xử lý lật mặt dựa trên hướng của Player
        if (direction.x > 0 && !movingRight)
        {
            Flip();
        }
        else if (direction.x < 0 && movingRight)
        {
            Flip();
        }
    }

    // LOGIC TUẦN TRA THEO ĐIỂM MỐC
    void PatrolLogic()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Lấy vị trí điểm mốc hiện tại
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector2 direction = (targetWaypoint.position - transform.position).normalized;

        // Di chuyển về phía điểm mốc
        rb.linearVelocity = direction * patrolSpeed;

        // Kiểm tra xem đã bay tới sát điểm mốc chưa
        if (Vector2.Distance(transform.position, targetWaypoint.position) < waypointRadius)
        {
            // Chuyển sang điểm mốc tiếp theo trong danh sách
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }

        // Lật mặt dựa trên hướng di chuyển tới điểm mốc
        if (direction.x > 0 && !movingRight)
        {
            Flip();
        }
        else if (direction.x < 0 && movingRight)
        {
            Flip();
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
        // Vẽ vòng tròn tầm quét
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, escapeRange);

        // Nối các điểm tuần tra lại với nhau để bạn dễ quan sát đường đi của nó
        if (waypoints != null && waypoints.Length > 1)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    Transform current = waypoints[i];
                    Transform next = waypoints[(i + 1) % waypoints.Length];
                    if (next != null)
                    {
                        Gizmos.DrawLine(current.position, next.position);
                    }
                }
            }
        }
    }
}
