using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject hudPanel;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip bgmMenuBGM;

    private bool isGamePlaying = false;

    private void Awake()
    {
        // 自动查找UI面板
        GameObject uiCanvas = GameObject.Find("UICanvas");
        if (uiCanvas != null)
        {
            mainMenuPanel = uiCanvas.transform.Find("MainMenu")?.gameObject;
            pauseMenuPanel = uiCanvas.transform.Find("PauseMenu")?.gameObject;
            settingPanel = uiCanvas.transform.Find("SettingPanel")?.gameObject;
            hudPanel = uiCanvas.transform.Find("HUD")?.gameObject;

            if (mainMenuPanel == null) Debug.LogError("MainMenu panel not found!");
            if (pauseMenuPanel == null) Debug.LogError("PauseMenu panel not found!");
            if (settingPanel == null) Debug.LogError("SettingPanel not found!");
            if (hudPanel == null) Debug.LogError("HUD panel not found!");
        }
        else
        {
            Debug.LogError("UICanvas not found!");
        }
    }

    private void Start()
    {
        // 游戏开始时显示主菜单
        ShowMainMenu();

        // 播放主菜单背景音乐
        if (bgmMenuBGM != null) AudioManager.Instance.PlayBGM(bgmMenuBGM);
    }

    private void Update()
    {
        // 游戏进行中按ESC打开暂停菜单
        if (Input.GetKeyDown(KeyCode.Escape) && isGamePlaying)
        {
            PauseGame();
        }
    }

    // 显示主菜单
    private void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        
        isGamePlaying = false;
        Time.timeScale = 0f;
    }

    // 点击Start按钮 - 开始游戏
    public void OnStartButtonClicked()
    {
        // 关闭主菜单
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        
        // 显示HUD
        if (hudPanel != null) hudPanel.SetActive(true);
        
        isGamePlaying = true;
        Time.timeScale = 1f;
    }

    // 从主菜单打开设置面板
    public void OnSettingsButtonClicked()
    {
        if (settingPanel != null) settingPanel.SetActive(true);
    }

    // 从暂停菜单打开设置面板
    public void OnPauseSettingsButtonClicked()
    {
        if (settingPanel != null) settingPanel.SetActive(true);
    }

    // 关闭设置面板
    public void OnSettingsBackButtonClicked()
    {
        if (settingPanel != null) settingPanel.SetActive(false);
    }

    // 点击Exit按钮 - 退出游戏
    public void OnExitButtonClicked()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // 暂停游戏（按ESC或手动调用）
    public void PauseGame()
    {
        Time.timeScale = 0f;
        isGamePlaying = false;
        
        if (hudPanel != null) hudPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    // 从暂停菜单恢复游戏
    public void ResumeGame()
    {
        Time.timeScale = 1f;
        isGamePlaying = true;
        
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
    }

    // 从暂停菜单回到主菜单
    public void BackToMainMenu()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        ShowMainMenu();
    }
}
