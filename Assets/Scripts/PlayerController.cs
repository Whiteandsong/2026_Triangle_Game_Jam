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
    [SerializeField] private float oxygenConsumptionSinkRate = 1f;

    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode scareKey = KeyCode.Space;
    [SerializeField] private GameObject interactPromptUI;

    [Header("Hit Flash Settings")]
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.2f;

    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    // State Variables
    private Color originalColor;
    
    // Timers (替换协程和Invoke)
    private float hitFlashTimer = 0f;
    private float scareTimer = 0f;
    private float scareDuration = 5f;

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
        if (currentXInput < 0) spriteRenderer.flipX = true;
        else if (currentXInput > 0) spriteRenderer.flipX = false;

        // 垂直翻转
        spriteRenderer.flipY = isSinkingInput;
    }

    private void HandleOxygenConsumption()
    {
        if (GameManager.Instance == null) return;

        float dt = Time.deltaTime;
        
        if (currentXInput != 0)
            GameManager.Instance.ChangeOxygen(-oxygenConsumptionRate * dt);

        if (isSinkingInput)
            GameManager.Instance.ChangeOxygen(-oxygenConsumptionSinkRate * dt);
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
            if (other.TryGetComponent(out MonoBehaviour mb))
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

    private void UseScare()
    {
        GameEvents.TriggerPlayerUseScare();
        
        isScareHiding = true;
        // 重置计时器 (替代 Invoke)
        scareTimer = scareDuration; 
        
        StartHiding();
        Debug.Log($"Player used scare skill and entered hiding for {scareDuration} seconds!");
    }
    
    private void EndScareHiding()
    {
        isScareHiding = false;
        StopHiding();
        Debug.Log("Scare hiding ended.");
    }
    
    #endregion

    #region Hiding Methods
    
    public void StartHiding()
    {
        if (!IsHiding) // 防止重复触发
        {
            IsHiding = true;
            GameEvents.TriggerPlayerStartHiding();
        }
    }
    
    public void StopHiding()
    {
        if (isScareHiding) return; // 如果正在技能隐身中，物理区域离开不应打断隐身
        
        if (IsHiding)
        {
            IsHiding = false;
            GameEvents.TriggerPlayerStopHiding();
        }
    }
    
    #endregion

    #region Event Handlers (Death, Hit, etc.)
    
    private void HandlePlayerDeath() { Debug.Log("Player died!"); }
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

        // 设置颜色为红色
        spriteRenderer.color = hitFlashColor;
        
        // 如果已经在闪烁，这会重置时间，这就实现了“连续受击刷新闪烁时间”的效果
        hitFlashTimer = hitFlashDuration;
    }
    
    #endregion
}