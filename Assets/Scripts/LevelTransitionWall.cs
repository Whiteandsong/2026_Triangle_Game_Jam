using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LevelTransitionWall : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private bool isNextLevelWall = true;
    [SerializeField] private int targetLevelIndex = -1;

    [Header("Requirements (For Game End Only)")]
    [SerializeField] private string requiredTreasureForEnding = ""; 
    
    [Header("Feedback")]
    [SerializeField] private AudioClip blockedSound;
    [SerializeField] private string blockedMessage = "Seems like I can't go further...maybe I miss something.";
    
    private bool isTriggered = false;
    
    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (isTriggered) return;
        if (LevelManager.Instance == null) return;
        
        // 1. 计算目标索引
        int currentIdx = LevelManager.Instance.CurrentLevelIndex;
        // 如果勾选了自动下一关，就+1，否则用指定索引
        int targetLevel = isNextLevelWall ? currentIdx + 1 : targetLevelIndex;
        
        // 获取总关卡数
        int totalLevels = LevelManager.Instance.TotalLevelCount; 
        // 【情况 A：通往结局 (Target Index 超出了数组范围)】
        if (isNextLevelWall && targetLevel >= totalLevels)
        {
            // 检查墙上写的宝箱要求
            if (CheckEndingRequirement())
            {
                // 满足要求，触发结局
                isTriggered = true;
                Debug.Log("LevelTransitionWall: Ending Requirements Met! Triggering Game End.");
                LevelManager.Instance.TriggerGameEnd();
            }
            else
            {
                // 不满足要求，弹出提示
                ShowBlockedMessage();
                Debug.Log("LevelTransitionWall: Blocked from Ending. Missing required treasure.");
            }
            return;
        }

        // 【情况 B：普通关卡切换】
        // 让 LevelManager 去检查下一关的数据配置
        if (LevelManager.Instance.CanEnterLevel(targetLevel))
        {
            isTriggered = true;
            Debug.Log($"LevelTransitionWall: Entering level {targetLevel}...");
            LevelManager.Instance.TransitionToNextLevel();
        }
        else
        {
            ShowBlockedMessage();
            Debug.Log($"LevelTransitionWall: Blocked from entering {targetLevel}.");
        }
    }
    
    // 检查结局要求
    private bool CheckEndingRequirement()
    {
        // 如果墙上没写要求，直接放行
        if (string.IsNullOrEmpty(requiredTreasureForEnding)) return true;

        // 检查 GameManager 是否有这个宝藏
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.HasTreasure(requiredTreasureForEnding);
        }
        
        return false;
    }

    private void ShowBlockedMessage()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDialogue(blockedMessage);
        }
        // 这里可以加播放音效 blockedSound
    }
    
    public void ResetTrigger()
    {
        isTriggered = false;
    }
}