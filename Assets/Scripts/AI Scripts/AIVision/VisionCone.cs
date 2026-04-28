using UnityEngine;

public class VisionCone : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Vision Cone Colour in Game")]
    [SerializeField] private Material visionConeMaterial;

    [Header("Vision")]
    [Tooltip("Vision Cones Range")]
    [SerializeField] private float visionRange = 5f; // how far the enemy can see

    [Tooltip("The width of the vision cone")]
    [SerializeField] private float visionAngle = 90f; // the angle of the vision cone

    [Tooltip("Layers treated as obstacles, The enemy cannot see through these")]
    [SerializeField] private LayerMask obstacleMask; // which layers block vision

    [Header("Detection")]
    [Tooltip("How long the player must stay in the cone to be fully detected.")]
    [SerializeField] private float timeToDetect = 1f; // how long it takes for detection to fill up when the player is in sight

    [Tooltip("How long it takes for detection to fully drain after losing sight of player")]
    [SerializeField] private float detectionDrainTime = 2f; // how long it takes for detection to drain after losing sight of the player

    [Tooltip("If enabled, detection level will not change")]
    [SerializeField] private bool freezeDetection = false; // for testing purposes, freezes the detection level so you can see the cone without it draining

    [Header("Cone Mesh")]
    [Tooltip("Show the vision cone mesh during play mode")]
    [SerializeField] private bool showConeInGame = true;// for testing purposes, shows the vision cone mesh during play mode

    [Header("Gizmo Color")]
    [Tooltip("Colour of the cone drawn in the editor.")]
    [SerializeField] private Color coneColor = new Color(0f, 1f, 1f, 0.25f); // cyan with some transparency

    private int coneResolution = 120; // how many vertices to use when generating the vision cone mesh 

    private float eyeHeight = 0.8f; // enemey eye height

    private int gizmoSegments = 30; // how many segments to use when drawing the cone in the editor


    public float DetectionAmount { get; private set; } 

    private Transform playerTransform;
    private Mesh coneMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        SetupMesh();
    }

    void Start() 
    {
        SetupMesh();

        if (meshRenderer != null && visionConeMaterial != null)
        {
            meshRenderer.material = visionConeMaterial;
        }
            
    }


    void Update() // this is where we update the detection level and draw the vision cone mesh
    {
        UpdateDetection();

        if (showConeInGame)
        {
           DrawTheCone();
        }
            
    }

    public void SetPlayerTransform(Transform player)  
    {
        playerTransform = player;
    }

    public void ResetDetection() //reset detection to 0
    {
        DetectionAmount = 0f;
    }

    void UpdateDetection() // this is where we update the detection level based on whether the player is in sight or not
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

    //does three checks - is the player close enough, is the player inside the cone, is there clear line of sight
    public bool CanSeePlayer(Transform player) // this is where we check if the player is within the vision cone and not behind an obstacle
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

        Vector3 flatToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z).normalized;

        Vector3 flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        float angleBetween = Vector3.Angle(flatForward, flatToPlayer);

        if (angleBetween > visionAngle * 0.5f)
        {
            return false;
        }

        // checks for raycast from the enemys eyes to the player to see if there is an obstacle that is in the way, if there is it cant see
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

    void SetupMesh() // this is where we set up the mesh components for the vision cone mesh
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();

            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
                
        }

        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();

            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
               
        }

        if (coneMesh == null)
        {
            coneMesh = new Mesh();
            coneMesh.name = "VisionConeMesh";
        }

        meshFilter.mesh = coneMesh;
    }

    void DrawTheCone() // this is where it generates the vision cone mesh 
    {
        SetupMesh();

        if (coneResolution < 3)
        {
            coneResolution = 3;
        }
           

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
            {
                verts[i + 1] = localDir * hit.distance;
            }  
            else
            {
                verts[i + 1] = localDir * visionRange;
            }
                

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

    private void OnDrawGizmosSelected() // this draws the vision cone in the editor so you can see where it is  
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








