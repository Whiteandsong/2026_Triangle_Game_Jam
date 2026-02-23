using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel; // 结局面板
    [SerializeField] private GameObject tutorialPanel;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip bgmMenuBGM;
    [SerializeField] private AudioClip buttonClickSound;


    [SerializeField] private List<GameObject> panelsToHide = new List<GameObject>();

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
            gameOverPanel = uiCanvas.transform.Find("GameOverPanel")?.gameObject; // 确保你的Panel叫这个名字
            tutorialPanel = uiCanvas.transform.Find("TutorialPanel")?.gameObject;

            if (mainMenuPanel == null) Debug.LogError("MainMenu panel not found!");
            if (pauseMenuPanel == null) Debug.LogError("PauseMenu panel not found!");
            if (settingPanel == null) Debug.LogError("SettingPanel not found!");
            if (hudPanel == null) Debug.LogError("HUD panel not found!");
            if (gameOverPanel == null) Debug.LogError("GameOverPanel not found!");
        }
        else
        {
            Debug.LogError("UICanvas not found!");
        }
    }

    private void OnEnable()
    {
        ForceClosePanels();
        GameEvents.OnGameComplete += ShowGameOverPanel;
    }

    private void OnDisable()
    {
        GameEvents.OnGameComplete -= ShowGameOverPanel;
    }

    private void Start()
    {
        ShowMainMenu();
        if (bgmMenuBGM != null) AudioManager.Instance.PlayBGM(bgmMenuBGM);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnESCButtonClicked();
        }
    }

    private void ForceClosePanels()
    {
        foreach (var panel in panelsToHide)
        {
            if (panel != null && panel.activeSelf)
            {
                panel.SetActive(false);
            }
        }
    }

    // 显示主菜单
    private void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        
        isGamePlaying = false;
        Time.timeScale = 0f;
    }

    // GameOver
    private void ShowGameOverPanel()
    {
        // 隐藏其他面板
        if (hudPanel != null) hudPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        
        // 显示结局面板 (这将触发 EndingPanelController 的 OnEnable)
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // 确保光标显示（如果游戏里隐藏了光标的话）
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // MainMenu Start -> Tutorial Panel
    public void OnTutorialButtonClicked()
    {
        PlayButtonClickSound();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
    }

    // 点击Start按钮 - 开始游戏
    public void OnStartButtonClicked()
    {
        PlayButtonClickSound(); 
        
        if (GameManager.Instance != null)
            GameManager.Instance.ResetGame();
        
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false); // 确保关闭结局面板
        if (hudPanel != null) hudPanel.SetActive(true);
        
        isGamePlaying = true;
        Time.timeScale = 1f;
    }

    public void OnSettingsButtonClicked() { PlayButtonClickSound(); if (settingPanel) settingPanel.SetActive(true); }
    public void OnPauseSettingsButtonClicked() { PlayButtonClickSound(); if (settingPanel) settingPanel.SetActive(true); }
    public void OnSettingsBackButtonClicked() { PlayButtonClickSound(); if (settingPanel) settingPanel.SetActive(false); }
    
    public void OnExitButtonClicked()
    {
        PlayButtonClickSound();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // Pause or Resume
    public void OnESCButtonClicked()
    {
        if (isGamePlaying)
            PauseGame();
        else
            ResumeGame();
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        isGamePlaying = false;
        if (hudPanel) hudPanel.SetActive(false);
        if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
    }

    public void ResumeGame()
    {
        PlayButtonClickSound();
        Time.timeScale = 1f;
        isGamePlaying = true;
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (hudPanel) hudPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        PlayButtonClickSound();
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false); // 也可以从结局面板回主菜单
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        ShowMainMenu();
    }
    
    private void PlayButtonClickSound()
    {
        if (buttonClickSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(buttonClickSound);
    }
}