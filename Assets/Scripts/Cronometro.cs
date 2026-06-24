using UnityEngine;
using TMPro;

/// <summary>
/// Cronômetro crescente que atualiza um TMP em tempo real.
/// Coloque este script num GameObject persistente da cena do jogo.
///
/// SETUP NO EDITOR:
/// 1. Arraste o TMP da HUD (ex: "00:00.00") em TextoCronometro.
/// 2. Arraste o TMP dentro do painel de vitória em TextoTempoFinal.
///    Esse texto aparece zerado até o jogo terminar.
/// 3. LevelManager.VitoriaFinal() chama Cronometro.Instance.Parar()
///    que preenche TextoTempoFinal automaticamente.
/// </summary>
public class Cronometro : MonoBehaviour
{
    public static Cronometro Instance { get; private set; }

    [Header("Textos")]
    [Tooltip("TMP na HUD que atualiza a cada frame")]
    public TMP_Text textoCronometro;

    [Tooltip("TMP dentro do painel de vitoria que mostra o tempo final")]
    public TMP_Text textoTempoFinal;

    [Header("Configuracao")]
    [Tooltip("Inicia automaticamente ao entrar na cena")]
    public bool iniciarAutomaticamente = true;

    public float TempoDecorrido { get; private set; }
    public bool Rodando { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (iniciarAutomaticamente)
            Iniciar();
    }

    void Update()
    {
        if (!Rodando) return;

        TempoDecorrido += Time.deltaTime;

        if (textoCronometro != null)
            textoCronometro.text = Formatar(TempoDecorrido);
    }

    // ── API PÚBLICA ──────────────────────────────────────────────────

    public void Iniciar()
    {
        TempoDecorrido = 0f;
        Rodando = true;
    }

    public void Pausar()
    {
        Rodando = false;
    }

    public void Retomar()
    {
        Rodando = true;
    }

    /// <summary>
    /// Para o cronômetro e exibe o tempo final no painel de vitória.
    /// Retorna o tempo total em segundos caso queira usá-lo em outro lugar.
    /// </summary>
    public float Parar()
    {
        Rodando = false;

        string tempoFormatado = Formatar(TempoDecorrido);

        if (textoCronometro != null)
            textoCronometro.text = tempoFormatado;

        if (textoTempoFinal != null)
            textoTempoFinal.text = tempoFormatado;

        return TempoDecorrido;
    }

    // ── HELPERS ──────────────────────────────────────────────────────

    /// <summary>Formata segundos como mm:ss.ff (ex: 01:23.45)</summary>
    public static string Formatar(float segundos)
    {
        int minutos = (int)(segundos / 60f);
        int segs = (int)(segundos % 60f);
        int centi = (int)((segundos - Mathf.Floor(segundos)) * 100f);

        return $"{minutos:D2}:{segs:D2}.{centi:D2}";
    }
}