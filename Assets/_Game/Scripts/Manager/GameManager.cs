using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Gameplay,
        Pause,
        Win
    }

    public GameState CurrentState { get; private set; }

    [Header("Layers cần kiểm tra để Win")]
    public string redLayer = "RedBall";
    public string blueLayer = "BlueBall";

    int redLayerIndex;
    int blueLayerIndex;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        redLayerIndex = LayerMask.NameToLayer(redLayer);
        blueLayerIndex = LayerMask.NameToLayer(blueLayer);
    }

    void Start()
    {
        SetState(GameState.Gameplay);
    }

    void Update()
    {
        if (CurrentState == GameState.Gameplay)
        {
            CheckWinCondition();
        }
    }

    // ==========================
    //     STATE CONTROL
    // ==========================

    public void SetState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Gameplay:
                Time.timeScale = 1f;
                UIManager.Instance.OpenUI<CanvasGameplay>();
                break;

            case GameState.Pause:
                Time.timeScale = 0f;
                break;

            case GameState.Win:
                //Time.timeScale = 1f;
                Erase.Instance.enabled = false;
                UIManager.Instance.OpenUI<CanvasWin>();
                Debug.Log("WIN!");
                break;
        }
    }

    public void NextLV()
    {
        LevelManager.Instance?.NextLevel();
    }

    public void RePlay()
    {
        LevelManager.Instance?.Replay();
    }

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


    // ==========================
    //     CHECK WIN
    // ==========================

    void CheckWinCondition()
    {
        // Nếu không còn bất kỳ object RedBall hoặc BlueBall nào
        if (!AnyObjectWithLayerExists(redLayerIndex) &&
            !AnyObjectWithLayerExists(blueLayerIndex))
        {
            SetState(GameState.Win);
        }
    }

    bool AnyObjectWithLayerExists(int layer)
    {
        GameObject[] all = FindObjectsOfType<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].layer == layer)
                return true;
        }
        return false;
    }

    public void ResetForNewLevel()
    {
        CurrentState = GameState.Gameplay;
        Time.timeScale = 1f;

        // Ẩn win UI
        //UIManager.Instance.CloseUI<CanvasWin>();

        // Reset các thứ gameplay nếu có
    }

}
