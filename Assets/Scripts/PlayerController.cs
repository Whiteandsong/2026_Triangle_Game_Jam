using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement Settings")]
    [SerializeField] private float moveForce = 50f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float oxygenConsumptionRate = 2f;

    [Header("Vertical Movement (Auto Rise / S Key Sink)")]
    [SerializeField] private float riseForce = 30f;
    [SerializeField] private float sinkForce = 30f;
    [SerializeField] private float maxVerticalSpeed = 3f;
    [SerializeField] private float oxygenConsumptionVerticalRate = 1f;

    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode scareKey = KeyCode.Space;
    [SerializeField] private GameObject interactPromptUI;

    [Header("Hit Flash Settings")]
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.2f;
    
    [Header("Animator Settings")]
    [SerializeField] private RuntimeAnimatorController defaultAnimatorController;
    [SerializeField] private RuntimeAnimatorController level4AnimatorController;
    

    [Header("Scare Settings")]
    [SerializeField] private float scareDuration = 5f;
    [SerializeField] private float scareCooldown = 2f; // 冷却时间，可调
    private float scareTimer = 0f;
    private bool isScareOnCooldown = false;


    [Header("Audio Settings")]
    [SerializeField] private AudioClip hitSoundEffect;
    [SerializeField] private AudioClip deathSoundEffect;
    [SerializeField] private AudioClip scareSoundEffect;
    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private CapsuleCollider2D capsuleCollider;
    private Vector2 originalColliderOffset;

    // State Variables
    private Color originalColor;
    
    // Timers (替换协程和Invoke)
    private float hitFlashTimer = 0f;

    // Hiding State
    public bool IsHiding { get; private set; } = false;
    private bool isScareHiding = false;

    // Interaction State
    private MonoBehaviour currentInteractableObject;
    private IInteractable currentInteractableInterface;

    // Input Cache
    private float currentXInput;
    private bool isSinkingInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        // 注意：linearDamping 是 Unity 6 的新API，如果是旧版本请改回 drag
        rb.linearDamping = 2f; 

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("PlayerController: Animator component not found!");
        }
        else
        {
            // 如果没有手动指定，则保存当前的AnimatorController作为默认
            if (defaultAnimatorController == null)
            {
                defaultAnimatorController = animator.runtimeAnimatorController;
            }
        }
        
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        if (capsuleCollider != null)
        {
            // 记录原始的 collider offset
            originalColliderOffset = capsuleCollider.offset;
        }
        else
        {
            Debug.LogWarning("PlayerController: CapsuleCollider2D component not found!");
        }

        if (interactPromptUI != null) 
        {
            // 缓存 transform 查找，避免 Awake 中潜在的开销（虽然只有一次）
            // 如果 interactPromptUI 已经在 Inspector 赋值，这里就不需要 Find
            if (interactPromptUI == null && transform.Find("UIPrompt") != null)
                interactPromptUI = transform.Find("UIPrompt").gameObject;
                
            interactPromptUI.SetActive(false);
        }
    }

    void OnEnable()
    {
        GameEvents.OnPlayerDeath += HandlePlayerDeath;
        GameEvents.OnPlayerInsane += HandlePlayerInsane;
        GameEvents.OnPlayerRespawn += HandlePlayerRespawn;
        GameEvents.OnPlayerHit += HandlePlayerHit;
    }

    void OnDisable()
    {
        GameEvents.OnPlayerDeath -= HandlePlayerDeath;
        GameEvents.OnPlayerInsane -= HandlePlayerInsane;
        GameEvents.OnPlayerRespawn -= HandlePlayerRespawn;
        GameEvents.OnPlayerHit -= HandlePlayerHit;
    }

    void Update()
    {
        // 1. 处理所有计时器 (替代协程和Invoke)
        HandleTimers();

        // 2. 处理输入 (在Update中读取输入最准确)
        currentXInput = Input.GetAxisRaw("Horizontal");
        isSinkingInput = Input.GetKey(KeyCode.S);

        // 3. 处理视觉朝向
        HandleSpriteOrientation();

        // 4. 处理交互输入
        CheckInteractionStatus();
        if (Input.GetKeyDown(interactKey)) TryInteract();
        
        // 5. 处理技能输入
        //TODO: CoolDown and Use times
        if (Input.GetKeyDown(scareKey)) UseScare();

        // 6. 处理氧气消耗 (逻辑放在 Update 没问题)
        HandleOxygenConsumption();
    }

    void FixedUpdate()
    {
        // 物理力学计算必须在 FixedUpdate
        ApplyMovementForces();
    }

    #region Timer Logic (核心优化点)

    private void HandleTimers()
    {
        float dt = Time.deltaTime;

        // 处理受击闪烁
        if (hitFlashTimer > 0)
        {
            hitFlashTimer -= dt;
            if (hitFlashTimer <= 0)
            {
                if (spriteRenderer != null) spriteRenderer.color = originalColor;
            }
        }

        // 处理惊吓技能持续时间
        if (isScareHiding)
        {
            scareTimer -= dt;
            if (scareTimer <= 0)
            {
                EndScareHiding();
            }
        }
    }

    #endregion

    #region Movement & Physics

    private void HandleSpriteOrientation()
    {
        if (spriteRenderer == null) return;

        // 水平翻转
        bool shouldFlipX = spriteRenderer.flipX;
        
        if (currentXInput < 0)
        {
            spriteRenderer.flipX = true;
            shouldFlipX = true;
        }
        else if (currentXInput > 0)
        {
            spriteRenderer.flipX = false;
            shouldFlipX = false;
        }
        
        // 同步翻转碰撞体 offset
        if (capsuleCollider != null)
        {
            Vector2 newOffset = originalColliderOffset;
            if (shouldFlipX)
            {
                newOffset.x = -originalColliderOffset.x;
            }
            capsuleCollider.offset = newOffset;
        }

        // 垂直翻转
        spriteRenderer.flipY = isSinkingInput;
    }

    private void HandleOxygenConsumption()
    {
        if (GameManager.Instance == null) return;

        float dt = Time.deltaTime;
        
        // 左右移动消耗
        if (currentXInput != 0)
            GameManager.Instance.ChangeOxygen(-oxygenConsumptionRate * dt);

        // 上下移动消耗（无论上升还是下沉都消耗）
        GameManager.Instance.ChangeOxygen(-oxygenConsumptionVerticalRate * dt);
    }

    private void ApplyMovementForces()
    {
        // 水平力
        rb.AddForce(Vector2.right * currentXInput * moveForce);
        
        // 限制水平速度
        if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
        {
            float limitedX = Mathf.Sign(rb.linearVelocity.x) * maxSpeed;
            rb.linearVelocity = new Vector2(limitedX, rb.linearVelocity.y);
        }

        // 垂直力
        float verticalForce = isSinkingInput ? -sinkForce : riseForce;
        rb.AddForce(Vector2.up * verticalForce);

        // 限制垂直速度
        if (Mathf.Abs(rb.linearVelocity.y) > maxVerticalSpeed)
        {
            float limitedY = Mathf.Sign(rb.linearVelocity.y) * maxVerticalSpeed;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, limitedY);
        }
    }

    #endregion

    #region Interactable Handling

    private void TryInteract()
    {
        if (currentInteractableInterface != null)
        {
            currentInteractableInterface.Interact(this.gameObject);
        }
    }

    private void CheckInteractionStatus()
    {
        // 如果当前交互对象意外销毁，清理状态
        if (currentInteractableObject == null && currentInteractableInterface != null)
        {
            ClearInteraction();
        }
    }

    private void ClearInteraction()
    {
        currentInteractableObject = null;
        currentInteractableInterface = null;
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("HidingSpot"))
        {
            StartHiding();
            return;
        }

        if (other.TryGetComponent(out IInteractable interactable))
        {
            // 检查是否可以交互
            if (interactable.CanInteract && other.TryGetComponent(out MonoBehaviour mb))
            {
                currentInteractableObject = mb;
                currentInteractableInterface = interactable;
                if (interactPromptUI != null) interactPromptUI.SetActive(true);
                Debug.Log($"Entered interaction range: {other.name}");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("HidingSpot"))
        {
            StopHiding();
            return;
        }

        // 检查离开的对象是否是当前交互对象
        if (currentInteractableObject != null && other.gameObject == currentInteractableObject.gameObject)
        {
            ClearInteraction();
            Debug.Log($"Left interaction range: {other.name}");
        }
    }

    #endregion

    #region Scare Skill

    public void UseScare()
    {
        // 冷却中不能触发
        if (isScareOnCooldown)
        {
            UIManager.Instance?.ShowDialogue("Scare Device is cooling down...");
            return;
        }

        // 检查是否还有Scare次数
        if (GameManager.Instance == null || !GameManager.Instance.HasScareCharges())
        {
            UIManager.Instance?.ShowDialogue("Scare device is dead...");
            return;
        }

        // 消耗一次Scare次数
        if (!GameManager.Instance.UseScareCharge())
        {
            return;
        }

        GameEvents.TriggerPlayerUseScare();

        isScareHiding = true;
        scareTimer = scareDuration;

        // 播放 Scare 动画
        if (animator != null)
        {
            animator.SetTrigger("Scare");
        }

        // Audio
        AudioManager.Instance.PlaySFX(scareSoundEffect);

        StartHiding();
        Debug.Log($"Player used scare skill and entered hiding for {scareDuration} seconds!");

        // 设置冷却
        isScareOnCooldown = true;
        Invoke(nameof(ResetScareCooldown), scareCooldown);
    }

    private void ResetScareCooldown()
    {
        isScareOnCooldown = false;
    }
    
    private void EndScareHiding()
    {
        isScareHiding = false;
        StopHiding();
        Debug.Log("Scare hiding ended.");
    }
    
    #endregion

    #region Hiding Methods
    
    public void StartHiding(float time = 0f)
    {
        if (!IsHiding) // 防止重复触发
        {
            IsHiding = true;
            
            // 平滑降低BGM音量到80%
            if (AudioManager.Instance != null) { AudioManager.Instance.FadeBGMToLower(0.8f);}
            
            GameEvents.TriggerPlayerStartHiding();
        }

        if (time > 0f)
        {
            Invoke(nameof(StopHiding), time);
        }
    }
    
    public void StopHiding()
    {
        if (isScareHiding) return; // 如果正在技能隐身中，物理区域离开不应打断隐身
        
        if (IsHiding)
        {
            IsHiding = false;
            
            // 平滑恢复BGM音量
            if (AudioManager.Instance != null) { AudioManager.Instance.FadeBGMToOriginal();}
            
            GameEvents.TriggerPlayerStopHiding();
        }
    }
    
    #endregion

    #region Event Handlers (Death, Hit, etc.)
    
    private void HandlePlayerDeath() {AudioManager.Instance.PlaySFX(deathSoundEffect); }
    private void HandlePlayerInsane() { Debug.Log("Player went insane!"); }
    
    private void HandlePlayerRespawn(Vector3 respawnPosition)
    {
        transform.position = respawnPosition;
        rb.linearVelocity = Vector2.zero;
        
        // 复活时重置状态
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        hitFlashTimer = 0f;
        scareTimer = 0f;
        isScareHiding = false;
        IsHiding = false;
    }
    
    private void HandlePlayerHit(float damage)
    {
        if (spriteRenderer == null) return;

        spriteRenderer.color = hitFlashColor;
        
        hitFlashTimer = hitFlashDuration;        
        if (hitSoundEffect != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(hitSoundEffect);
        }    }
    
    #endregion
    
    #region Animator Switching
    
    // 切换到关卡四的AnimatorController（用于关卡四形象变化）
    public void SwitchToLevel4Animator()
    {
        if (animator == null)
        {
            Debug.LogWarning("Animator component not found!");
            return;
        }
        
        if (level4AnimatorController != null)
        {
            animator.runtimeAnimatorController = level4AnimatorController;
            Debug.Log("Switched to Level 4 Animator Controller");
        }
        else
        {
            Debug.LogWarning("Level 4 Animator Controller not assigned!");
        }
    }
    
    #endregion
}