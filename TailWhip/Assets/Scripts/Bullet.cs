using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float defaultSpeed = 50f;
    [SerializeField] private float defaultLifetime = 3f;
    private Rigidbody rb;
    private Transform owner;

    private float currentDamage;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void Initialize(float damage, float scaleMultiplier, float speedMultiplier, Transform ownerRoot = null)
    {
        currentDamage = damage;
        owner = ownerRoot;
        transform.localScale *= scaleMultiplier;
        
        if (owner != null)
        {
            Collider myCol = GetComponent<Collider>();
            foreach(Collider col in owner.GetComponentsInChildren<Collider>())
            {
                Physics.IgnoreCollision(myCol, col);
            }
        }

        rb.linearVelocity = transform.forward * (defaultSpeed * speedMultiplier);
        Destroy(gameObject, defaultLifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Bullet touched: {other.name} (layer: {LayerMask.LayerToName(other.gameObject.layer)}, trigger: {other.isTrigger})", other);
        if (other.isTrigger) return;
        if (owner != null && other.transform.IsChildOf(owner)) return;

        Destroy(gameObject);
    }
}
