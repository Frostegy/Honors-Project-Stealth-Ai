using UnityEngine;

public class HearingCircle : MonoBehaviour
{
    [Header("Hearing")]
    public float hearingRadius = 8f;

    [Header("Detection")]
    // how fast the hearing meter fills up when a sound is heard
    public float hearingFillSpeed = 0.8f;
    // how long it takes for the hearing meter to drain back to 0
    public float hearingDrainTime = 4f;

    [Header("Occlusion (Walls)")]
    public bool blockedSound = true;
    public LayerMask obstacleMask;
    // if there is a wall between the enemy and the sound, multiply the strength by this
    [Range(0f, 1f)] public float blockedSoundfalloff = 0.35f;

    [Header("Debug")]
    public bool drawGizmos = true;

    // 0 = heard nothing, 1 = fully alerted by sound
    // the ai controller reads this the same way it reads the vision cone detection
    public float HearingLevel { get; private set; }

    // when true the hearing level wont go up or down
    // same as the vision cone freezeDetection, set by the ai controller while chasing
    public bool stopDetecting = false;

    // store the last sound position so the controller can use it for searching
    public Vector3 LastHeardPosition { get; private set; }
    public bool HasHeardSomething { get; set; }

    // used internally to know how strong the latest sound was
    private float latestSoundStrength = 0f;
    private bool receivedSoundThisFrame = false;

    private AiAgentController ownerController;

    void Awake()
    {
        ownerController = GetComponentInParent<AiAgentController>();
    }

    void OnEnable()
    {
        NoiseSystem.OnNoiseEmitted += OnNoise;
    }

    void OnDisable()
    {
        NoiseSystem.OnNoiseEmitted -= OnNoise;
    }

    void Update()
    {
        if (stopDetecting) return;

        if (receivedSoundThisFrame)
        {
            // fill the hearing meter based on how strong the sound was
            HearingLevel += latestSoundStrength * hearingFillSpeed * Time.deltaTime;
        }
        else
        {
            // no sound this frame, drain it down
            if (hearingDrainTime > 0f)
                HearingLevel -= (1f / hearingDrainTime) * Time.deltaTime;
        }

        HearingLevel = Mathf.Clamp01(HearingLevel);

        // reset for next frame
        receivedSoundThisFrame = false;
        latestSoundStrength = 0f;
    }

    void OnNoise(NoiseEvent e)
    {
        if (stopDetecting) return;

        float dist = Vector3.Distance(transform.position, e.position);

        // sound didnt reach us
        if (dist > e.radius) return;
        if (dist > hearingRadius) return;

        // work out how strong the sound is based on distance
        float maxRange = Mathf.Min(e.radius, hearingRadius);
        float strength = 1f - (dist / maxRange);
        strength = Mathf.Clamp01(strength);

        // reduce strength if theres a wall in the way
        if (blockedSound)
        {
            Vector3 start = transform.position + Vector3.up * 1.6f;
            Vector3 end = e.position + Vector3.up * 0.1f;
            Vector3 dir = end - start;
            float len = dir.magnitude;

            if (len > 0.01f)
            {
                dir = dir / len;
                if (Physics.Raycast(start, dir, len, obstacleMask, QueryTriggerInteraction.Ignore))
                    strength *= blockedSoundfalloff;
            }
        }

        if (strength <= 0f) return;

        // keep the strongest sound if multiple noises happened this frame
        if (strength > latestSoundStrength)
        {
            latestSoundStrength = strength;
            LastHeardPosition = e.position;
            HasHeardSomething = true;
        }

        receivedSoundThisFrame = true;
    }

    public void ResetHearing()
    {
        HearingLevel = 0f;
        HasHeardSomething = false;
        receivedSoundThisFrame = false;
        latestSoundStrength = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, hearingRadius);
    }
}