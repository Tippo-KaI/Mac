using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [Header("Damage Setting")]
    [SerializeField] private float damageToPlayer = 20f;
    [SerializeField] private float damageCooldown = 1f;

    private float nextDamageTime;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            if(Time.time >= nextDamageTime)
            {
                Health playerHealth = collision.gameObject.GetComponent<Health>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damageToPlayer, transform.position);
                    nextDamageTime = Time.time + damageCooldown;
                }
            }
        }
    }
}
