using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The camera transform that follows the player.")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("Audio source used for footstep sounds.")]
    [SerializeField] private AudioSource source;
    [Tooltip("The footstep audio clip to play.")]
    [SerializeField] private AudioClip footstepClip;

    [Header("Movement")]
    [Tooltip("Base movement speed.")]
    [SerializeField] private float speed = 5f;
    [Tooltip("Speed multiplier applied when sprinting. Also used to slow down crouching.")]
    public float speedMultiplier = 1.5f;

    [Header("Noise")]
    [Tooltip("Noise radius emitted while walking.")]
    [SerializeField] private float walkRadius = 6f;
    [Tooltip("Noise radius emitted while sprinting.")]
    [SerializeField] private float runRadius = 10f;

    [Header("Footsteps")]
    [Tooltip("Time between footstep sounds while walking.")]
    private float walkStepInterval = 0.35f;
    [Tooltip("Time between footstep sounds while sprinting.")]
    private float runStepInterval = 0.2f;

    [Header("Gizmos")]
    [Tooltip("Draw the noise radius in the editor.")]
    [SerializeField] private bool drawNoiseRadius = true;
    [Tooltip("If enabled, always shows both walk and run radii. If disabled, shows the current movement state radius only.")]
    [SerializeField] private bool alwaysShowRadius = true;

    [Header("Debug")]
    [Tooltip("Whether the player is currently sprinting.")]
    public bool isSprinting = false;
    [Tooltip("Whether the player is currently crouching.")]
    public bool isCrouching = false;

    private CharacterController controller;
    private Vector3 cameraOffset = new Vector3(0f, 20f, 1f);
    private float footstepTimer = 0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform != null)
        {
            cameraTransform.position = transform.position + cameraOffset;
            cameraTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private void LateUpdate() // this is where we update the camera position to follow the player
    {
        if (cameraTransform != null)
        {
            cameraTransform.position = transform.position + cameraOffset;
        }
            
    }

    private void Update() // this is where we handle player input and movement
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        isSprinting = Input.GetKey(KeyCode.LeftShift);
        isCrouching = Input.GetKey(KeyCode.LeftControl);

        Vector3 move = new Vector3(moveX, 0f, moveZ);

        if (isSprinting)
        {
            move = move * speedMultiplier;
        }
            

        if (isCrouching)
        {
            move = move / speedMultiplier;
        }
           

        controller.Move(move * speed * Time.deltaTime);

        bool isMoving = move.magnitude > 0.1f;

        if (isCrouching)
        {
            footstepTimer = 0f;
        }
        else if (isMoving)
        {
            float interval = isSprinting ? runStepInterval : walkStepInterval;

            footstepTimer += Time.deltaTime;

            if (footstepTimer >= interval)
            {
                EmitFootstep(isSprinting);
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    public void EmitFootstep(bool running) // call this from  movement code whenever the player takes a step
    {
        if (NoiseSystem.Instance == null) 
        {

            return;
        }
            

        float r = running ? runRadius : walkRadius;
        NoiseType type = running ? NoiseType.Running : NoiseType.Footstep;

        NoiseSystem.Instance.Emit(transform.position, r, type, transform);

        if (footstepClip != null && source != null)
        {
            source.pitch = Random.Range(0.9f, 1.1f);
            source.PlayOneShot(footstepClip);
        }
    }

    private void OnDrawGizmosSelected() // this draws the noise radius in the editor when the player is selected
    {
        if (!drawNoiseRadius) return;

        Vector3 pos = transform.position;

        if (alwaysShowRadius)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
            Gizmos.DrawWireSphere(pos, walkRadius);

            Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
            Gizmos.DrawWireSphere(pos, runRadius);
            return;
        }

        if (isCrouching)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
            Gizmos.DrawWireSphere(pos, 0.5f);
        }
        else if (isSprinting)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
            Gizmos.DrawWireSphere(pos, runRadius);
        }
        else
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
            Gizmos.DrawWireSphere(pos, walkRadius);
        }
    }
}