using System.Collections.Generic;
using UnityEngine;

public class PecaFisica : MonoBehaviour
{
    [Header("Fisica")]
    public float atrito = 6f;
    public float velocidadeMaxima = 600f;
    public float restituicao = 0.5f;
    public float raioColisao = 80f;
    public float forcaRepulsao = 500f;

    private RectTransform rt;
    private Canvas canvas;
    private RectTransform areaDeSpawn;
    private Transform containerPecas;
    private Vector2 velocidade;

    private static List<PecaFisica> todasAtivas = new List<PecaFisica>();

    public bool Arrastando { get; private set; } = false;

    public void Inicializar(Canvas canvas, RectTransform area, Transform container = null)
    {
        rt = GetComponent<RectTransform>();
        this.canvas = canvas;
        this.areaDeSpawn = area;
        this.containerPecas = container != null ? container : canvas.transform;
        todasAtivas.Add(this);
    }

    public void AplicarImpulso(Vector2 impulso)
    {
        if (Arrastando) return;
        velocidade += impulso;
        velocidade = Vector2.ClampMagnitude(velocidade, velocidadeMaxima);
    }

    public void PausarFisica()
    {
        Arrastando = true;
        velocidade = Vector2.zero;
    }

    public void RetomarFisica(Vector2 velocidadeInicial)
    {
        Arrastando = false;
        velocidade = Vector2.ClampMagnitude(velocidadeInicial, velocidadeMaxima);
    }

    private void Update()
    {
        if (Arrastando || rt == null) return;
        if (transform.parent != containerPecas) return;

        // repulsao entre pecas (todas no mesmo container, anchoredPosition comparavel)
        foreach (var outra in todasAtivas)
        {
            // REMOVIDO: '|| outra.Arrastando'.
            // Agora, se a 'outra' peça estiver sendo arrastada pelo mouse, a peça solta 
            // ainda vai calcular a distância e ser empurrada para longe dela!
            if (outra == this || outra == null) continue;

            if (outra.rt == null) continue;

            Vector2 delta = rt.anchoredPosition - outra.rt.anchoredPosition;
            float dist = delta.magnitude;

            if (dist < raioColisao && dist > 0.01f)
            {
                float overlap = raioColisao - dist;
                Vector2 empurrao = delta.normalized * overlap * forcaRepulsao * Time.deltaTime;
                velocidade += empurrao;
            }
        }

        rt.anchoredPosition += velocidade * Time.deltaTime;

        ColidiComParedes();

        velocidade = Vector2.Lerp(velocidade, Vector2.zero, atrito * Time.deltaTime);

        if (velocidade.magnitude < 0.5f)
        {
            velocidade = Vector2.zero;
            GetComponent<DragDrop>()?.DefinirPosicaoOriginal();
        }
    }

    private void ColidiComParedes()
    {
        if (areaDeSpawn == null) return;

        // Usa rt.position (espaco de mundo) em vez de anchoredPosition tratado como
        // coordenada de canvas — assim funciona independente de qual parent a peca esta.
        Vector3 worldPos = rt.position;
        Vector3 localPos = areaDeSpawn.InverseTransformPoint(worldPos);
        Rect rect = areaDeSpawn.rect;
        bool colidiu = false;

        if (localPos.x < rect.xMin) { localPos.x = rect.xMin; velocidade.x = Mathf.Abs(velocidade.x) * restituicao; colidiu = true; }
        if (localPos.x > rect.xMax) { localPos.x = rect.xMax; velocidade.x = -Mathf.Abs(velocidade.x) * restituicao; colidiu = true; }
        if (localPos.y < rect.yMin) { localPos.y = rect.yMin; velocidade.y = Mathf.Abs(velocidade.y) * restituicao; colidiu = true; }
        if (localPos.y > rect.yMax) { localPos.y = rect.yMax; velocidade.y = -Mathf.Abs(velocidade.y) * restituicao; colidiu = true; }

        if (colidiu)
        {
            Vector3 clampedWorld = areaDeSpawn.TransformPoint(localPos);
            // Converte de volta para anchoredPosition relativo ao parent ATUAL da peca
            RectTransform parentRT = rt.parent as RectTransform;
            if (parentRT != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRT,
                    RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, clampedWorld),
                    canvas.worldCamera,
                    out Vector2 localAnchoredPos
                );
                rt.anchoredPosition = localAnchoredPos;
            }
        }
    }

    private void OnDestroy()
    {
        todasAtivas.Remove(this);
    }
}