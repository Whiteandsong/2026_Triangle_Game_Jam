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
    
    // Hiding events
    public static event Action OnPlayerStartHiding;
    public static event Action OnPlayerStopHiding;
    
    // Scare skill event
    public static event Action OnPlayerUseScare;
    
    // Player hit event (for camera shake)
    public static event Action<float> OnPlayerHit; // float parameter is damage amount
    
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
    
    public static void TriggerPlayerStartHiding()
    {
        OnPlayerStartHiding?.Invoke();
        Debug.Log("Event: Player started hiding");
    }
    
    public static void TriggerPlayerStopHiding()
    {
        OnPlayerStopHiding?.Invoke();
        Debug.Log("Event: Player stopped hiding");
    }
    
    public static void TriggerPlayerUseScare()
    {
        OnPlayerUseScare?.Invoke();
        Debug.Log("Event: Player used scare skill");
    }
    
    public static void TriggerPlayerHit(float damage)
    {
        OnPlayerHit?.Invoke(damage);
        Debug.Log($"Event: Player hit for {damage} damage");
    }
}
