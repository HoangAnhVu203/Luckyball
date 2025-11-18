using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class Erase : MonoBehaviour
{
    [Header("Brush Settings")]
    [SerializeField] int brushRadius = 40;

    [Header("Debug")]
    [SerializeField] bool showBrushGizmo = true;

    SpriteRenderer sr;
    Texture2D runtimeTex;
    Sprite originalSprite;

    Rect spriteRectPx;
    Vector2 spritePivotPx;
    float ppu;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        InitRuntimeTexture();
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector2 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (PaintEraseAtWorld(world))
            {
                // Xoá collider cũ và tạo mới collider mới
                var oldCollider = GetComponent<PolygonCollider2D>();
                if (oldCollider != null)
                    Destroy(oldCollider);

                var newCollider = gameObject.AddComponent<PolygonCollider2D>();
                newCollider.isTrigger = false;
            }
        }
    }

    // Khởi tạo texture runtime có thể thay đổi alpha
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
            enabled = false; 
            return;
        }

        spriteRectPx = originalSprite.rect;
        spritePivotPx = originalSprite.pivot;
        ppu = originalSprite.pixelsPerUnit;

        int w = (int)spriteRectPx.width;
        int h = (int)spriteRectPx.height;

        runtimeTex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
        Color[] block = src.GetPixels((int)spriteRectPx.x, (int)spriteRectPx.y, w, h);
        runtimeTex.SetPixels(block);
        runtimeTex.Apply();

        Sprite newSprite = Sprite.Create(runtimeTex, new Rect(0, 0, w, h),
            spritePivotPx / new Vector2(w, h), ppu, 0, SpriteMeshType.Tight);
        sr.sprite = newSprite;
    }

    // Xoá alpha tại vị trí chuột
    bool PaintEraseAtWorld(Vector2 worldPos)
    {
        if (!runtimeTex) return false;

        Vector2 local = sr.transform.InverseTransformPoint(worldPos);
        Vector2 px = local * ppu + spritePivotPx;

        int cx = Mathf.RoundToInt(px.x);
        int cy = Mathf.RoundToInt(px.y);
        int w = runtimeTex.width, h = runtimeTex.height;
        int r = brushRadius, r2 = r * r;
        bool changed = false;

        int xMin = Mathf.Clamp(cx - r, 0, w - 1);
        int xMax = Mathf.Clamp(cx + r, 0, w - 1);
        int yMin = Mathf.Clamp(cy - r, 0, h - 1);
        int yMax = Mathf.Clamp(cy + r, 0, h - 1);

        Color[] pixels = runtimeTex.GetPixels(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
        int bw = xMax - xMin + 1, bh = yMax - yMin + 1;

        for (int j = 0; j < bh; j++)
        {
            for (int i = 0; i < bw; i++)
            {
                int idx = j * bw + i;
                int dx = (xMin + i) - cx;
                int dy = (yMin + j) - cy;
                if (dx * dx + dy * dy <= r2)
                {
                    var c = pixels[idx];
                    if (c.a > 0f)
                    {
                        c.a = 0f;
                        pixels[idx] = c;
                        changed = true;
                    }
                }
            }
        }

        if (changed)
        {
            runtimeTex.SetPixels(xMin, yMin, bw, bh, pixels);
            runtimeTex.Apply(false);
        }
        return changed;
    }

    // Hiển thị bán kính cọ trong Scene View
    void OnDrawGizmosSelected()
    {
        if (!showBrushGizmo || !Camera.main) return;
        if (!sr) sr = GetComponent<SpriteRenderer>();
        Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0;
        Gizmos.color = Color.yellow;
        float r = (sr && sr.sprite) ? brushRadius / sr.sprite.pixelsPerUnit : brushRadius * 0.01f;
        Gizmos.DrawWireSphere(world, r);
    }
}
