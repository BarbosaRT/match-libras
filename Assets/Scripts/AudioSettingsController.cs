using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsController : MonoBehaviour
{
    [Header("Slider de Música (usa o MusicManager existente)")]
    public Slider sliderMusica;

    [Header("Slider de Efeitos Sonoros")]
    public Slider sliderEfeitos;
    public AudioMixer sfxMixer; // pode ser o mesmo asset do MusicManager (outro grupo) ou um AudioMixer separado
    public string sfxParametro = "SFXVolume";

    [Header("Persistęncia (opcional)")]
    public bool salvarPreferencias = true;

    [Header("Imagens para Música")]
    public Image musicaImagem;
    public Sprite musicaOnSprite;
    public Sprite musicaOffSprite;

    [Header("Imagens para SFX")]
    public Image sfxImagem;
    public Sprite sfxOnSprite;
    public Sprite sfxOffSprite;

    private const string PREF_MUSICA = "VolumeMusica";
    private const string PREF_SFX = "VolumeEfeitos";
    private const float VOLUME_MINIMO_DB = -80f;

    void Start()
    {
        float volumeMusica = salvarPreferencias ? PlayerPrefs.GetFloat(PREF_MUSICA, 1f) : 1f;
        float volumeSfx = salvarPreferencias ? PlayerPrefs.GetFloat(PREF_SFX, 1f) : 1f;

        if (sliderMusica != null)
        {
            // SetValueWithoutNotify apenas move a bolinha do slider visualmente,
            // sem disparar o evento que mata a nossa coroutine.
            sliderMusica.SetValueWithoutNotify(volumeMusica);
            sliderMusica.onValueChanged.AddListener(OnMusicaChanged);
        }

        if (sliderEfeitos != null)
        {
            sliderEfeitos.SetValueWithoutNotify(volumeSfx);
            sliderEfeitos.onValueChanged.AddListener(OnEfeitosChanged);
        }

        // DELETADAS AS DUAS LINHAS ABAIXO:
        // Elas estavam matando a transiçăo de música ao carregar a nova cena!
        // OnMusicaChanged(volumeMusica);
        // OnEfeitosChanged(volumeSfx);

        // Atualiza as imagens com os volumes iniciais
        AtualizarImagemMusica(volumeMusica);
        AtualizarImagemSFX(volumeSfx);
    }

    public void OnMusicaChanged(float valorLinear)
    {
        // duracao = 0f -> aplica instantaneamente, sem fade (essencial pro arraste do slider responder em tempo real)
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetVolume(valorLinear, 0f);

        if (salvarPreferencias)
            PlayerPrefs.SetFloat(PREF_MUSICA, valorLinear);

        AtualizarImagemMusica(valorLinear);
    }

    public void OnEfeitosChanged(float valorLinear)
    {
        if (sfxMixer != null)
            sfxMixer.SetFloat(sfxParametro, LinearParaDb(valorLinear));

        if (salvarPreferencias)
            PlayerPrefs.SetFloat(PREF_SFX, valorLinear);

        AtualizarImagemSFX(valorLinear);
    }

    private float LinearParaDb(float volumeLinear)
    {
        return volumeLinear > 0.0001f ? 20f * Mathf.Log10(volumeLinear) : VOLUME_MINIMO_DB;
    }

    private void AtualizarImagemMusica(float volume)
    {
        if (musicaImagem != null)
        {
            // Considera que o volume é zero quando é menor ou igual a 0.0001f
            bool isMuted = volume <= 0.0001f;
            musicaImagem.sprite = isMuted ? musicaOffSprite : musicaOnSprite;
        }
    }

    private void AtualizarImagemSFX(float volume)
    {
        if (sfxImagem != null)
        {
            bool isMuted = volume <= 0.0001f;
            sfxImagem.sprite = isMuted ? sfxOffSprite : sfxOnSprite;
        }
    }
}