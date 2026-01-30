using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SeprateCards : MonoBehaviour
{
    [SerializeField] GridLayoutGroup gridLayoutGroup;
    [SerializeField] TextMeshProUGUI tapAnywhere;
    [SerializeField] GameObject tapTextContainer;
    [SerializeField] float speed;
    public bool isDeckOpen;


    [Header("Sounds")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip openDeckClip;
    void Start()
    {
        StartCoroutine(TextBlink());
    }

    public void DetectScreenTap()
    {
        tapTextContainer.SetActive(false);
        if (!isDeckOpen)
            StartCoroutine(OpenDeckofCards());
    }

    IEnumerator TextBlink()
    {
        while (true)
        {
            float alpha = Mathf.PingPong(Time.time, 1f);

            Color c = tapAnywhere.color;
            c.a = alpha;
            tapAnywhere.color = c;

            yield return null;
        }
    }

    IEnumerator OpenDeckofCards()
    {
        float t = 0f;
        audioSource.PlayOneShot(openDeckClip);
        while (t < 1)
        {
            t += Time.deltaTime;

            float tempValX = Mathf.Lerp(0, 700, t * speed);

            gridLayoutGroup.spacing = new Vector2(tempValX, 0);

            yield return null;
        }

        isDeckOpen = true;
    }
}
