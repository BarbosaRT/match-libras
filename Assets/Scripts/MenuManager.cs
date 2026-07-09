using UnityEngine;

// Coloque este script num GameObject do menu (ex: "MenuManager") e arraste o
// metodo JogarClicado() no OnClick() do botao Play, no Inspector.
public class MenuManager : MonoBehaviour
{
    [Header("Nomes das cenas (precisam bater com o Build Settings)")]
    [SerializeField] private string cenaTutorial = "Scenes/Tutorial";
    [SerializeField] private string cenaJogo = "Scenes/Jogo";

    [Header("Transicao (Tile Flip)")]
    [Tooltip("Arraste o GameObject 'SceneLoader' com o TileFlipTransitionController. O Next Scene dele sera ajustado automaticamente antes de tocar a transicao.")]
    [SerializeField] private TileFlipTransitionController transicao;

    private const string CHAVE_TUTORIAL_JOGADO = "TutorialJogado";

    /// Conecte este metodo no OnClick() do botao Play.
    public void JogarClicado()
    {
        if (transicao == null)
        {
            Debug.LogError("MenuManager: 'Transicao' (TileFlipTransitionController) nao foi atribuido no Inspector.", this);
            return;
        }

        bool jaJogouTutorial = PlayerPrefs.GetInt(CHAVE_TUTORIAL_JOGADO, 0) == 1;
        transicao.nextScene = jaJogouTutorial ? cenaJogo : cenaTutorial;
        transicao.PlayTransition();
    }

    // Util para testar o menu de novo sem precisar apagar o PlayerPrefs manualmente.
    [ContextMenu("Resetar progresso do tutorial")]
    public void ResetarTutorial()
    {
        PlayerPrefs.DeleteKey(CHAVE_TUTORIAL_JOGADO);
        PlayerPrefs.Save();
    }
}
