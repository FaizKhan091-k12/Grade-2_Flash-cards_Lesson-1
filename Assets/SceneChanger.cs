using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneChanger : MonoBehaviour
{
    [Header("Optional Fade Canvas (assign CanvasGroup)")]
    public CanvasGroup fadeCanvas;

    [Header("Transition Settings")]
    public float fadeDuration = 1f;    // Time to fade in/out
    public float delayBeforeLoad = 0f; // Optional delay before loading

    private bool isTransitioning = false;

    // 🔹 Call this from Button OnClick() or other scripts
    public void LoadScene(string sceneName)
    {
        if (!isTransitioning)
            StartCoroutine(LoadSceneRoutine(sceneName));
    }

    // 🔹 Overload: load by scene index
    public void LoadScene(int sceneIndex)
    {
        if (!isTransitioning)
            StartCoroutine(LoadSceneRoutine(SceneManager.GetSceneByBuildIndex(sceneIndex).name));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isTransitioning = true;

        // 1️⃣ Optional Fade to Black
        if (fadeCanvas)
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // 2️⃣ Optional Delay (for dramatic timing)
        if (delayBeforeLoad > 0)
            yield return new WaitForSeconds(delayBeforeLoad);

        // 3️⃣ Load Scene
        SceneManager.LoadScene(sceneName);

        // Wait a frame to allow scene to initialize
        yield return null;

        // 4️⃣ Optional Fade back from black
        if (fadeCanvas)
            StartCoroutine(Fade(1f, 0f, fadeDuration));
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        fadeCanvas.gameObject.SetActive(true);
        float elapsed = 0f;
        fadeCanvas.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        fadeCanvas.alpha = to;

        if (to == 0f)
            fadeCanvas.gameObject.SetActive(false);
    }
}
