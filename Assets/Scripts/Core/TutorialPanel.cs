using UnityEngine;
using UnityEngine.UI;

public class TutorialPanel : MonoBehaviour
{
    [Header("Tutorial Content")]
    public Sprite[] tutorialSprites;

    [Header("UI References")]
    public Image displayImage;
    
    [Tooltip("Previous)")]
    public Button leftButton;
    
    [Tooltip("Next")]
    public Button rightButton;
    
    [Tooltip("Start Game")]
    public Button startGameButton;

    // 当前查看到的图片索引
    private int currentIndex = 0;

    void Start()
    {
        // 绑定按钮事件（你也可以在Inspector里手动拖拽）
        leftButton.onClick.AddListener(OnLeftClick);
        rightButton.onClick.AddListener(OnRightClick);
        startGameButton.onClick.AddListener(OnStartGameClick);

        // 初始化显示第一张
        currentIndex = 0;
        UpdateUI();
    }

    private void OnEnable()
    {
        // 每次面板打开时，重置到第一页
        currentIndex = 0;
        UpdateUI();
    }

    // 点击左箭头
    private void OnLeftClick()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateUI();
        }
    }

    // 点击右箭头
    private void OnRightClick()
    {
        if (currentIndex < tutorialSprites.Length - 1)
        {
            currentIndex++;
            UpdateUI();
        }
    }

    // 点击正式开始
    private void OnStartGameClick()
    {
        // 1. 关闭教程面板
        gameObject.SetActive(false);

        // 2. 通知 LevelManager 正式开始第一关
        // 假设 index 0 是你的第一个游戏关卡
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadLevel(0, showDialogue: true);
        }
        else
        {
            Debug.LogError("LevelManager instance not found!");
        }
    }

    // 更新界面状态
    private void UpdateUI()
    {
        // 1. 更新图片
        if (tutorialSprites != null && tutorialSprites.Length > 0)
        {
            displayImage.sprite = tutorialSprites[currentIndex];
        }

        leftButton.gameObject.SetActive(currentIndex > 0);

        rightButton.gameObject.SetActive(currentIndex < tutorialSprites.Length - 1);
        
        startGameButton.gameObject.SetActive(currentIndex == tutorialSprites.Length - 1);
    }
}