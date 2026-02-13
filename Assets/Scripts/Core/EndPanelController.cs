using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // 必须引用，用于 List

public class EndingPanelController : MonoBehaviour
{
    [System.Serializable]
    public struct EndingSlide
    {
        public Sprite image;
        public float duration;
    }

    [Header("UI References")]
    [SerializeField] private Image displayImage; 
    [SerializeField] private GameObject backButton;

    [Header("Slideshow Settings")]
    [SerializeField] private List<EndingSlide> slides = new List<EndingSlide>();
    [SerializeField] private bool stayOnLastImage = true;

    private int currentSlideIndex = 0;
    private bool skipToNext = false;

    // 当 Panel 被 SetActive(true) 时自动调用
    private void OnEnable()
    {
        UIManager.Instance?.HideDialogue();
        currentSlideIndex = 0;
        skipToNext = false;
        StartCoroutine(PlayEndingSequence());
    }

    public void SkipToNextSlide()
    {
        skipToNext = true;
    }

    private IEnumerator PlayEndingSequence()
    {
        if (backButton != null) backButton.SetActive(false);
        
        for (int i = 0; i < slides.Count; i++)
        {
            currentSlideIndex = i;
            skipToNext = false;
            
            EndingSlide currentSlide = slides[i];

            if (displayImage != null && currentSlide.image != null)
            {
                displayImage.sprite = currentSlide.image;
            }

            float timer = 0f;
            while (timer < currentSlide.duration && !skipToNext)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        
        if (!stayOnLastImage && displayImage != null)
        {
            displayImage.sprite = null; 
        }

        if (backButton != null) backButton.SetActive(true);
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}