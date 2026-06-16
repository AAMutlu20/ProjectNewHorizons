using IrminTimerPackage.Tools;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Projectile : MonoBehaviour
{
    [SerializeField] IrminTimer _projectileLifeTimeTimer;

    [SerializeField] private Stats _connectedPlayerStats;

    [SerializeField] private Rigidbody _projectileRigidBody;

    [SerializeField] private E_DamageTypes _damageType;
    [SerializeField] private float _baseDamage;

    [SerializeField] private Vector3 _shootingDirection;
    [SerializeField] private float _baseShootingSpeed;
    [SerializeField] private float _shootingSpeed;

    public Stats ConnectedPlayerStats { get { return _connectedPlayerStats; } set { _connectedPlayerStats = value; } }

    public E_DamageTypes DamageTypes { get { return _damageType; } set { _damageType = value; } }
    public float BaseDamage { get { return _baseDamage; } set { _baseDamage = value; } }

    public UnityEvent<Projectile> OnProjectileDestroyed;

    private void Start()
    {
        UpdateShootingSpeed();
        _projectileLifeTimeTimer.OnTimeElapsed += DestroyProjectile;
        _projectileLifeTimeTimer.StartTimer();
    }

    private void Update()
    {
        _projectileLifeTimeTimer.UpdateTimer(Time.deltaTime);
        UpdateMove();
    }

    public void UpdateMove()
    {
        _shootingSpeed = _baseShootingSpeed + (_baseShootingSpeed + (_baseShootingSpeed * (_connectedPlayerStats.ProjectileSpeedPercentage / 100)));
        float moveDistance =  _shootingSpeed * Time.deltaTime;
        _projectileRigidBody.MovePosition((Vector3)transform.position + transform.forward * moveDistance);
    }

    private void OnCollisionEnter(Collision collision)
    {
        
    }

    public void DestroyProjectile()
    {
        OnProjectileDestroyed?.Invoke(this);
        Destroy(this.gameObject);
    }

    private void UpdateShootingSpeed()
    {
        _shootingSpeed = _baseShootingSpeed += ((_connectedPlayerStats.ProjectileSpeedPercentage / 100) * _baseShootingSpeed);
    }

}