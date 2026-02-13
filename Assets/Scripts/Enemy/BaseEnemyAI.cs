using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum EnemyState { Patrol, Chase, Attack }
public enum PatrolType { RandomPoint, Waypoints }

[RequireComponent(typeof(NavMeshAgent), typeof(SpriteRenderer))]
public class BaseEnemyAI : MonoBehaviour
{
    [Header("Settings")]
    public EnemyState currentState = EnemyState.Patrol;
    public PatrolType patrolType;
    public float patrolSpeed = 3f, chaseSpeed = 5f;
    public float patrolRange = 5f, waitTimeAtPoint = 2f;
    public Transform[] waypoints;

    [Header("Chase & Attack")]
    public float detectRange = 5f, stopChaseDist = 7f, maxChaseDistFromSpawn = 20f;
    public float attackRange = 1.5f, attackCooldown = 2f, attackDamage = 10f;
    public float damageDelay = 0.4f;
    public bool stopMoveWhileAttacking = true;

    [Header("Visuals")]
    public bool useRotation = true;
    public float rotateSpeed = 10f, idleSpeedThreshold = 0.1f;

    // Components & State
    protected NavMeshAgent agent;
    protected SpriteRenderer sr;
    protected Animator anim;
    protected Transform playerT; // playerTransform 简写
    protected Vector2 startPos;
    protected int wpIndex = 0;
    protected float waitTimer = 0f, lastAttackTime = -999f;
    protected bool isAttacking = false;

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        agent.updateRotation = agent.updateUpAxis = false;
        startPos = transform.position;
        agent.speed = patrolSpeed;
        SetNewPatrolTarget();
    }

    protected virtual void OnEnable()
    {
        GameEvents.OnPlayerStartHiding += OnPlayerStartHiding;
        GameEvents.OnPlayerUseScare += OnPlayerUseScare;
    }

    protected virtual void OnDisable()
    {
        GameEvents.OnPlayerStartHiding -= OnPlayerStartHiding;
        GameEvents.OnPlayerUseScare -= OnPlayerUseScare;
    }

    protected virtual void Update()
    {
        // Z轴修正
        if (Mathf.Abs(transform.position.z) > 0.01f)
            transform.position = new Vector3(transform.position.x, transform.position.y, 0);

        // 攻击状态：只负责盯着玩家
        if (isAttacking)
        {
            if (playerT) UpdateFacing(playerT.position - transform.position);
            return;
        }

        // 状态机
        switch (currentState)
        {
            case EnemyState.Patrol: PatrolLogic(); DetectPlayer(); break;
            case EnemyState.Chase:  ChaseLogic(); break;
        }

        // 动画与朝向
        float speed = agent.velocity.magnitude;
        if (anim) anim.SetBool("IsSwimming", speed > idleSpeedThreshold);
        if (speed > idleSpeedThreshold) UpdateFacing(agent.velocity);
    }

    // --- Logic Methods ---

    protected virtual void PatrolLogic()
    {
        if (agent.speed != patrolSpeed) agent.speed = patrolSpeed;

        // 计时器逻辑
        if ((waitTimer -= Time.deltaTime) > 0) return;
        
        // 刚结束等待，恢复移动
        if (agent.isStopped && waitTimer <= 0) 
        {
            agent.isStopped = false;
            SetNewPatrolTarget();
            return;
        }

        // 寻路中，跳过
        if (agent.pathPending) return;

        if (!agent.hasPath && agent.remainingDistance > 0.5f)
        {
            waitTimer = 0.5f;
        }
        else
        {
            waitTimer = waitTimeAtPoint;
        }

        if (!agent.hasPath)
        {
            agent.isStopped = true;
            waitTimer = 0.5f;
        }
    }

    protected virtual void ChaseLogic()
    {
        if (!playerT) { StopChase(); return; }

        float dist = Vector2.Distance(transform.position, playerT.position);

        // 距离检查：超出追击范围 或 超出出生点范围
        if (dist > stopChaseDist || (maxChaseDistFromSpawn > 0 && Vector2.Distance(transform.position, startPos) > maxChaseDistFromSpawn))
        {
            ReturnToSpawn(); return;
        }

        // 攻击逻辑
        if (dist <= attackRange)
        {
            if (stopMoveWhileAttacking) agent.isStopped = true;
            if (Time.time >= lastAttackTime + attackCooldown) StartCoroutine(PerformAttackCoroutine());
        }
        else // 追击逻辑
        {
            if (stopMoveWhileAttacking) agent.isStopped = false;
            if (Vector2.Distance(agent.destination, playerT.position) > 0.5f)
                agent.SetDestination(playerT.position);
        }
    }

    protected virtual void DetectPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectRange, LayerMask.GetMask("Player"));
        if (hit && hit.TryGetComponent(out PlayerController p) && !p.IsHiding)
            StartChase(hit.transform);
    }

    // --- Actions ---

    private System.Collections.IEnumerator PerformAttackCoroutine()
    {
        if (isAttacking) yield break;
        
        isAttacking = true;
        lastAttackTime = Time.time;
        if (anim) anim.SetTrigger("Attack");

        // 等待伤害延迟
        yield return new WaitForSeconds(damageDelay);
        
        // 伤害判定
        if (playerT && Vector2.Distance(transform.position, playerT.position) <= attackRange + 0.5f)
        {
            GameManager.Instance?.ChangeOxygen(-attackDamage);
            GameEvents.TriggerPlayerHit(attackDamage); // 触发摄像机震动
            Debug.Log("Hit Player!");
        }
        
        // 后摇
        yield return new WaitForSeconds(0.5f);

        isAttacking = false;
        if (agent && agent.enabled && stopMoveWhileAttacking) agent.isStopped = false;
    }

    protected void UpdateFacing(Vector2 dir)
    {
        if (dir.magnitude < 0.01f) return;

        // FlipX (三元运算简化)
        if (Mathf.Abs(dir.x) > 0.1f) sr.flipX = dir.x > 0;

        if (!useRotation) { transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * 2f); return; }

        // Rotation
        float angle = Mathf.Atan2(dir.y, Mathf.Abs(dir.x)) * Mathf.Rad2Deg;
        angle = Mathf.Clamp(angle, -48f, 48f);
        if (!sr.flipX) angle = -angle;

        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * rotateSpeed);
    }

    // --- Movement Helpers ---

    void SetNewPatrolTarget()
    {
        agent.speed = patrolSpeed;
        if (patrolType == PatrolType.RandomPoint)
        {
            agent.SetDestination(GetRandomPoint());
        }
        else if (waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[wpIndex].position);
            wpIndex = (wpIndex + 1) % waypoints.Length;
        }
    }

    Vector2 GetRandomPoint()
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 pt = startPos + Random.insideUnitCircle * patrolRange;
            if (NavMesh.SamplePosition(pt, out NavMeshHit hit, 2f, NavMesh.AllAreas)) return hit.position;
        }
        return transform.position; // 找不到就原地待命
    }

    // --- Public Control ---

    public virtual void StartChase(Transform target)
    {
        playerT = target;
        currentState = EnemyState.Chase;
        if (!agent.enabled) agent.enabled = true;
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        waitTimer = 0f;
    }

    public virtual void ReturnToSpawn() => ResetToPatrol(null);
    public virtual void StopChase() => ResetToPatrol(null);
    
    private void ResetToPatrol(Transform _)
    {
        playerT = null;
        currentState = EnemyState.Patrol;
        if (agent.enabled)
        {
            agent.speed = patrolSpeed;
            agent.isStopped = false;
            // 如果是回出生点模式，直接设目标；否则找新巡逻点
            if (Vector2.Distance(transform.position, startPos) > maxChaseDistFromSpawn) agent.SetDestination(startPos);
            else SetNewPatrolTarget();
        }
    }

    // Events
    protected virtual void OnPlayerStartHiding() { if (currentState == EnemyState.Chase) StopChase(); }
    protected virtual void OnPlayerUseScare() { if (currentState == EnemyState.Chase) StopChase(); }

    // --- Debug ---
    protected virtual void OnDrawGizmosSelected()
    {
        Vector2 c = Application.isPlaying ? startPos : (Vector2)transform.position;
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        if (patrolType == PatrolType.RandomPoint) Gizmos.DrawWireSphere(c, patrolRange);
        
        Gizmos.color = Color.green; Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;   Gizmos.DrawWireSphere(transform.position, stopChaseDist);
        Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(transform.position, attackRange);
        
        if (maxChaseDistFromSpawn > 0) {
            Gizmos.color = new Color(1, 0.5f, 0, 0.2f); Gizmos.DrawWireSphere(c, maxChaseDistFromSpawn);
        }
    }
}