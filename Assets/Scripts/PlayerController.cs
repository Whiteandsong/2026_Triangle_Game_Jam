using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float oxygenConsumptionRate = 2f;

    [Header("Vertical Movement (Auto Rise / S Key Sink)")]
    [SerializeField] private float riseSpeed = 3f; 
    [SerializeField] private float sinkSpeed = 3f; 
    [SerializeField] private float oxygenConsumptionSinkRate = 1f;

    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject interactPromptUI;
    private Rigidbody2D rb;
    private Vector2 currentVelocity;
    
    // Used to store the current interactable object within range
    private MonoBehaviour currentInteractableObject;
    private IInteractable currentInteractableInterface;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; 
        rb.freezeRotation = true;
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
    }

    void FixedUpdate()
    {
        rb.linearVelocity = currentVelocity;
    }

    #region Movement Handling

    void HandleHorizontalMovement()
    {
        float xInput = Input.GetAxisRaw("Horizontal");
        
        // Only set the X-axis velocity, keep the current Y-axis velocity to avoid overwriting
        currentVelocity.x = xInput * moveSpeed;

        // Consume oxygen when moving horizontally
        if (xInput != 0 && GameManager.Instance != null)
        {
            GameManager.Instance.ChangeOxygen(-oxygenConsumptionRate * Time.deltaTime);
        }
    }

    void HandleVerticalMovement()
    {
        float yVelocity;

        // Hold S to sink, otherwise auto rise
        if (Input.GetKey(KeyCode.S))
        {
            yVelocity = -sinkSpeed;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeOxygen(-oxygenConsumptionSinkRate * Time.deltaTime);
            }
        }
        else
        {
            yVelocity = riseSpeed;
        }

        currentVelocity.y = yVelocity;
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
        currentVelocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        Debug.Log($"Player respawned at {respawnPosition}");
    }
    
    #endregion
}