using UnityEngine;
using System;

public static class GameEvents
{
    // Player events
    public static event Action OnPlayerDeath;
    public static event Action OnPlayerInsane;
    public static event Action<Vector3> OnPlayerRespawn;
    
    // Resource events
    public static event Action<float> OnOxygenChanged;
    public static event Action<float> OnSanityChanged;
    
    // Checkpoint events
    public static event Action<DivingBell> OnCheckpointActivated;
    
    // Invoke methods
    public static void TriggerPlayerDeath()
    {
        OnPlayerDeath?.Invoke();
        Debug.Log("Event: Player Death triggered");
    }
    
    public static void TriggerPlayerInsane()
    {
        OnPlayerInsane?.Invoke();
        Debug.Log("Event: Player went insane");
    }
    
    public static void TriggerPlayerRespawn(Vector3 position)
    {
        OnPlayerRespawn?.Invoke(position);
        Debug.Log($"Event: Player Respawn at {position}");
    }
    
    public static void TriggerOxygenChanged(float normalizedAmount)
    {
        OnOxygenChanged?.Invoke(normalizedAmount);
    }
    
    public static void TriggerSanityChanged(float normalizedAmount)
    {
        OnSanityChanged?.Invoke(normalizedAmount);
    }
    
    public static void TriggerCheckpointActivated(DivingBell checkpoint)
    {
        OnCheckpointActivated?.Invoke(checkpoint);
        Debug.Log($"Event: Checkpoint {checkpoint.name} activated");
    }
}
