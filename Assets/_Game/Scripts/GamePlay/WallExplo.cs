using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap), typeof(TilemapCollider2D))]
public class WallExplo : MonoBehaviour
{
    [Header("Explosion Effect")]
    [Tooltip("Bán kính phá tile (world units)")]
    public float maxDistanceEffect = 4f;

    [Header("VFX cho từng tile bị phá")]
    public GameObject breakVFX;
    public float vfxLifeTime = 2f;

    Tilemap tilemap;

    void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    /// <summary>
    /// Hàm được Bomb gọi khi nổ
    /// </summary>
    public void OnExploded(Vector2 explosionCenter)
    {
        if (!tilemap) return;

        // ---- Tính vùng quét (hình vuông bao xung quanh vụ nổ) ----
        Vector2 worldMin = explosionCenter + new Vector2(-maxDistanceEffect, -maxDistanceEffect);
        Vector2 worldMax = explosionCenter + new Vector2( maxDistanceEffect,  maxDistanceEffect);

        Vector3Int cellMin = tilemap.WorldToCell(worldMin);
        Vector3Int cellMax = tilemap.WorldToCell(worldMax);

        // ---- Quét từng tile ----
        for (int x = cellMin.x; x <= cellMax.x; x++)
        {
            for (int y = cellMin.y; y <= cellMax.y; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);

                // Nếu ô không có tile → bỏ qua
                if (!tilemap.HasTile(cellPos)) continue;

                // Tính khoảng cách từ tile đến bom
                Vector3 cellWorldPos = tilemap.GetCellCenterWorld(cellPos);
                float dist = Vector2.Distance(explosionCenter, cellWorldPos);

                // Tile nằm trong vùng nổ
                if (dist <= maxDistanceEffect)
                {
                    // Spawn vfx tại đúng tâm tile
                    if (breakVFX)
                    {
                        var fx = Object.Instantiate(breakVFX, cellWorldPos, Quaternion.identity);
                        Object.Destroy(fx, vfxLifeTime);
                    }

                    // Xóa tile
                    tilemap.SetTile(cellPos, null);
                }
            }
        }
    }
}
