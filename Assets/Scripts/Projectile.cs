using System;
using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public bool HasImpacted { get; private set; }

    [SerializeField] private float lifeSeconds = 8f;
    [Tooltip("Évite que le projectile touche tout de suite le corps du tireur (canon dans le collider, nom au-dessus du tube, etc.).")]
    [SerializeField] private float ignoreShooterCollisionSeconds = 0.2f;
    private Rigidbody _rb;
    private Collider _projectileCollider;
    private Collider[] _shooterCollidersIgnored;

    private float _spawnTime;
    private Agents.Agent _shooter;
    private float _explosionRadius = 3f;
    private float _maxDamage = 40f;
    private bool _damageShooter = true;

    private void Start()
    {
        _spawnTime = Time.time;
        
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _projectileCollider = GetComponent<Collider>();
    }

    private void OnDestroy()
    {
        RestoreShooterCollisions();
    }

    /// <summary>À appeler juste après Instantiate (avant le premier FixedUpdate si possible).</summary>
    public void Init(Agents.Agent shooter, float explosionRadius, float maxDamageAtCenter, bool damageShooter, Vector3 direction, float speed)
    {
        _shooter = shooter;
        _explosionRadius = explosionRadius;
        _maxDamage = maxDamageAtCenter;
        _damageShooter = damageShooter;
        
        _rb.linearVelocity = direction * speed;

        if (_projectileCollider != null && _shooter != null && ignoreShooterCollisionSeconds > 0f)
            StartCoroutine(IgnoreShooterCollisionsRoutine());
    }

    private IEnumerator IgnoreShooterCollisionsRoutine()
    {
        RestoreShooterCollisions();

        var cols = _shooter.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
        {
            if (c == null || !c.enabled || c == _projectileCollider) continue;
            Physics.IgnoreCollision(_projectileCollider, c, true);
        }

        _shooterCollidersIgnored = cols;
        yield return new WaitForSeconds(ignoreShooterCollisionSeconds);
        RestoreShooterCollisions();
    }

    private void RestoreShooterCollisions()
    {
        if (_projectileCollider == null || _shooterCollidersIgnored == null) return;
        foreach (var c in _shooterCollidersIgnored)
        {
            if (c != null)
                Physics.IgnoreCollision(_projectileCollider, c, false);
        }

        _shooterCollidersIgnored = null;
    }

    private void Update()
    {
        if (!HasImpacted && (Time.time - _spawnTime) > lifeSeconds)
            ExplodeAt(transform.position);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (HasImpacted) return;
        Vector3 point = collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;
        ExplodeAt(point);
    }

    private void ExplodeAt(Vector3 center)
    {
        if (HasImpacted) return;
        HasImpacted = true;

        Collider[] hits = Physics.OverlapSphere(center, _explosionRadius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider col in hits)
        {
            var agent = col.GetComponentInParent<Agents.Agent>();
            if (agent == null) continue;
            if (!_damageShooter && agent == _shooter) continue;

            float dist = Vector3.Distance(center, agent.transform.position);
            if (dist > _explosionRadius) continue;

            float falloff = 1f - (dist / _explosionRadius);
            falloff = Mathf.Clamp01(falloff);
            float dmg = _maxDamage * falloff;
            agent.TakeDamage(dmg);
        }
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        float r = _explosionRadius > 0.01f ? _explosionRadius : 3f;
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, r);
    }
#endif
}
