using UnityEngine;
using System.Threading.Tasks;

public class PassiveEnemy : BaseEnemyAI
{
    [Header("Passive Enemy Settings")]
    public bool followPlayer = true;
    public float stopDistance = 2.5f;

    protected override void DetectPlayer()
    {
        if (!followPlayer) return;

        base.DetectPlayer();
    }

    protected override void ChaseLogic()
    {
        if (!playerT) { StopChase(); return; }

        float dist = Vector2.Distance(transform.position, playerT.position);

        if (dist > stopChaseDist || (maxChaseDistFromSpawn > 0 && Vector2.Distance(transform.position, startPos) > maxChaseDistFromSpawn))
        {
            ReturnToSpawn(); return;
        }

        if (dist <= stopDistance)
        {
            agent.isStopped = true;
            UpdateFacing(playerT.position - transform.position);
        }
        else
        {
            agent.isStopped = false;
            if (Vector2.Distance(agent.destination, playerT.position) > 0.5f)
                agent.SetDestination(playerT.position);
        }
    }
}
