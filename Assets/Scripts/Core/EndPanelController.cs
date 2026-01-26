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

    // 当 Panel 被 SetActive(true) 时自动调用
    private void OnEnable()
    {
        UIManager.Instance?.HideDialogue();
        StartCoroutine(PlayEndingSequence());
    }

    private IEnumerator PlayEndingSequence()
    {
        if (backButton != null) backButton.SetActive(false);
        
        for (int i = 0; i < slides.Count; i++)
        {
            EndingSlide currentSlide = slides[i];

            if (displayImage != null && currentSlide.image != null)
            {
                displayImage.sprite = currentSlide.image;
            }

            yield return new WaitForSecondsRealtime(currentSlide.duration);
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