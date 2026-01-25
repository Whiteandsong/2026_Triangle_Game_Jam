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
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    
    // Hiding state
    public bool IsHiding { get; private set; } = false;
    private bool isScareHiding = false;
    
    // Used to store the current interactable object within range
    private MonoBehaviour currentInteractableObject;
    private IInteractable currentInteractableInterface;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = 2f;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        interactPromptUI = gameObject.transform.Find("UIPrompt").gameObject;
        if(interactPromptUI != null) interactPromptUI.SetActive(false);
    }

    void OnEnable()
    {
        // Subscribe to death and respawn events
        GameEvents.OnPlayerDeath += HandlePlayerDeath;
        GameEvents.OnPlayerInsane += HandlePlayerInsane;
        GameEvents.OnPlayerRespawn += HandlePlayerRespawn;
    }
    
    void OnDisable()
    {
        // Unsubscribe from events
        GameEvents.OnPlayerDeath -= HandlePlayerDeath;
        GameEvents.OnPlayerInsane -= HandlePlayerInsane;
        GameEvents.OnPlayerRespawn -= HandlePlayerRespawn;
    }

    void Update()
    {
        // Handle movement logic for different directions separately
        HandleHorizontalMovement();
        HandleVerticalMovement();

        // Handle interaction input
        CheckInteractionStatus();
        HandleInteractionInput();
        
        // Handle scare skill input
        HandleScareInput();
    }

    void FixedUpdate()
    {
        HandleHorizontalForce();
        HandleVerticalForce();
    }

    #region Movement Handling

    void HandleHorizontalMovement()
    {
        float xInput = Input.GetAxisRaw("Horizontal");
        
        // Flip sprite based on movement direction
        if (xInput < 0 && spriteRenderer != null)
        {
            spriteRenderer.flipX = true;
        }
        else if (xInput > 0 && spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
        }

        // Consume oxygen when moving horizontally
        if (xInput != 0 && GameManager.Instance != null)
        {
            GameManager.Instance.ChangeOxygen(-oxygenConsumptionRate * Time.deltaTime);
        }
    }

    void HandleHorizontalForce()
    {
        float xInput = Input.GetAxisRaw("Horizontal");
        rb.AddForce(Vector2.right * xInput * moveForce);
        
        // 限制最大水平速度
        if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, rb.linearVelocity.y);
        }
    }

    void HandleVerticalMovement()
    {
        bool isSinking = Input.GetKey(KeyCode.S);
        
        if (spriteRenderer != null)
        {
            spriteRenderer.flipY = isSinking;
        }

        if (isSinking && GameManager.Instance != null)
        {
            GameManager.Instance.ChangeOxygen(-oxygenConsumptionSinkRate * Time.deltaTime);
        }
    }

    void HandleVerticalForce()
    {
        float verticalForce = Input.GetKey(KeyCode.S) ? -sinkForce : riseForce;
        rb.AddForce(Vector2.up * verticalForce);
        
        // 限制最大垂直速度
        if (Mathf.Abs(rb.linearVelocity.y) > maxVerticalSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Sign(rb.linearVelocity.y) * maxVerticalSpeed);
        }
    }
    #endregion

    #region Interactable Handling

    private void HandleInteractionInput()
    {
        // If the interact key is pressed and there is a valid interactable object
        if (Input.GetKeyDown(interactKey) && currentInteractableInterface != null)
        {
            currentInteractableInterface.Interact(this.gameObject);
        }
    }

    private void CheckInteractionStatus()
    {
        // Check status of current interactable object
        if (currentInteractableObject == null)
        {
            currentInteractableInterface = null;
            
            // Hide UIprompt
            if (interactPromptUI.activeSelf)
            {
                interactPromptUI.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // If the player enters a trigger collider with an interactable component
        if (other.CompareTag("HidingSpot"))
        {
            StartHiding();
            return;
        }

        MonoBehaviour mb = other.GetComponent<MonoBehaviour>();
        IInteractable interactable = other.GetComponent<IInteractable>();
        
        if (interactable != null)
        {
            currentInteractableObject = mb;
            currentInteractableInterface = interactable;
            if(interactPromptUI != null) interactPromptUI.SetActive(true);            
            Debug.Log($"Entered interaction range: {other.name}");
        }
    }

    // When the player leaves the trigger range of an object
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("HidingSpot"))
        {
            StopHiding();
            return;
        }

        IInteractable interactable = other.GetComponent<IInteractable>();

        // If the object leaving is the currently recorded interactable, clear the record
        if (interactable != null && interactable == currentInteractableInterface)
        {
            currentInteractableObject = null;
            currentInteractableInterface = null;
            //Hide interaction UI prompt
            if(interactPromptUI != null) interactPromptUI.SetActive(false);            
            Debug.Log($"Left interaction range: {other.name}");
        }
    }
    #endregion

    #region Scare Skill
    
    private void HandleScareInput()
    {
        if (Input.GetKeyDown(scareKey))
        {
            UseScare();
        }
    }
    
    private void UseScare()
    {
        GameEvents.TriggerPlayerUseScare();
        
        isScareHiding = true;
        StartHiding();
        
        Invoke(nameof(EndScareHiding), 5f);
        
        Debug.Log("Player used scare skill and entered hiding for 5 seconds!");
    }
    
    private void EndScareHiding()
    {
        if (isScareHiding)
        {
            isScareHiding = false;
            StopHiding();
            Debug.Log("Scare hiding ended after 5 seconds");
        }
    }
    
    #endregion

    #region Hiding Methods
    
    public void StartHiding()
    {
        IsHiding = true;
        GameEvents.TriggerPlayerStartHiding();
    }
    
    public void StopHiding()
    {
        if (isScareHiding) return;
        
        IsHiding = false;
        GameEvents.TriggerPlayerStopHiding();
    }
    
    #endregion

    #region Death Handling
    
    private void HandlePlayerDeath()
    {
        Debug.Log("Player died from lack of oxygen!");
    }
    
    private void HandlePlayerInsane()
    {
        Debug.Log("Player went insane!");
    }
    
    private void HandlePlayerRespawn(Vector3 respawnPosition)
    {
        transform.position = respawnPosition;
        rb.linearVelocity = Vector2.zero;
        Debug.Log($"Player respawned at {respawnPosition}");
    }
    
    #endregion
}