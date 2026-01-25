using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : Singleton<UIManager>
{
    [Header("UI Component References")]
    [SerializeField] private Image oxygenFillImage;
    [SerializeField] private Image sanityFillImage;
    [SerializeField] private TMP_Text dialogueText;
    
    [Header("Typewriter Settings")]
    [SerializeField] private float typewriterSpeed = 0.05f;
    
    private Coroutine typewriterCoroutine;

    protected override void Awake()
    {
        base.Awake();
        
        // Find and assign UI components if not set in inspector
        oxygenFillImage = GameObject.Find("OxygenBar").transform.Find("Fill").GetComponent<Image>();
        sanityFillImage = GameObject.Find("SanityBar").transform.Find("Fill").GetComponent<Image>();
        
        // Try to find dialogue text if not assigned
        if (dialogueText == null)
        {
            GameObject dialogueObj = GameObject.Find("DialogueText");
            if (dialogueObj != null)
                dialogueText = dialogueObj.GetComponent<TMP_Text>();
        }
        
        if (oxygenFillImage == null) Debug.LogError("Oxygen Fill Image not found!");
        if (sanityFillImage == null) Debug.LogError("Sanity Fill Image not found!");
    }
    
    void OnEnable()
    {
        // Subscribe to game events
        GameEvents.OnOxygenChanged += UpdateOxygenUI;
        GameEvents.OnSanityChanged += UpdateSanityUI;
    }
    
    void OnDisable()
    {
        // Unsubscribe from game events
        GameEvents.OnOxygenChanged -= UpdateOxygenUI;
        GameEvents.OnSanityChanged -= UpdateSanityUI;
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
    
    #region Dialogue System
    // 显示对话文本，使用打字机效果
    public void ShowDialogue(string text, bool instant = false)
    {
        if (dialogueText == null)
        {
            Debug.LogWarning("Dialogue Text is not assigned!");
            return;
        }
        
        // 停止之前的打字机效果
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
        
        if (instant)
        {
            // 立即显示全部文本
            dialogueText.text = text;
        }
        else
        {
            // 使用打字机效果
            typewriterCoroutine = StartCoroutine(TypewriterEffect(text));
        }
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
        dialogueText.text = fullText;
        
        dialogueText.ForceMeshUpdate();
        
        int totalCharacters = dialogueText.textInfo.characterCount;
        
        dialogueText.maxVisibleCharacters = 0;
        
        for (int i = 0; i <= totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typewriterSpeed);
        }
        
        typewriterCoroutine = null;
    }
    
    public void SetTypewriterSpeed(float speed)
    {
        typewriterSpeed = Mathf.Max(0.01f, speed);
    }
    
    #endregion
}