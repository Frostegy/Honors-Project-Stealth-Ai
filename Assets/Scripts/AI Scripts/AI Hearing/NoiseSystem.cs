using System;
using UnityEngine;

public enum NoiseType // different types of noise for more nuanced AI reactions
{
    Footstep,
    Running 
}

public struct NoiseEvent // this struct is used to pass noise information to listeners
{
    public Vector3 position; // where the noise happened
    public float radius; // how far the noise can be heard
    public NoiseType type; // what type of noise it is (footstep, running, etc.)
    public Transform source; // optional reference to the source of the noise (e.g. the player transform) for more advanced AI reactions
}

// put on empty GameObject in the scene
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
    // to use NoiseSystem.Instance.Emit(transform.position, 5f, NoiseType.Footstep);
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