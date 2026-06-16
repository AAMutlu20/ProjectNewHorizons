using UnityEngine;

public class DatabaseSystem : MonoBehaviour
{
    [SerializeField] private bool _isSingleton = false;
    public static DatabaseSystem Singleton;

    [SerializeField] private SOS_AbilityDatabase _AbilityDatabase;

    public SOS_AbilityDatabase AbilityDatabase { get { return _AbilityDatabase; } }

    private void Awake()
    {
        if(_isSingleton)
        {
            if (Singleton != null && Singleton != this)
            {
                _isSingleton = false;
            }
            else
            {
                Singleton = this;
            }
        }

    }
}
