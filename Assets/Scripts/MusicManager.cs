using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Persistent background music manager with fade in/out support, driven through an
/// AudioMixer exposed parameter (in dB) instead of AudioSource.volume.
///
/// SETUP NO EDITOR:
/// 1. Crie um AudioMixer asset, adicione um grupo filho "Music" dentro de "Master".
/// 2. No grupo "Music", clique direito no slider de Volume -> "Expose to script".
/// 3. Na aba "Exposed Parameters" do Mixer, renomeie o parâmetro para "MusicVolume"
///    (ou o nome que você colocar em `parametroVolume` abaixo).
/// 4. No AudioSource deste GameObject, defina Output = grupo "Music".
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;
    public string parametroVolume = "Volume";

    [Header("Configuração Padrão")]
    public AudioClip musicaInicial;
    public bool tocarAoIniciar = true;
    [Range(0f, 1f)] public float volumeMaximo = 1f; // linear 0-1, convertido para dB internamente
    public float duracaoFadeDefault = 1.5f;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    private const float VOLUME_MINIMO_DB = -80f; // silêncio efetivo

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        // Garante silêncio no mixer ao iniciar, para o fade-in funcionar corretamente
        audioMixer.SetFloat(parametroVolume, VOLUME_MINIMO_DB);
    }

    void Start()
    {
        // 1. Lê o volume salvo (ou 1f se for a primeira vez)
        volumeMaximo = PlayerPrefs.GetFloat("VolumeMusica", 1f);

        if (tocarAoIniciar && musicaInicial != null)
        {
            // O fade já vai usar o 'volumeMaximo' atualizado automaticamente
            PlayMusic(musicaInicial, duracaoFadeDefault);
        }
        else
        {
            // Se não for tocar música no início, já aplica o volume correto no Mixer
            audioMixer.SetFloat(parametroVolume, LinearParaDb(volumeMaximo));
        }
    }

    // ── API PÚBLICA ──────────────────────────────────────────────────

    /// <summary>
    /// Toca uma música com fade-in. Se já estiver tocando a mesma música, não faz nada.
    /// </summary>
    /// 


    public void PlayMusic(AudioClip clip, float fadeInDuration = -1f)
    {
        if (clip == null) return;
        if (audioSource.clip == clip && audioSource.isPlaying) return;

        if (fadeInDuration < 0f) fadeInDuration = duracaoFadeDefault;

        audioSource.clip = clip;
        audioSource.Play();
        IniciarFade(volumeMaximo, fadeInDuration);
    }

    /// <summary>
    /// Faz fade-out da música atual e a para no final.
    /// </summary>
    public void StopMusic(float fadeOutDuration = -1f)
    {
        if (fadeOutDuration < 0f) fadeOutDuration = duracaoFadeDefault;
        IniciarFade(0f, fadeOutDuration, pararAoFinalizar: true);
    }

    /// Faz fade-out da música atual e, ao terminar, faz fade-in da nova música.
    /// Use isso para trocar de faixa entre cenas (ex: menu -> fase).
    public void SwitchMusic(AudioClip novaMusica, float fadeOutDuration = -1f, float fadeInDuration = -1f)
    {
        if (fadeOutDuration < 0f) fadeOutDuration = duracaoFadeDefault;
        if (fadeInDuration < 0f) fadeInDuration = duracaoFadeDefault;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(TrocarMusicaCoroutine(novaMusica, fadeOutDuration, fadeInDuration));
    }

    /// Ajusta o volume gradualmente (0-1 linear) sem trocar de música ou pará-la.
    /// Útil para um slider de volume nas configurações.
    public void SetVolume(float volumeAlvo, float duracao = -1f)
    {
        if (duracao < 0f) duracao = duracaoFadeDefault;
        volumeMaximo = volumeAlvo;
        IniciarFade(Mathf.Clamp01(volumeAlvo), duracao);
    }
    public void SetVolumeSlider(float volumeAlvo)
    {
        volumeMaximo = volumeAlvo;
        IniciarFade(Mathf.Clamp01(volumeAlvo), 0);
    }

    // ── INTERNO ──────────────────────────────────────────────────────

    private void IniciarFade(float volumeLinearAlvo, float duracao, bool pararAoFinalizar = false)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCoroutine(volumeLinearAlvo, duracao, pararAoFinalizar));
    }

    private IEnumerator FadeCoroutine(float volumeLinearAlvo, float duracao, bool pararAoFinalizar)
    {
        audioMixer.GetFloat(parametroVolume, out float dbAtual);
        float volumeLinearAtual = DbParaLinear(dbAtual);
        float tempo = 0f;

        if (duracao <= 0f)
        {
            AplicarVolume(volumeLinearAlvo);
        }
        else
        {
            while (tempo < duracao)
            {
                tempo += Time.unscaledDeltaTime; // funciona mesmo com Time.timeScale = 0
                float volumeLinear = Mathf.Lerp(volumeLinearAtual, volumeLinearAlvo, tempo / duracao);
                AplicarVolume(volumeLinear);
                yield return null;
            }
            AplicarVolume(volumeLinearAlvo);
        }

        if (pararAoFinalizar && volumeLinearAlvo <= 0f)
            audioSource.Stop();

        fadeCoroutine = null;
    }

    private IEnumerator TrocarMusicaCoroutine(AudioClip novaMusica, float fadeOutDuration, float fadeInDuration)
    {
        if (audioSource.isPlaying)
        {
            audioMixer.GetFloat(parametroVolume, out float dbAtual);
            float volumeInicial = DbParaLinear(dbAtual);
            float tempo = 0f;
            while (tempo < fadeOutDuration)
            {
                tempo += Time.unscaledDeltaTime;
                AplicarVolume(Mathf.Lerp(volumeInicial, 0f, tempo / fadeOutDuration));
                yield return null;
            }
            AplicarVolume(0f);
            audioSource.Stop();
        }

        if (novaMusica != null)
        {
            audioSource.clip = novaMusica;
            audioSource.Play();

            float tempo2 = 0f;
            while (tempo2 < fadeInDuration)
            {
                tempo2 += Time.unscaledDeltaTime;
                AplicarVolume(Mathf.Lerp(0f, volumeMaximo, tempo2 / fadeInDuration));
                yield return null;
            }
            AplicarVolume(volumeMaximo);
        }

        fadeCoroutine = null;
    }

    private void AplicarVolume(float volumeLinear01)
    {
        audioMixer.SetFloat(parametroVolume, LinearParaDb(volumeLinear01));
    }

    // dB e amplitude linear não são a mesma escala: o ouvido humano percebe volume
    // de forma logarítmica, então convertendo assim o fade soa suave e natural.
    private float LinearParaDb(float volumeLinear)
    {
        return volumeLinear > 0.0001f ? 20f * Mathf.Log10(volumeLinear) : VOLUME_MINIMO_DB;
    }

    private float DbParaLinear(float db)
    {
        return Mathf.Pow(10f, db / 20f);
    }
}