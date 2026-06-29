using UnityEngine;
using Round2;

public class Round2MovingDebrisHazard : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform pointA;
    public Transform pointB;
    public float moveSpeed = 1.0f;
    public float bobAmplitude = 0.05f;
    public float bobSpeed = 2.0f;
    public bool rotateTowardMovement = true;

    [Header("Damage Settings")]
    public int damageAmount = 1;
    public float damageCooldown = 1.0f;
    
    [Header("References")]
    public Round2RealtimeRoundController roundController;

    private float pingPongTime;
    private float lastDamageTime = -999f;
    private Vector3 startPos;
    
    private void Start()
    {
        if (roundController == null)
            roundController = FindObjectOfType<Round2RealtimeRoundController>();
            
        startPos = transform.position;
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning("[Round2MovingDebrisHazard] Missing waypoints on " + gameObject.name);
        }
    }

    private void Update()
    {
        if (roundController == null || !roundController.IsPlaying())
            return;

        if (pointA != null && pointB != null)
        {
            float dist = Vector3.Distance(pointA.position, pointB.position);
            if (dist > 0)
            {
                pingPongTime += Time.deltaTime * moveSpeed / dist;
                float t = Mathf.PingPong(pingPongTime, 1f);
                
                Vector3 targetPos = Vector3.Lerp(pointA.position, pointB.position, t);
                targetPos.y += Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
                
                Vector3 moveDir = targetPos - transform.position;
                transform.position = targetPos;
                
                if (rotateTowardMovement && moveDir.sqrMagnitude > 0.0001f)
                {
                    moveDir.y = 0;
                    if (moveDir != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 5f);
                    }
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (roundController == null || !roundController.IsPlaying()) return;

        if (Time.time - lastDamageTime < damageCooldown) return;

        var boatController = other.GetComponentInParent<Round2FirstPersonBoatController>();
        if (boatController != null)
        {
            lastDamageTime = Time.time;
            roundController.ApplyDamage(damageAmount);
            roundController.ShowFeedback("Va chạm vật trôi! Độ bền thuyền -" + damageAmount);
        }
    }
}
