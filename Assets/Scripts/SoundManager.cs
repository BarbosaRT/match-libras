using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Gerenciador central de efeitos sonoros. Qualquer script chama
/// SoundManager.Instance.Play("nome_do_som") para tocar um som nomeado.
///
/// SETUP NO EDITOR:
/// 1. Coloque este script num GameObject persistente (igual o MusicManager).
/// 2. No AudioSource deste GameObject, defina Output = grupo "SFX" do seu
///    AudioMixer (o mesmo que você expôs o parâmetro "SFXVolume").
/// 3. Na lista "Sons" do Inspector, adicione uma entrada por ação: dê um id
///    (ex: "Acerto", "Erro", "PerderVida") e arraste o AudioClip correspondente.
/// 4. Em qualquer outro script: SoundManager.Instance?.Play("Acerto");
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [System.Serializable]
    public class SomEntry
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Variação aleatória de pitch (0 = sempre igual). Ex: 0.1 evita som repetitivo em cliques.")]
        [Range(0f, 0.3f)] public float variacaoPitch = 0f;
    }

    [Header("Sons")]
    public List<SomEntry> sons = new List<SomEntry>();

    public AudioSource audioSource;
    private Dictionary<string, SomEntry> mapa;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource.playOnAwake = false;

        mapa = new Dictionary<string, SomEntry>();
        foreach (var entry in sons)
        {
            if (string.IsNullOrEmpty(entry.id)) continue;
            if (!mapa.ContainsKey(entry.id))
                mapa.Add(entry.id, entry);
            else
                Debug.LogWarning($"SoundManager: id de som duplicado '{entry.id}', a primeira entrada será usada.");
        }
    }

    /// <summary>
    /// Toca o som com o id indicado. Sons se sobrepõem normalmente
    /// (PlayOneShot), então pode chamar vários ao mesmo tempo sem problema.
    /// </summary>
    public void Play(string id)
    {
        if (mapa == null || !mapa.TryGetValue(id, out var entry) || entry.clip == null)
        {
            Debug.LogWarning($"SoundManager: som '{id}' não encontrado ou sem clip atribuído.");
            return;
        }

        float pitchOriginal = audioSource.pitch;
        if (entry.variacaoPitch > 0f)
            audioSource.pitch = 1f + Random.Range(-entry.variacaoPitch, entry.variacaoPitch);

        audioSource.PlayOneShot(entry.clip, entry.volume);

        audioSource.pitch = pitchOriginal;
    }
}