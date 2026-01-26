using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Rendering.Universal;

[Serializable]
public class LevelData
{
    [Header("Basic Info")]
    public string levelName;
    
    [Header("Spawn Settings")]
    public Vector3 spawnPosition;
    
    [Header("Camera Bounds")]
    public float cameraMinY = -50f;
    public float cameraMaxY = 10f;
    
    [Header("Audio & Light")]
    public AudioClip levelBGM;
    [Range(0f, 100f)] public float globalLightIntensity = 1f;
    [Range(0f, 5f)] public float playerSpotlightIntensity = 1f;
    
    [Header("Requirements")]
    public string requiredTreasure = "";
    
    [Header("Dialogue")]
    [TextArea(2, 5)] public string startDialogue;
    [TextArea(2, 5)] public string transitionDialogue;
    
}

public class LevelManager : Singleton<LevelManager>
{
    [Header("Config")]
    [SerializeField] private LevelData[] levels;
    
    [Header("References")]
    [SerializeField] private LevelTransitionUI transitionUI; 
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private GameObject player;
    [SerializeField] private Light2D globalLight;
    [SerializeField] private Light2D playerSpotlight;
    
    private int currentLevelIndex = 0;
    public int CurrentLevelIndex => currentLevelIndex;
    public int TotalLevelCount => levels != null ? levels.Length : 0;
    
    protected override void Awake()
    {
        base.Awake();
        // 自动查找引用
        if (!cameraFollow && Camera.main) cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (!player) player = GameObject.FindGameObjectWithTag("Player");
        
        if (!globalLight)
        {
            var obj = GameObject.Find("Global Light 2D");
            if (obj) globalLight = obj.GetComponent<Light2D>();
        }
        
        if (!playerSpotlight && player) playerSpotlight = player.GetComponentInChildren<Light2D>();
        if (!transitionUI) transitionUI = FindFirstObjectByType<LevelTransitionUI>();
    }
    
    void Start()
    {
        if (levels != null && levels.Length > 0)
            LoadLevel(0, showDialogue: true);
    }

    // --- 数据加载逻辑 (只负责搬运数据，不负责流程等待) ---
    public void LoadLevel(int levelIndex, bool showDialogue = true, bool restoreStats = true)
    {
        if (levels == null || levelIndex < 0 || levelIndex >= levels.Length) return;
        
        currentLevelIndex = levelIndex;
        LevelData level = levels[levelIndex];
        
        // 1. 设置相机和光照
        if (cameraFollow)
        {
            cameraFollow.useBounds = true;
            cameraFollow.minY = level.cameraMinY;
            cameraFollow.maxY = level.cameraMaxY;
        }
        
        if (globalLight) globalLight.intensity = level.globalLightIntensity;
        if (playerSpotlight) playerSpotlight.intensity = level.playerSpotlightIntensity;
        GameEvents.TriggerLightingChanged(level.globalLightIntensity, level.playerSpotlightIntensity);
        
        // 2. 播放音乐
        if (level.levelBGM) AudioManager.Instance?.PlayBGM(level.levelBGM);
        
        // 3. 移动玩家位置 (这是导致瞬移的关键一步，必须在黑幕后调用)
        if (player)
        {
            player.transform.position = level.spawnPosition;
            if (player.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
            
            if (levelIndex == 3) 
            {
                var pc = player.GetComponent<PlayerController>();
                if (pc) pc.SwitchToLevel4Animator();
            }
        }
        
        // 4. 设置重生点和数值
        if (GameManager.Instance)
        {
            GameManager.Instance.SetDefaultRespawnPosition(level.spawnPosition);
            if (restoreStats) GameManager.Instance.RestoreFullStats();
        }
        
        // 5. 显示对话
        if (showDialogue && !string.IsNullOrEmpty(level.startDialogue))
            UIManager.Instance?.ShowDialogue(level.startDialogue);
            
        GameEvents.TriggerLevelStarted(levelIndex);
    }

    // --- 核心过渡流程 (严格控制顺序) ---

    public void TransitionToNextLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        
        // 结局判断
        if (nextIndex >= levels.Length)
        {
            TriggerGameEnd();
            return;
        }
        
        // 资格判断
        if (!CanEnterLevel(nextIndex)) return;
        
        // 启动协程：这里只启动，不直接LoadLevel
        StartCoroutine(ProcessLevelTransition(nextIndex));
    }

