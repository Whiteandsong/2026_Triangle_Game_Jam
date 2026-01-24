using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [Header("Player Stats Settings")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float maxSanity = 100f;

    [Header("Checkpoint System")]
    private DivingBell currentCheckpoint;
    private Vector3 defaultRespawnPosition = Vector3.zero;
    
    public float CurrentOxygen { get; private set; }
    public float CurrentSanity { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        CurrentOxygen = maxOxygen;
        CurrentSanity = maxSanity;
    }

    void Start()
    {
        // Initialize UI
        UpdateAllUI();
    }
    
    void OnEnable()
    {
        // 订阅checkpoint激活事件
        GameEvents.OnCheckpointActivated += OnCheckpointActivated;
        GameEvents.OnPlayerDeath += HandlePlayerDeath;
        GameEvents.OnPlayerInsane += HandlePlayerInsane;
    }
    
    void OnDisable()
    {
        // 取消订阅
        GameEvents.OnCheckpointActivated -= OnCheckpointActivated;
        GameEvents.OnPlayerDeath -= HandlePlayerDeath;
        GameEvents.OnPlayerInsane -= HandlePlayerInsane;
    }

    // Change Oxygen
    public void ChangeOxygen(float amount)
    {
        CurrentOxygen = Mathf.Clamp(CurrentOxygen + amount, 0, maxOxygen);
        float normalizedOxygen = CurrentOxygen / maxOxygen;
        
        // Trigger event to update UI
        GameEvents.TriggerOxygenChanged(normalizedOxygen);
        
        // Check if player runs out of oxygen
        if (CurrentOxygen <= 0)
        {
            GameEvents.TriggerPlayerDeath();
        }
    }   

    // Change Sanity
    public void ChangeSanity(float amount)
    {
        CurrentSanity = Mathf.Clamp(CurrentSanity + amount, 0, maxSanity);
        float normalizedSanity = CurrentSanity / maxSanity;
        
        // Trigger event to update UI
        GameEvents.TriggerSanityChanged(normalizedSanity);
        
        // Check if player goes insane
        if (CurrentSanity <= 0)
        {
            GameEvents.TriggerPlayerInsane();
        }
    }

    // Update all UI elements
    private void UpdateAllUI()
    {
        GameEvents.TriggerOxygenChanged(CurrentOxygen / maxOxygen);
        GameEvents.TriggerSanityChanged(CurrentSanity / maxSanity);
    }
    
    #region Checkpoint System
    
    private void OnCheckpointActivated(DivingBell checkpoint)
    {
        currentCheckpoint = checkpoint;
        Debug.Log($"Current checkpoint set to: {checkpoint.name}");
    }
    
    private void HandlePlayerDeath()
    {
        Debug.Log("Player died, preparing respawn...");
        RespawnPlayer();
    }
    
    private void HandlePlayerInsane()
    {
        Debug.Log("Player went insane, preparing respawn...");
        RespawnPlayer();
    }
    
    private void RespawnPlayer()
    {
        // restore player stats
        CurrentOxygen = maxOxygen;
        CurrentSanity = maxSanity;
        UpdateAllUI();
        
        // get respawn position
        Vector3 respawnPosition = currentCheckpoint != null 
            ? currentCheckpoint.RespawnPosition
            : defaultRespawnPosition;
        
        // trigger respawn event
        GameEvents.TriggerPlayerRespawn(respawnPosition);
    }
    
    public void SetDefaultRespawnPosition(Vector3 position)
    {
        defaultRespawnPosition = position;
    }
    
    #endregion
}