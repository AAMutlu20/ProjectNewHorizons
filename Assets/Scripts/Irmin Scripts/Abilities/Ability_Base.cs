using UnityEngine;

public class Ability_Base : MonoBehaviour
{
    [SerializeField] protected Stats _connectedPlayerStats;

    public Stats ConnectedPlayerStats { get { return _connectedPlayerStats; } set { _connectedPlayerStats = value; } }
}
