using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class HandAutoEraser : MonoBehaviour
{
    [Header("Path settings")]
    public Transform[] pathPoints;      // P0, P1, P2, ...
    public float moveSpeed = 3f;

    [Header("Erase Settings")]
    public float eraseInterval = 0.02f;   // thời gian giữa 2 lần xoá

    private EraseDemo erase;

    void Start()
    {
        erase = EraseDemo.Instance;

        if (pathPoints == null || pathPoints.Length < 2)
        {
            Debug.LogWarning("[HandAutoEraser] Không có đủ pathPoints.");
            enabled = false;
            return;
        }

        // đặt tay tại điểm đầu tiên
        transform.position = pathPoints[0].position;

        StartCoroutine(AutoPlayRoutine());
    }

    IEnumerator AutoPlayRoutine()
    {
        float eraseTimer = 0f;

        // Đi lần lượt qua tất cả segment P0->P1, P1->P2, ...
        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            Vector3 start = pathPoints[i].position;
            Vector3 end   = pathPoints[i + 1].position;

            float dist = Vector3.Distance(start, end);
            float t = 0f;

            // di chuyển từ start → end
            while (t < 1f)
            {
                t += Time.deltaTime * (moveSpeed / Mathf.Max(0.01f, dist));
                Vector3 pos = Vector3.Lerp(start, end, t);
                transform.position = pos;

                // xoá cát theo timer
                eraseTimer += Time.deltaTime;
                if (erase != null && eraseTimer >= eraseInterval)
                {
                    eraseTimer = 0f;
                    erase.EraseAtWorld(pos);
                }

                yield return null;
            }

            // đảm bảo cuối đoạn cũng xoá 1 lần
            if (erase != null)
                erase.EraseAtWorld(transform.position);
        }

        // ==== ĐÃ TỚI ĐIỂM CUỐI ====
        if (erase != null)
            erase.RebuildCollider();          // rebuild collider 1 lần

        
        // chờ 3s rồi replay lại level demo hiện tại
        yield return new WaitForSeconds(3f);

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.Replay();
        }
        yield break;
    }
}
