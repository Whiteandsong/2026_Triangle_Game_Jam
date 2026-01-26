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
    public static event Action<int, int> OnScareChargesChanged; // (current, max)
    
    // Checkpoint events
    public static event Action<DivingBell> OnCheckpointActivated;
    
    // Hiding events
    public static event Action OnPlayerStartHiding;
    public static event Action OnPlayerStopHiding;
    
    // Scare skill event
    public static event Action OnPlayerUseScare;
    
    // Player hit event (for camera shake)
    public static event Action<float> OnPlayerHit; // float parameter is damage amount
    
    // Level events
    public static event Action<int> OnLevelStarted; // int parameter is level index
    public static event Action OnGameComplete;
    
    // Treasure events
    public static event Action<string> OnTreasureCollected; // string parameter is treasure name
    
    // Lighting events
    public static event Action<float, float> OnLightingChanged; // (globalIntensity, spotlightIntensity)
    
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
    
    public static void TriggerScareChargesChanged(int current, int max)
    {
        OnScareChargesChanged?.Invoke(current, max);
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
    
    public static void TriggerLevelStarted(int levelIndex)
    {
        OnLevelStarted?.Invoke(levelIndex);
        Debug.Log($"Event: Level {levelIndex} started");
    }
    
    public static void TriggerGameComplete()
    {
        OnGameComplete?.Invoke();
        Debug.Log("Event: Game completed!");
    }
    
    public static void TriggerTreasureCollected(string treasureName)
    {
        OnTreasureCollected?.Invoke(treasureName);
        Debug.Log($"Event: Treasure collected - {treasureName}");
    }
    
    public static void TriggerLightingChanged(float globalIntensity, float spotlightIntensity)
    {
        OnLightingChanged?.Invoke(globalIntensity, spotlightIntensity);
        Debug.Log($"Event: Lighting changed - Global={globalIntensity}, Spotlight={spotlightIntensity}");
    }
}
