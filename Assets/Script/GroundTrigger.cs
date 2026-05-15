using UnityEngine;

public class GroundTrigger : MonoBehaviour
{
    private RetractableGround ground;

    void Start()
    {
        ground = GetComponentInParent<RetractableGround>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ground.TriggerGround();
        }
    }
}