using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Laser : MonoBehaviour
{
    [Header("Refs")]
    public ParticleSystem beamPS;          
    public Transform beamOrigin;           
    public Transform directionRef;         
    public GameObject explosionPrefab;   
    public GameObject smokePrefab;   
    public AudioSource lazerAudio;      

    [Header("Ray/Mask")]
    public LayerMask hitMask;              
    public float maxDistance = 20f;
    public float selfOffset = 0.08f;       

    [Header("Lifetime Length Mode (Mesh Shape)")]
    public bool useMainStartSpeed = true;
    public float particleSpeedOverride = 10f;
    public float minLifetime = 0.02f;      
    public float minLength  = 0.05f;       

    [Header("Keep PS at origin")]
    public bool lockPSToOrigin = true;     

    [Header("Explosion")]
    public bool reuseExplosion = true;
    public float destroyExplosionAfter = 2f;
    public bool alignExplosionToNormal = true;

    [Header("Debug")]
    public bool logHit = false;
    public bool drawRay = true;

    // cache
    GameObject explosionInstance;
    ParticleSystem.MainModule  main;
    ParticleSystem.ShapeModule shape;
    int layerBlue, layerRed, layerRock, layerEnemy;

    void OnValidate()
    {
        if (!beamPS) beamPS = GetComponentInChildren<ParticleSystem>(true);
        if (!beamOrigin && beamPS) beamOrigin = beamPS.transform;
    }

    void Awake()
    {
        lazerAudio.mute = false;
        if (!beamPS) beamPS = GetComponentInChildren<ParticleSystem>(true);
        if (!beamOrigin && beamPS) beamOrigin = beamPS.transform;
        if (beamPS) { main = beamPS.main; shape = beamPS.shape; }

        layerBlue  = LayerMask.NameToLayer("BlueBall");
        layerRed   = LayerMask.NameToLayer("RedBall");
        layerRock  = LayerMask.NameToLayer("Rock");
        layerEnemy = LayerMask.NameToLayer("Enemy");
    }

    void Update()
    {
        if (!beamPS || !beamOrigin) return;

        if (lockPSToOrigin)
        {
            beamPS.transform.position = beamOrigin.position;
            beamPS.transform.rotation = beamOrigin.rotation;
        }

        // hướng bắn ổn định
        Vector2 dir = (directionRef && (directionRef.position - beamOrigin.position).sqrMagnitude > 1e-8f)
                        ? (directionRef.position - beamOrigin.position).normalized
                        : ((Vector2)beamOrigin.right).normalized;

        Vector2 origin = (Vector2)beamOrigin.position + dir * selfOffset;

        int finalMask = hitMask & ~(1 << gameObject.layer);
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxDistance, finalMask);

        float length = hit ? hit.distance : maxDistance;
        if (drawRay) Debug.DrawRay(origin, dir * length, hit ? Color.green : Color.cyan);

        // ---- CHIỀU DÀI = RAYCAST (bằng Start Lifetime) ----
        ApplyLifetimeFromLength(length);

        // Xử lý nổ & đối tượng va chạm
        HandleHit(hit);
    }

    void ApplyLifetimeFromLength(float length)
    {
        length = Mathf.Max(minLength, length);

        float speed = GetEffectiveSpeed();                   
        float neededLifetime = Mathf.Max(minLifetime, length / speed);

        main.startLifetime = new ParticleSystem.MinMaxCurve(neededLifetime);

        var p = shape.position;
        p.x = 0f; p.y = 0f; p.z = 0f;
        shape.position = p;
    }

    float GetEffectiveSpeed()
    {
        if (useMainStartSpeed)
        {
            switch (main.startSpeed.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return Mathf.Max(0.001f, main.startSpeed.constant);
                case ParticleSystemCurveMode.TwoConstants:
                    return Mathf.Max(0.001f, (main.startSpeed.constantMin + main.startSpeed.constantMax) * 0.5f);
            }
        }
        return Mathf.Max(0.001f, particleSpeedOverride);
    }

    void HandleHit(RaycastHit2D hit)
    {
        if (!hit)
        {
            if (reuseExplosion && explosionInstance)
            {
                var ps = explosionInstance.GetComponent<ParticleSystem>();
                if (ps) ps.Stop();
            }
            return;
        }

        int hitLayer = hit.collider.gameObject.layer;
        Vector3 hitPos = hit.point;
        Quaternion rot = Quaternion.identity;

        if (alignExplosionToNormal)
        {
            float ang = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg - 90f;
            rot = Quaternion.Euler(0, 0, ang);
        }

        // --- Va chạm với Ball hoặc Enemy ---
        if (hitLayer == layerBlue || hitLayer == layerRed || hitLayer == layerEnemy)
        {
            var go = hit.collider.gameObject;
            go.SetActive(false);

            // Hiệu ứng nổ (nếu có)
            SpawnExplosion(hitPos, rot);

            if(hitLayer == layerBlue || hitLayer == layerRed)
            {
                AudioManager.Instance?.PlayLose();
                StartCoroutine(WaitReplay());
            }
            if (smokePrefab)
            {
                var smoke = Instantiate(smokePrefab, hitPos, Quaternion.identity);
                Destroy(smoke, 2f); // tự huỷ sau 2s
            }

            if (logHit) Debug.Log($"[Laser] Deactivated {LayerMask.LayerToName(hitLayer)} at {hitPos}");
            return;
        }

        // --- Va chạm với Rock ---
        if (hitLayer == layerRock)
        {
            SpawnExplosion(hitPos, rot);
            if (logHit) Debug.Log("[Laser] Hit Rock (stopping beam).");
            return;
        }

        // --- Mặc định (Wall, v.v.) ---
        SpawnExplosion(hitPos, rot);
        if (logHit)
            Debug.Log($"[Laser] Hit {hit.collider.name} at {hitPos}");
    }

    void SpawnExplosion(Vector3 pos, Quaternion rot)
    {
        if (!explosionPrefab) return;

        // tìm parent an toàn cho mọi VFX runtime
        Transform parent = null;
        if (LevelManager.Instance != null)
            parent = LevelManager.Instance.runtimeRoot;   // <<< quan trọng

        if (reuseExplosion)
        {
            if (!explosionInstance)
            {
                // gán parent = runtimeRoot
                explosionInstance = Instantiate(explosionPrefab, pos, rot, parent);
            }
            else
            {
                explosionInstance.transform.SetPositionAndRotation(pos, rot);
                var ps = explosionInstance.GetComponent<ParticleSystem>();
                if (ps && !ps.isPlaying) ps.Play();
            }
        }
        else
        {
            var fx = Instantiate(explosionPrefab, pos, rot, parent);
            if (destroyExplosionAfter > 0) Destroy(fx, destroyExplosionAfter);
        }
    }

    IEnumerator WaitReplay()
    {
        yield return new WaitForSeconds(2f);

        GameManager.Instance.RePlay();
    }
}
