using Unity.VisualScripting;
using UnityEditor.Tilemaps;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, Attack, Die }

    [Header("base state")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Base Movement")]
    public float walkSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public Transform[] patrolPoints;
    protected int currentPatrolIndex = 0;
    protected bool isMovingRight = true;

    [Header("base attack")]
    public float detectionRange = 5f;
    public float attackRange = 1.5f;
    protected Transform player;
    protected Rigidbody2D rb;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    protected virtual void Update()
    {
        if (currentState == EnemyState.Die) return;
        EvalueteState();
        HandleStateBehavior();
    }

    // Đánh giá trạng thái dựa trên khoảng cách
    protected virtual void EvalueteState()
    {
        if (player == null) return;
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= attackRange) currentState = EnemyState.Attack;
        else if (distance <= detectionRange) currentState = EnemyState.Chase;
        else currentState = EnemyState.Patrol;
    }

    // Điều hướng hành vi
    protected void HandleStateBehavior()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                PatrolUpdate();
                break;
            case EnemyState.Chase:
                ChaseUpdate();
                break;
            case EnemyState.Attack:
                AttackUpdate();
                break;
        }
    }

    protected virtual void PatrolUpdate()
    {
        if (patrolPoints.Length == 0) return;
        MoveTowards(patrolPoints[currentPatrolIndex].position, walkSpeed);
        if (Vector2.Distance(transform.position, patrolPoints[currentPatrolIndex].position) < 0.3f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }
    protected virtual void ChaseUpdate()
    {
        if (player == null) return;
        Vector2 targetPos = new Vector2(player.position.x, transform.position.y);
        MoveTowards(targetPos, chaseSpeed);
    }
    protected virtual void AttackUpdate()
    {

    }

    protected void MoveTowards(Vector2 target, float speed)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * speed, rb.linearVelocity.y);
        Flip(direction.x);
    }
    protected void Flip(float horizontalDir)
    {
        if ((horizontalDir > 0 && !isMovingRight) || (horizontalDir < 0 && isMovingRight))
        {
            isMovingRight = !isMovingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

}

