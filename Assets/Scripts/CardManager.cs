using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [Header("Prefab & Parent")]
    public GameObject cardPrefab;
    public RectTransform parentPanel;

    [Header("Card Data")]
    public List<string> keywords = new List<string>();    // front keywords
    public List<Sprite> backImages = new List<Sprite>();  // back info images
    [TextArea(2,6)]
    public List<string> backInfos = new List<string>();   // back descriptions

    List<GameObject> spawned = new List<GameObject>();
    ResponsiveGrid responsiveGrid;

    void Awake()
    {
        if (parentPanel != null) responsiveGrid = parentPanel.GetComponent<ResponsiveGrid>();
    }

    void Start()
    {
        CreateCards();
    }

    public void CreateCards()
    {
        // cleanup old
        foreach (var g in spawned) if (g) Destroy(g);
        spawned.Clear();

        int count = Mathf.Min(keywords.Count, backImages.Count, backInfos.Count);
        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(cardPrefab, parentPanel);
            var cc = go.GetComponent<CardController>();
            if (cc != null)
            {
                cc.Setup(keywords[i], backImages[i], backInfos[i]);
            }
            spawned.Add(go);
        }

        if (responsiveGrid != null) responsiveGrid.ForceUpdateLater(0.05f);
    }

    // Example runtime add
    public void AddCard(string keyword, Sprite image, string info)
    {
        keywords.Add(keyword);
        backImages.Add(image);
        backInfos.Add(info);

        GameObject go = Instantiate(cardPrefab, parentPanel);
        var cc = go.GetComponent<CardController>();
        if (cc != null) cc.Setup(keyword, image, info);
        spawned.Add(go);

        if (responsiveGrid != null) responsiveGrid.ForceUpdateLater();
    }
}
