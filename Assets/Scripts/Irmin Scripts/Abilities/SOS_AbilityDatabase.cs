using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_AbilityDatabase", menuName = "Scriptable Objects/SOS_AbilityDatabase")]
public class SOS_AbilityDatabase : ScriptableObject
{
    [SerializeField] private List<Ability_Base> _abilities = new();

    public List<Ability_Base> Abilities { get { return _abilities; } }

}
