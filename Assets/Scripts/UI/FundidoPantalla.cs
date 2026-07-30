using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crea una cortina negra independiente de la resolución para transiciones
/// entre escenas. Usa tiempo no escalado para funcionar incluso si el juego
/// estaba pausado.
/// </summary>
[DisallowMultipleComponent]
public class FundidoPantalla : MonoBehaviour
{
    private CanvasGroup grupo;

    public float Opacidad
    {
        get => grupo != null ? grupo.alpha : 0f;
        set
        {
            if (grupo != null) grupo.alpha = Mathf.Clamp01(value);
        }
    }

    public static FundidoPantalla Crear(
        string nombre,
        int orden,
        float opacidadInicial,
        bool bloquearInteraccion = true)
    {
        GameObject raiz = new GameObject(
            nombre,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup),
            typeof(FundidoPantalla));

        Canvas canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = orden;

        CanvasScaler escalador = raiz.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;

        GameObject cortina = new GameObject(
            "Cortina_Negra",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        cortina.transform.SetParent(raiz.transform, false);

        RectTransform rect = cortina.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image imagen = cortina.GetComponent<Image>();
        imagen.color = Color.black;
        imagen.raycastTarget = bloquearInteraccion;

        FundidoPantalla fundido = raiz.GetComponent<FundidoPantalla>();
        fundido.grupo = raiz.GetComponent<CanvasGroup>();
        fundido.grupo.alpha = Mathf.Clamp01(opacidadInicial);
        fundido.grupo.interactable = bloquearInteraccion;
        fundido.grupo.blocksRaycasts = bloquearInteraccion;
        return fundido;
    }

    public void BloquearInteraccion(bool bloquear)
    {
        if (grupo == null) grupo = GetComponent<CanvasGroup>();
        if (grupo == null) return;

        grupo.interactable = bloquear;
        grupo.blocksRaycasts = bloquear;
    }

    public IEnumerator CambiarOpacidad(float destino, float duracion)
    {
        if (grupo == null) grupo = GetComponent<CanvasGroup>();
        if (grupo == null) yield break;

        destino = Mathf.Clamp01(destino);
        float origen = grupo.alpha;

        if (duracion <= 0f)
        {
            grupo.alpha = destino;
            yield break;
        }

        float transcurrido = 0f;
        while (transcurrido < duracion)
        {
            transcurrido += Time.unscaledDeltaTime;
            float progreso = Mathf.Clamp01(transcurrido / duracion);
            grupo.alpha = Mathf.SmoothStep(origen, destino, progreso);
            yield return null;
        }

        grupo.alpha = destino;
    }
}
