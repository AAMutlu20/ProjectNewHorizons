using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Stats : MonoBehaviour
{
    [SerializeField] private float _damageValue;
    // Two hundred should fire two projectiles per thingy
    [SerializeField] private float _attackSpeedPercentage;
    [SerializeField] private float _abilityPercentageValue;

    [SerializeField] private int _projectileAmountValue;

    [SerializeField] private float _projectileSpeedPercentage;

    [SerializeField] private List<float> _resistancesByDamageType = new();
    [SerializeField] private List<float> _addDamageByDamageType = new();


    public float DamageValue { get { return _damageValue; } set { _damageValue = value; } }
    public float AttackSpeedPercentage { get { return _attackSpeedPercentage; } set { _attackSpeedPercentage = value; } }
    public float AbilityPercentageValue { get { return _abilityPercentageValue; } set { _abilityPercentageValue = value; } }

    public int ProjectileAmountValue { get { return _projectileAmountValue; } set {_projectileAmountValue = value; } }

    public float ProjectileSpeedPercentage { get { return _projectileSpeedPercentage; } set { _projectileSpeedPercentage = value; } }
}
