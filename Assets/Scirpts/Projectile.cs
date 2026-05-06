using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform target;
    private float speed = 5f;
    private float damage;
    private PlayerAttack attacker;
    private GameObject hitEffectPrefab;
    private float hitEffectDuration = 0.3f;
    private float hitEffectOffsetY = 0.5f;
    private Vector3 lastTargetPos;

    public void Init(Transform target, float speed, float damage, PlayerAttack attacker, GameObject hitEffectPrefab = null, float hitEffectDuration = 0.3f, float hitEffectOffsetY = 0.5f)
    {
        this.target = target;
        this.speed = speed;
        this.damage = damage;
        this.attacker = attacker;
        this.hitEffectPrefab = hitEffectPrefab;
        this.hitEffectDuration = hitEffectDuration;
        this.hitEffectOffsetY = hitEffectOffsetY;

        csParticleMove pm = GetComponentInChildren<csParticleMove>();
        if (pm != null) pm.enabled = false;

        if (target != null)
        {
            lastTargetPos = target.position;
            Vector3 dir = (target.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f) { Destroy(gameObject); return; }

        if (target != null) lastTargetPos = target.position;

        Vector3 moveTarget = lastTargetPos;
        float step = speed * Time.deltaTime;

        Vector3 dir = (moveTarget - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.position = Vector3.MoveTowards(transform.position, moveTarget, step);

        if (Vector2.Distance(transform.position, moveTarget) < 0.05f)
        {
            if (target != null)
            {
                EnemyHealth health = target.GetComponent<EnemyHealth>();
                if (health != null) health.TakeDamage(damage, attacker);
            }
            if (hitEffectPrefab != null)
            {
                Vector3 effectPos = lastTargetPos + Vector3.up * hitEffectOffsetY;
                Instantiate(hitEffectPrefab, effectPos, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}