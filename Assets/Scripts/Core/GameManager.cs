using UnityEngine;
using System.Collections.Generic;
public class GameManager : Singleton<GameManager>
{
    [Header("Player Stats Settings")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float maxSanity = 100f;
    [SerializeField] private int maxScareCharges = 3;

    [Header("Checkpoint System")]
    private DivingBell currentCheckpoint;
    private Vector3 defaultRespawnPosition = Vector3.zero;
    private int checkpointScareCharges = 0; // checkpoint保存的scare次数
    
    [Header("Treasure Collection")]
    private HashSet<string> collectedTreasures = new HashSet<string>();
    
    public float CurrentOxygen { get; private set; }
    public float CurrentSanity { get; private set; }
    public int CurrentScareCharges { get; private set; }

    public float MaxSanity => maxSanity;
    public float MaxOxygen => maxOxygen;

    [Header("UI Elements")]
    [SerializeField] private GameObject gameOverPanel;

    protected override void Awake()
    {
        base.Awake();
        CurrentOxygen = maxOxygen;
        CurrentSanity = maxSanity;
        
        // 从PlayerPrefs加载scare次数，默认为3次
        CurrentScareCharges = PlayerPrefs.GetInt("ScareCharges", maxScareCharges);
        checkpointScareCharges = CurrentScareCharges;
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
        GameEvents.OnGameComplete += HandleGameComplete;
        GameEvents.OnLevelStarted += OnLevelStarted;
    }
    
    void OnDisable()
    {
        // 取消订阅
        GameEvents.OnCheckpointActivated -= OnCheckpointActivated;
        GameEvents.OnPlayerDeath -= HandlePlayerDeath;
        GameEvents.OnPlayerInsane -= HandlePlayerInsane;
        GameEvents.OnGameComplete -= HandleGameComplete;
        GameEvents.OnLevelStarted -= OnLevelStarted;
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
        GameEvents.TriggerScareChargesChanged(CurrentScareCharges, maxScareCharges);
    }
    
    #region Checkpoint System
    
    private void OnCheckpointActivated(DivingBell checkpoint)
    {
        currentCheckpoint = checkpoint;
        
        // Diving Bell激活时恢复一次scare次数（最多3次）
        if (CurrentScareCharges < maxScareCharges)
        {
            CurrentScareCharges++;
            SaveScareCharges();
            GameEvents.TriggerScareChargesChanged(CurrentScareCharges, maxScareCharges);
            Debug.Log($"Diving Bell activated! Scare charge restored to {CurrentScareCharges}/{maxScareCharges}");
        }
        
        // 保存当前的scare次数到checkpoint
        checkpointScareCharges = CurrentScareCharges;
        
        Debug.Log($"Current checkpoint set to: {checkpoint.name}");
    }
    
    private void OnLevelStarted(int levelIndex)
    {
        // 关卡切换时保存当前的scare次数
        SaveScareCharges();
        checkpointScareCharges = CurrentScareCharges;
        Debug.Log($"Level {levelIndex} started. Scare charges saved: {CurrentScareCharges}/{maxScareCharges}");
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
        //不恢复sanity
        // CurrentSanity = maxSanity;
        
        // 恢复checkpoint保存的scare次数，而不是最大值
        CurrentScareCharges = checkpointScareCharges;
        
        UpdateAllUI();
        
        // get respawn position
        Vector3 respawnPosition = currentCheckpoint != null 
            ? currentCheckpoint.RespawnPosition
            : defaultRespawnPosition;
        
        // 防止玩家在重生时被怪物攻击
        GameObject.FindGameObjectWithTag("Player").gameObject.GetComponent<PlayerController>().StartHiding(2f);
        // trigger respawn event
        GameEvents.TriggerPlayerRespawn(respawnPosition);
    }
    
    public void SetDefaultRespawnPosition(Vector3 position)
    {
        currentCheckpoint = null;
        defaultRespawnPosition = position;
    }
    
    // 重置游戏状态，用于开始新游戏
    public void ResetGame()
    {
        // 1. 重置数值
        CurrentOxygen = maxOxygen;
        CurrentSanity = maxSanity;
        CurrentScareCharges = maxScareCharges;
        checkpointScareCharges = maxScareCharges;

        // 2. 清空checkpoint和宝箱收集
        currentCheckpoint = null;
        collectedTreasures.Clear();
        
        // 保存初始的scare次数到PlayerPrefs
        PlayerPrefs.SetInt("ScareCharges", maxScareCharges);
        PlayerPrefs.Save();

        // 4. 刷新UI
        UpdateAllUI();

        // 5. 重新加载第一关（刷新场景和出生点）
        LevelManager.Instance?.ResetLevels();
    }
    
    // 恢复玩家满状态（用于手动切换关卡/调试）
    public void RestoreFullStats()
    {
        CurrentOxygen = maxOxygen;
        //CurrentSanity = maxSanity;
        UpdateAllUI();
    }
    
    #endregion
    
    #region Scare System
    
    // 检查是否还有Scare次数
    public bool HasScareCharges()
    {
        return CurrentScareCharges > 0;
    }
    
    // 使用一次Scare次数
    public bool UseScareCharge()
    {
        if (CurrentScareCharges > 0)
        {
            CurrentScareCharges--;
            SaveScareCharges();
            GameEvents.TriggerScareChargesChanged(CurrentScareCharges, maxScareCharges);
            Debug.Log($"Scare charge used. Remaining: {CurrentScareCharges}/{maxScareCharges}");
            return true;
        }
        else
        {
            Debug.LogWarning("No scare charges remaining!");
            return false;
        }
    }
    
    // 重置Scare次数（用于特殊情况，如回到checkpoint）
    public void ResetScareCharges()
    {
        CurrentScareCharges = maxScareCharges;
        SaveScareCharges();
        GameEvents.TriggerScareChargesChanged(CurrentScareCharges, maxScareCharges);
    }
    
    // 保存Scare次数到PlayerPrefs
    private void SaveScareCharges()
    {
        PlayerPrefs.SetInt("ScareCharges", CurrentScareCharges);
        PlayerPrefs.Save();
    }
    
    #endregion
    
    #region Treasure System
    
    // 添加收集的宝藏
    public void AddTreasure(string treasureName)
    {
        if (string.IsNullOrEmpty(treasureName)) return;
        
        if (collectedTreasures.Add(treasureName))
        {
            Debug.Log($"Treasure collected: {treasureName}");
            GameEvents.TriggerTreasureCollected(treasureName);
        }
    }
    
    // 检查是否拥有指定宝藏
    public bool HasTreasure(string treasureName)
    {
        return !string.IsNullOrEmpty(treasureName) && collectedTreasures.Contains(treasureName);
    }
    
    // 获取已收集的宝藏数量
    public int GetTreasureCount()
    {
        return collectedTreasures.Count;
    }
    
    #endregion


    private void HandleGameComplete()
    {
        if (gameOverPanel != null) {gameOverPanel.SetActive(true);}
    }
}