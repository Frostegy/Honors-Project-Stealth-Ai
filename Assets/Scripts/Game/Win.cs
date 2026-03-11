using UnityEngine;

// put this script on the object the player needs to touch to win
// make sure the object has a collider and Is Trigger is ticked
public class Win : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // check if it was the player that touched it
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerWin();
        }
    }
}