    private IEnumerator ProcessLevelTransition(int targetIndex)
    {
        // =================步骤 1: 冻结与黑幕开始=================
        
        // 1.1 立即暂停时间 (防止怪物攻击)
        Time.timeScale = 0f;
        
        // 1.2 禁用玩家控制 (防止玩家乱跑)
        if (player)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc) pc.enabled = false;
            // 清除速度，防止惯性
            if (player.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
        }

        // 1.3 播放 Exit 动画 (屏幕变黑)
        // 警告：确保你的 UI Animator UpdateMode 设置为 Unscaled Time，否则这里动画不动！
        float animDuration = 1f;
        if (transitionUI)
        {
            transitionUI.PlayExitAnimation();
            animDuration = transitionUI.transitionDuration;
        }
        
        // 1.4 等待变黑 (使用 Realtime，因为 timeScale 是 0)
        yield return new WaitForSecondsRealtime(animDuration);


        // =================步骤 2: 在黑幕中搬运玩家=================
        
        // 此时屏幕完全黑了，玩家看不见，现在才移动玩家位置！
        
        // 2.1 扣除理智
        if (GameManager.Instance)
            GameManager.Instance.ChangeSanity(-GameManager.Instance.MaxSanity * 0.25f);
            
        // 2.2 调用 LoadLevel 移动位置 (这是核心移动代码)
        LoadLevel(targetIndex, showDialogue: true, restoreStats: false);
        
        // 2.3 处理过场对话
        LevelData targetLevel = levels[targetIndex];
        if (!string.IsNullOrEmpty(targetLevel.transitionDialogue))
        {
            UIManager.Instance?.ShowDialogue(targetLevel.transitionDialogue);
        }

        // 2.4 缓冲一帧，确保相机已经瞬移到新位置，没有残影
        yield return null; 


        // =================步骤 3: 黑幕结束与解冻=================

        // 3.1 播放 Enter 动画 (屏幕变亮)
        if (transitionUI) transitionUI.PlayEnterAnimation();

        // 3.2 等待变亮动画播放完毕 (玩家此时已在出生点，但游戏仍暂停)
        yield return new WaitForSecondsRealtime(animDuration);

        // 3.3 动画结束，解冻时间，恢复玩家控制
        Time.timeScale = 1f;
        
        if (player)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc) pc.enabled = true;
        }
    }

    // --- 结局流程 ---

    public void TriggerGameEnd()
    {
        StartCoroutine(ProcessGameEndSequence());
    }

    private IEnumerator ProcessGameEndSequence()
    {
        Time.timeScale = 0f;
        
        if (player)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc) pc.enabled = false;
            if (player.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
        }

        float waitTime = 1f;
        if (transitionUI)
        {
            transitionUI.PlayExitAnimation();
            waitTime = transitionUI.transitionDuration;
        }

        yield return new WaitForSecondsRealtime(waitTime);

        // 保持黑屏并触发 UI
        GameEvents.TriggerGameComplete();
    }

    // --- Helpers ---

    public bool CanEnterLevel(int index)
    {
        if (index < 0 || index >= levels.Length) return false;
        string req = levels[index].requiredTreasure;
        if (string.IsNullOrEmpty(req)) return true;
        return GameManager.Instance != null && GameManager.Instance.HasTreasure(req);
    }

    public void LoadNextLevel() => LoadLevel(currentLevelIndex + 1);
    public void LoadPreviousLevel() => LoadLevel(currentLevelIndex - 1);
    public void ReloadCurrentLevel() => LoadLevel(currentLevelIndex, false);
    
    public string GetNextLevelRequiredTreasure()
    {
        int next = currentLevelIndex + 1;
        return (next < levels.Length) ? levels[next].requiredTreasure : "";
    }

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool enableDebugKeys = true;
    void Update()
    {
        if (!enableDebugKeys) return;
        if (Input.GetKeyDown(KeyCode.F1)) LoadLevel(0);
        if (Input.GetKeyDown(KeyCode.F2)) LoadLevel(1);
        if (Input.GetKeyDown(KeyCode.F3)) LoadLevel(2);
        if (Input.GetKeyDown(KeyCode.F4)) LoadLevel(3);
        if (Input.GetKeyDown(KeyCode.PageUp)) LoadPreviousLevel();
        if (Input.GetKeyDown(KeyCode.PageDown)) LoadNextLevel();
        if (Input.GetKeyDown(KeyCode.F5)) ReloadCurrentLevel();
    }
#endif
}