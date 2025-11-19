using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("Jump Settings")]
    public float jumpForce = 1f;           // lực nhảy lên
    public float jumpIntervalMin = 2f;     // thời gian chờ tối thiểu giữa mỗi lần nhảy
    public float jumpIntervalMax = 2.1f;     // thời gian chờ tối đa

    [Header("Physics")]
    public float gravityScale = 2f;        // trọng lực rơi
    public bool faceRight = true;          // hướng quay mặt (nếu có animation)
    
    Rigidbody2D rb;
    float nextJumpTime = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;
    }

    void Start()
    {
        ScheduleNextJump();
    }

    void Update()
    {
        if (Time.time >= nextJumpTime)
        {
            DoJump();
            ScheduleNextJump();
        }
    }

    void DoJump()
    {
        // reset vận tốc dọc để nhảy ổn định
        rb.velocity = new Vector2(rb.velocity.x, 0f);

        // nhảy lên
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    void ScheduleNextJump()
    {
        nextJumpTime = Time.time + Random.Range(jumpIntervalMin, jumpIntervalMax);
    }
}
