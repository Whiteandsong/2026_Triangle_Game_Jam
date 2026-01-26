using UnityEngine;

public class DivingBell : MonoBehaviour, IInteractable
{
    [Header("Checkpoint Settings")]
    [SerializeField] private bool isActivated = false;
    
    public bool CanInteract => !isActivated;
    
    [Header("Recovery Settings")]
    [SerializeField] private float oxygenRecoveryRate = 5f;
    [SerializeField] private float sanityRecoveryRate = 3f;
    [SerializeField] private bool enableRecovery = true;
    
    [Header("Visual Feedback")]
    [SerializeField] private Sprite inactiveSprite;
    [SerializeField] private Sprite activeSprite;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioClip activationSound;
    
    private PlayerController playerInRange;
    private SpriteRenderer spriteRenderer;
    private Transform respawnPoint;

    public Vector3 RespawnPosition => respawnPoint != null ? respawnPoint.position : transform.position;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 自动查找子物体中名为respawnPoint的Transform
        Transform child = transform.Find("respawnPoint");
        if (child != null) respawnPoint = child;
        
        // 设置初始图片
        UpdateVisuals();
    }

    void Update()
    {
        if (isActivated && enableRecovery && playerInRange != null)
        {
            GameManager.Instance?.ChangeOxygen(oxygenRecoveryRate * Time.deltaTime);
           //GameManager.Instance?.ChangeSanity(sanityRecoveryRate * Time.deltaTime);
        }
    }
    
    public void Interact(GameObject player)
    {
        if (!isActivated)
        {
            isActivated = true;
            UpdateVisuals();
            GameEvents.TriggerCheckpointActivated(this);
            
            if (activationSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(activationSound);
            }
            
            Debug.Log($"Checkpoint {gameObject.name} activated!");
        }
    }
    
    private void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            Sprite targetSprite = isActivated ? activeSprite : inactiveSprite;
            if (targetSprite != null)
            {
                spriteRenderer.sprite = targetSprite;
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            playerInRange = player;
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player == playerInRange)
        {
            playerInRange = null;
        }
    }
}
