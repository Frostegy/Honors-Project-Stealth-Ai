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

    [Header("References")]
    [Tooltip("The player transform. Leave empty to auto-find by tag.")]
    public Transform player;
    [Tooltip("The patrol points the enemy walks between.")]
    public Transform[] patrolPoints;
    [Tooltip("Vision cone sensors attached to this enemy.")]
    public VisionCone[] visionCones;
    [Tooltip("Hearing sensor attached to this enemy.")]
    public HearingCircle hearingSensor;

    [Header("Stationary")]
    [Tooltip("If enabled the enemy will not patrol and will instead look left and right.")]
    public bool isStationary = false;
    [Tooltip("How far left and right the enemy looks in degrees.")]
    public float lookAngle = 60f;
    [Tooltip("How fast the enemy looks left and right.")]
    public float lookSpeed = 1f;

    [Header("Patrol")]
    [Tooltip("How close the enemy needs to be to a patrol point before moving to the next one.")]
    public float stopDistance = 0.6f;
    [Tooltip("How long the enemy waits at each patrol point before moving on.")]
    public float patrolWaitTime = 2f;

    [Header("Detection")]
    [Tooltip("How much detection is needed before the enemy starts searching. 0 = any detection, 1 = fully detected.")]
    [Range(0f, 1f)]
    public float suspiciousThreshold = 0.15f;

    [Header("Searching")]
    [Tooltip("How long the enemy spins and looks around after reaching a search position.")]
    public float lookAroundTime = 2f;

    [Header("Debug Info")]
    [Tooltip("The enemy's current behaviour state.")]
    public EnemyState currentState = EnemyState.Patrolling;
    [Tooltip("Current detection level from 0 to 1.")]
    [Range(0f, 1f)]
    public float currentDetectionLevel = 0f;
    [Tooltip("True on the frame the player is fully detected.")]
    public bool isFullyDetected = false;
    [Tooltip("True on the frame detection is fully lost.")]
    public bool hasLostDetection = false;

    // all values below are for internal use and are not meant to be set in the editor
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

        // try find player automatically (saves forgetting to assign it tbh)
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");

            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
            else
            {
                Debug.LogWarning(name + " couldn't find player (tag = Player)");
            }
        }

        // grab sensors if not set manually
        if (visionCones == null || visionCones.Length == 0)
        {
            visionCones = GetComponentsInChildren<VisionCone>(true);
        }

        if (hearingSensor == null)
        {
            hearingSensor = GetComponentInChildren<HearingCircle>();
        }

        // pass player into all cones
        for (int i = 0; i < visionCones.Length; i++)
        {
            if (visionCones[i] != null)
            {
                visionCones[i].SetPlayerTransform(player);
            }
        }
    }


    private void Start() 
    {
        startRotation = transform.rotation;

        if (isStationary == false) // if we're not stationary, start patrolling to the first point
        {
            GoToNextPatrolPoint();
        }
        else
        {
            navAgent.isStopped = true;
        }
    }


    private void Update()
    {
        if (player == null)
        {
            return;
        }

        isFullyDetected = false;
        hasLostDetection = false;

        UpdateDetectionLevel();

        if (currentDetectionLevel >= 1f) // if we've fully detected the player, trigger game over and stop updating the enemy
        {
            isFullyDetected = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
            }

            return;
        }

        if (isStationary) // if the enemy is stationary they don't do normal patrols, they just look left and right and react to sounds/vision as normal
        {
            DoLookLeftRight();
            return;
        }

        switch (currentState)// State Machine
        {
            case EnemyState.Patrolling: // in this state the enemy moves between patrol points and waits at them, if they see or hear something they go to suspicious
                {
                    DoPatrol();

                    if (currentDetectionLevel >= suspiciousThreshold) // if we see or hear something enough to be suspicious it goes to suspicious state which is the same as patrolling but if we lose the player it goes back to patrolling instead of looking around for a bit first like searching
                    {
                        currentState = EnemyState.Suspicious;
                    }

                    break;
                }

            case EnemyState.Suspicious: // in this state the enemy has seen or heard something and is more alert, if they lose it they go back to patrols, if they keep it or see/hear more they go to searching
                {
                    DoPatrol(); 

                    if (currentDetectionLevel <= 0f) // if we lose detection completely, go back to patrolling
                    {
                        currentState = EnemyState.Patrolling;
                        hasLostDetection = true;
                    }

                    break;
                }

            case EnemyState.Searching: // in this state the enemy moves to the last known position of the player or the location of a noise, and looks around for them
                {
                    if (hasSearchPos == false) // if for some reason we don't have a search position (shouldn't really happen) just go back to patrolling instead of getting stuck
                    {
                        currentState = EnemyState.Patrolling;
                        GoToClosestPatrolPoint();
                        break;
                    }

                    navAgent.isStopped = false;
                    navAgent.SetDestination(searchPos);

                    if (navAgent.pathStatus == NavMeshPathStatus.PathInvalid) // if for some reason we can't path to the search position, just go back to patrolling instead of getting stuck
                    {
                        hasSearchPos = false;
                        currentState = EnemyState.Patrolling;
                        GoToClosestPatrolPoint();
                        break;
                    }

                    if (navAgent.pathPending == false && navAgent.remainingDistance <= stopDistance && isLookingAround == false) // once we reach the search position, start looking around
                    {
                        hasSearchPos = false;
                        isLookingAround = true;
                        StartCoroutine(LookAroundRoutine());
                    }

                    break;
                }

            case EnemyState.LookingAround: // looks around for a bit after reaching a search position, then goes back to patrolling. If they see or hear the player while looking around they go back to searching and reset the look around timer
                {
                    // coroutine handles this bit
                    break;
                }
        }
    }

    private void DoLookLeftRight()// used when the enemy is stationary, just makes them look left and right to simulate them "patrolling" an area with their sight
    {
        lookTimer += Time.deltaTime * lookSpeed;

        float angle = Mathf.Sin(lookTimer) * lookAngle;

        transform.rotation = startRotation * Quaternion.Euler(0f, angle, 0f);
    }

    private void UpdateDetectionLevel() // checks vision cones and hearing sensor to update the current detection level, also handles hearing reactions like going to investigate a noise
    {
        // hearing reaction first
        if (isStationary == false && hearingSensor != null && hearingSensor.HasHeardSomething && hearingSensor.LastHeardStrength >= suspiciousThreshold)
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
            // clear it anyway (prevents weird repeats)
            hearingSensor.HasHeardSomething = false;
        }

        float highest = 0f;

        for (int i = 0; i < visionCones.Length; i++)
        {
            if (visionCones[i] == null)
            {
                continue;
            }

            if (visionCones[i].gameObject.activeInHierarchy == false)
            {
                continue;
            }

            if (visionCones[i].DetectionAmount > highest)
            {
                highest = visionCones[i].DetectionAmount;
            }
        }

        if (hearingSensor != null && hearingSensor.HearingLevel > highest)
        {
            highest = hearingSensor.HearingLevel;
        }

        // if we kinda see the player, go to them
        if (isStationary == false && highest >= suspiciousThreshold && player != null)
        {
            NavMeshHit navHit;

            bool foundSpot = NavMesh.SamplePosition(player.position, out navHit, 3f, NavMesh.AllAreas);

            if (foundSpot)
            {
                searchPos = navHit.position;
                hasSearchPos = true;

                currentState = EnemyState.Searching;

                navAgent.isStopped = false;
                navAgent.SetDestination(searchPos);
            }
        }

        currentDetectionLevel = highest;
    }

    private IEnumerator LookAroundRoutine() // called when the enemy reaches a search position, makes them look around for a bit before resuming patrols
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

    private void DoPatrol() // handles moving between patrol points and waiting at them
    {
        if (waitingAtPoint)
        {
            return;
        }

        if (navAgent.pathPending == false && navAgent.remainingDistance <= stopDistance)
        {
            StartCoroutine(WaitAtPoint());
        }
    }

    private IEnumerator WaitAtPoint() // called when the enemy reaches a patrol point, makes them wait for a bit before moving to the next one
    {
        waitingAtPoint = true;

        navAgent.isStopped = true;

        yield return new WaitForSeconds(patrolWaitTime);

        navAgent.isStopped = false;

        if (patrolPoints == null || patrolPoints.Length == 0) 
        {
            waitingAtPoint = false;
            yield break;
        }

        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;

        GoToNextPatrolPoint();

        waitingAtPoint = false;
    }

    private void GoToNextPatrolPoint() // sets the nav agent's destination to the next patrol point
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return;
        }

        if (patrolPoints[patrolIndex] == null)
        {
            return;
        }

        navAgent.SetDestination(patrolPoints[patrolIndex].position);
    }

    private void GoToClosestPatrolPoint() // used when losing the player or after searching, to resume patrols from the nearest point
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return;
        }

        int closestIndex = 0;
        float closestDist = float.MaxValue;

        for (int i = 0; i < patrolPoints.Length; i++) // loop through all patrol points to find the closest one
        {
            if (patrolPoints[i] == null)
            {
                continue;
            }

            float d = Vector3.Distance(transform.position, patrolPoints[i].position);

            if (d < closestDist) 
            {
                closestDist = d;
                closestIndex = i;
            }
        }

        patrolIndex = closestIndex;

        navAgent.SetDestination(patrolPoints[patrolIndex].position); // set destination to the closest patrol point
    }

    private void OnDrawGizmosSelected()  // this draws debug visuals in the editor when the enemy is selected like the last heard position and the current search position
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