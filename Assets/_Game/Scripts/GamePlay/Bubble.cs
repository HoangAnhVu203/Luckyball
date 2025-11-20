using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bubble : MonoBehaviour
{
    [Header("Float Settings")]
    public float floatAmplitude = 0.2f;
    public float floatFrequency = 2f;

    [Header("Capture Layers")]
    public string blueLayerName = "BlueBall";
    public string redLayerName  = "RedBall";
    public string bombLayerName = "Bomb";

    [Header("Pop VFX")]
    [Tooltip("Prefab hiệu ứng nổ bong bóng")]
    public GameObject popVfxPrefab;
    [Tooltip("Thời gian tự huỷ VFX (giây)")]
    public float popVfxLifeTime = 1.5f;
    public GameObject heartVFX;
    

    [Header("Merge When Carry Ball")]
    [Tooltip("Thời gian hút 2 quả lại với nhau khi Bubble đang chứa 1 quả")]
    public float mergeDuration = 0.25f;
    public AnimationCurve mergeEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Rigidbody2D rb;
    private Transform capturedBall;
    private bool isCaptured = false;
    private float startTime;

    // cache layer int
    int blueLayer;
    int redLayer;
    int bombLayer;
    int trapLayer;

    bool isMerging = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;

        if (rb.gravityScale >= 0)
            rb.gravityScale = -1.5f;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        startTime = Time.time;

        blueLayer = LayerMask.NameToLayer(blueLayerName);
        redLayer  = LayerMask.NameToLayer(redLayerName);
        bombLayer = LayerMask.NameToLayer(bombLayerName);
        trapLayer = LayerMask.NameToLayer("Trap");
    }

    void Update()
    {
        if (floatAmplitude > 0f && !isMerging) // đang merge thì thôi không lắc nữa
        {
            Vector2 pos = rb.position;
            pos.x += Mathf.Sin((Time.time - startTime) * floatFrequency)
                     * floatAmplitude * Time.deltaTime;
            rb.MovePosition(pos);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        int otherLayer = collision.gameObject.layer;

        // 1) BUBBLE ĐANG CHỨA BALL + VA CHẠM BALL CÒN LẠI -> MERGE
        if (isCaptured && !isMerging && capturedBall != null)
        {
            int capLayer = capturedBall.gameObject.layer;

            bool isRedInside_BlueOutside =
                (capLayer == redLayer && otherLayer == blueLayer);

            bool isBlueInside_RedOutside =
                (capLayer == blueLayer && otherLayer == redLayer);

            if (isRedInside_BlueOutside || isBlueInside_RedOutside)
            {
                StartCoroutine(MergeInsideAndOutside(
                    capturedBall.gameObject,
                    collision.gameObject
                ));
                return; // không xử lý gì thêm va chạm này nữa
            }
        }

        // 2) BUBBLE CHƯA CHỨA GÌ -> BẮT BALL / BOMB
        if (!isCaptured)
        {
            if (otherLayer == blueLayer ||
                otherLayer == redLayer ||
                otherLayer == bombLayer)
            {
                CaptureBall(collision.gameObject);
            }
        }

        // 3) ĐỤNG TRAP -> NỔ
        if (otherLayer == trapLayer)
        {
            Pop();
        }
    }

    void CaptureBall(GameObject ball)
    {
        if (isCaptured) return;
        isCaptured = true;

        var rbBall  = ball.GetComponent<Rigidbody2D>();
        var colBall = ball.GetComponent<Collider2D>();

        if (rbBall)
        {
            rbBall.velocity = Vector2.zero;
            rbBall.angularVelocity = 0f;
            rbBall.gravityScale = 0f;
            rbBall.isKinematic = true;
        }
        if (colBall) colBall.enabled = false;

        // Đưa ball vào giữa Bubble (theo transform)
        ball.transform.SetParent(transform);
        ball.transform.localPosition = Vector3.zero;
        capturedBall = ball.transform;
    }

    // ========= MERGE BALL TRONG & NGOÀI =========
    IEnumerator MergeInsideAndOutside(GameObject innerBall, GameObject outerBall)
    {
        isMerging = true;

        // tắt MergeBall để nó không tự xử lý nữa (nếu có)
        var mb1 = innerBall.GetComponent<MergeBall>();
        var mb2 = outerBall.GetComponent<MergeBall>();
        if (mb1) mb1.enabled = false;
        if (mb2) mb2.enabled = false;

        // khoá vật lý 2 quả cho khỏi bay lung tung
        FreezeBall(innerBall);
        FreezeBall(outerBall);

        // điểm bắt đầu & điểm giữa
        Vector3 startA = innerBall.transform.position;
        Vector3 startB = outerBall.transform.position;
        Vector3 mid    = (startA + startB) * 0.5f;

        float t = 0f;
        while (t < mergeDuration)
        {
            t += Time.deltaTime;
            float k = mergeEase.Evaluate(Mathf.Clamp01(t / mergeDuration));

            innerBall.transform.position = Vector3.Lerp(startA, mid, k);
            outerBall.transform.position = Vector3.Lerp(startB, mid, k);

            yield return null;
        }

        // VFX merge / pop
        if (popVfxPrefab)
        {
            var fx = Instantiate(popVfxPrefab, mid, Quaternion.identity);
            Destroy(fx, popVfxLifeTime);
        }

        // Huỷ 2 quả & bong bóng
        Destroy(innerBall);
        Destroy(outerBall);
        if (heartVFX)
            Instantiate(heartVFX, mid, heartVFX.transform.rotation);
        

        capturedBall = null;
        Destroy(gameObject); // huỷ luôn Bubble
    }

    void FreezeBall(GameObject ball)
    {
        var r = ball.GetComponent<Rigidbody2D>();
        var c = ball.GetComponent<Collider2D>();

        if (r)
        {
            r.velocity = Vector2.zero;
            r.angularVelocity = 0f;
            r.gravityScale = 0f;
            r.isKinematic = true;
            r.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        if (c) c.enabled = false;
    }

    public void Pop()
    {
        // nếu đang merge rồi thì không Pop nữa
        if (isMerging)
            return;

        if (capturedBall)
        {
            Bomb bomb = capturedBall.GetComponent<Bomb>();
            if (bomb != null)
            {
                capturedBall.SetParent(null);
                bomb.TriggerImmediateExplosion();
            }
            else
            {
                var rbBall  = capturedBall.GetComponent<Rigidbody2D>();
                var colBall = capturedBall.GetComponent<Collider2D>();

                if (colBall) colBall.enabled = true;
                if (rbBall)
                {
                    rbBall.isKinematic = false;
                    rbBall.gravityScale = 1f;
                }
                capturedBall.SetParent(null);
            }

            capturedBall = null;
        }

        if (popVfxPrefab != null)
        {
            var fx = Instantiate(popVfxPrefab, transform.position, Quaternion.identity);
            Destroy(fx, popVfxLifeTime);
        }

        Destroy(gameObject);
    }
}
