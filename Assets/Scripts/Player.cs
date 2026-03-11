using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Player : MonoBehaviour
{
    private CharacterController controller;

    [SerializeField] private float speed = 5f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    private Vector3 camera = new Vector3(0f, 20f, 1f);

    [SerializeField] private AudioSource source = null;

    public bool isSprinting = false;
    public bool isCrouching = false;
    public float speedMultiplier = 1.5f;

    [Header("Noise Radius")]
    [SerializeField] private float walkRadius = 6f;
    [SerializeField] private float runRadius = 10f;

    [Header("Footstep Timing")]
    [SerializeField] private float walkStepInterval = 0.4f;
    [SerializeField] private float runStepInterval = 0.25f;

    [SerializeField] private AudioClip footstepClip;

    private float footstepTimer = 0f;
    private void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform != null)
        {
            cameraTransform.position = transform.position + camera;
            cameraTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private void LateUpdate()
    {
        // move the camera with the player every frame
        if (cameraTransform != null)
        {
            cameraTransform.position = transform.position + camera;
        }
    }

    private void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        isSprinting = Input.GetKey(KeyCode.LeftShift);
        isCrouching = Input.GetKey(KeyCode.LeftControl);

        Vector3 move = new Vector3(moveX, 0f, moveZ);

        // go faster when sprinting
        if (isSprinting)
        {
            move = move * speedMultiplier;
        }

        // go slower when crouching
        if (isCrouching)
        {
            move = move / speedMultiplier;
        }

        controller.Move(move * speed * Time.deltaTime);

        // -------- FOOTSTEP NOISE --------
        bool isMoving = move.magnitude > 0.1f;

        if (isCrouching)
        {
            // crouching makes no noise so reset the timer
            footstepTimer = 0f;
        }
        else if (isMoving)
        {
            // pick the right interval depending on if we are sprinting or not
            float interval = walkStepInterval;

            if (isSprinting)
            {
                interval = runStepInterval;
            }

            footstepTimer = footstepTimer + Time.deltaTime;

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

    public void EmitFootstep(bool running)
    {
        if (NoiseSystem.Instance == null) return;

        float r = walkRadius;

        if (running)
        {
            r = runRadius;
        }

        if (running)
        {
            NoiseSystem.Instance.Emit(transform.position, r, NoiseType.RunStep, transform);
        }
        else
        {
            NoiseSystem.Instance.Emit(transform.position, r, NoiseType.Footstep, transform);
        }

        if (footstepClip != null && source != null)
        {
            source.pitch = Random.Range(0.9f, 1.1f);
            source.PlayOneShot(footstepClip);
        }
    }
}