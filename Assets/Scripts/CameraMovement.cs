using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    [Header("Particle System Settings")]
    public ParticleSystem marineSnowPS;
    public float startY = -10f;
    public float stopY = -30f;
    

    [Header("Smooth Settings")]
    [Range(0, 1)]
    public float smoothTime = 0.2f; // smooth time(transition duration)
    private Vector3 currentVelocity = Vector3.zero;

    [Header("Camera Bounds")]
    public bool useBounds = true;
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -50f;
    public float maxY = 10f;

    void Awake()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        float currentY = transform.position.y;

        var emission = marineSnowPS.emission;
        
        if (currentY <= startY && currentY >= stopY)
        {
            if (!emission.enabled) emission.enabled = true;
            marineSnowPS.gameObject.SetActive(true);
        }
        else
        {
            if (emission.enabled) emission.enabled = false;
            Invoke("DisableParticleSystem", 2f);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Get the target position
        // Note: We must force the Z axis to -10 (or other negative value), otherwise if the camera reaches Z=0 it will overlap with the background, causing the screen to be completely black or only show objects
        Vector3 targetPosition = new Vector3(target.position.x, target.position.y, -10f);

        // 2. Use SmoothDamp for smooth movement
        // This is an algorithm more suitable for cameras than Lerp, it has built-in buffering, making start and stop very natural
        Vector3 newPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);

        // 3. Apply bounds to camera position
        if (useBounds)
        {
            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);
        }

        transform.position = newPosition;
    }

    void DisableParticleSystem()
    {
        marineSnowPS.gameObject.SetActive(false);
    }
}