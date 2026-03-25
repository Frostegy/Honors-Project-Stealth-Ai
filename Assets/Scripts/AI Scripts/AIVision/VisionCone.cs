using UnityEngine;

public class VisionCone : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Vision Cone Colour in Game")]
    public Material visionConeMaterial;

    [Header("Vision")]
    [Tooltip("Vision Cones Range")]
    public float visionRange = 5f;
    [Tooltip("The width of the vision cone")]
    public float visionAngle = 90f;
    [Tooltip("Layers treated as obstacles, The enemy cannot see through these")]
    public LayerMask obstacleMask;

    [Header("Detection")]
    [Tooltip("How long the player must stay in the cone to be fully detected.")]
    public float timeToDetect = 1f;
    [Tooltip("How long it takes for detection to fully drain after losing sight of player")]
    public float detectionDrainTime = 2f;
    [Tooltip("If enabled, detection level will not change")]
    private bool freezeDetection = false;


    [Header("Cone Mesh")]
    [Tooltip("Show the vision cone mesh during play mode")]
    public bool showConeInGame = true;

    [Header("Gizmo Color")]
    [Tooltip("Colour of the cone drawn in the editor.")]
    public Color coneColor = new Color(0f, 1f, 1f, 0.25f);

    private int coneResolution = 120;
    private float eyeHeight = 0.8f;
    private int gizmoSegments = 30;

    public float DetectionAmount { get; private set; }

    private Transform playerTransform;
    private Mesh coneMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        SetupMeshStuff();
    }

    void Start()
    {
        SetupMeshStuff();

        if (meshRenderer != null && visionConeMaterial != null)
            meshRenderer.material = visionConeMaterial;
    }

    void Update()
    {
        UpdateDetection();

        if (showConeInGame)
            DrawTheCone();
    }

    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
    }

    public void ResetDetection()
    {
        DetectionAmount = 0f;
    }

    void UpdateDetection()
    {
        if (freezeDetection)
        {
            return;
        }

        if (playerTransform == null)
        {
            DetectionAmount = 0f;
            return;
        }

        if (CanSeePlayer(playerTransform))
        {
            float fillSpeed = 1f / timeToDetect;
            DetectionAmount += fillSpeed * Time.deltaTime;
        }
        else
        {
           
            DetectionAmount -= (1f / detectionDrainTime) * Time.deltaTime;   
            
        }

        DetectionAmount = Mathf.Clamp01(DetectionAmount);
    }

    public bool CanSeePlayer(Transform player)
    {
        if (player == null)
        {
            return false;
        }

        Vector3 toPlayer = player.position - transform.position;
        float distToPlayer = toPlayer.magnitude;

        if (distToPlayer > visionRange)
        {
            return false;
        }

        // flatten both directions so height does not affect cone angle
        Vector3 flatToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z).normalized;
        Vector3 flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        float angleBetween = Vector3.Angle(flatForward, flatToPlayer);

        if (angleBetween > visionAngle * 0.5f)
        {
            return false;
        }

        Vector3 rayStart = transform.position + Vector3.up * eyeHeight;
        Vector3 rayEnd = player.position + Vector3.up * 0.9f;
        Vector3 rayDir = rayEnd - rayStart;
        float rayLength = rayDir.magnitude;

        if (rayLength <= 0.01f)
        {
            return true;
        }

        rayDir /= rayLength;

        if (Physics.Raycast(rayStart, rayDir, rayLength, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return true;
    }

    void SetupMeshStuff()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if (coneMesh == null)
        {
            coneMesh = new Mesh();
            coneMesh.name = "VisionConeMesh";
        }

        meshFilter.mesh = coneMesh;
    }

    void DrawTheCone()
    {
        SetupMeshStuff();

        if (coneResolution < 3)
            coneResolution = 3;

        float angleInRadians = visionAngle * Mathf.Deg2Rad;

        Vector3[] verts = new Vector3[coneResolution + 1];
        int[] tris = new int[(coneResolution - 1) * 3];

        verts[0] = Vector3.zero;

        float currentAngle = -angleInRadians / 2f;
        float angleStep = angleInRadians / (coneResolution - 1);

        for (int i = 0; i < coneResolution; i++)
        {
            float sin = Mathf.Sin(currentAngle);
            float cos = Mathf.Cos(currentAngle);

            Vector3 worldDir = (transform.forward * cos) + (transform.right * sin);
            Vector3 localDir = (Vector3.forward * cos) + (Vector3.right * sin);

            RaycastHit hit;
            if (Physics.Raycast(transform.position, worldDir, out hit, visionRange, obstacleMask))
                verts[i + 1] = localDir * hit.distance;
            else
                verts[i + 1] = localDir * visionRange;

            currentAngle += angleStep;
        }

        for (int i = 0, j = 0; i < tris.Length; i += 3, j++)
        {
            tris[i] = 0;
            tris[i + 1] = j + 1;
            tris[i + 2] = j + 2;
        }

        coneMesh.Clear();
        coneMesh.vertices = verts;
        coneMesh.triangles = tris;
        coneMesh.RecalculateNormals();
        meshFilter.mesh = coneMesh;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        float halfAngle = visionAngle * 0.5f;
        float step = visionAngle / Mathf.Max(1, gizmoSegments);

        Gizmos.color = coneColor;

        Vector3 prevPoint = origin + Quaternion.Euler(0, -halfAngle, 0) * transform.forward * visionRange;

        for (int i = 1; i <= gizmoSegments; i++)
        {
            float angle = -halfAngle + step * i;
            Vector3 nextPoint = origin + Quaternion.Euler(0, angle, 0) * transform.forward * visionRange;
            Gizmos.DrawLine(origin, nextPoint);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, transform.forward * visionRange);

        if (Application.isPlaying && DetectionAmount > 0f)
        {
            Vector3 barStart = transform.position + Vector3.up * 2.8f;
            Gizmos.color = Color.grey;
            Gizmos.DrawLine(barStart, barStart + transform.right * 1f);

            Gizmos.color = DetectionAmount < 0.5f ? Color.yellow : Color.red;
            Gizmos.DrawLine(barStart, barStart + transform.right * DetectionAmount);
        }
    }
}








