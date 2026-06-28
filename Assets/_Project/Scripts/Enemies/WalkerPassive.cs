using UnityEngine;

public class WalkerPassive : MonoBehaviour
{

    public float walkSpeed = 2f;
    private bool movingRight = true;

    public Transform groundCheck;
    public Transform wallCheck;
    public float checkDistance = 0.5f;
    public LayerMask obstacleLayer;

    private Rigidbody2D rb;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if(rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private void FixedUpdate()
    {
        float moveDir = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDir * walkSpeed, rb.linearVelocity.y);
        if (!groundCheck || !wallCheck) return;

        // Bắn một tia từ điểm Check xuống dưới xem có đất không
        bool isGroundedAhead = Physics2D.Raycast(groundCheck.position, Vector2.down, checkDistance, obstacleLayer);

        // Bắn một tia ra phía trước xem có đâm vào tường không
        Vector2 forwardDirection = movingRight ? Vector2.right : Vector2.left;
        bool isWallAhead = Physics2D.Raycast(wallCheck.position, forwardDirection, checkDistance, obstacleLayer);

        // Nếu phía trước KHÔNG có đất (gặp vực) HOẶC phía trước CÓ tường -> Quay đầu liền!
        if (!isGroundedAhead || isWallAhead)
        {
            ChangeDirection();
        }

    }

    void ChangeDirection()
    {
        movingRight = !movingRight;

        // Lật ngược Sprite của quái lại bằng cách đổi dấu trục X của LocalScale
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    private void OnDrawGizmos()
    {
        if (!groundCheck || !wallCheck)
            return;

        Gizmos.color = Color.cyan;

        // Tia check vực
        Gizmos.DrawLine(
            groundCheck.position,
            groundCheck.position + Vector3.down * checkDistance
        );

        // Tia check tường
        Vector3 forward = movingRight ? Vector3.right : Vector3.left;

        Gizmos.DrawLine(
            wallCheck.position,
            wallCheck.position + forward * checkDistance
        );
    }
}
