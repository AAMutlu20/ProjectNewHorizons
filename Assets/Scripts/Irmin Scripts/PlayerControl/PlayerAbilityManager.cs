using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAbilityManager : MonoBehaviour
{
    [SerializeField] private List<Ability_Base> _spawnedAbilities = new();

    [SerializeField] private int _abilityIndexToSpawn;

    [SerializeField] private Stats _connectedPlayerStats;

    public void SpawnAbility(int pAbilityIndex)
    {
        Ability_Base foundAbility = DatabaseSystem.Singleton.AbilityDatabase.Abilities[pAbilityIndex];
        Ability_Base spawnedAbility = Instantiate(foundAbility, transform);
        spawnedAbility.ConnectedPlayerStats = _connectedPlayerStats;
        spawnedAbility.transform.rotation = transform.rotation;
        _spawnedAbilities.Add(spawnedAbility);
    }

    public void SpawnSelectedAbility()
    {
        SpawnAbility(_abilityIndexToSpawn);
    }

    public void RemoveAbility(int pAbilityIndex) 
    {
        if(pAbilityIndex < 0 || pAbilityIndex >= _spawnedAbilities.Count)
        {
            Debug.LogError("Tried to remove ability with invalid index. Cancelling.");
            return;
        }
        Ability_Base foundAbility = _spawnedAbilities[pAbilityIndex];
        if (foundAbility != null)
        {
            _spawnedAbilities.RemoveAt(pAbilityIndex);
            Destroy(foundAbility.gameObject);
        }
        else
        {
            _spawnedAbilities.RemoveAt(pAbilityIndex);
            Debug.LogError("Tried to remove null ability. Cancelling. Removed from list.");
        }
    }
}
