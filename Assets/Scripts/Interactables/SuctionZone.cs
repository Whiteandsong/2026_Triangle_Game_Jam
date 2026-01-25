using UnityEngine;

public class SuctionZone : MonoBehaviour
{
    [Header("Suction Settings")]
    [SerializeField] private float suctionForce = 8f;
    
    private Rigidbody2D playerRb;
    private bool isPlayerInRange = false;

    private void FixedUpdate()
    {
        if (isPlayerInRange && playerRb != null)
        {
            Vector2 direction = (transform.position - playerRb.transform.position).normalized;
            playerRb.AddForce(direction * suctionForce);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerRb = other.GetComponent<Rigidbody2D>();
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerRb = null;
        }
    }
}
