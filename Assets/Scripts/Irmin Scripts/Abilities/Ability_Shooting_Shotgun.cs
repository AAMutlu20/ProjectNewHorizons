using IrminStaticUtilities.Tools;
using System.Collections.Generic;
using UnityEngine;

public class Ability_Shooting_Shotgun : Ability_Shooting
{
    [SerializeField] protected float _spreadInDegrees; 


    protected override void Start()
    {
        base.Start();
    }

    protected override void Shoot()
    {
        UpdateProjectileAmount();
        UpdateFireTimerTime();
        List<Vector3> directions = EulerRotationUtility.GetDirectionsWithAngleDegrees(_currentActualProjectileAmount, _spreadInDegrees);
        for (int i = 0; i < directions.Count; i++)
        {
            //Vector3 direction = transform.rotation * directions[i];
            Vector3 direction = transform.rotation * directions[i];
            direction.y = 0;
            direction.Normalize();

            //Projectile spawnedProjectile = Instantiate(_projectile, _connectedPlayerStats.transform.position + (direction * _shootOffset), Quaternion.LookRotation(direction));
            Projectile spawnedProjectile = Instantiate(_projectile, _connectedPlayerStats.transform.position + (direction * _shootOffset), Quaternion.LookRotation(direction));
            spawnedProjectile.ConnectedPlayerStats = _connectedPlayerStats;
            _spawnedProjectiles.Add(spawnedProjectile);
        }
    }
}
