using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [Header("UI Component References")]
    [SerializeField] private Image oxygenFillImage;
    [SerializeField] private Image sanityFillImage;

    protected override void Awake()
    {
        base.Awake();
        
        // Find and assign UI components if not set in inspector
        oxygenFillImage = GameObject.Find("OxygenBar").transform.Find("Fill").GetComponent<Image>();
        sanityFillImage = GameObject.Find("SanityBar").transform.Find("Fill").GetComponent<Image>();
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
}