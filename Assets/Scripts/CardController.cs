using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class CardController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    // ---------- Registry so we can cancel pending audio across all cards ----------
    static readonly List<CardController> allCards = new List<CardController>();

    [Header("FRONT")]
    public TextMeshProUGUI frontKeyword;

    [Header("BACK")]
    public GameObject backSide;
    public Image backImage;
    public TextMeshProUGUI backInfo;

    [Header("Flip Timing")]
    public float flipDuration = 0.35f;

    [Tooltip("Delay only before FRONT -> BACK animation. Back->Front has NO delay.")]
    public float delayBeforeFlipAnimation = 0.2f;

    [Header("Click-lock (global)")]
    [Tooltip("If TRUE, automatically lock clicks for all cards for (flipDuration + delayBeforeFlipAnimation).")]
    public bool lockClicksForFlipDuration = true;

    [Tooltip("If lockClicksForFlipDuration is FALSE, this duration (seconds) will be used to lock clicks globally.")]
    public float globalClickLockSeconds = 0.35f;

    // static field that stores until when clicks are disabled globally
    static float clicksDisabledUntil = 0f; // Time.time value until which clicks are ignored across all cards

    [Header("Audio")]
    // main audio source for flip/definition/voice playback
    public AudioSource audioSource;

    // separate audio source used only for hover (keywordSound) so it isn't stopped by global stop calls
    [Tooltip("Optional: separate AudioSource used only for hover keyword sound. If null, one will be created.")]
    public AudioSource hoverAudioSource;

    [Header("Keyword Sound (front → back only)")]
    public AudioClip keywordSound;            // SFX for front->back (we now play this on hover)
    [Range(0f, 1f)] public float keywordVolume = 0.9f;

    [Header("Flip Sound (voice-over)")]
    public AudioClip flipSound;               // voice-over
    [Range(0f, 1f)] public float flipVolume = 1f;

    [Tooltip("Play keywordSound + flipSound at the same time (front side only).")]
    public bool playBothSimultaneously = false;

    [Tooltip("If TRUE, flipSound starts after animation (front side only).")]
    public bool playFlipSoundAfterAnimation = false;

    public float extraDelayBeforeFlipSound = 0f;

    [Header("Definition Sound (front → back only)")]
    public AudioClip definitionSound;        // post-flip sound
    [Range(0f, 1f)] public float definitionVolume = 1f;

    [Header("Definition Sound Delay (front → back only)")]
    [Tooltip("Delay before playing definitionSound, AFTER flip animation ends.")]
    public float delayBeforeDefinitionSound = 0.25f;

    // coroutine handles so we can cancel pending scheduled audio per-instance
    Coroutine delayedFlipSoundRoutine = null;
    Coroutine delayedDefinitionRoutine = null;
    Coroutine delayedVoiceRoutine = null;

    bool isFlipped = false;
    bool flipping = false;

    // ---------------- Hover control ----------------
    [Header("Hover Settings")]
    [Tooltip("Minimum seconds between hover keyword sound plays to avoid spam")]
    public float hoverCooldownSeconds = 0.5f;
    float lastHoverPlayTime = -999f;

    // -------------------------------------------------------------------------
    void OnEnable()
    {
        if (!allCards.Contains(this)) allCards.Add(this);
    }

    void OnDisable()
    {
        if (allCards.Contains(this)) allCards.Remove(this);
        // ensure we cancel any pending audio when disabled
        CancelPendingAudio();
    }

    void Awake()
    {
        // main audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }
        }

        // hover audio source: separate so hover SFX doesn't get stopped by global audio stop
        if (hoverAudioSource == null)
        {
            // create a separate audio source
            hoverAudioSource = gameObject.AddComponent<AudioSource>();
            hoverAudioSource.playOnAwake = false;
            hoverAudioSource.spatialBlend = 0f;
        }
    }

    // Convenience: cancels this instance's scheduled coroutines
    public void CancelPendingAudio()
    {
        if (delayedFlipSoundRoutine != null) { StopCoroutine(delayedFlipSoundRoutine); delayedFlipSoundRoutine = null; }
        if (delayedDefinitionRoutine != null) { StopCoroutine(delayedDefinitionRoutine); delayedDefinitionRoutine = null; }
        if (delayedVoiceRoutine != null) { StopCoroutine(delayedVoiceRoutine); delayedVoiceRoutine = null; }
    }

    // Static helper: cancel pending audio on ALL CardController instances
    public static void CancelAllPendingAudioOnAllCards()
    {
        for (int i = 0; i < allCards.Count; i++)
        {
            var c = allCards[i];
            if (c != null) c.CancelPendingAudio();
        }
    }

    public void Setup(string keyword, Sprite infoSprite, string infoText)
    {
        frontKeyword.text = keyword;
        backImage.sprite = infoSprite;
        backInfo.text = infoText;

        isFlipped = false;
        flipping = false;

        if (frontKeyword != null) frontKeyword.gameObject.SetActive(true);
        if (backSide != null) backSide.SetActive(false);
        transform.localEulerAngles = Vector3.zero;
    }

    // -------------------- Global click-lock check --------------------
    // Call this to set global lock until a future time
    static void SetGlobalClickLock(float seconds)
    {
        float until = Time.time + Mathf.Max(0f, seconds);
        // ensure we only extend the lock if it's later than what we already have
        if (until > clicksDisabledUntil) clicksDisabledUntil = until;
    }

    // Optionally call to clear lock immediately
    public static void ClearGlobalClickLock()
    {
        clicksDisabledUntil = 0f;
    }

    [System.Obsolete]
    public void OnPointerClick(PointerEventData eventData)
    {
        // If global click lock is active, ignore pointer clicks on all cards
        if (Time.time < clicksDisabledUntil)
        {
            // optionally you could play a "can't click" sound or visual feedback here
            return;
        }

        if (!flipping)
            StartCoroutine(FlipCoroutine());
    }

    // ---------- Hover: play keyword sound when mouse enters (front only) ----------
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only play when card is on front side and not flipping, and obey cooldown
        if (!isFlipped && !flipping && keywordSound != null && hoverAudioSource != null)
        {
            if (Time.time - lastHoverPlayTime >= hoverCooldownSeconds)
            {
                lastHoverPlayTime = Time.time;
                hoverAudioSource.PlayOneShot(keywordSound, keywordVolume);
            }
        }
    }

    // You can use this to stop any looping hover sound if you ever use one.
    public void OnPointerExit(PointerEventData eventData)
    {
        // currently keywordSound is one-shot; no action needed.
        // If you used a looping hover sound, you'd stop it here: hoverAudioSource.Stop();
    }

    [System.Obsolete]
    IEnumerator FlipCoroutine()
    {
        flipping = true;

        // set a global click lock so other cards can't be clicked for a bit
        float lockSeconds = globalClickLockSeconds;
        if (lockClicksForFlipDuration)
        {
            // use flipDuration + pre-delay as a sensible default so clicks are blocked for the animation time
            lockSeconds = Mathf.Max(0f, flipDuration + delayBeforeFlipAnimation);
        }
        SetGlobalClickLock(lockSeconds);

        bool flippingFromFront = !isFlipped;

        // ----------------- STOP/ cancel previous audio everywhere -----------------
        // 1) stop currently playing audio sources (quick stop)
        StopAllAudioSourcesInScene();

        // 2) cancel any pending scheduled audio coroutines across all card instances
        CancelAllPendingAudioOnAllCards();

        // Also cancel this instance's references (safe)
        CancelPendingAudio();

        // =====================================================
        // 🔊 AUDIO START (NO DELAY)
        // =====================================================
        if (audioSource != null)
        {
            if (flippingFromFront)
            {
                // FRONT -> BACK
                if (playBothSimultaneously)
                {
                    if (keywordSound != null)
                        audioSource.PlayOneShot(keywordSound, keywordVolume);

                    if (flipSound != null)
                        audioSource.PlayOneShot(flipSound, flipVolume);
                }
                else
                {
                    if (keywordSound != null)
                    {
                        // NOTE: keywordSound already played on hover. To avoid doubling, only play here if you still want it.
                        // If you do not want it on click as well, comment out the next line.
                        // audioSource.PlayOneShot(keywordSound, keywordVolume);

                        if (flipSound != null && !playFlipSoundAfterAnimation)
                        {
                            // schedule flipSound after keywordSound's length + optional extra delay
                            delayedFlipSoundRoutine = StartCoroutine(PlayDelayedFlipSound(keywordSound.length + extraDelayBeforeFlipSound));
                        }
                    }
                    else
                    {
                        if (flipSound != null && !playFlipSoundAfterAnimation)
                            audioSource.PlayOneShot(flipSound, flipVolume);
                    }
                }
            }
            else
            {
                // BACK -> FRONT: only play flipSound (voice) immediately
                if (flipSound != null)
                    audioSource.PlayOneShot(flipSound, flipVolume);
            }
        }

        // =====================================================
        // 🕒 DELAY BEFORE ANIMATION (front side only)
        // =====================================================
        if (flippingFromFront && delayBeforeFlipAnimation > 0f)
            yield return new WaitForSeconds(delayBeforeFlipAnimation);

        // =====================================================
        // 🎞 FLIP ANIMATION (first half)
        // =====================================================
        float half = flipDuration / 2f;
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            float angle = Mathf.Lerp(0f, 90f, t / half);
            transform.localEulerAngles = new Vector3(0f, angle, 0f);
            yield return null;
        }

        // Swap sides
        isFlipped = !isFlipped;
        if (frontKeyword != null) frontKeyword.gameObject.SetActive(!isFlipped);
        if (backSide != null) backSide.SetActive(isFlipped);

        // =====================================================
        // 🔊 POST-FLIP / DEFINITION SOUND (schedule, front->back only)
        // =====================================================
        if (flippingFromFront && definitionSound != null)
        {
            if (delayedDefinitionRoutine != null) StopCoroutine(delayedDefinitionRoutine);
            delayedDefinitionRoutine = StartCoroutine(PlayDelayedDefinitionSound(delayBeforeDefinitionSound));
        }

        // =====================================================
        // 🔊 FlipSound AFTER animation (if chosen)
        // =====================================================
        if (flippingFromFront && playFlipSoundAfterAnimation && !playBothSimultaneously)
        {
            if (delayedFlipSoundRoutine != null) StopCoroutine(delayedFlipSoundRoutine);
            delayedFlipSoundRoutine = StartCoroutine(PlayDelayedFlipSound(extraDelayBeforeFlipSound));
        }

        // If voice should play after animation (and not simultaneous), schedule it
        if (flippingFromFront && !playBothSimultaneously && playFlipSoundAfterAnimation && flipSound != null)
        {
            if (delayedVoiceRoutine != null) StopCoroutine(delayedVoiceRoutine);
            delayedVoiceRoutine = StartCoroutine(PlayDelayedVoice(extraDelayBeforeFlipSound));
        }

        // second half: rotate 90 -> 0
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float angle = Mathf.Lerp(90f, 0f, t / half);
            transform.localEulerAngles = new Vector3(0f, angle, 0f);
            yield return null;
        }

        // finalize
        transform.localEulerAngles = Vector3.zero;
        flipping = false;
    }

    // Stop all AudioSources in the scene (quick method). If you'd prefer to only stop this card's audio,
    // replace this with audioSource.Stop() or add tag checks to skip music.
    [System.Obsolete]
    void StopAllAudioSourcesInScene()
    {
        AudioSource[] all = FindObjectsOfType<AudioSource>();
        foreach (var s in all)
        {
            // Skip hover audio sources so hover sounds are not cut mid-play.
            // We identify hover sources by comparing to known instances' hoverAudioSource references.
            bool isHoverSource = false;
            for (int i = 0; i < allCards.Count; i++)
            {
                var c = allCards[i];
                if (c != null && c.hoverAudioSource != null && c.hoverAudioSource == s)
                {
                    isHoverSource = true;
                    break;
                }
            }

            if (!isHoverSource)
            {
                s.Stop();
            }
        }
    }

    IEnumerator PlayDelayedVoice(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        delayedVoiceRoutine = null;
        if (audioSource != null && flipSound != null)
            audioSource.PlayOneShot(flipSound, flipVolume);
    }

    IEnumerator PlayDelayedFlipSound(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        delayedFlipSoundRoutine = null;
        if (audioSource != null && flipSound != null)
            audioSource.PlayOneShot(flipSound, flipVolume);
    }

    IEnumerator PlayDelayedDefinitionSound(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        delayedDefinitionRoutine = null;
        if (audioSource != null && definitionSound != null)
            audioSource.PlayOneShot(definitionSound, definitionVolume);
    }
}
