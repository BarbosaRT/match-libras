using UnityEngine;

/// <summary>
/// Coloque este script no mesmo GameObject do Button (ou em qualquer objeto
/// que o botão referencie no OnClick()). Ele resolve SoundManager.Instance /
/// MusicManager.Instance EM TEMPO DE EXECUÇÃO, em vez de depender de uma
/// referência fixa arrastada no Inspector.
///
/// POR QUE ISSO É NECESSÁRIO:
/// SoundManager e MusicManager são singletons com DontDestroyOnLoad. Quando
/// uma cena (ex: Menu) é recarregada, uma NOVA instância desses managers é
/// criada a partir do arquivo da cena, percebe que já existe uma instância
/// persistida, e se autodestrói no Awake(). Se um botão estiver ligado
/// diretamente a essa instância "nova" (a que acabou de se destruir), a
/// referência fica inválida e o clique não faz nada.
///
/// Este script nunca é destruído por esse motivo (não é singleton), então a
/// referência do botão a ELE continua válida entre recarregamentos de cena -
/// e ele sempre busca o Instance vivo no momento do clique.
///
/// SETUP NO EDITOR:
/// 1. Adicione este componente no GameObject do botão.
/// 2. Defina "Som Id" (ex: "Click") e/ou marque "Tocar Musica Inicial".
/// 3. No Button -> On Click(), arraste ESTE GameObject e escolha
///    UIAudioButton -> TocarSom() (ou TocarMusica(), PausarMusica(), etc).
/// </summary>
public class UIAudioButton : MonoBehaviour
{
    [Header("Efeito Sonoro (SoundManager)")]
    [Tooltip("Id registrado na lista 'Sons' do SoundManager, ex: 'Click'")]
    public string somId = "Click";

    [Header("Música (MusicManager)")]
    public AudioClip musicaParaTocar;
    public float fadeMusica = 1f;

    /// <summary>Toca o efeito sonoro configurado em somId via SoundManager.</summary>
    public void TocarSom()
    {
        SoundManager.Instance?.Play(somId);
    }

    /// <summary>Toca o efeito sonoro indicado, ignorando o campo somId.</summary>
    public void TocarSom(string id)
    {
        SoundManager.Instance?.Play(id);
    }

    /// <summary>Toca/troca a música configurada em musicaParaTocar via MusicManager.</summary>
    public void TocarMusica()
    {
        if (musicaParaTocar == null) return;
        MusicManager.Instance?.PlayMusic(musicaParaTocar, fadeMusica);
    }

    /// <summary>Para a música atual com fade-out.</summary>
    public void PausarMusica()
    {
        MusicManager.Instance?.StopMusic(fadeMusica);
    }
}