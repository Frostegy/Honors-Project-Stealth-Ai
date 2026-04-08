using UnityEngine;


public class Win : MonoBehaviour// win script
{
    private void OnTriggerEnter(Collider other)// when the player enters the trigger
    {
        // check if it was the player that touched it
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerWin(); // you win :)))))))
        }
    }
}
