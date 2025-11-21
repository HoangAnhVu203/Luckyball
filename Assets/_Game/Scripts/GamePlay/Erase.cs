using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class Erase : MonoBehaviour
{
    public static Erase Instance { get; private set; }

    [Header("Brush Settings")]
    [SerializeField] int brushRadius = 40;

    [Header("Debug")]
    [SerializeField] bool showBrushGizmo = true;

    SpriteRenderer sr;
    Texture2D runtimeTex;
    Sprite originalSprite;
    PolygonCollider2D poly;
    Camera cam;

    Rect spriteRectPx;
    Vector2 spritePivotPx;
    float ppu;

    // buffer pixels để không GetPixels mỗi lần
    Color32[] pixelsAll;
    int texW, texH;

    // trạng thái 1 nét vẽ
    bool strokeActive = false;
    bool strokeChangedPixels = false;

    void Awake()
    {
        Instance = this;

        sr = GetComponent<SpriteRenderer>();
        poly = GetComponent<PolygonCollider2D>();
        cam = Camera.main;

        if (!poly) poly = gameObject.AddComponent<PolygonCollider2D>();

        InitRuntimeTexture();
        RebuildCollider();  // collider ban đầu
    }

    void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameState.Gameplay)
            return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // BẮT ĐẦU 1 NÉT XOÁ
        if (Input.GetMouseButtonDown(0))
        {
            strokeActive = true;
            strokeChangedPixels = false;
        }

        // ĐANG XOÁ – chỉ đổi texture, KHÔNG rebuild collider
        if (strokeActive && Input.GetMouseButton(0))
        {
            Vector2 world = cam.ScreenToWorldPoint(Input.mousePosition);
            if (PaintEraseAtWorld(world))
            {
                strokeChangedPixels = true;
                // apply ngay để người chơi thấy hình ảnh thay đổi mượt
                runtimeTex.SetPixels32(pixelsAll);
                runtimeTex.Apply(false);
            }
        }

        // KẾT THÚC NÉT XOÁ → rebuild collider 1 LẦN
        if (strokeActive && Input.GetMouseButtonUp(0))
        {
            strokeActive = false;

            if (strokeChangedPixels)
            {
                RebuildCollider();   // thao tác nặng nhưng chỉ 1 lần / nét
            }
        }
    }

    // ==========================
    //   KHỞI TẠO TEXTURE
    // ==========================
    void InitRuntimeTexture()
    {
        originalSprite = sr.sprite;
        if (!originalSprite)
        {
            enabled = false;
            return;
        }

        Texture2D src = originalSprite.texture;
        if (!src.isReadable)
        {
            Debug.LogError($"{name}: Texture chưa bật Read/Write Enabled.");
            enabled = false;
            return;
        }

        spriteRectPx = originalSprite.rect;
        spritePivotPx = originalSprite.pivot;
        ppu = originalSprite.pixelsPerUnit;

        texW = (int)spriteRectPx.width;
        texH = (int)spriteRectPx.height;

        runtimeTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false, false);

        // Lấy toàn bộ pixels 1 lần
        Color32[] srcPixels = src.GetPixels32();
        pixelsAll = new Color32[texW * texH];

        int srcW = src.width;
        int x0 = (int)spriteRectPx.x;
        int y0 = (int)spriteRectPx.y;

        for (int y = 0; y < texH; y++)
        {
            int srcY = y0 + y;
            int rowDst = y * texW;
            int rowSrc = srcY * srcW;
            for (int x = 0; x < texW; x++)
            {
                int srcX = x0 + x;
                pixelsAll[rowDst + x] = srcPixels[rowSrc + srcX];
            }
        }

        runtimeTex.SetPixels32(pixelsAll);
        runtimeTex.Apply(false);

        Sprite newSprite = Sprite.Create(
            runtimeTex,
            new Rect(0, 0, texW, texH),
            spritePivotPx / new Vector2(texW, texH),
            ppu,
            0,
            SpriteMeshType.Tight   // để Unity sinh collider sát hình hơn
        );
        sr.sprite = newSprite;
    }

    // ==========================
    //   XOÁ PIXEL THEO WORLD POS
    // ==========================
    bool PaintEraseAtWorld(Vector2 worldPos)
    {
        if (runtimeTex == null || pixelsAll == null) return false;

        Vector2 local = sr.transform.InverseTransformPoint(worldPos);
        Vector2 px = local * ppu + spritePivotPx;

        int cx = Mathf.RoundToInt(px.x);
        int cy = Mathf.RoundToInt(px.y);

        int r = brushRadius;
        int r2 = r * r;
        bool changed = false;

        int xMin = Mathf.Clamp(cx - r, 0, texW - 1);
        int xMax = Mathf.Clamp(cx + r, 0, texW - 1);
        int yMin = Mathf.Clamp(cy - r, 0, texH - 1);
        int yMax = Mathf.Clamp(cy + r, 0, texH - 1);

        for (int y = yMin; y <= yMax; y++)
        {
            int dy = y - cy;
            int dy2 = dy * dy;
            int row = y * texW;

            for (int x = xMin; x <= xMax; x++)
            {
                int dx = x - cx;
                if (dx * dx + dy2 > r2) continue;

                int idx = row + x;
                var c = pixelsAll[idx];
                if (c.a > 0)
                {
                    c.a = 0;
                    pixelsAll[idx] = c;
                    changed = true;
                }
            }
        }

        return changed;
    }

    // ==========================
    //   REBUILD COLLIDER
    // ==========================
    void RebuildCollider()
    {
        if (!poly) poly = GetComponent<PolygonCollider2D>();
        if (!poly) poly = gameObject.AddComponent<PolygonCollider2D>();

        // Huỷ rồi tạo lại để Unity tự sinh shape mới từ sprite/texture hiện tại
        Destroy(poly);
        poly = gameObject.AddComponent<PolygonCollider2D>();
        poly.isTrigger = false;
    }

    // ==========================
    //   GIZMO CỌ
    // ==========================
    void OnDrawGizmosSelected()
    {
        if (!showBrushGizmo) return;

        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (cam == null) cam = Camera.main;
        if (!cam) return;

        Vector3 mouse = Input.mousePosition;
        if (float.IsNaN(mouse.x) || float.IsNaN(mouse.y) ||
            float.IsInfinity(mouse.x) || float.IsInfinity(mouse.y))
            return;

        float z = Mathf.Abs((sr ? sr.transform.position.z : 0f) - cam.transform.position.z);
        if (z < cam.nearClipPlane) z = cam.nearClipPlane + 0.01f;
        if (z > cam.farClipPlane) z = cam.farClipPlane - 0.01f;
        mouse.z = z;

        Vector3 world = cam.ScreenToWorldPoint(mouse);
        world.z = sr ? sr.transform.position.z : 0f;

        Gizmos.color = Color.yellow;
        float r = (sr && sr.sprite)
            ? brushRadius / sr.sprite.pixelsPerUnit
            : brushRadius * 0.01f;

        Gizmos.DrawWireSphere(world, r);
    }
}
