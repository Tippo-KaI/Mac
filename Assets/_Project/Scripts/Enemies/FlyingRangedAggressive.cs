using UnityEngine;
using System.Collections;

public class FlyingRangedAggressive : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] waypoints;
    public float patrolSpeed = 2f;
    private int currentWaypointIndex = 0;
    private float waypointRadius = 0.2f;

    [Header("Chase Settings")]
    public float chaseSpeed = 3f;
    public float chaseRange = 7f; // Chỉ dùng để kích hoạt lượt đuổi đầu tiên

    [Header("Ranged Attack & Kiting Settings")]
    public float attackRange = 5f;       // Tầm bắn lý tưởng quái muốn giữ
    public float attackCooldown = 2f;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float spreadAngle = 30f;

    private Rigidbody2D rb;
    private Transform playerTransform;
    private bool isChasing = false;
    private bool isAttacking = false;
    private bool canAttack = true;
    private bool movingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 0f;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private void FixedUpdate()
    {
        // Khi đang thực hiện hành động bắn đạn, đứng im sạc/bắn
        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Kiểm tra kích hoạt đuổi theo lần đầu
        if (!isChasing && playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= chaseRange)
            {
                isChasing = true;
                Debug.Log("Đã vào tầm ngắm! Đuổi theo đến chết!");
            }
        }

        // Logic điều khiển hành vi
        if (isChasing && playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

            // Luôn ưu tiên bắn nếu hồi chiêu xong và nằm trong tầm bắn được
            if (distanceToPlayer <= attackRange + 0.5f && canAttack)
            {
                StartCoroutine(AttackRoutine());
            }
            else
            {
                // Khi chưa bắn hoặc đang hồi chiêu, quái sẽ giữ khoảng cách (Kiting)
                KitingMovementLogic(distanceToPlayer);
            }
        }
        else
        {
            PatrolLogic();
        }
    }

    // LOGIC DI CHUYỂN THẢ DIỀU (GIỮ KHOẢNG CÁCH)
    void KitingMovementLogic(float distanceToPlayer)
    {
        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;

        // Trường hợp 1: Player ở quá xa -> Bay tiến lại gần
        if (distanceToPlayer > attackRange + 0.5f)
        {
            rb.linearVelocity = directionToPlayer * chaseSpeed;
        }
        // Trường hợp 2: Player áp sát quá gần -> Bay giật lùi ra xa để bảo toàn mạng sống
        else if (distanceToPlayer < attackRange - 1.5f)
        {
            rb.linearVelocity = -directionToPlayer * chaseSpeed; // Dấu trừ để bay ngược hướng player
        }
        // Trường hợp 3: Nằm trong vùng bắn đẹp -> Lơ lửng tại chỗ
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Luôn quay mặt về phía Player khi đang trong trạng thái truy đuổi
        if (directionToPlayer.x > 0 && !movingRight) Flip();
        else if (directionToPlayer.x < 0 && movingRight) Flip();
    }

    void PatrolLogic()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector2 direction = (targetWaypoint.position - transform.position).normalized;
        rb.linearVelocity = direction * patrolSpeed;

        if (Vector2.Distance(transform.position, targetWaypoint.position) < waypointRadius)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }

        if (direction.x > 0 && !movingRight) Flip();
        else if (direction.x < 0 && movingRight) Flip();
    }

    IEnumerator AttackRoutine()
    {
        canAttack = false;
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        // Cố định hướng mặt về Player trước khi nã đạn
        float directionToPlayerX = playerTransform.position.x - transform.position.x;
        if (directionToPlayerX > 0 && !movingRight) Flip();
        else if (directionToPlayerX < 0 && movingRight) Flip();

        yield return new WaitForSeconds(0.2f); // Delay vung người sạc đạn

        ShootThreeProjectiles();

        yield return new WaitForSeconds(0.2f);
        isAttacking = false;

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    void ShootThreeProjectiles()
    {
        if (projectilePrefab == null || firePoint == null || playerTransform == null) return;

        Vector2 baseDirection = (playerTransform.position - firePoint.position).normalized;

        SpawnProjectile(baseDirection);
        SpawnProjectile(RotateVector(baseDirection, spreadAngle));
        SpawnProjectile(RotateVector(baseDirection, -spreadAngle));
    }

    void SpawnProjectile(Vector2 dir)
    {
        GameObject projObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        BasicEnemyProjectile proj = projObj.GetComponent<BasicEnemyProjectile>();
        if (proj != null)
        {
            proj.Launch(dir);
        }
    }

    Vector2 RotateVector(Vector2 v, float degrees)
    {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
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
        // Vòng tròn đỏ: Tầm phát hiện ban đầu
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Vòng tròn xanh dương: Tầm bắn lý tưởng muốn giữ vững
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}