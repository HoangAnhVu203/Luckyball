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

    [Header("Next Level Button Hint")]
    public RectTransform nextLvButton;
    public float nextWaitTime = 15f;
    public float nextScaleUp = 1.2f;
    public float nextScaleDuration = 0.3f;

    [Header("Hint Button Hint")]
    public RectTransform hintButton;
    public float hintWaitTime = 7f;
    public float hintScaleUp = 1.2f;
    public float hintScaleDuration = 0.3f;

    Vector3 nextOriginalScale;
    Vector3 hintOriginalScale;

    Coroutine nextScaleRoutine;
    Coroutine hintScaleRoutine;

    void OnEnable()
    {
        // Lưu & reset scale gốc
        if (nextLvButton != null)
        {
            nextOriginalScale = nextLvButton.localScale;
            nextLvButton.localScale = nextOriginalScale;
        }
        if (hintButton != null)
        {
            hintOriginalScale = hintButton.localScale;
            hintButton.localScale = hintOriginalScale;
        }

        StartNextScaleHint();
        StartHintButtonScaleHint();
    }

    void OnDisable()
    {
        StopNextScaleHint();
        StopHintButtonScaleHint();
    }

    // ================== BUTTON ==================

    public void NextLVBTN()
    {
        StopNextScaleHint();                 // chỉ dừng scale của nút NextLV
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

    public void HintBTN()
    {
        StopHintButtonScaleHint();           // chỉ dừng scale của nút Hint
        HintSystem.Instance?.TryUseHint();
    }

    // ================== HIỂN THỊ LEVEL ==================

    public void ShowLevel(int levelIndex)
    {
        if (levelRoutine != null) StopCoroutine(levelRoutine);
        levelRoutine = StartCoroutine(ShowLevelRoutine(levelIndex));
    }

    IEnumerator ShowLevelRoutine(int levelIndex)
    {
        if (levelPanel) levelPanel.SetActive(true);
        if (levelText) levelText.text = $"Level {levelIndex  - 1}";

        yield return new WaitForSeconds(levelShowTime);

        if (levelPanel) levelPanel.SetActive(false);
        levelRoutine = null;
    }

    // ================== SCALE NEXTLV ==================

    void StartNextScaleHint()
    {
        if (nextLvButton == null) return;

        if (nextScaleRoutine != null)
            StopCoroutine(nextScaleRoutine);

        nextScaleRoutine = StartCoroutine(NextButtonScaleLoop());
    }

    void StopNextScaleHint()
    {
        if (nextScaleRoutine != null)
            StopCoroutine(nextScaleRoutine);

        if (nextLvButton != null)
            nextLvButton.localScale = nextOriginalScale;

        nextScaleRoutine = null;
    }

    IEnumerator NextButtonScaleLoop()
    {
        yield return new WaitForSeconds(nextWaitTime);

        while (true)
        {
            // scale up
            float t = 0f;
            while (t < nextScaleDuration)
            {
                t += Time.deltaTime;
                float k = t / nextScaleDuration;
                nextLvButton.localScale =
                    Vector3.Lerp(nextOriginalScale, nextOriginalScale * nextScaleUp, k);
                yield return null;
            }

            // scale down
            t = 0f;
            while (t < nextScaleDuration)
            {
                t += Time.deltaTime;
                float k = t / nextScaleDuration;
                nextLvButton.localScale =
                    Vector3.Lerp(nextOriginalScale * nextScaleUp, nextOriginalScale, k);
                yield return null;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    // ================== SCALE HINT BTN ==================

    void StartHintButtonScaleHint()
    {
        if (hintButton == null) return;

        if (hintScaleRoutine != null)
            StopCoroutine(hintScaleRoutine);

        hintScaleRoutine = StartCoroutine(HintButtonScaleLoop());
    }

    void StopHintButtonScaleHint()
    {
        if (hintScaleRoutine != null)
            StopCoroutine(hintScaleRoutine);

        if (hintButton != null)
            hintButton.localScale = hintOriginalScale;

        hintScaleRoutine = null;
    }

    IEnumerator HintButtonScaleLoop()
    {
        yield return new WaitForSeconds(hintWaitTime);

        while (true)
        {
            // scale up
            float t = 0f;
            while (t < hintScaleDuration)
            {
                t += Time.deltaTime;
                float k = t / hintScaleDuration;
                hintButton.localScale =
                    Vector3.Lerp(hintOriginalScale, hintOriginalScale * hintScaleUp, k);
                yield return null;
            }

            // scale down
            t = 0f;
            while (t < hintScaleDuration)
            {
                t += Time.deltaTime;
                float k = t / hintScaleDuration;
                hintButton.localScale =
                    Vector3.Lerp(hintOriginalScale * hintScaleUp, hintOriginalScale, k);
                yield return null;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    public void ResetHintTimers()
    {
        // dừng toàn bộ hiệu ứng cũ
        StopNextScaleHint();
        StopHintButtonScaleHint();

        // trả scale về gốc (phòng trường hợp không chạy OnEnable)
        if (nextLvButton != null)
            nextLvButton.localScale = nextOriginalScale;

        if (hintButton != null)
            hintButton.localScale = hintOriginalScale;

        // bắt đầu đếm lại từ đầu
        StartNextScaleHint();
        StartHintButtonScaleHint();
    }
}
