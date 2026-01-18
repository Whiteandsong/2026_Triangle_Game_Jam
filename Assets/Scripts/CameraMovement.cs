using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;

    [Header("Smooth Settings")]
    [Range(0, 1)]
    public float smoothTime = 0.2f; // smooth time(transition duration)
    private Vector3 currentVelocity = Vector3.zero;

    void Awake()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Get the target position
        // Note: We must force the Z axis to -10 (or other negative value), otherwise if the camera reaches Z=0 it will overlap with the background, causing the screen to be completely black or only show objects
        Vector3 targetPosition = new Vector3(target.position.x, target.position.y, -10f);

        // 2. Use SmoothDamp for smooth movement
        // This is an algorithm more suitable for cameras than Lerp, it has built-in buffering, making start and stop very natural
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }
}