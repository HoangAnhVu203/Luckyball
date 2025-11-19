using System.Collections;
using UnityEngine;
using Spine.Unity;   // 👈 nhớ import Spine Unity

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MergeBall : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] string blueLayerName  = "BlueBall";
    [SerializeField] string redLayerName   = "RedBall";
    [SerializeField] string enemyLayerName = "Enemy";   // 👈 thêm tên layer Enemy

    [Header("Merge Settings")]
    [SerializeField, Range(0.05f, 1.5f)] float mergeDuration = 0.25f; // thời gian hút
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("VFX")]
    public GameObject vfxPrefab; // VFX sẽ spawn sau khi biến mất

    [Header("Spine Color When Hit Enemy")]
    public Color enemyHitColor = Color.gray;  // màu xám khi chạm Enemy

    bool _isMerging = false;                  // chặn chạy trùng
    static readonly System.Collections.Generic.HashSet<int> _busy = new(); // tránh đôi va chạm chạy 2 lần

    int blueLayer, redLayer, enemyLayer;
    Rigidbody2D rb;
    Collider2D col;

    // Spine
    SkeletonAnimation skeletonAnim;
    Color originalColor;
    bool colorChangedByEnemy = false;

    void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        blueLayer  = LayerMask.NameToLayer(blueLayerName);
        redLayer   = LayerMask.NameToLayer(redLayerName);
        enemyLayer = LayerMask.NameToLayer(enemyLayerName);

        if (blueLayer == -1 || redLayer == -1)
            Debug.LogWarning("[MergeBall] Chưa khai báo Layer BlueBall/RedBall trong Project Settings > Tags and Layers.");

        if (enemyLayer == -1)
            Debug.LogWarning("[MergeBall] Chưa khai báo Layer Enemy trong Project Settings > Tags and Layers.");

        // Lấy Spine SkeletonAnimation
        skeletonAnim = GetComponent<SkeletonAnimation>();
        if (skeletonAnim != null && skeletonAnim.Skeleton != null)
        {
            originalColor = skeletonAnim.Skeleton.GetColor();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryMerge(collision.collider);
    }

    void OnTriggerEnter2D(Collider2D other) // phòng trường hợp bạn dùng trigger
    {
        TryMerge(other);
    }

    void TryMerge(Collider2D other)
    {
        if (_isMerging || other == null) return;

        int myL    = gameObject.layer;
        int otherL = other.gameObject.layer;

        if (otherL == enemyLayer)
        {
            SetGrayOnEnemyHit();
            StartCoroutine(WaitforReplay());

            return;
        }

        // Chỉ xử lý nếu là cặp (Blue, Red)
        bool isPair = (myL == blueLayer && otherL == redLayer) ||
                      (myL == redLayer  && otherL == blueLayer);

        if (!isPair) return;

        // Tránh cả hai vật đều chạy coroutine: chỉ cho object có InstanceID nhỏ hơn chủ trì merge
        if (gameObject.GetInstanceID() > other.gameObject.GetInstanceID()) return;

        // Tránh race-condition nếu có nhiều va chạm đồng thời
        int keyA = gameObject.GetInstanceID();
        int keyB = other.gameObject.GetInstanceID();
        if (_busy.Contains(keyA) || _busy.Contains(keyB)) return;
        _busy.Add(keyA); _busy.Add(keyB);

        StartCoroutine(MergeRoutine(other.gameObject));
    }

    IEnumerator MergeRoutine(GameObject other)
    {
        _isMerging = true;

        // Lấy component bên kia
        var otherRB     = other.GetComponent<Rigidbody2D>();
        var otherCol    = other.GetComponent<Collider2D>();
        var otherScript = other.GetComponent<MergeBall>();
        if (otherRB == null || otherCol == null || otherScript == null)
        {
            CleanupBusy(other);
            yield break;
        }

        // Khoá vật lý: dừng lực, không phát sinh thêm va chạm
        FreezeBody(rb, col);
        FreezeBody(otherRB, otherCol);

        Vector3 startA = transform.position;
        Vector3 startB = other.transform.position;
        Vector3 mid    = (startA + startB) * 0.5f;

        float t = 0f;
        while (t < mergeDuration)
        {
            t += Time.deltaTime;
            float k = ease.Evaluate(Mathf.Clamp01(t / mergeDuration));
            transform.position = Vector3.Lerp(startA, mid, k);
            other.transform.position = Vector3.Lerp(startB, mid, k);
            yield return null;
        }

        // Spawn VFX tại điểm giữa
        if (vfxPrefab)
            Instantiate(vfxPrefab, mid, vfxPrefab.transform.rotation);

        // Hủy cả hai
        Destroy(other);
        Destroy(gameObject);

        // tháo cờ bận
        CleanupBusy(other);
    }

    void FreezeBody(Rigidbody2D body, Collider2D collider2D)
    {
        if (body)
        {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.isKinematic = true;                // tránh lực tác động trong lúc hút
            body.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        if (collider2D) collider2D.enabled = false; // tránh thêm va chạm
    }

    void CleanupBusy(GameObject other)
    {
        _busy.Remove(gameObject.GetInstanceID());
        if (other) _busy.Remove(other.GetInstanceID());
    }

    // ---------- Spine Color ----------

    void SetGrayOnEnemyHit()
    {
        if (colorChangedByEnemy) return;   // chỉ đổi 1 lần
        colorChangedByEnemy = true;

        if (skeletonAnim != null && skeletonAnim.Skeleton != null)
        {
            skeletonAnim.Skeleton.SetColor(enemyHitColor);
            // Nếu bạn dùng shader hỗ trợ Tint/Color, toàn bộ Spine sẽ chuyển xám
        }
        else
        {
            // fallback: nếu vì lý do gì đó không có Spine, có thể đổi màu SpriteRenderer
            var sr = GetComponent<SpriteRenderer>();
            if (sr) sr.color = enemyHitColor;
        }
    }

    IEnumerator WaitforReplay()
    {
        yield return new WaitForSeconds(2f);

        GameManager.Instance.RePlay();
    }
}
