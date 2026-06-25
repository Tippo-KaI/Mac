using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("KnockBack")]
    [SerializeField] private float knockbackForceX = 8f;
    [SerializeField] private float knockbackForceY = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool isPlayer = false;

    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        if(isPlayer)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
    }

    public void TakeDamage(float damageAmount, Vector2 attackerPosition)
    {
        currentHealth -= damageAmount;
        Debug.Log(gameObject.name + "Mau con: " + currentHealth);
        if(currentHealth > 0)
        {
            TriggerKnockBack(attackerPosition);
        } else
        {
            Die();
        }
    }

    private void TriggerKnockBack(Vector2 attackerPosition)
    {
        if (rb == null)
        {
            return;
        }
        // 1. Xác định hướng đẩy lùi (Ngược hướng với kẻ tấn công)
        float knockbackDirection = transform.position.x > attackerPosition.x ? 1f : -1f;

        // 2. Triệt tiêu vận tốc cũ để lực đẩy chuẩn xác
        rb.linearVelocity = Vector2.zero;

        // 3. Áp dụng lực đẩy lùi
        rb.AddForce(new Vector2(knockbackDirection * knockbackForceX, knockbackForceY), ForceMode2D.Impulse);

        // 4. Khóa di chuyển tạm thời tùy theo thực thể là Player hay Quái
        StartCoroutine(KnockbackRoutine());
    }

    private IEnumerator KnockbackRoutine()
    {
        if (isPlayer && playerMovement != null)
        {
            // Tận dụng biến isWallJumping hoặc tự tạo biến khóa di chuyển. 
            // Ở đây mình mượn tạm biến isWallJumping của bạn vì nó có sẵn tính năng khóa MovePlayer() trong FixedUpdate!
            // Cách này cực kỳ gọn mà không cần sửa cấu trúc code di chuyển của bạn.
            typeof(PlayerMovement).GetField("isWallJumping", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(playerMovement, true);
            typeof(PlayerMovement).GetField("wallJumpCounter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(playerMovement, knockbackDuration);
        }
        else
        {
            // Nếu là Quái: Có thể tạm thời tắt script AI hoặc dừng vận tốc tại đây nếu bạn có script AI sau này.
        }

        yield return new WaitForSeconds(knockbackDuration);
    }
    void Die()
    {
        Debug.Log(gameObject.name + " đã chết!");
        if (isPlayer)
        {
            currentHealth = maxHealth;
            Debug.Log("Player hồi sinh tạm thời để test!");
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
