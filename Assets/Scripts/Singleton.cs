using UnityEngine;

/// Generic Singleton base class for MonoBehaviours
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<T>();
                
                if (instance == null)
                {
                    Debug.LogError($"An instance of {typeof(T)} is needed in the scene, but there is none.");
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
        }
        else if (instance != this)
        {
            Debug.LogWarning($"Duplicate instance of {typeof(T)} found. Destroying {gameObject.name}");
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
