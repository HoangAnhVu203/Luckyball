using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Wood : MonoBehaviour
{
    [Header("Support Layers")]
    public LayerMask supportLayers;          // chọn layer Sand / Wall đỡ thanh gỗ

    [Header("Check Box Settings")]
    [Tooltip("Chiều cao vùng check dưới chân (world units)")]
    public float checkHeight = 0.05f;
    [Tooltip("Thu hẹp bớt bề rộng so với collider (0 = full, 0.2 = bớt 20%)")]
    [Range(0f, 0.5f)] public float widthShrink = 0.1f;
    [Tooltip("Khoảng cách đẩy box check xuống dưới đáy collider")]
    public float extraOffsetY = 0.01f;

    Rigidbody2D rb;
    Collider2D col;

    bool hasFallen = false;

    void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // ban đầu đứng im, tránh bị giật khi cát thay collider
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = false;
    }

    void Update()
    {
        if (hasFallen) return;

        // lấy bounds hiện tại của collider
        Bounds b = col.bounds;

        // kích thước hộp check
        float width  = b.size.x * (1f - widthShrink);
        float height = checkHeight;

        Vector2 size = new Vector2(width, height);

        // tâm của hộp check: ngay dưới đáy collider
        Vector2 center = new Vector2(
            b.center.x,
            b.min.y - height * 0.5f - extraOffsetY
        );

        // kiểm tra xem còn gì đỡ ở dưới không
        bool supported = Physics2D.OverlapBox(center, size, 0f, supportLayers);

        if (!supported)
        {
            // không còn gì đỡ → cho rơi
            rb.bodyType = RigidbodyType2D.Dynamic;
            hasFallen = true;
        }
        else
        {
            // còn cát/tường đỡ → giữ Kinematic
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!col) col = GetComponent<Collider2D>();

        Bounds b = col.bounds;
        float width  = b.size.x * (1f - widthShrink);
        float height = checkHeight;
        Vector2 size = new Vector2(width, height);
        Vector2 center = new Vector2(
            b.center.x,
            b.min.y - height * 0.5f - extraOffsetY
        );

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }
}
