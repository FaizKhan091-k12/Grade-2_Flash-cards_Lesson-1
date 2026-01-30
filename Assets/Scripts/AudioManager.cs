using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central AudioManager for card sounds:
/// - single AudioSource used for all card playback (so Stop reliably stops them)
/// - scheduling of delayed plays tracked by ID so they can be cancelled individually or globally
/// - optional DontDestroyOnLoad to persist across scenes
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Tooltip("Used for all card playback.")]
    public AudioSource audioSource;

    // scheduled plays tracked by ID so they can be cancelled
    Dictionary<int, Coroutine> scheduled = new Dictionary<int, Coroutine>();
    int nextId = 1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // comment out if you want scene-local manager

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    /// <summary>Play clip immediately (one-shot)</summary>
    public void PlayImmediate(AudioClip clip, float volume = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, volume);
        Debug.Log($"[AudioManager] PlayImmediate: {clip.name}");
    }

    /// <summary>Schedule clip to play after delay. Returns an int id you can use to cancel.</summary>
    public int PlayDelayed(AudioClip clip, float delay, float volume = 1f)
    {
        if (clip == null || audioSource == null) return -1;
        int id = nextId++;
        Coroutine c = StartCoroutine(DelayedPlayCoroutine(id, clip, delay, volume));
        scheduled[id] = c;
        Debug.Log($"[AudioManager] PlayDelayed scheduled id={id} clip={clip.name} delay={delay}");
        return id;
    }

    IEnumerator DelayedPlayCoroutine(int id, AudioClip clip, float delay, float volume)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // make sure still scheduled (might have been cancelled)
        if (!scheduled.ContainsKey(id)) yield break;

        audioSource.PlayOneShot(clip, volume);
        Debug.Log($"[AudioManager] Delayed Play executed id={id} clip={clip.name}");

        // cleanup
        if (scheduled.ContainsKey(id)) scheduled.Remove(id);
    }

    /// <summary>Cancel a scheduled play by ID.</summary>
    public void CancelScheduled(int id)
    {
        if (scheduled.TryGetValue(id, out Coroutine c))
        {
            if (c != null) StopCoroutine(c);
            scheduled.Remove(id);
            Debug.Log($"[AudioManager] CancelScheduled id={id}");
        }
    }

    /// <summary>Cancel all scheduled plays.</summary>
    public void CancelAllScheduledPlays()
    {
        foreach (var kv in new List<KeyValuePair<int, Coroutine>>(scheduled))
        {
            if (kv.Value != null) StopCoroutine(kv.Value);
        }
        scheduled.Clear();
        Debug.Log("[AudioManager] CancelAllScheduledPlays");
    }

    /// <summary>Stop currently playing audio and cancel scheduled plays.</summary>
    public void StopAllCardSounds()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            Debug.Log("[AudioManager] StopAllCardSounds (audioSource.Stop)");
        }
        CancelAllScheduledPlays();
    }
}
