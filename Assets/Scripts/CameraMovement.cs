using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;

    [Header("Particle System Settings")]
    public ParticleSystem marineSnowPS;
    public float startY = -10f;
    public float stopY = -30f;
    private float disablePSTimer = 0f;
    private ParticleSystem.EmissionModule psEmission; // 缓存模块
    private bool isPSEnabled = false; // 状态缓存，减少API调用

    [Header("Smooth Settings")]
    [Range(0, 1)]
    public float smoothTime = 0.2f;
    private Vector3 currentVelocity = Vector3.zero;
    private float initialZ; // 缓存初始Z轴

    [Header("Camera Bounds")]
    public bool useBounds = true;
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -50f;
    public float maxY = 10f;

    [Header("Shake Settings")]
    private float shakeTimer = 0f;
    private float shakeMagnitude = 0f;
    private float startShakeDuration = 0f;

    void Awake()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        initialZ = transform.position.z;

        if (marineSnowPS != null)
        {
            psEmission = marineSnowPS.emission;
            isPSEnabled = psEmission.enabled;
        }
    }

    void OnEnable()
    {
        GameEvents.OnPlayerHit += HandlePlayerHit;
    }

    void OnDisable()
    {
        GameEvents.OnPlayerHit -= HandlePlayerHit;
    }

    void Update()
    {
        HandleParticleSystem();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Calculate Smooth Position
        Vector3 targetPosition = new Vector3(target.position.x, target.position.y, initialZ);
        Vector3 smoothPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);

        // 2. Apply Bounds
        if (useBounds)
        {
            smoothPosition.x = Mathf.Clamp(smoothPosition.x, minX, maxX);
            smoothPosition.y = Mathf.Clamp(smoothPosition.y, minY, maxY);
        }

        // 3. Apply Shake
        Vector3 finalPosition = smoothPosition;

        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;

            float progress = shakeTimer / startShakeDuration; 
            float currentStrength = shakeMagnitude * Mathf.SmoothStep(0f, 1f, progress); 

            Vector2 shake2D = Random.insideUnitCircle * currentStrength;
            
            finalPosition += (Vector3)shake2D;
        }

        transform.position = finalPosition;
    }

    public void TriggerShake(float duration, float magnitude)
    {
        // 逻辑保持不变，这部分写得很好
        bool isStronger = magnitude >= shakeMagnitude;
        // 只要当前还有剩余时间，就视为正在震动
        bool isActive = shakeTimer > 0;

        if (!isActive || isStronger)
        {
            shakeMagnitude = magnitude;
            shakeTimer = duration;
            startShakeDuration = duration;
        }
    }

    void HandleParticleSystem()
    {
        if (marineSnowPS == null) return;

        float currentY = transform.position.y;
        bool inZone = currentY <= startY && currentY >= stopY;

        if (inZone)
        {
            if (!marineSnowPS.gameObject.activeSelf)
            {
                marineSnowPS.gameObject.SetActive(true);
            }

            if (!isPSEnabled)
            {
                psEmission.enabled = true;
                isPSEnabled = true;
                disablePSTimer = 0f;
            }
        }
        else
        {
            if (isPSEnabled)
            {
                psEmission.enabled = false;
                isPSEnabled = false;
            }

            if (marineSnowPS.gameObject.activeSelf)
            {
                disablePSTimer += Time.deltaTime;
                if (disablePSTimer >= 2f)
                {
                    marineSnowPS.gameObject.SetActive(false);
                    disablePSTimer = 0f;
                }
            }
        }
    }

    void HandlePlayerHit(float damage)
    {
        float shakeStrength = Mathf.Clamp(damage * 0.3f, 0.3f, 0.5f);
        TriggerShake(0.3f, shakeStrength);
    }
}