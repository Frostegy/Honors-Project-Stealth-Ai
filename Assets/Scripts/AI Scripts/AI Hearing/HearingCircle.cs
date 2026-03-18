using UnityEngine;

public class HearingCircle : MonoBehaviour
{
    [Header("Hearing")]
    [Tooltip("How far the enemy can hear sounds.")]
    public float hearingRadius = 8f;

    [Header("Detection")]
    [Tooltip("How fast the hearing meter fills when a sound is heard.")]
    public float hearingFillSpeed = 0.8f;
    [Tooltip("How long it takes for the hearing meter to drain back to zero after a sound.")]
    public float hearingDrainTime = 4f;

    [Header("Occlusion")]
    [Tooltip("If enabled, walls will reduce how clearly the enemy hears sounds.")]
    public bool blockedSound = true;
    [Tooltip("Layers treated as walls for sound occlusion.")]
    public LayerMask obstacleMask;
    [Tooltip("How much a wall reduces the sound strength. 0 = fully blocked, 1 = no reduction.")]
    [Range(0f, 1f)] public float blockedSoundFalloff = 0.35f;

    [Header("Gizmos")]
    [Tooltip("Draw the hearing radius in the editor.")]
    public bool drawGizmos = true;
    [Tooltip("Draw a line in the scene view showing whether the last sound was blocked by a wall.")]
    public bool drawSoundRay = true;

    public float HearingLevel { get; private set; }
    public bool stopDetecting = false;
    public Vector3 LastHeardPosition { get; private set; }
    public bool HasHeardSomething { get; set; }
    public float LastHeardStrength { get; private set; }

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
            HearingLevel += latestSoundStrength * hearingFillSpeed * Time.deltaTime;
        }
        else
        {
            if (hearingDrainTime > 0f)
                HearingLevel -= (1f / hearingDrainTime) * Time.deltaTime;
        }

        HearingLevel = Mathf.Clamp01(HearingLevel);

        receivedSoundThisFrame = false;
        latestSoundStrength = 0f;
    }

    void OnNoise(NoiseEvent e)
    {
        if (stopDetecting) return;

        float dist = Vector3.Distance(transform.position, e.position);

        float maxRange = hearingRadius + e.radius;
        if (dist > maxRange) return;

        float strength = 1f - (dist / maxRange);
        strength = Mathf.Clamp01(strength);

        if (blockedSound)
        {
            Vector3 start = transform.position + Vector3.up * 1f;
            Vector3 end = e.position + Vector3.up * 1f;

            bool hitWall = Physics.Linecast(start, end, obstacleMask, QueryTriggerInteraction.Ignore);

            if (drawSoundRay)
                Debug.DrawLine(start, end, hitWall ? Color.red : Color.green, 1f);

            if (hitWall)
                strength *= blockedSoundFalloff;
        }

        if (strength <= 0f) return;

        LastHeardStrength = strength;

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
        LastHeardStrength = 0f;
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