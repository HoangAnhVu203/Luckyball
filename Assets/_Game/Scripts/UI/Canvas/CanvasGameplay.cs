using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CanvasGameplay : UICanvas
{
    [Header("Level Panel")]
    [SerializeField] GameObject levelPanel;          
    [SerializeField] Text levelText;      
    [SerializeField] float levelShowTime = 3f;     

    Coroutine levelRoutine;

    // ============ BUTTON ============

    public void NextLVBTN()
    {
        GameManager.Instance.NextLV();
    }

    public void RePlayBTN()
    {
        GameManager.Instance.RePlay();
    }

    public void SettingBTN()
    {
        GameManager.Instance.PauseGame();
        UIManager.Instance.OpenUI<CanvasSetting>();
    }

    // ============ LEVEL SHOW ============

    /// <summary>
    /// Gọi hàm này khi bắt đầu 1 level mới để hiện "LEVEL X" trong 3s.
    /// </summary>
    public void ShowLevel(int levelIndex)
    {
        if (levelRoutine != null)
            StopCoroutine(levelRoutine);

        levelRoutine = StartCoroutine(ShowLevelRoutine(levelIndex));
    }

    IEnumerator ShowLevelRoutine(int levelIndex)
    {
        if (levelPanel != null)
            levelPanel.SetActive(true);

        if (levelText != null)
            levelText.text = $"Level {levelIndex + 1}";

        yield return new WaitForSeconds(levelShowTime);

        if (levelPanel != null)
            levelPanel.SetActive(false);

        levelRoutine = null;
    }
}
