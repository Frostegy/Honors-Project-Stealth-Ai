using UnityEngine;

public class VisionCone : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The material used to render the vision cone in the game view.")]
    public Material visionConeMaterial;

    [Header("Vision")]
    [Tooltip("How far the enemy can see.")]
    public float visionRange = 5f;
    [Tooltip("The width of the vision cone in degrees.")]
    public float visionAngle = 90f;
    [Tooltip("Layers treated as obstacles. The enemy cannot see through these.")]
    public LayerMask obstacleMask;

    [Header("Detection")]
    [Tooltip("How long the player must stay in the cone to be fully detected.")]
    public float timeToDetect = 1f;
    [Tooltip("If enabled, detection level will not change.")]
    public bool freezeDetection = false;

    [Header("Cone Mesh")]
    [Tooltip("Show the vision cone mesh during play mode.")]
    public bool showConeInGame = true;

    [Header("Gizmos")]
    [Tooltip("Colour of the cone drawn in the editor.")]
    public Color coneColor = new Color(0f, 1f, 1f, 0.25f);

    [HideInInspector] public int coneResolution = 120;
    [HideInInspector] public float eyeHeight = 0.8f;
    [HideInInspector] public int gizmoSegments = 30;

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
        if (freezeDetection) return;

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
            DetectionAmount = 0f;
        }

        DetectionAmount = Mathf.Clamp01(DetectionAmount);
    }

    public bool CanSeePlayer(Transform player)
    {
        if (player == null) return false;

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (distToPlayer > visionRange) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angleBetween = Vector3.Angle(transform.forward, dirToPlayer);
        if (angleBetween > visionAngle / 2f) return false;

        Vector3 rayStart = transform.position + Vector3.up * eyeHeight;
        Vector3 rayEnd = player.position + Vector3.up * 0.9f;
        Vector3 rayDir = rayEnd - rayStart;
        float rayLength = rayDir.magnitude;

        if (rayLength <= 0.01f) return true;

        rayDir = rayDir / rayLength;

        if (Physics.Raycast(rayStart, rayDir, rayLength, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

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








