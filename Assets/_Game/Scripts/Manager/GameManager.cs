using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Demo,
        Gameplay,
        Pause,
        Win
    }

    public GameState CurrentState { get; private set; }

    [Header("Layers cần kiểm tra để Win")]
    public string redLayer  = "RedBall";
    public string blueLayer = "BlueBall";

    int redLayerIndex;
    int blueLayerIndex;

    [Header("Demo Settings")]
    public int[] demoLevelIndices = { 0, 1 };   // 2 level demo
    public float demoSwitchDelay = 11f;
    public int firstRealLevelIndex = 2;

    const string PP_SEEN_DEMO = "LB_SEEN_DEMO";

    bool firstStart = true;
    Coroutine demoLoopCo;
    int demoSlot = 0;

    void Awake()
    {
        Application.targetFrameRate = 60;

        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        redLayerIndex  = LayerMask.NameToLayer(redLayer);
        blueLayerIndex = LayerMask.NameToLayer(blueLayer);
    }

    void Start()
    {
        var lm = LevelManager.Instance;

        // Nếu đang TẮT saveProgress → coi như reset hoàn toàn
        if (!lm.saveProgress)
        {
            // Xoá level đã lưu + cờ đã xem demo
            PlayerPrefs.DeleteKey(LevelManager.PP_LEVEL_INDEX);
            PlayerPrefs.DeleteKey(PP_SEEN_DEMO);
            PlayerPrefs.Save();

            // Luôn chạy DEMO như lần đầu
            UIManager.Instance.OpenUI<PanelLoading>();
            StartCoroutine(WaitStartDemo());
            return;
        }

        // ----- ĐOẠN DƯỚI GIỮ NGUYÊN NHƯ HÔM TRƯỚC -----
        bool hasSave  = PlayerPrefs.HasKey(LevelManager.PP_LEVEL_INDEX);
        bool seenDemo = PlayerPrefs.GetInt(PP_SEEN_DEMO, 0) == 1;

        if (hasSave)
        {
            // Có level đã lưu -> vào thẳng gameplay
            firstStart   = false;
            CurrentState = GameState.Gameplay;

            int index = PlayerPrefs.GetInt(LevelManager.PP_LEVEL_INDEX);
            LevelManager.Instance.LoadLevel(index);
            UIManager.Instance.OpenUI<CanvasGameplay>();
        }
        else if (seenDemo)
        {
            // Đã xem demo nhưng chưa có save -> vào level thật đầu tiên
            firstStart   = false;
            CurrentState = GameState.Gameplay;

            LevelManager.Instance.LoadLevel(firstRealLevelIndex);
            UIManager.Instance.OpenUI<CanvasGameplay>();
        }
        else
        {
            // Lần đầu thực sự -> chạy demo
            UIManager.Instance.OpenUI<PanelLoading>();
            StartCoroutine(WaitStartDemo());
        }
    }


    void Update()
    {
        if (CurrentState == GameState.Gameplay)
            CheckWinCondition();
    }

    // ================= DEMO =================

    IEnumerator WaitStartDemo()
    {
        yield return new WaitForSeconds(1.5f);
        StartDemo();
    }

    void StartDemo()
    {
        CurrentState = GameState.Demo;
        Time.timeScale = 1f;
        firstStart = true;
        demoSlot = 0;

        LoadCurrentDemoLevel();

        if (demoLoopCo != null) StopCoroutine(demoLoopCo);
        demoLoopCo = StartCoroutine(DemoLoop());
    }

    void LoadCurrentDemoLevel()
    {
        if (demoLevelIndices == null || demoLevelIndices.Length == 0)
            return;

        int lvIndex = demoLevelIndices[Mathf.Clamp(demoSlot, 0, demoLevelIndices.Length - 1)];

        // load level demo
        LevelManager.Instance.LoadLevel(lvIndex);

        // Đảm bảo KHÔNG có UI gameplay/win
        UIManager.Instance.CloseUIDirectly<CanvasGameplay>();
        UIManager.Instance.CloseUIDirectly<CanvasWin>();

        // chỉ panel demo
        UIManager.Instance.OpenUI<PanelDemo>();
    }

    IEnumerator DemoLoop()
    {
        while (CurrentState == GameState.Demo)
        {
            yield return new WaitForSeconds(demoSwitchDelay);

            demoSlot = (demoSlot + 1) % demoLevelIndices.Length;
            LoadCurrentDemoLevel();
        }
    }

    // Gọi từ PanelDemo.OnClickPlay()
    public void StartRealGame()
    {
        if (demoLoopCo != null)
        {
            StopCoroutine(demoLoopCo);
            demoLoopCo = null;
        }

        firstStart   = false;
        CurrentState = GameState.Gameplay;

        // đánh dấu đã xem demo -> lần sau không vào demo nữa
        PlayerPrefs.SetInt(PP_SEEN_DEMO, 1);
        PlayerPrefs.Save();

        UIManager.Instance.CloseUIDirectly<PanelDemo>();

        // bắt đầu level thật đầu tiên
        LevelManager.Instance.LoadLevel(firstRealLevelIndex);

        UIManager.Instance.OpenUI<CanvasGameplay>();
    }

    // =============== STATE CONTROL ===============

    public void SetState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Gameplay:
                Time.timeScale = 1f;
                if (!firstStart)
                    UIManager.Instance.OpenUI<CanvasGameplay>();
                break;

            case GameState.Pause:
                Time.timeScale = 0f;
                break;

            case GameState.Win:
                // Chỉ win nếu đang ở level thật (>= firstRealLevelIndex)
                if (LevelManager.Instance.CurrentIndex < firstRealLevelIndex)
                    return;

                Erase.Instance.enabled = false;
                AudioManager.Instance?.PlayWin();
                UIManager.Instance.OpenUI<CanvasWin>();
                break;

            case GameState.Demo:
                Time.timeScale = 1f;
                UIManager.Instance.CloseUIDirectly<CanvasGameplay>();
                UIManager.Instance.CloseUIDirectly<CanvasWin>();
                UIManager.Instance.OpenUI<PanelDemo>();
                break;
        }
    }

    // =============== BUTTONS ===============

    public void NextLV() => LevelManager.Instance?.NextLevel();
    public void RePlay() => LevelManager.Instance?.Replay();

    public void PauseGame()
    {
        if (CurrentState == GameState.Gameplay)
            SetState(GameState.Pause);
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Pause)
            SetState(GameState.Gameplay);
    }

    // =============== CHECK WIN ===============

    void CheckWinCondition()
    {
        // Bỏ qua toàn bộ logic win nếu đang demo
        if (CurrentState != GameState.Gameplay) return;
        if (LevelManager.Instance.CurrentIndex < firstRealLevelIndex) return;

        if (!AnyObjectWithLayerExists(redLayerIndex) &&
            !AnyObjectWithLayerExists(blueLayerIndex))
        {
            SetState(GameState.Win);
        }
    }

    bool AnyObjectWithLayerExists(int layer)
    {
        foreach (var obj in FindObjectsOfType<GameObject>())
        {
            if (obj.layer == layer) return true;
        }
        return false;
    }

    // =============== RESET KHI LOAD LEVEL ===============

    public void ResetForNewLevel()
    {
        if (CurrentState == GameState.Demo)
        {
            Time.timeScale = 1f;
            return;
        }

        CurrentState = GameState.Gameplay;
        Time.timeScale = 1f;

        if (!firstStart)
        {
            var canvas = UIManager.Instance.GetUI<CanvasGameplay>();
            canvas?.ResetHintTimers();
        }
    }
}
