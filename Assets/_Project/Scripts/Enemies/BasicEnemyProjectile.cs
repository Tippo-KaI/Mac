using UnityEngine;

public class BasicEnemyProjectile : MonoBehaviour
{
    public float speed = 5f;
    public int damage = 10;
    public float lifetime = 4f; // Tự hủy sau 4 giây để tránh rác RAM

    private Vector2 moveDirection;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    // Hàm này sẽ được con quái gọi để truyền hướng bay cho viên đạn
    public void Launch(Vector2 direction)
    {
        moveDirection = direction.normalized;

        // Xoay viên đạn theo hướng bay (nếu đạn của bạn có hình mũi tên/thon dài)
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void FixedUpdate()
    {
        // Di chuyển đạn theo hướng đã định
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Chạm vào Player thì gây sát thương
        if (collision.CompareTag("Player"))
        {
            Health playerHealth = collision.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, transform.position);
            }
            Destroy(gameObject); // Bắn trúng thì hủy đạn
        }

        // Chạm vào tường/đất cũng hủy đạn (bạn có thể check Layer tùy ý)
        if (((1 << collision.gameObject.layer) & LayerMask.GetMask("Ground", "Wall")) != 0)
        {
            Destroy(gameObject);
        }
    }
}
