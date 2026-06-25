using UnityEngine;

public class CollisionReporter : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("[Boat Collision Enter] " + collision.gameObject.name, collision.gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        Debug.Log("[Boat Collision Stay] " + collision.gameObject.name, collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[Boat Trigger Enter] " + other.gameObject.name, other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        Debug.Log("[Boat Trigger Stay] " + other.gameObject.name, other.gameObject);
    }
}