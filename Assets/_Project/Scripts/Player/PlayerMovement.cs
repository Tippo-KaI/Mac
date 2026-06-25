using UnityEngine;
using System.Collections; 
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(10f, 14f);
    [SerializeField] private float wallJumpDuration = 0.15f;
    [SerializeField] private float wallCatchDelay = 0.1f;

    [Header("Ground/Wall Check Settings")]
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private Transform groundCheckPosition;
    [SerializeField] private Transform wallCheckPosition;
    [SerializeField] private float wallCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundCheckLayer;
    [SerializeField] private LayerMask wallCheckLayer;

    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float attackOffsetDistance = 0.5f;
    [SerializeField] private float attackDamege = 25f;

    [Header("Ranged Attack (Sword Throw)")]
    [SerializeField] private float swordPickUpDistance = 1f;
    [SerializeField] private GameObject swordPrefab;
    private GameObject activeSword;
    private bool throwInput;

    [Header("Dash to Sword Settings")]
    [SerializeField] private float dashSpeed = 25f;
    private bool isDashing = false;
    private Vector2 dashTargetPosition;
    private float originalGravity;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool jumpInput;
    private bool attackInput;
    private bool isWallSliding = false;
    private bool isWallJumping = false;
    private bool isWallFreezing = false;
    private float wallJumpDirection;
    private float wallJumpCounter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravity = rb.gravityScale;
    }

    void Update()
    {
        if (isDashing)
        {
            CheckDashArrival();
        }
        else
        {
            GatherInput();
            HandleWallSlideState(); // Kiểm tra trạng thái bám tường liên tục
        }

        RotateAttackPointTowardsMouse();
        CheckAutoPickUpSword();
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            ExecuteDash();
            return;
        }

        // Logic di chuyển vật lý
        if (!isWallJumping) // Nếu đang trong thời gian nhảy bật tường, khóa phím di chuyển một tí để lực đẩy tự nhiên
        {
            MovePlayer();
        }

        HandleWallSlidePhysics(); // Áp dụng lực trượt tường
        HandleJump();
        HandleAttack();
        HandleThrow();
    }

    void GatherInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump")) jumpInput = true;
        if (Input.GetMouseButtonDown(0))
        {
            if (activeSword == null)
            {
                attackInput = true;
            }
            else
            {
                RecallSword();
            }
        }

        if (Input.GetMouseButtonDown(1)) throwInput = true;
    }

    // Viết thêm hàm này vào bên dưới GatherInput hoặc bất cứ đâu trong class
    void RecallSword()
    {
        if (activeSword != null)
        {
            Sword swordScript = activeSword.GetComponent<Sword>();
            if (swordScript != null)
            {
                // Gọi hàm CallRecall đã có sẵn trong script Sword của bạn
                swordScript.CallRecall();
            }
        }
    }

    void MovePlayer()
    {
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        if (horizontalInput > 0) transform.localScale = new Vector3(1f, 1f, 1f);
        else if (horizontalInput < 0) transform.localScale = new Vector3(-1f, 1f, 1f);
    }

    // XỬ LÝ NHẢY THƯỜNG VÀ NHẢY TƯỜNG
    void HandleJump()
    {
        if (jumpInput)
        {
            if (IsGrounded())
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
            else if (isWallSliding || (IsTouchingWall() && !IsGrounded()))
            {
                isWallJumping = true;
                wallJumpDirection = -transform.localScale.x;

                rb.linearVelocity = new Vector2(wallJumpDirection * wallJumpForce.x, wallJumpForce.y);
                wallJumpCounter = wallJumpDuration;
            }
        }

        jumpInput = false;

        if (isWallJumping)
        {
            wallJumpCounter -= Time.fixedDeltaTime;
            if (wallJumpCounter <= 0)
            {
                isWallJumping = false;
            }
        }
    }

    // XỬ LÝ TRẠNG THÁI BÁM TƯỜNG (Slide)
    void HandleWallSlideState()
    {
        if (isWallJumping)
        {
            isWallSliding = false;
            isWallFreezing = false;
            return;
        }

        bool currentlyTouchingWall = IsTouchingWall() && !IsGrounded();

        if (currentlyTouchingWall)
        {
            // Tính toán xem bức tường đang nằm ở bên nào của nhân vật dựa vào scale (hướng mặt)
            // Vì chúng ta đã làm hàm Flip(), nhân vật quay mặt sang hướng nào thì wallCheck ở hướng đó.
            float wallDirection = transform.localScale.x;

            // ĐIỀU KIỆN MỚI CHUẨN XỊN:
            // Nếu đang bay lên (Y > 0) VÀ vận tốc X của nhân vật đang cùng hướng với bức tường (đang ép vào tường từ đất)
            // HOẶC vận tốc X bằng 0 (nhảy thẳng đứng sát tường lên)
            if (rb.linearVelocity.y > 0.1f && (Mathf.Sign(rb.linearVelocity.x) == wallDirection || Mathf.Abs(rb.linearVelocity.x) < 0.1f))
            {
                // Cho phép lực nhảy từ đất hoạt động trọn vẹn, KHÔNG bật trượt tường để không bị ghì xuống!
                isWallSliding = false;
                isWallFreezing = false;
            }
            // TẤT CẢ CÁC TRƯỜNG HỢP KHÁC: Đang rơi xuống, HOẶC đang bay lên do Wall Jump (vận tốc X đang lao ra xa tường và người chơi ghì ngược lại)
            else
            {
                isWallSliding = true;

                // Chỉ cho khựng khi thực sự rơi xuống
                if (!isWallFreezing && rb.linearVelocity.y <= 0.1f)
                {
                    StartCoroutine(WallCatchRoutine());
                }
            }
        }
        else
        {
            isWallSliding = false;
            isWallFreezing = false;
            StopCoroutine(WallCatchRoutine());
        }
    }
    IEnumerator WallCatchRoutine()
    {
        isWallFreezing = true; // Bật trạng thái đóng băng
        isWallSliding = true;   // Kích hoạt trạng thái tường luôn để có thể Wall Jump nếu muốn
        yield return new WaitForSeconds(wallCatchDelay);
        isWallFreezing = false; // Hết thời gian khựng, chuyển sang trượt xuống
    }

    void HandleWallSlidePhysics()
    {
        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
    }

    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheckPosition.position, groundCheckRadius, groundCheckLayer);
    }

    public bool IsTouchingWall()
    {
        return Physics2D.OverlapCircle(wallCheckPosition.position, wallCheckRadius, wallCheckLayer);
    }

    void HandleAttack()
    {
        if (attackInput)
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
            foreach (Collider2D enemy in hitEnemies)
            {
                Health enemyHealth = enemy.GetComponent<Health>();
                if(enemyHealth != null)
                {
                    enemyHealth.TakeDamage(attackDamege, transform.position);
                }
            }
            attackInput = false;
        }
    }

    void RotateAttackPointTowardsMouse()
    {
        if (attackPoint == null) return;
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;
        Vector3 direction = (mouseWorldPosition - transform.position).normalized;
        attackPoint.position = transform.position + direction * attackOffsetDistance;
    }

    void HandleThrow()
    {
        if (throwInput)
        {
            throwInput = false;

            if (activeSword != null)
            {
                Sword swordScript = activeSword.GetComponent<Sword>();
                if (swordScript != null && swordScript.CanDashTo)
                {
                    dashTargetPosition = activeSword.transform.position;
                    isDashing = true;
                    rb.gravityScale = 0f;
                }
                return;
            }

            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPosition.z = 0f;
            Vector2 throwDirection = (mouseWorldPosition - transform.position).normalized;

            activeSword = Instantiate(swordPrefab, transform.position, Quaternion.identity);

            Sword newSwordScript = activeSword.GetComponent<Sword>();
            if (newSwordScript != null)
            {
                newSwordScript.Launch(throwDirection, transform);
            }
        }
    }

    void ExecuteDash()
    {
        Vector2 currentPos = transform.position;
        Vector2 dashDirection = (dashTargetPosition - currentPos).normalized;
        rb.linearVelocity = dashDirection * dashSpeed;
    }

    void CheckDashArrival()
    {
        float distanceToTarget = Vector2.Distance(transform.position, dashTargetPosition);
        bool isDashingDown = rb.linearVelocity.y < -0.1f;

        // Thêm IsTouchingWall để dừng lướt nếu đâm sầm vào vách tường khi lướt ngang
        if (distanceToTarget < 0.4f || (isDashingDown && IsGrounded()) || IsTouchingWall())
        {
            isDashing = false;
            rb.gravityScale = originalGravity;
            rb.linearVelocity = Vector2.zero;

            if (distanceToTarget < 0.4f)
            {
                transform.position = dashTargetPosition;
            }
            if (activeSword != null)
            {
                Destroy(activeSword);
                activeSword = null;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
        if (groundCheckPosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheckPosition.position, groundCheckRadius);
        }
        if (wallCheckPosition != null)
        {
            Gizmos.color = Color.yellow; // Vẽ vòng tròn check tường màu xanh lá cây
            Gizmos.DrawWireSphere(wallCheckPosition.position, wallCheckRadius);
        }
    }

    private void CheckAutoPickUpSword()
    {
        if (activeSword != null && !isDashing)
        {
            // Lấy script Sword từ cây kiếm đang active
            Sword swordScript = activeSword.GetComponent<Sword>();

            // CHỈ tự nhặt khi kiếm đã bay đủ xa (CanBePickedUp == true)
            if (swordScript != null && swordScript.CanBePickedUp)
            {
                float distanceToSword = Vector2.Distance(transform.position, activeSword.transform.position);

                if (distanceToSword <= swordPickUpDistance)
                {
                    Debug.Log("Nhân vật đi đến gần và tự động nhặt lại kiếm!");
                    Destroy(activeSword);
                    activeSword = null;
                }
            }
        }
    }
}