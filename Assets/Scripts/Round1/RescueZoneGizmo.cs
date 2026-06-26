using UnityEngine;

namespace Round1
{
    public class RescueZoneGizmo : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            var boxCol = GetComponent<BoxCollider>();
            var sphereCol = GetComponent<SphereCollider>();

            Gizmos.color = new Color(0f, 0.5f, 1f, 0.4f);

            if (boxCol != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            }
            else if (sphereCol != null)
            {
                Gizmos.DrawWireSphere(transform.position + sphereCol.center, sphereCol.radius);
            }
        }
    }
}
