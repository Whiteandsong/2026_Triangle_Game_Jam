using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum EnemyState
{
    Patrol,
    Chase,
    Attack
}

public enum PatrolType
{
    RandomPoint,
    Waypoints
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(SpriteRenderer))]
public class BaseEnemyAI : MonoBehaviour
{
        [Header("Base Enemy AI Settings")]
        public EnemyState currentState;
        public PatrolType patrolType;

        [Header("Movement Parameters")]
        public float patrolSpeed = 3f;
        public float chaseSpeed = 4f;

        [Header("Patrol Logic")]
        public float patrolRange = 5f;    
        public Transform[] waypoints; 
        public float waitTimeAtPoint = 2f;

        [Header("Chase Logic")]
        public string playerTag = "Player";
        public float stopChaseDistance = 5f;
        
        [Tooltip("Max distance from spawn point before giving up chase (0 = unlimited)")]
        public float maxChaseDistanceFromSpawn =20f;

        [Header("Attack Logic")]
        public float attackRange = 1f;
        public float attackCooldown = 2f;
        public float attackDamage = 10f;
        protected float lastAttackTime = -999f;

        // TODO: Animation

        // Components
        protected NavMeshAgent agent;
        protected SpriteRenderer spriteRenderer;
        protected Transform playerTransform;

        // Internal State
        protected Vector2 startPosition;
        protected int currentWaypointIndex = 0;
        protected float waitTimer = 0f;

        protected virtual void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            agent.updateRotation = agent.updateUpAxis = false;
            
            startPosition = transform.position;
            currentState = EnemyState.Patrol;
            agent.speed = patrolSpeed;
            SetNewPatrolTarget();
        }

        protected virtual void OnEnable()
        {
            // 订阅玩家躲藏事件
            GameEvents.OnPlayerStartHiding += OnPlayerStartHiding;
            GameEvents.OnPlayerStopHiding += OnPlayerStopHiding;
            // 订阅玩家驱赶技能事件
            GameEvents.OnPlayerUseScare += OnPlayerUseScare;
        }

        protected virtual void OnDisable()
        {
            // 取消订阅
            GameEvents.OnPlayerStartHiding -= OnPlayerStartHiding;
            GameEvents.OnPlayerStopHiding -= OnPlayerStopHiding;
            GameEvents.OnPlayerUseScare -= OnPlayerUseScare;
        }

        protected virtual void Update()
        {
            if (currentState == EnemyState.Patrol) PatrolLogic();
            else if (currentState == EnemyState.Chase) ChaseLogic();
            
            HandleSpriteFlip();
        }

        # region State Logic Methods

        protected virtual void PatrolLogic()
        {
            if (waitTimer > 0)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0)
                {
                    agent.isStopped = false;
                    SetNewPatrolTarget();
                }
                return;
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                waitTimer = waitTimeAtPoint;
                agent.isStopped = true;
            }
        }

        protected virtual void ChaseLogic()
        {
            if (playerTransform == null) 
            {
                StopChase();
                return;
            }

            // Check if too far from spawn point
            if (maxChaseDistanceFromSpawn > 0)
            {
                float distanceFromSpawn = Vector2.Distance(transform.position, startPosition);
                if (distanceFromSpawn > maxChaseDistanceFromSpawn)
                {
                    ReturnToSpawn();
                    return;
                }
            }

            // Only update destination if player moved significantly
            if (Vector2.Distance(agent.destination, playerTransform.position) > 0.5f)
            {
                agent.SetDestination(playerTransform.position);
            }

            if (Vector2.Distance(transform.position, playerTransform.position) > stopChaseDistance)
            {
                StopChase();
            }
        }
        #endregion

        # region State Methods

        protected virtual void HandleSpriteFlip()
        {
            if (agent.velocity.x > 0.1f) spriteRenderer.flipX = false;
            else if (agent.velocity.x < -0.1f) spriteRenderer.flipX = true;
        }

        void SetNewPatrolTarget()
        {
            agent.speed = patrolSpeed;

            if (patrolType == PatrolType.RandomPoint)
            {
                agent.SetDestination(GetRandomPointOnNavMesh());
            }
            else if (patrolType == PatrolType.Waypoints && waypoints.Length > 0)
            {
                Transform targetPoint = waypoints[currentWaypointIndex];
                if (targetPoint != null)
                {
                    agent.SetDestination(targetPoint.position);
                    currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                }
            }
        }

        Vector2 GetRandomPointOnNavMesh()
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 randomPoint = startPosition + Random.insideUnitCircle * patrolRange;
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                    return hit.position;
            }
            return startPosition;
        }
        # endregion

        # region Public Control Methods
        
        protected virtual void OnPlayerStartHiding()
        {
            if (currentState == EnemyState.Chase)
            {
                StopChase();
            }
        }
        
        protected virtual void OnPlayerStopHiding()
        {
        }
        
        // Player used scare ability
        protected virtual void OnPlayerUseScare()
        {
            if (currentState == EnemyState.Chase)
            {
                StopChase();
            }
        }
        
        public virtual void StartChase(Transform target)
        {
            playerTransform = target;
            currentState = EnemyState.Chase;
            
            // Ensure agent is enabled before modifying it
            if (!agent.enabled)
            {
                agent.enabled = true;
            }
            
            agent.speed = chaseSpeed;
            agent.isStopped = false;
            waitTimer = 0f;
        }

        public virtual void StopChase()
        {
            playerTransform = null;
            currentState = EnemyState.Patrol;
            
            if (agent.enabled)
            {
                agent.speed = patrolSpeed;
                agent.isStopped = false;
                SetNewPatrolTarget();
            }
        }

        public virtual void ReturnToSpawn()
        {
            playerTransform = null;
            currentState = EnemyState.Patrol;
            
            if (agent.enabled)
            {
                agent.speed = patrolSpeed;
                agent.isStopped = false;
                agent.SetDestination(startPosition);
            }
        }

        #endregion

        #region Attack Methods

        protected virtual void Attack()
        {
            if (playerTransform == null) return;
            GameManager.Instance?.ChangeOxygen(-attackDamage);
        }

        #endregion

        #region Detection Methods

        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag(playerTag))
            {
                // detect player
                PlayerController player = collision.GetComponent<PlayerController>();
                if (player != null && !player.IsHiding)
                {
                    StartChase(collision.transform);
                }
            }
        }

        protected virtual void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag(playerTag) && 
                currentState == EnemyState.Chase && 
                Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
                lastAttackTime = Time.time;
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (patrolType == PatrolType.RandomPoint)
            {
                Gizmos.color = new Color(1, 1, 0, 0.3f);
                Vector2 center = Application.isPlaying ? startPosition : (Vector2)transform.position;
                Gizmos.DrawWireSphere(center, patrolRange);
            }

            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, stopChaseDistance);

            // Draw max chase range from spawn
            if (maxChaseDistanceFromSpawn > 0)
            {
                Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
                Vector2 center = Application.isPlaying ? startPosition : (Vector2)transform.position;
                Gizmos.DrawWireSphere(center, maxChaseDistanceFromSpawn);
            }

            if (Application.isPlaying && agent != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, agent.destination);
            }
        }

        #endregion
}
