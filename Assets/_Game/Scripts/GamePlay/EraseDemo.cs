using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class EraseDemo : MonoBehaviour
{
    public static EraseDemo Instance { get; private set; }

    [Header("Brush Settings")]
    [SerializeField] int brushRadius = 40;

    [Header("Debug")]
    [SerializeField] bool showBrushGizmo = true;

    SpriteRenderer sr;
    Texture2D runtimeTex;
    Sprite originalSprite;
    PolygonCollider2D poly;

    Rect spriteRectPx;
    Vector2 spritePivotPx;
    float ppu;

    // buffer giống Erase
    Color32[] pixelsAll;
    int texW, texH;

    void Awake()
    {
        Instance = this;

        sr   = GetComponent<SpriteRenderer>();
        poly = GetComponent<PolygonCollider2D>();
        if (!poly) poly = gameObject.AddComponent<PolygonCollider2D>();

        InitRuntimeTexture();
        RebuildCollider();          // collider ban đầu
    }

    /// <summary>
    /// Hàm được HandAutoErase gọi liên tục.
    /// Chỉ xoá pixel + Apply, KHÔNG rebuild collider.
    /// </summary>
    public void EraseAtWorld(Vector2 worldPos)
    {
        if (runtimeTex == null || pixelsAll == null) return;

        if (PaintEraseAtWorld(worldPos))
        {
            runtimeTex.SetPixels32(pixelsAll);
            runtimeTex.Apply(false);
        }
    }

    /// <summary>
    /// Gọi hàm này THỈNH THOẢNG (ví dụ cuối đường tay)
    /// để Unity sinh lại collider một lần.
    /// </summary>
    public void RebuildCollider()
    {
        if (!poly) poly = GetComponent<PolygonCollider2D>();
        if (!poly) poly = gameObject.AddComponent<PolygonCollider2D>();

        Destroy(poly);
        poly = gameObject.AddComponent<PolygonCollider2D>();
        poly.isTrigger = false;
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
            Debug.LogError("[EraseDemo] Texture phải bật Read/Write.");
            enabled = false;
            return;
        }

        spriteRectPx  = originalSprite.rect;
        spritePivotPx = originalSprite.pivot;
        ppu           = originalSprite.pixelsPerUnit;

        texW = (int)spriteRectPx.width;
        texH = (int)spriteRectPx.height;

        runtimeTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false, false);

        // giống Erase: copy bằng GetPixels32
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
            SpriteMeshType.Tight
        );
        sr.sprite = newSprite;
    }

    // ==========================
    //   XOÁ PIXEL THEO WORLD POS
    // ==========================
    bool PaintEraseAtWorld(Vector2 worldPos)
    {
        Vector2 local = sr.transform.InverseTransformPoint(worldPos);
        Vector2 px    = local * ppu + spritePivotPx;

        int cx = Mathf.RoundToInt(px.x);
        int cy = Mathf.RoundToInt(px.y);

        int r  = brushRadius;
        int r2 = r * r;
        bool changed = false;

        int xMin = Mathf.Clamp(cx - r, 0, texW - 1);
        int xMax = Mathf.Clamp(cx + r, 0, texW - 1);
        int yMin = Mathf.Clamp(cy - r, 0, texH - 1);
        int yMax = Mathf.Clamp(cy + r, 0, texH - 1);

        for (int y = yMin; y <= yMax; y++)
        {
            int dy  = y - cy;
            int dy2 = dy * dy;
            int row = y * texW;

            for (int x = xMin; x <= xMax; x++)
            {
                int dx = x - cx;
                if (dx * dx + dy2 > r2) continue;

                int idx = row + x;
                var c   = pixelsAll[idx];
                if (c.a > 0)
                {
                    c.a       = 0;
                    pixelsAll[idx] = c;
                    changed   = true;
                }
            }
        }

        return changed;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!showBrushGizmo || !Camera.main) return;
        if (!sr) sr = GetComponent<SpriteRenderer>();

        Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        world.z = sr ? sr.transform.position.z : 0f;

        Gizmos.color = Color.cyan;
        float r = (sr && sr.sprite)
            ? brushRadius / sr.sprite.pixelsPerUnit
            : brushRadius * 0.01f;
        Gizmos.DrawWireSphere(world, r);
    }
#endif
}
