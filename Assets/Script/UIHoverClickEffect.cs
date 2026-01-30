using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to a UI element (Button, Image, etc.) to get a smooth hover scale and click pulse effect.
/// Honors Selectable.interactable if present (e.g. Button).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIHoverClickEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Hover Settings")]
    [Tooltip("Target scale multiplier when hovered (1 = no change).")]
    public float hoverScale = 1.08f;
    [Tooltip("Seconds to interpolate to hover scale.")]
    public float hoverDuration = 0.12f;
    [Tooltip("Use unscaled time (ignores timescale).")]
    public bool useUnscaledTime = true;

    [Header("Click / Pulse Settings")]
    [Tooltip("Pulse multiplier applied on click (applied relative to current scale).")]
    public float clickScale = 1.15f;
    [Tooltip("Total seconds for the click pulse (go-and-back).")]
    public float clickDuration = 0.18f;
    [Tooltip("Play click pulse even if not hovered.")]
    public bool clickAlwaysPlays = false;

    [Header("Events (optional)")]
    public UnityEvent onHoverEnter;
    public UnityEvent onHoverExit;
    public UnityEvent onClick;

    // internals
    RectTransform rt;
    Vector3 baseScale;
    Coroutine scaleCoroutine;
    bool isHovered = false;
    Selectable selectableComponent;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        baseScale = rt.localScale;
        selectableComponent = GetComponent<Selectable>(); // optional (Button, Toggle...)
    }

    bool IsInteractable()
    {
        // If a selectable component exists, only respond when interactable
        return selectableComponent == null || selectableComponent.interactable;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        isHovered = true;
        StopScaleCoroutine();
        Vector3 target = baseScale * hoverScale;
        scaleCoroutine = StartCoroutine(ScaleTo(rt.localScale, target, hoverDuration));
        onHoverEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        isHovered = false;
        StopScaleCoroutine();
        scaleCoroutine = StartCoroutine(ScaleTo(rt.localScale, baseScale, hoverDuration));
        onHoverExit?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        onClick?.Invoke();

        if (!clickAlwaysPlays && !isHovered) return;

        // If a click pulse is requested, run a quick pulse coroutine.
        StopScaleCoroutine();
        scaleCoroutine = StartCoroutine(ClickPulseCoroutine());
    }

    void StopScaleCoroutine()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
    }

    IEnumerator ScaleTo(Vector3 start, Vector3 target, float duration)
    {
        float t = 0f;
        if (duration <= 0f)
        {
            rt.localScale = target;
            yield break;
        }

        while (t < duration)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
            float f = Mathf.Clamp01(t / duration);
            // smooth interpolation
            float eased = Mathf.SmoothStep(0f, 1f, f);
            rt.localScale = Vector3.LerpUnclamped(start, target, eased);
            yield return null;
        }

        rt.localScale = target;
        scaleCoroutine = null;
    }

    IEnumerator ClickPulseCoroutine()
    {
        // Pulse from current => current*clickScale => back to hover/base depending on isHovered
        Vector3 start = rt.localScale;
        Vector3 peak = start * clickScale;
        float half = clickDuration * 0.5f;
        // go up
        float t = 0f;
        while (t < half)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
            float f = Mathf.Clamp01(t / half);
            float eased = Mathf.SmoothStep(0f, 1f, f);
            rt.localScale = Vector3.LerpUnclamped(start, peak, eased);
            yield return null;
        }
        rt.localScale = peak;

        // go back to target (hover target if still hovered, else baseScale)
        Vector3 returnTarget = isHovered ? baseScale * hoverScale : baseScale;
        t = 0f;
        while (t < half)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
            float f = Mathf.Clamp01(t / half);
            float eased = Mathf.SmoothStep(0f, 1f, f);
            rt.localScale = Vector3.LerpUnclamped(peak, returnTarget, eased);
            yield return null;
        }
        rt.localScale = returnTarget;
        scaleCoroutine = null;
    }

    // Optional public API to set base scale (useful if parent scale changed at runtime)
    public void ResetBaseScaleToCurrent()
    {
        baseScale = rt.localScale;
    }
}
