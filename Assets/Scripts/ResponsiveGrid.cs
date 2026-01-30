using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(GridLayoutGroup))]
public class ResponsiveGrid : UIBehaviour
{
    GridLayoutGroup grid;
    RectTransform rect;

    [Header("Sizing constraints")]
    public float preferredCellWidth = 250f;
    public float minCellWidth = 120f;
    public int maxColumns = 0; // 0 = no limit
    public int maxRows = 0;    // 0 = no limit

    [Header("Aspect (height = width * aspect)")]
    public float cellAspect = 1.4f;

    [Header("Spacing & padding (override if desired)")]
    public bool useGridSpacing = true;
    public Vector2 overrideSpacing = new Vector2(20f, 20f);
    public RectOffset overridePadding = null;

    void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        rect = transform as RectTransform;
        if (overridePadding == null) overridePadding = grid.padding;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateLayout();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        UpdateLayout();
    }

    public void UpdateLayout()
    {
        if (!gameObject.activeInHierarchy) return;
        if (grid == null || rect == null) return;

        RectOffset pad = overridePadding != null ? overridePadding : grid.padding;
        Vector2 spacing = useGridSpacing ? grid.spacing : overrideSpacing;

        float availableWidth = rect.rect.width - pad.left - pad.right;
        float availableHeight = rect.rect.height - pad.top - pad.bottom;
        availableWidth = Mathf.Max(1f, availableWidth);
        availableHeight = Mathf.Max(1f, availableHeight);

        int childCount = Mathf.Max(1, transform.childCount);

        int maxPossibleColumns = Mathf.FloorToInt((availableWidth + spacing.x) / (preferredCellWidth + spacing.x));
        if (maxPossibleColumns < 1) maxPossibleColumns = 1;
        if (maxColumns > 0) maxPossibleColumns = Mathf.Min(maxPossibleColumns, maxColumns);

        int chosenColumns = maxPossibleColumns;
        float finalCellWidth = preferredCellWidth;

        for (int cols = maxPossibleColumns; cols >= 1; cols--)
        {
            float totalSpacingX = spacing.x * (cols - 1);
            float widthForCells = availableWidth - totalSpacingX;
            float candidateCellWidth = widthForCells / cols;

            if (candidateCellWidth >= minCellWidth)
            {
                chosenColumns = cols;
                finalCellWidth = candidateCellWidth;
                break;
            }
        }

        if (finalCellWidth < minCellWidth) finalCellWidth = minCellWidth;
        float finalCellHeight = finalCellWidth * cellAspect;

        if (maxRows > 0)
        {
            int requiredRows = Mathf.CeilToInt((float)transform.childCount / chosenColumns);
            if (requiredRows > maxRows)
            {
                for (int cols = chosenColumns + 1; cols <= (maxColumns > 0 ? maxColumns : 100); cols++)
                {
                    float totalSpacingX = spacing.x * (cols - 1);
                    float widthForCells = availableWidth - totalSpacingX;
                    float candidateCellWidth = widthForCells / cols;
                    if (candidateCellWidth < minCellWidth) break;
                    int rowsWithCols = Mathf.CeilToInt((float)transform.childCount / cols);
                    if (rowsWithCols <= maxRows)
                    {
                        chosenColumns = cols;
                        finalCellWidth = candidateCellWidth;
                        finalCellHeight = finalCellWidth * cellAspect;
                        break;
                    }
                }
            }
        }

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, chosenColumns);
        grid.cellSize = new Vector2(Mathf.Floor(finalCellWidth), Mathf.Floor(finalCellHeight));
        if (!useGridSpacing) grid.spacing = overrideSpacing;
        if (overridePadding != null) grid.padding = overridePadding;

        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    public void ForceUpdateLater(float delay = 0.05f)
    {
        StopAllCoroutines();
        StartCoroutine(DelayedUpdate(delay));
    }

    System.Collections.IEnumerator DelayedUpdate(float d)
    {
        yield return new WaitForSeconds(d);
        UpdateLayout();
    }
}
