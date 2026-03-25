using UnityEngine;

public class Projectile : MonoBehaviour
{
    public bool HasImpacted { get; private set; } = false;

    [SerializeField] private float lifeSeconds = 8f;

    private float _spawnTime;

    private void Start()
    {
        _spawnTime = Time.time;
    }

    private void Update()
    {
        if (!HasImpacted && (Time.time - _spawnTime) > lifeSeconds)
            HasImpacted = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        HasImpacted = true;
    }
}

