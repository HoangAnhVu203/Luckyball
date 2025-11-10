using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WallExplo : MonoBehaviour
{
    [Header("Wall Settings")]
    public float health = 100f;                  // độ bền tường
    public float explosionSensitivity = 50f;     // mức lực nổ cần thiết để phá
    public float maxDistanceEffect = 4f;         // tầm ảnh hưởng tối đa

    [Header("VFX")]
    public GameObject breakVFX;                  // hiệu ứng vỡ
    public GameObject debrisPrefab;              // mảnh vỡ (nếu có)
    public int debrisCount = 5;
    public float debrisForce = 200f;

    bool destroyed = false;

    public void OnExploded(Vector2 explosionCenter)
    {
        if (destroyed) return;

        float dist = Vector2.Distance(explosionCenter, transform.position);
        if (dist > maxDistanceEffect) return;

        // Tính lực ảnh hưởng theo khoảng cách (gần nổ mạnh hơn)
        float forceEffect = Mathf.Clamp01(1f - dist / maxDistanceEffect) * explosionSensitivity;

        health -= forceEffect;

        if (health <= 0f)
        {
            Break(explosionCenter);
        }
    }

    void Break(Vector2 explosionCenter)
    {
        destroyed = true;

        // Hiệu ứng nổ (vỡ)
        if (breakVFX)
        {
            var fx = Instantiate(breakVFX, transform.position, Quaternion.identity);
            Destroy(fx, 3f);
        }

        // Spawn mảnh vỡ
        if (debrisPrefab)
        {
            for (int i = 0; i < debrisCount; i++)
            {
                var debris = Instantiate(debrisPrefab, transform.position, Random.rotation);
                var rb = debris.GetComponent<Rigidbody2D>();
                if (rb)
                {
                    Vector2 dir = (rb.transform.position - (Vector3)explosionCenter).normalized;
                    rb.AddForce(dir * debrisForce);
                }
                Destroy(debris, 3f);
            }
        }

        Destroy(gameObject);
    }
}
