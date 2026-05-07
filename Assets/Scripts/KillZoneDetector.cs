using UnityEngine;

public class KillZoneDetector : MonoBehaviour
{
    [SerializeField] private string ballObjectName = "WreckingBall";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning)
        {
            return;
        }

        if (other.attachedRigidbody == null)
        {
            return;
        }

        if (other.attachedRigidbody.gameObject.name == ballObjectName)
        {
            GameManager.Instance.EndGame(false);
        }
    }
}
