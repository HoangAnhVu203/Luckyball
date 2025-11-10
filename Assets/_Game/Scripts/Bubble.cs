using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bubble : MonoBehaviour
{
    [Header("Float Settings")]
    public float floatAmplitude = 0.2f;
    public float floatFrequency = 2f;

    [Header("Capture Layers")]
    public string blueLayerName = "BlueBall";
    public string redLayerName = "RedBall";
    public string bombLayerName = "Bomb";

    [Header("Pop VFX")]
    [Tooltip("Prefab hiệu ứng nổ bong bóng")]
    public GameObject popVfxPrefab;
    [Tooltip("Thời gian tự huỷ VFX (giây)")]
    public float popVfxLifeTime = 1.5f;

    private Rigidbody2D rb;
    private Transform capturedBall;
    private bool isCaptured = false;
    private float startTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;

        if (rb.gravityScale >= 0)
            rb.gravityScale = -1.5f;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        startTime = Time.time;
    }

    void Update()
    {
        if (floatAmplitude > 0f)
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

        if (otherLayer == LayerMask.NameToLayer(blueLayerName) ||
            otherLayer == LayerMask.NameToLayer(redLayerName) ||
            otherLayer == LayerMask.NameToLayer(bombLayerName))
        {
            CaptureBall(collision.gameObject);
        }

        if (otherLayer == LayerMask.NameToLayer("Trap"))
        {
            Pop();
        }
    }

    void CaptureBall(GameObject ball)
    {
        if (isCaptured) return;
        isCaptured = true;

        var rbBall = ball.GetComponent<Rigidbody2D>();
        var colBall = ball.GetComponent<Collider2D>();

        if (rbBall)
        {
            rbBall.velocity = Vector2.zero;
            rbBall.angularVelocity = 0f;
            rbBall.gravityScale = 0f;
            rbBall.isKinematic = true;
        }
        if (colBall) colBall.enabled = false;

        ball.transform.SetParent(transform);
        ball.transform.localPosition = Vector3.zero;
        capturedBall = ball.transform;
    }

    public void Pop()
    {
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
                var rbBall = capturedBall.GetComponent<Rigidbody2D>();
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
