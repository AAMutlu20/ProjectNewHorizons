using IrminTimerPackage;
using IrminTimerPackage.Tools;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Ability_Shooting : Ability_Base
{
    [SerializeField] private IrminTimer _fireTimer = new();
    [SerializeField] private Projectile _projectile;

    [SerializeField] private float _abilityFireBaseTime = 4;

    [SerializeField] private int _baseProjectileAmount = 1;
    [SerializeField] private int _currentActualProjectileAmount;

    [SerializeField] private float _baseFireSpeed;

    [SerializeField] private float _shootOffset = 2;

    [SerializeField] private List<Projectile> _spawnedProjectiles = new();

    private void Start()
    {

        _fireTimer.OnTimeElapsed += RestartTimer;
        _fireTimer.OnTimeElapsed += Shoot;
        UpdateFireTimerTime();
        _fireTimer.StartTimer();
    }

    private void Update()
    {
        _fireTimer.UpdateTimer(Time.deltaTime);
    }

    private void RestartTimer()
    {
        UpdateFireTimerTime();
        _fireTimer.StartTimer();
    }

    public void Shoot()
    {
        UpdateProjectileAmount();
        UpdateFireTimerTime();
        List<Vector3> directions = GetDirections(_currentActualProjectileAmount);
        for (int i = 0; i < directions.Count; i++)
        {
            Vector3 direction = transform.rotation * directions[i];
            direction.y = 0;
            direction.Normalize();

            Projectile spawnedProjectile = Instantiate(_projectile, _connectedPlayerStats.transform.position + (direction * _shootOffset), Quaternion.LookRotation(direction));
            spawnedProjectile.ConnectedPlayerStats = _connectedPlayerStats;
            _spawnedProjectiles.Add(spawnedProjectile);
        }
        
    }

    private void UpdateProjectileAmount()
    {
        _currentActualProjectileAmount = _baseProjectileAmount + _connectedPlayerStats.ProjectileAmountValue;
        //int extraProjectilesFromSpeed = (int)MathF.Truncate(_connectedPlayerStats.AttackSpeedPercentage / 100);
        //_currentActualProjectileAmount = _baseProjectileAmount + extraProjectilesFromSpeed;
    }

    private List<Vector3> GetDirections(int pAmount)
    {
        List<Vector3> directions = new List<Vector3>();

        if (pAmount <= 0) return directions;

        float angleStep = 360f / pAmount;

        for (int i = 0; i < pAmount; i++)
        {
            float currentAngle = angleStep * i;
            float radians = currentAngle * Mathf.Deg2Rad;

            // X and Z form the circle, Y stays 0
            float x = Mathf.Sin(radians);
            float z = Mathf.Cos(radians);

            Vector3 dir = new Vector3(x, 0f, z).normalized;
            directions.Add(dir);
        }

        return directions;
    }

    public void UpdateFireTimerTime()
    {
        _fireTimer.Time = ((100 - _connectedPlayerStats.AttackSpeedPercentage) / 100) * _baseFireSpeed;
    }

}
