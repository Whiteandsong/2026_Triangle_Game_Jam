using UnityEngine;

public class SimplePatrol : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform pointA; // 起点（拖入一个空物体）
    public Transform pointB; // 终点（拖入一个空物体）
    public float speed = 2.0f;

    [Header("Rotation Settings")]
    // 是否启用平滑旋转？如果不勾选，就是瞬间切换 0 和 180
    public bool smoothRotation = false; 
    public float rotationSpeed = 5f;

    // 内部变量
    private Vector3 targetPoint;
    private float targetXRotation;
    private float targetZRotation;

    void Start()
    {
        // 初始设置：先往 B 点走
        targetPoint = pointA.position;
        targetXRotation = 0f;
        targetZRotation = 0f;// 假设去 B 点时角度是 0
    }

    void Update()
    {
        if (pointA == null || pointB == null) return;

        HandleMovement();
        HandleRotation();
    }

    void HandleMovement()
    {
        // 1. 移动逻辑：从当前位置向目标点移动
        transform.position = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);

        // 2. 检查距离：如果非常接近目标点（比如小于 0.1f）
        if (Vector3.Distance(transform.position, targetPoint) < 0.1f)
        {
            // 切换目标
            if (targetPoint == pointA.position)
            {
                targetPoint = pointB.position;
                targetXRotation = 180f;
                targetZRotation = 180f;
            }
            else
            {
                targetPoint = pointA.position;
                targetXRotation = 0f;
                targetZRotation = 0f;
            }
        }
    }

    void HandleRotation()
    {
        // 目标旋转角度 (X轴和Z轴)
        Quaternion targetRot = Quaternion.Euler(targetXRotation, 0, targetZRotation);

        if (smoothRotation)
        {
            // 平滑旋转
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
        else
        {
            // 瞬间旋转
            transform.rotation = targetRot;
        }
    }

    // --- 辅助功能：在编辑器里画线，方便你看路径 ---
    void OnDrawGizmos()
    {
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pointA.position, 0.3f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pointB.position, 0.3f);
        }
    }
}