using UnityEngine;
using UnityEngine.AI;

public class DashEnemy : BaseEnemyAI
{
    [Header("Dash Attack Settings")]
    public float dashDetectionRange = 5f;
    public float dashSpeed = 15f;
    public float dashDistance = 5f;
    public float dashCooldown = 3f;
    public float dashDamage = 15f;
    
    [Header("Collision Settings")]
    [Tooltip("Layer mask for walls/obstacles to stop dash")]
    public LayerMask obstacleLayer;

    [Header("Visual Effects")]
    public Color dashColor = new Color(1f, 0.3f, 0.3f, 1f);
    public float dashScaleMultiplier = 1.2f;

    private bool isDashing;
    private float lastDashTime = -999f;
    private Vector2 dashDirection, dashStartPosition;
    private float dashDistanceTraveled;
    private Color originalColor;
    private Vector3 originalScale;

    protected override void Start()
    {
        base.Start();
        originalColor = spriteRenderer.color;
        originalScale = transform.localScale;
    }

    protected override void Update()
    {
        if (isDashing) DashLogic();
        else base.Update();
    }

    protected override void ChaseLogic()
    {
        if (!isDashing && playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer <= dashDetectionRange && 
                distanceToPlayer > 1.5f && 
                Time.time >= lastDashTime + dashCooldown &&
                !Physics2D.Linecast(transform.position, playerTransform.position, obstacleLayer))
            {
                StartDash();
                return;
            }
        }

        base.ChaseLogic();
    }

    private void StartDash()
    {
        if (playerTransform == null) return;

        isDashing = true;
        lastDashTime = Time.time;
        dashStartPosition = transform.position;
        dashDistanceTraveled = 0f;
        dashDirection = (playerTransform.position - transform.position).normalized;

        agent.enabled = false;

        spriteRenderer.color = dashColor;
        transform.localScale = originalScale * dashScaleMultiplier;
    }

    private void DashLogic()
    {
        float moveStep = dashSpeed * Time.deltaTime;

        if (Physics2D.Raycast(transform.position, dashDirection, moveStep + 0.5f, obstacleLayer).collider != null)
        {
            StopDash();
            return;
        }

        transform.position += (Vector3)(dashDirection * moveStep);
        dashDistanceTraveled += moveStep;

        float pulseScale = 1f + Mathf.Sin(Time.time * 20f) * 0.05f;
        transform.localScale = originalScale * dashScaleMultiplier * pulseScale;

        if (dashDistanceTraveled >= dashDistance)
            StopDash();
    }

    private void StopDash()
    {
        isDashing = false;

        spriteRenderer.color = originalColor;
        transform.localScale = originalScale;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            transform.position = hit.position;

        agent.enabled = true;
        
        if (currentState == EnemyState.Chase && playerTransform != null)
            agent.SetDestination(playerTransform.position);
    }

    public override void StopChase()
    {
        if (isDashing) StopDash();
        base.StopChase();
    }

    public override void ReturnToSpawn()
    {
        if (isDashing) StopDash();
        base.ReturnToSpawn();
    }

    protected override void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(playerTag))
        {
            if (isDashing)
            {
                GameManager.Instance?.ChangeOxygen(-dashDamage);
                StopDash();
            }
            else if (currentState == EnemyState.Chase)
            {
                base.OnCollisionStay2D(collision);
            }
        }
    }
}
