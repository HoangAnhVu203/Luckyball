using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class Bomb : MonoBehaviour
{
    public float fuseTime = 2f;
    public bool startOnTouch = true;

    [Header("Fuse Trigger Filter")]
    public LayerMask fuseTriggerLayers;

    [Header("Explosion")]
    public float explosionRadius = 3f;
    public float explosionForce = 500f;
    public LayerMask affectedLayers = ~0;

    [Header("Effects (optional)")]
    public GameObject explosionVFX;
    public AudioClip explosionSfx;
    public float destroyDelay = 0.2f;

    [Header("Behaviour")]
    public bool freezeOnFuse = true;

    [Header("Scale Effect (Normal Fuse)")]
    public bool enableScaleOnFuse = true;
    public float fuseMaxScaleMultiplier = 1.3f;

    [Header("Scale Effect (Bubble Trigger)")]
    public float bubbleFuseDuration = 2f;
    public float bubbleMaxScaleMultiplier = 1.4f;

    bool fuseStarted = false;
    Coroutine fuseCoroutine;

    Rigidbody2D rb;
    Vector3 initialScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initialScale = transform.localScale;
    }

    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c) c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!startOnTouch) return;
        StartFuseIfNeeded(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!startOnTouch) return;
        StartFuseIfNeeded(collision.gameObject);
    }


    void StartFuseIfNeeded(GameObject triggerer)
    {
        if (fuseStarted) return;

        // chỉ kích hoạt nếu layer hợp lệ
        if (((1 << triggerer.layer) & fuseTriggerLayers.value) == 0)
            return;

        fuseStarted = true;

        if (freezeOnFuse && rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        // Fuse bình thường: dùng fuseTime + fuseMaxScaleMultiplier, scale từ initialScale
        fuseCoroutine = StartCoroutine(FuseAndExplode(fuseTime, fuseMaxScaleMultiplier, true));
    }

    public void TriggerImmediateExplosion()
    {
        if (!gameObject.activeInHierarchy)
            return;

        // dừng fuse cũ nếu có
        if (fuseCoroutine != null)
        {
            StopCoroutine(fuseCoroutine);
            fuseCoroutine = null;
        }

        fuseStarted = true;

        // đóng băng bom
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        // Fuse riêng cho case bong bóng: dùng thời gian & scale riêng, bắt đầu từ scale hiện tại
        fuseCoroutine = StartCoroutine(FuseAndExplode(bubbleFuseDuration, bubbleMaxScaleMultiplier, false));
    }

    IEnumerator FuseAndExplode(float duration, float maxScaleMultiplier, bool useInitialScaleAsBase)
    {
        Vector3 startScale = useInitialScaleAsBase ? initialScale : transform.localScale;
        Vector3 targetScale = (maxScaleMultiplier > 0f)
            ? startScale * maxScaleMultiplier
            : startScale;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (enableScaleOnFuse && maxScaleMultiplier > 0f)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.SmoothStep(0f, 1f, t);
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            }

            yield return null;
        }

        Explode();
    }

    void Explode()
    {
        // VFX
        if (explosionVFX)
        {
            var v = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            Destroy(v, 5f);
        }

        // SFX
        if (explosionSfx)
            AudioSource.PlayClipAtPoint(explosionSfx, transform.position);

        // Ảnh hưởng vật lý
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, affectedLayers);

        foreach (var hit in hits)
        {
            // 1. Lực nổ vật lý (nếu có Rigidbody2D động)
                Rigidbody2D hitRb = hit.attachedRigidbody;
                if (hitRb != null && hitRb.bodyType == RigidbodyType2D.Dynamic && !hitRb.isKinematic)
                {
                    Vector2 dir = hitRb.worldCenterOfMass - (Vector2)transform.position;
                    float dist = Mathf.Max(0.01f, dir.magnitude);

                    float attenuation = Mathf.Clamp01(1f - dist / explosionRadius);
                    attenuation = Mathf.Max(attenuation, 0.2f);

                    Vector2 impulse = dir.normalized * explosionForce * attenuation;
                    hitRb.WakeUp();
                    hitRb.AddForce(impulse, ForceMode2D.Impulse);
                }

                // 2. Gọi WallExplo nếu có (phá tường)
                WallExplo wall = hit.GetComponent<WallExplo>();
                    if (wall != null)
                        wall.OnExploded(transform.position);

                TrapExplo trap = hit.GetComponent<TrapExplo>();
                    if (trap != null)
                    {
                        trap.OnExploded(transform.position);
                    }



            // Nếu thích dùng SendMessage kiểu generic:
            // hit.SendMessage("OnExploded", (Vector2)transform.position, SendMessageOptions.DontRequireReceiver);
        }


        // Ẩn & huỷ bom
        var rend = GetComponent<Renderer>(); if (rend) rend.enabled = false;
        var col = GetComponent<Collider2D>(); if (col) col.enabled = false;

        Destroy(gameObject, destroyDelay);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
