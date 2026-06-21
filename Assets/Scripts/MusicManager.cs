using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField] private AudioSource audioSource;

    private Coroutine fadeRoutine;
    private float defaultVolume;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            defaultVolume = audioSource.volume;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMusic(AudioClip clip, float fadeTime = 1f)
    {
        if (clip == null)
            return;

        // Same music? Keep playing.
        if (audioSource.clip == clip && audioSource.isPlaying)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(ChangeMusicRoutine(clip, fadeTime));
    }

    IEnumerator ChangeMusicRoutine(AudioClip clip, float fadeTime)
    {
        if (audioSource.isPlaying)
            yield return FadeOut(fadeTime);

        audioSource.clip = clip;
        audioSource.Play();

        yield return FadeIn(fadeTime);
    }

    public Coroutine FadeOutMusic(float duration)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeOut(duration));
        return fadeRoutine;
    }

    public Coroutine FadeInMusic(float duration)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeIn(duration));
        return fadeRoutine;
    }

    public IEnumerator FadeOut(float duration)
    {
        float startVolume = audioSource.volume;

        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / duration;
            yield return null;
        }

        audioSource.volume = 0;
        audioSource.Stop();
    }

    public IEnumerator FadeIn(float duration)
    {
        audioSource.volume = 0;

        if (!audioSource.isPlaying)
            audioSource.Play();

        while (audioSource.volume < defaultVolume)
        {
            audioSource.volume += defaultVolume * Time.deltaTime / duration;
            yield return null;
        }

        audioSource.volume = defaultVolume;
    }
}