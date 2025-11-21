using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PanelLoading : UICanvas
{
    [Header("UI")]
    public GameObject panel;
    public Slider progressBar;        
    public float fakeLoadingTime = 1.5f;

    [Header("Fade Out")]
    public CanvasGroup fadeGroup;
    public float fadeDuration = 0.5f;

    void Start()
    {
        if (panel) panel.SetActive(true);
        if (fadeGroup) fadeGroup.alpha = 1f;

        StartCoroutine(LoadingRoutine());
    }

    IEnumerator LoadingRoutine()
    {
        float t = 0f;

        while (t < fakeLoadingTime)
        {
            t += Time.deltaTime;
            float k = t / fakeLoadingTime;

            if (progressBar)
                progressBar.value = k;

            yield return null;
        }

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Hide panel hoàn toàn
        if (panel) panel.SetActive(false);
    }

    IEnumerator FadeOut()
    {
        if (!fadeGroup) yield break;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeGroup.alpha = 1f - (t / fadeDuration);
            yield return null;
        }

        fadeGroup.alpha = 0f;
    }
}
