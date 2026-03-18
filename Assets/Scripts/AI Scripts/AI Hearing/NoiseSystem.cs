using System;
using UnityEngine;

public enum NoiseType
{
    Footstep,
    Running
}

public struct NoiseEvent
{
    public Vector3 position;
    public float radius;
    public NoiseType type;
    public Transform source;
}

// put this on an empty gameobject in your scene
// other scripts call NoiseSystem.Instance.Emit() to make a sound
public class NoiseSystem : MonoBehaviour
{
    public static NoiseSystem Instance;

    // hearing sensors subscribe to this event to know when a sound happens
    public static event Action<NoiseEvent> OnNoiseEmitted;

    void Awake()
    {
        // make sure there is only ever one noise system
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // call this from anywhere to make a noise
    // e.g. NoiseSystem.Instance.Emit(transform.position, 5f, NoiseType.Footstep);
    public void Emit(Vector3 position, float radius, NoiseType type, Transform source = null)
    {
        NoiseEvent newEvent = new NoiseEvent();
        newEvent.position = position;
        newEvent.radius = radius;
        newEvent.type = type;
        newEvent.source = source;

        OnNoiseEmitted?.Invoke(newEvent);
    }
}