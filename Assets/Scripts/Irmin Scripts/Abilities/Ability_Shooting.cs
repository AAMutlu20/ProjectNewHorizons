using IrminTimerPackage;
using IrminTimerPackage.Tools;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Ability_Shooting : Ability_Base
{
    [SerializeField] protected IrminTimer _fireTimer = new();
    [SerializeField] protected Projectile _projectile;

    [SerializeField] protected int _baseProjectileAmount = 1;
    [SerializeField] protected int _currentActualProjectileAmount;

    [SerializeField] protected float _baseFireSpeed;

    [SerializeField] protected float _shootOffset = 2;

    [SerializeField] protected List<Projectile> _spawnedProjectiles = new();

    protected virtual void Start()
    {

        _fireTimer.OnTimeElapsed += RestartTimer;
        _fireTimer.OnTimeElapsed += Shoot;
        UpdateFireTimerTime();
        _fireTimer.StartTimer();
    }

    protected void Update()
    {
        _fireTimer.UpdateTimer(Time.deltaTime);
    }

    protected void RestartTimer()
    {
        UpdateFireTimerTime();
        _fireTimer.StartTimer();
    }

    protected virtual void Shoot()
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

    protected void UpdateProjectileAmount()
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

    private List<Vector3> GetDirectionsWithAngleDegrees(int pAmount, float pAngleDegrees = 360f)
    {
        List<Vector3> directions = new List<Vector3>();

        if (pAmount <= 0) return directions;

        // Clamp angle to valid range (0-360)
        float constrainedAngle = Mathf.Clamp(pAngleDegrees, 0f, 360f);

        // If angle is 0, return just the forward direction
        if (constrainedAngle == 0f)
        {
            directions.Add(Vector3.forward);
            return directions;
        }

        // Calculate starting angle to center the spread around forward
        float startAngle = -constrainedAngle / 2f;
        float angleStep = constrainedAngle / (pAmount - 1); // -1 so we include both ends

        for (int i = 0; i < pAmount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            float radians = currentAngle * Mathf.Deg2Rad;

            // Rotate around Y axis from forward direction
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
