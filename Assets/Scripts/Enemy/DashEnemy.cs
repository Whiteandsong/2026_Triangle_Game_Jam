using UnityEngine;
using UnityEngine.AI;

public class DashEnemy : BaseEnemyAI
{
    [Header("Dash Settings")]
    public float dashDetectRange = 5f;
    public float dashSpeed = 15f;
    public float dashDistance = 5f;
    public float dashCooldown = 3f;
    public float dashDamage = 15f;
    
    [Header("Layers")]
    public LayerMask obstacleLayer;
    public LayerMask playerLayer;

    [Header("Visuals")]
    public Color dashColor = new Color(1f, 0.3f, 0.3f, 1f);
    public float scaleMult = 1.2f;

    // Internal State
    private bool isDashing;
    private float lastDashTime = -999f;
    private Vector2 dashDir;
    private float dashDistMoved;
    private Color orgColor;
    private Vector3 orgScale;

    protected override void Start()
    {
        base.Start();
        // 父类已经给 sr 赋值了，这里直接用
        if (sr) orgColor = sr.color;
        orgScale = transform.localScale;
    }

    protected override void Update()
    {
        if (isDashing) DashLogic();
        else base.Update(); // 不冲刺时，完全交给父类（巡逻、普通朝向）
    }

    protected override void ChaseLogic()
    {
        // 变量名变成了 playerT
        if (isDashing || !playerT) return;

        float dist = Vector2.Distance(transform.position, playerT.position);

        // 判定冲刺条件
        if (dist <= dashDetectRange && dist > 1.5f && Time.time >= lastDashTime + dashCooldown)
        {
            // 只有当前方无墙时才冲
            if (!Physics2D.Linecast(transform.position, playerT.position, obstacleLayer))
            {
                StartDash();
                return; // 阻止父类逻辑
            }
        }

        // 没冲刺则执行父类普通逻辑 (追逐/普通攻击)
        base.ChaseLogic();
    }

    private void StartDash()
    {
        isDashing = true;
        lastDashTime = Time.time;
        dashDistMoved = 0f;
        
        // 锁定冲刺方向
        dashDir = (playerT.position - transform.position).normalized;

        if (agent) agent.enabled = false; // 关导航
        
        // 视觉特效
        if (sr) sr.color = dashColor;
        transform.localScale = orgScale * scaleMult;
        if (anim) anim.SetTrigger("Attack");

        //修正朝向 (FlipX + Rotation)
        UpdateFacing(dashDir); 
    }

    private void DashLogic()
    {
        float step = dashSpeed * Time.deltaTime;

        // 1. 合并检测：前方 step+0.5 距离内是否有 墙 或 玩家
        // 撞墙？
        if (Physics2D.Raycast(transform.position, dashDir, step + 0.5f, obstacleLayer))
        {
            StopDash(); return;
        }

        // 撞人？
        if (Physics2D.Raycast(transform.position, dashDir, step + 0.5f, playerLayer))
        {
            GameManager.Instance?.ChangeOxygen(-dashDamage);
            GameEvents.TriggerPlayerHit(dashDamage); // 触发摄像机震动
            StopDash(); return;
        }

        // 2. 移动
        transform.position += (Vector3)(dashDir * step);
        dashDistMoved += step;

        // 3. 视觉脉冲
        transform.localScale = orgScale * scaleMult * (1f + Mathf.Sin(Time.time * 20f) * 0.05f);

        // 4. 距离结束
        if (dashDistMoved >= dashDistance) StopDash();
    }

    private void StopDash()
    {
        isDashing = false;
        
        // 还原视觉
        if (sr) sr.color = orgColor;
        transform.localScale = orgScale;

        // 还原导航位置 (防止卡墙)
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            transform.position = hit.position;

        if (agent) agent.enabled = true;
        
        // 如果还在追，立刻更新目标
        if (currentState == EnemyState.Chase && playerT) 
            agent.SetDestination(playerT.position);
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
}