using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : Singleton<UIManager>
{
    [Header("UI Component References")]
    [SerializeField] private Image oxygenFillImage;
    [SerializeField] private Image sanityFillImage;
    [SerializeField] private Image scareFillImage;
    [SerializeField] private TMP_Text dialogueText;
    
    [Header("Typewriter Settings")]
    [SerializeField] private float typewriterSpeed = 0.05f;
    [SerializeField] private float dialogueDisplayDuration = 5f;
    
    private Coroutine typewriterCoroutine;

    protected override void Awake()
    {
        base.Awake();
        
        // Find and assign UI components if not set in inspector
        if(oxygenFillImage == null)
            oxygenFillImage = GameObject.Find("OxygenBar").transform.Find("Fill").GetComponent<Image>();
        if(sanityFillImage == null)
            sanityFillImage = GameObject.Find("SanityBar").transform.Find("Fill").GetComponent<Image>();
        if(scareFillImage == null)
        {
            GameObject scareBarObj = GameObject.Find("ScareBar");
            if (scareBarObj != null)
                scareFillImage = scareBarObj.transform.Find("Fill").GetComponent<Image>();
        }
        
        // Try to find dialogue text if not assigned
        if (dialogueText == null)
        {
            GameObject dialogueObj = GameObject.Find("DialogueText");
            if (dialogueObj != null)
                dialogueText = dialogueObj.GetComponent<TMP_Text>();
            dialogueText.gameObject.SetActive(false);
        }
        
        if (oxygenFillImage == null) Debug.LogError("Oxygen Fill Image not found!");
        if (sanityFillImage == null) Debug.LogError("Sanity Fill Image not found!");
        if (scareFillImage == null) Debug.LogError("Scare Fill Image not found!");
    }

    void Start()
    {
        HideDialogue();
    }
    
    void OnEnable()
    {
        // Subscribe to game events
        GameEvents.OnOxygenChanged += UpdateOxygenUI;
        GameEvents.OnSanityChanged += UpdateSanityUI;
        GameEvents.OnScareChargesChanged += UpdateScareUI;
    }
    
    void OnDisable()
    {
        // Unsubscribe from game events
        GameEvents.OnOxygenChanged -= UpdateOxygenUI;
        GameEvents.OnSanityChanged -= UpdateSanityUI;
        GameEvents.OnScareChargesChanged -= UpdateScareUI;
    }

    // Update Oxygen Bar UI
    public void UpdateOxygenUI(float fillAmount)
    {
        if (oxygenFillImage != null)
        {
            oxygenFillImage.fillAmount = Mathf.Clamp01(fillAmount);
        }
    }

    // Update Sanity Bar UI
    public void UpdateSanityUI(float fillAmount)
    {
        if (sanityFillImage != null)
        {
            sanityFillImage.fillAmount = Mathf.Clamp01(fillAmount);
        }
    }
    
    // Update Scare Bar UI
    public void UpdateScareUI(int charges, int maxCharges)
    {
        if (scareFillImage != null)
        {
            float fillAmount = maxCharges > 0 ? (float)charges / maxCharges : 0f;
            scareFillImage.fillAmount = fillAmount;
        }
    }
    
    #region Dialogue System
    // 显示对话文本，使用打字机效果
    public void ShowDialogue(string text, bool instant = false)
    {
        if (dialogueText == null) return;

        // 1. 激活文本对象
        dialogueText.gameObject.SetActive(true);
        
        // 2. 停止所有之前的协程和计时器，防止冲突
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        CancelInvoke(nameof(HideDialogue));

        // 3. 开启新的打字机协程
        typewriterCoroutine = StartCoroutine(TypewriterEffect(text));
    }
    
    /// 清空对话文本
    public void ClearDialogue()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
    }
    
    // 跳过打字机效果，立即显示全部文本
    public void SkipTypewriter()
    {
        if (typewriterCoroutine != null && dialogueText != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
            
            dialogueText.maxVisibleCharacters = int.MaxValue;
        }
    }
    
    private IEnumerator TypewriterEffect(string fullText)
    {
        // 设置完整文本，但通过 maxVisibleCharacters 控制可见性
        dialogueText.text = fullText;
        dialogueText.maxVisibleCharacters = 0; // 初始不可见

        // 强制刷新网格，获取正确的字符数量
        dialogueText.ForceMeshUpdate();
        int totalCharacters = dialogueText.textInfo.characterCount;

        // 逐字显示
        for (int i = 0; i <= totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            // 如果是空格，不等待，直接跳过，增加流畅度
            if (i < fullText.Length && fullText[i] != ' ') 
            {
                yield return new WaitForSeconds(typewriterSpeed);
            }
        }
        
        // 打字结束，引用置空
        typewriterCoroutine = null;

        // 启动自动隐藏计时器
        Invoke(nameof(HideDialogue), dialogueDisplayDuration);
    }
    
    /// 隐藏对话框
    public void HideDialogue()
    {
        if (dialogueText != null)
        {
            dialogueText.text = ""; // 清空内容
            dialogueText.gameObject.SetActive(false);
        }
    }
    
    public void SetTypewriterSpeed(float speed)
    {
        typewriterSpeed = Mathf.Max(0.01f, speed);
    }
    
    #endregion
}