using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelManager : MonoBehaviour
{
    [Serializable]
    public class LevelEntry
    {
        public string id;
        public GameObject prefab;
    }

    public static LevelManager Instance { get; private set; }

    [Header("Danh sách level (kéo prefab vào)")]
    public List<LevelEntry> levels = new List<LevelEntry>();

    [Header("Nơi spawn level")]
    public Transform levelRoot;

    [Header("Lưu tiến trình")]
    public bool saveProgress = true;
    public bool loopAtEnd = true;
    public int defaultStartIndex = 0;

    public const string PP_LEVEL_INDEX = "LUCKYBALL_LEVEL";

    public int CurrentIndex { get; private set; } = -1;
    public GameObject CurrentLevelGO { get; private set; }

    public event Action<GameObject, int> OnLevelLoaded;
    public event Action<int> OnLevelUnloaded;

    [Header("Runtime Root (để clear rác mỗi level)")]
    public Transform runtimeRoot;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!levelRoot) levelRoot = transform;

        if (!runtimeRoot)
        {
            GameObject rt = new GameObject("__RuntimeRoot");
            runtimeRoot = rt.transform;
            runtimeRoot.SetParent(transform);
        }
    }

    void Start()
    {
        int startIndex = defaultStartIndex;

        if (saveProgress && PlayerPrefs.HasKey(PP_LEVEL_INDEX))
            startIndex = PlayerPrefs.GetInt(PP_LEVEL_INDEX);

        startIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, levels.Count - 1));

        LoadLevel(startIndex);
    }

    // ==========================
    //       PUBLIC API 
    // ==========================

    public void Replay()
    {
        if (CurrentIndex >= 0)
            LoadLevel(CurrentIndex);
    }

    public void NextLevel()
    {
        if (levels.Count == 0) return;

        int next = CurrentIndex + 1;

        if (next >= levels.Count)
            next = loopAtEnd ? 0 : levels.Count - 1;

        LoadLevel(next);
    }

    public void LoadLevelById(string id)
    {
        int index = levels.FindIndex(l => l.id == id);
        if (index >= 0) LoadLevel(index);
        else Debug.LogWarning($"[LevelManager] Không tìm thấy id = {id}");
    }

    // ==========================
    //         CORE LOAD
    // ==========================

    public void LoadLevel(int index)
    {
        if (levels.Count == 0)
        {
            Debug.LogError("[LevelManager] Chưa có level nào trong danh sách!");
            return;
        }

        index = Mathf.Clamp(index, 0, levels.Count - 1);

        // 1. Clear runtime object
        ClearRuntime();

        // 2. Destroy level cũ
        if (CurrentLevelGO)
        {
            OnLevelUnloaded?.Invoke(CurrentIndex);
            Destroy(CurrentLevelGO);
        }

        // 3. Spawn level mới
        LevelEntry entry = levels[index];

        if (!entry.prefab)
        {
            Debug.LogError($"[LevelManager] Prefab rỗng tại Level {index}");
            return;
        }

        CurrentLevelGO = Instantiate(entry.prefab, levelRoot);
        CurrentLevelGO.name = string.IsNullOrEmpty(entry.id) ? $"Level_{index}" : entry.id;

        CurrentIndex = index;

        HintSystem.Instance?.HideHint();

        // 4. Save progress
        if (saveProgress)
        {
            PlayerPrefs.SetInt(PP_LEVEL_INDEX, CurrentIndex);
            PlayerPrefs.Save();
        }

        // 5. Thông báo GameManager reset gameplay
        GameManager.Instance?.ResetForNewLevel();

        OnLevelLoaded?.Invoke(CurrentLevelGO, CurrentIndex);

        Debug.Log($"[LevelManager] Loaded level index: {CurrentIndex}");
        if(CurrentIndex == 0)
        {
            StartCoroutine(Waitlevel());
        }
        else
        {
            UIManager.Instance.GetUI<CanvasGameplay>()?.ShowLevel(LevelManager.Instance.CurrentIndex);
        }
        
    }

    // ==========================
    //         CLEAR RUNTIME
    // ==========================

    public void ClearRuntime()
    {
        // Xoá toàn bộ object runtime (Bubble, Trap, VFX…)
        if (runtimeRoot)
        {
            for (int i = runtimeRoot.childCount - 1; i >= 0; i--)
                Destroy(runtimeRoot.GetChild(i).gameObject);
        }

        // Xoá bomb fragments nếu có Bomb tạo debris
        var all = FindObjectsOfType<GameObject>();
        foreach (var go in all)
        {
            if (!go || !go.activeInHierarchy) continue;

            string n = go.name.ToLower();
            if (n.Contains("bubble") || n.Contains("bombfragment") || n.Contains("debris"))
                Destroy(go);
        }

        
    }
    IEnumerator Waitlevel()
        {
            yield return new WaitForSeconds(1.5f);

            UIManager.Instance.GetUI<CanvasGameplay>()?.ShowLevel(LevelManager.Instance.CurrentIndex);
        }
}
