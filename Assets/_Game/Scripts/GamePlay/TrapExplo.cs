using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TrapExplo : MonoBehaviour
{
    [Header("Explosion Effect")]
    [Tooltip("Khoảng cách tối đa từ tâm bomb để trap bị phá (world units)")]
    public float maxDistanceEffect = 4f;

    [Header("VFX khi phá (tuỳ chọn)")]
    public GameObject breakVFX;
    public float vfxLifeTime = 2f;

    [Header("Mảnh vỡ (tuỳ chọn)")]
    public GameObject debrisPrefab;
    public int debrisCount = 5;
    public float debrisForce = 200f;

    bool destroyed = false;

    /// <summary>
    /// Hàm này được bomb gọi khi nổ.
    /// </summary>
    public void OnExploded(Vector2 explosionCenter)
    {
        if (destroyed) return;

        float dist = Vector2.Distance(explosionCenter, transform.position);
        if (dist > maxDistanceEffect) return;

        Break(explosionCenter);
    }

    void Break(Vector2 explosionCenter)
    {
        destroyed = true;

        // VFX
        if (breakVFX)
        {
            var fx = Instantiate(breakVFX, transform.position, Quaternion.identity);
            Destroy(fx, vfxLifeTime);
        }

        // Mảnh vỡ
        if (debrisPrefab && debrisCount > 0)
        {
            for (int i = 0; i < debrisCount; i++)
            {
                var debris = Instantiate(debrisPrefab, transform.position, Random.rotation);
                var rb = debris.GetComponent<Rigidbody2D>();
                if (rb)
                {
                    Vector2 dir = ((Vector2)rb.transform.position - explosionCenter).normalized;
                    rb.AddForce(dir * debrisForce);
                }
                Destroy(debris, 3f);
            }
        }

        Destroy(gameObject);
    }
}
