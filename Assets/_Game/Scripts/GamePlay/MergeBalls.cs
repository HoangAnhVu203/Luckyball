using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;   // Spine runtime

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class MergeBalls : MonoBehaviour
{
    [Header("Layers (by name)")]
    [SerializeField] string blueLayerName  = "BlueBall";
    [SerializeField] string redLayerName   = "RedBall";
    [SerializeField] string enemyLayerName = "Enemy";

    [Header("Merge Settings")]
    [SerializeField, Range(0.05f, 1.5f)]
    float mergeDuration = 0.25f;                     // thời gian hút
    [SerializeField]
    AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Overlap Settings")]
    [Tooltip("Bán kính check quanh tâm ball để tìm ball/Enemy khác")]
    public float checkRadius = 0.45f;
    [Tooltip("Thời gian giữa 2 lần check (giảm để nhẹ CPU)")]
    public float checkInterval = 0.02f;              // ~50 lần/giây

    [Header("VFX")]
    public GameObject vfxPrefab;                     // VFX khi merge xong

    [Header("Spine Color When Hit Enemy")]
    public Color enemyHitColor = Color.gray;         // màu khi chạm Enemy

    // ===== INTERNAL STATE =====
    bool _isMerging = false;
    bool _colorChangedByEnemy = false;

    static readonly HashSet<int> _busy = new();      // tránh đôi chạy 2 lần
    static readonly Collider2D[] _overlapBuffer = new Collider2D[8]; // buffer dùng chung

    int blueLayer, redLayer, enemyLayer;
    int ballMask;   // mask chỉ chứa Blue + Red
    int enemyMask;  // mask Enemy
    int allMask;    // ball + enemy

    Rigidbody2D rb;           // optional
    Collider2D col;

    // Spine
    SkeletonAnimation skeletonAnim;
    Color originalColor;

    float _nextCheckTime = 0f;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        rb  = GetComponent<Rigidbody2D>();           // có cũng được, không có cũng không sao

        blueLayer  = LayerMask.NameToLayer(blueLayerName);
        redLayer   = LayerMask.NameToLayer(redLayerName);
        enemyLayer = LayerMask.NameToLayer(enemyLayerName);

        if (blueLayer == -1 || redLayer == -1)
            Debug.LogWarning("[MergeBall] Chưa khai báo layer BlueBall / RedBall.");
        if (enemyLayer == -1)
            Debug.LogWarning("[MergeBall] Chưa khai báo layer Enemy.");

        ballMask  = (1 << blueLayer) | (1 << redLayer);
        enemyMask = (1 << enemyLayer);
        allMask   = ballMask | enemyMask;

        // Lấy Spine
        skeletonAnim = GetComponent<SkeletonAnimation>();
        if (skeletonAnim != null && skeletonAnim.Skeleton != null)
            originalColor = skeletonAnim.Skeleton.GetColor();
    }

    void FixedUpdate()
    {
        if (!enabled || !gameObject.activeInHierarchy) return;
        if (_isMerging || _colorChangedByEnemy) return;       // đang merge hoặc đã trúng enemy thì khỏi check
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + checkInterval;

        CheckOverlapAndHandle();
    }

    // =====================================
    //        OVERLAP & HANDLE LOGIC
    // =====================================

    void CheckOverlapAndHandle()
    {
        Vector2 center = transform.position;

        int count = Physics2D.OverlapCircleNonAlloc(
            center,
            checkRadius,
            _overlapBuffer,
            allMask);

        int myLayer = gameObject.layer;

        for (int i = 0; i < count; i++)
        {
            var other = _overlapBuffer[i];
            if (!other || other.gameObject == gameObject) continue;

            int otherLayer = other.gameObject.layer;

            // 1) Ưu tiên xử lý Enemy trước
            if (otherLayer == enemyLayer)
            {
                HitEnemyOnce();
                return;
            }

            // 2) Cặp Blue + Red mới merge
            bool isPair =
                (myLayer == blueLayer && otherLayer == redLayer) ||
                (myLayer == redLayer  && otherLayer == blueLayer);

            if (!isPair) continue;

            TryStartMerge(other);
            return;        // merge 1 cặp là đủ, không xử lý thêm
        }
    }

    void HitEnemyOnce()
    {
        if (_colorChangedByEnemy) return;
        _colorChangedByEnemy = true;

        // Đổi màu spine
        if (skeletonAnim != null && skeletonAnim.Skeleton != null)
        {
            skeletonAnim.Skeleton.SetColor(enemyHitColor);
        }
        else
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr) sr.color = enemyHitColor;
        }

        // cho 1 chút delay rồi replay
        StartCoroutine(WaitForReplay());
    }

    void TryStartMerge(Collider2D other)
    {
        if (_isMerging || other == null) return;

        int idA = gameObject.GetInstanceID();
        int idB = other.gameObject.GetInstanceID();

        // chỉ để object có id nhỏ hơn khởi xướng merge
        if (idA > idB) return;

        if (_busy.Contains(idA) || _busy.Contains(idB)) return;
        _busy.Add(idA);
        _busy.Add(idB);

        StartCoroutine(MergeRoutine(other.gameObject));
    }

    IEnumerator MergeRoutine(GameObject other)
    {
        _isMerging = true;

        var otherCol = other.GetComponent<Collider2D>();
        var otherScript = other.GetComponent<MergeBalls>();

        if (otherScript == null || otherCol == null)
        {
            CleanupBusy(other);
            yield break;
        }

        // Optional freeze rigidbody nếu có
        FreezeBody(rb, col);
        FreezeBody(other.GetComponent<Rigidbody2D>(), otherCol);

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

        if (vfxPrefab)
            Instantiate(vfxPrefab, mid, vfxPrefab.transform.rotation);

        Destroy(other);
        Destroy(gameObject);

        CleanupBusy(other);
    }

    void FreezeBody(Rigidbody2D body, Collider2D coll)
    {
        if (!body) return;

        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.isKinematic = true;
        body.constraints = RigidbodyConstraints2D.FreezeAll;

        if (coll) coll.enabled = false;
    }

    void CleanupBusy(GameObject other)
    {
        _busy.Remove(gameObject.GetInstanceID());
        if (other) _busy.Remove(other.GetInstanceID());
    }

    IEnumerator WaitForReplay()
    {
        yield return new WaitForSeconds(2f);
        GameManager.Instance.RePlay();
    }

#if UNITY_EDITOR
    // Gizmo để bạn thấy vùng Overlap trong Scene View
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
#endif
}
