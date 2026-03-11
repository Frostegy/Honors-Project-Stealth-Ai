using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class AiAgentController : MonoBehaviour
{
    public enum EnemyState
    {
        Patrolling,
        Suspicious,
        Searching,
        LookingAround
    }

    [Tooltip("The player transform. If left empty, the script will try to find it automatically on start.")]
    public Transform player;
    [Tooltip("The patrol points the enemy will walk between.")]
    public Transform[] patrolPoints;
    public VisionCone[] visionCones;
    public HearingCircle hearingSensor;

    [Header("Stationary")]
    public bool isStationary = false;
    public float lookAngle = 60f;
    public float lookSpeed = 1f;

    [Header("Movement")]
    public float stopDistance = 0.6f;
    public float patrolWaitTime = 2f;
    public float suspiciousSpeedMultiplier = 0.6f;
    [Range(0f, 1f)] public float suspiciousThreshold = 0.15f;
    public float lookAroundTime = 2f;

    public EnemyState currentState = EnemyState.Patrolling;
    public float currentDetectionLevel = 0f;
    public bool isFullyDetected = false;
    public bool hasLostDetection = false;

    private NavMeshAgent navAgent;
    private bool isLookingAround = false;
    private int patrolIndex = 0;
    private bool waitingAtPoint = false;
    private Vector3 lastHeardPos;
    private bool hasHeardSomething = false;
    private Vector3 searchPos;
    private bool hasSearchPos = false;
    private Quaternion startRotation;
    private float lookTimer = 0f;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();

        // grab cones if none were assigned
        if (visionCones == null || visionCones.Length == 0)
            visionCones = GetComponentsInChildren<VisionCone>(true);

        if (hearingSensor == null)
            hearingSensor = GetComponentInChildren<HearingCircle>();

        for (int i = 0; i < visionCones.Length; i++)
        {
            if (visionCones[i] != null)
                visionCones[i].SetPlayerTransform(player);
        }
    }

    private void Start()
    {
        startRotation = transform.rotation;

        if (isStationary == false)
            GoToNextPatrolPoint();
        else
            navAgent.isStopped = true;
    }

    private void Update()
    {
        if (player == null) return;

        isFullyDetected = false;
        hasLostDetection = false;

        UpdateDetectionLevel();

        if (currentDetectionLevel >= 1f)
        {
            isFullyDetected = true;
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerGameOver();
            return;
        }

        if (isStationary)
        {
            DoLookLeftRight();
            return;
        }

        switch (currentState)
        {
            case EnemyState.Patrolling:
                {
                    DoPatrol();

                    if (currentDetectionLevel >= suspiciousThreshold)
                    {
                        currentState = EnemyState.Suspicious;
                        navAgent.speed = navAgent.speed * suspiciousSpeedMultiplier;
                    }
                    break;
                }

            case EnemyState.Suspicious:
                {
                    DoPatrol();

                    if (currentDetectionLevel <= 0f)
                    {
                        currentState = EnemyState.Patrolling;
                        navAgent.speed = navAgent.speed / suspiciousSpeedMultiplier;
                        hasLostDetection = true;
                    }
                    break;
                }

            case EnemyState.Searching:
                {
                    if (hasSearchPos == false)
                    {
                        currentState = EnemyState.Patrolling;
                        GoToClosestPatrolPoint();
                        break;
                    }

                    navAgent.isStopped = false;
                    navAgent.SetDestination(searchPos);

                    if (navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
                    {
                        hasSearchPos = false;
                        currentState = EnemyState.Patrolling;
                        GoToClosestPatrolPoint();
                        break;
                    }

                    if (navAgent.pathPending == false && navAgent.remainingDistance <= stopDistance && isLookingAround == false)
                    {
                        hasSearchPos = false;
                        isLookingAround = true;
                        StartCoroutine(LookAroundRoutine());
                    }
                    break;
                }

            case EnemyState.LookingAround:
                {
                    // handled in the coroutine
                    break;
                }
        }
    }

    private void DoLookLeftRight()
    {
        lookTimer += Time.deltaTime * lookSpeed;
        float angle = Mathf.Sin(lookTimer) * lookAngle;
        transform.rotation = startRotation * Quaternion.Euler(0f, angle, 0f);
    }

    private void UpdateDetectionLevel()
    {
        if (isStationary == false && hearingSensor != null && hearingSensor.HasHeardSomething)
        {
            NavMeshHit navHit;
            bool foundSpot = NavMesh.SamplePosition(hearingSensor.LastHeardPosition, out navHit, 3f, NavMesh.AllAreas);

            if (foundSpot)
            {
                lastHeardPos = navHit.position;
                hasHeardSomething = true;
                searchPos = navHit.position;
                hasSearchPos = true;
                currentState = EnemyState.Searching;
                navAgent.isStopped = false;
                navAgent.SetDestination(searchPos);
            }

            hearingSensor.HasHeardSomething = false;
        }
        else if (hearingSensor != null && hearingSensor.HasHeardSomething)
        {
            hearingSensor.HasHeardSomething = false;
        }

        float highest = 0f;

        for (int i = 0; i < visionCones.Length; i++)
        {
            if (visionCones[i] == null) continue;
            if (visionCones[i].gameObject.activeInHierarchy == false) continue;

            if (visionCones[i].DetectionAmount > highest)
                highest = visionCones[i].DetectionAmount;
        }

        if (hearingSensor != null && hearingSensor.HearingLevel > highest)
            highest = hearingSensor.HearingLevel;

        currentDetectionLevel = highest;
    }

    private IEnumerator LookAroundRoutine()
    {
        currentState = EnemyState.LookingAround;
        navAgent.isStopped = true;

        float timer = 0f;
        while (timer < lookAroundTime)
        {
            timer += Time.deltaTime;
            transform.Rotate(0f, 120f * Time.deltaTime, 0f);
            yield return null;
        }

        navAgent.isStopped = false;
        isLookingAround = false;
        currentState = EnemyState.Patrolling;
        GoToClosestPatrolPoint();
    }

    private void DoPatrol()
    {
        if (waitingAtPoint) return;

        if (navAgent.pathPending == false && navAgent.remainingDistance <= stopDistance)
            StartCoroutine(WaitAtPoint());
    }

    private IEnumerator WaitAtPoint()
    {
        waitingAtPoint = true;
        navAgent.isStopped = true;
        yield return new WaitForSeconds(patrolWaitTime);
        navAgent.isStopped = false;
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        GoToNextPatrolPoint();
        waitingAtPoint = false;
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        if (patrolPoints[patrolIndex] == null) return;
        navAgent.SetDestination(patrolPoints[patrolIndex].position);
    }

    private void GoToClosestPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        int closestIndex = 0;
        float closestDist = float.MaxValue;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;

            float d = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (d < closestDist)
            {
                closestDist = d;
                closestIndex = i;
            }
        }

        patrolIndex = closestIndex;
        navAgent.SetDestination(patrolPoints[patrolIndex].position);
    }

    private void OnDrawGizmosSelected()
    {
        if (hasHeardSomething)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lastHeardPos, 0.2f);
            Gizmos.DrawLine(transform.position + Vector3.up * 1.2f, lastHeardPos);
        }

        if (hasSearchPos)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(searchPos, 0.22f);
        }
    }
}