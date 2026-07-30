using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Ejecuta un cierre reutilizable de nivel: golpe final, espera, fundido,
/// texto y carga de una escena. Puede usar una UI asignada o crear una
/// presentación funcional automáticamente.
/// </summary>
[DisallowMultipleComponent]
public class SecuenciaFinalNivel : MonoBehaviour
{
    [Header("--- Activación ---")]
    [SerializeField] private bool activa = true;

    [Header("--- Contenido ---")]
    [SerializeField] private string textoFinal = "Continuará...";
    [SerializeField] private string escenaDestino = "MenuPrincipal";

    [Header("--- Ritmo ---")]
    [SerializeField, Min(0f)] private float pausaImpacto = 0.1f;
    [SerializeField, Min(0f)] private float esperaTrasVictoria = 1.5f;
    [SerializeField, Min(0.01f)] private float duracionFundido = 1.5f;
    [SerializeField, Min(0f)] private float permanenciaTexto = 2f;

    [Header("--- Presentación ---")]
    [SerializeField] private bool fundirAudio = true;
    [SerializeField] private Color colorFondo = Color.black;
    [SerializeField] private Color colorTexto = Color.white;
    [SerializeField, Min(10f)] private float tamanoTexto = 54f;
    [SerializeField, Range(0.8f, 1f)] private float escalaInicialTexto = 0.97f;
    [SerializeField] private int ordenCanvas = 32000;

    [Header("--- UI Personalizada Opcional ---")]
    [Tooltip("Si queda vacío, se crea automáticamente un Canvas de pantalla completa.")]
    [SerializeField] private CanvasGroup grupoFundido;
    [SerializeField] private TextMeshProUGUI etiquetaFinal;

    private bool ejecutandose;
    private AudioSource[] fuentesAudio;
    private float[] volumenesOriginales;
    private MenuPausa[] menusPausa;
    private Vector3 escalaObjetivoTexto = Vector3.one;

    public bool Activa => activa;
    public bool EstaEjecutandose => ejecutandose;

    /// <summary>
    /// Inicia el final una sola vez. Devuelve false si está desactivado
    /// o si ya había comenzado.
    /// </summary>
    public bool Iniciar(Jugador jugador, ControladorArmas armas)
    {
        if (!activa || ejecutandose) return false;

        ejecutandose = true;
        StartCoroutine(EjecutarSecuencia(jugador, armas));
        return true;
    }

    private IEnumerator EjecutarSecuencia(Jugador jugador, ControladorArmas armas)
    {
        Time.timeScale = 1f;
        PrepararInterfaz();
        BloquearPausa();

        if (jugador != null)
            jugador.EstablecerInvulnerabilidadCinematica(true);

        if (armas != null)
            armas.puedeAtacar = false;

        if (pausaImpacto > 0f)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(pausaImpacto);
            Time.timeScale = 1f;
        }

        if (esperaTrasVictoria > 0f)
            yield return new WaitForSecondsRealtime(esperaTrasVictoria);

        if (jugador != null)
            jugador.CongelarCinematica();

        CapturarFuentesAudio();
        yield return FundirAPantallaNegra();

        if (permanenciaTexto > 0f)
            yield return new WaitForSecondsRealtime(permanenciaTexto);

        yield return CargarEscenaDestino(jugador, armas);
    }

    private void PrepararInterfaz()
    {
        if (grupoFundido == null)
            CrearInterfazAutomatica();

        if (grupoFundido == null)
        {
            Debug.LogError("[FINAL NIVEL] No se pudo crear ni encontrar la interfaz de fundido.");
            return;
        }

        grupoFundido.alpha = 0f;
        grupoFundido.interactable = false;
        grupoFundido.blocksRaycasts = true;
        grupoFundido.gameObject.SetActive(true);

        if (etiquetaFinal != null)
        {
            etiquetaFinal.text = textoFinal;
            etiquetaFinal.color = colorTexto;
            etiquetaFinal.fontSize = tamanoTexto;
            escalaObjetivoTexto = etiquetaFinal.rectTransform.localScale;
            etiquetaFinal.rectTransform.localScale =
                escalaObjetivoTexto * escalaInicialTexto;
        }
    }

    private void CrearInterfazAutomatica()
    {
        GameObject raiz = new GameObject(
            "Canvas_FinalTemporal",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(CanvasGroup));

        Canvas canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ordenCanvas;

        CanvasScaler escalador = raiz.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;

        grupoFundido = raiz.GetComponent<CanvasGroup>();

        GameObject fondo = new GameObject(
            "Fondo_Negro",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        fondo.transform.SetParent(raiz.transform, false);
        RectTransform rectFondo = fondo.GetComponent<RectTransform>();
        EstirarAPantallaCompleta(rectFondo);
        Image imagenFondo = fondo.GetComponent<Image>();
        imagenFondo.color = colorFondo;
        imagenFondo.raycastTarget = false;

        GameObject texto = new GameObject(
            "Texto_Continuara",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        texto.transform.SetParent(raiz.transform, false);
        RectTransform rectTexto = texto.GetComponent<RectTransform>();
        EstirarAPantallaCompleta(rectTexto);
        rectTexto.offsetMin = new Vector2(80f, 80f);
        rectTexto.offsetMax = new Vector2(-80f, -80f);

        etiquetaFinal = texto.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            etiquetaFinal.font = TMP_Settings.defaultFontAsset;

        etiquetaFinal.alignment = TextAlignmentOptions.Center;
        etiquetaFinal.enableWordWrapping = false;
        etiquetaFinal.raycastTarget = false;
    }

    private static void EstirarAPantallaCompleta(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private void BloquearPausa()
    {
        menusPausa = FindObjectsOfType<MenuPausa>(true);
        foreach (MenuPausa menu in menusPausa)
        {
            if (menu != null)
                menu.enabled = false;
        }

        Time.timeScale = 1f;
    }

    private void CapturarFuentesAudio()
    {
        if (!fundirAudio) return;

        fuentesAudio = FindObjectsOfType<AudioSource>(true);
        volumenesOriginales = new float[fuentesAudio.Length];

        for (int i = 0; i < fuentesAudio.Length; i++)
        {
            if (fuentesAudio[i] != null)
                volumenesOriginales[i] = fuentesAudio[i].volume;
        }
    }

    private IEnumerator FundirAPantallaNegra()
    {
        float transcurrido = 0f;
        float duracion = Mathf.Max(0.01f, duracionFundido);

        while (transcurrido < duracion)
        {
            transcurrido += Time.unscaledDeltaTime;
            float progreso = Mathf.Clamp01(transcurrido / duracion);
            float progresoSuave = Mathf.SmoothStep(0f, 1f, progreso);

            if (grupoFundido != null)
                grupoFundido.alpha = progresoSuave;

            if (etiquetaFinal != null)
            {
                etiquetaFinal.rectTransform.localScale = Vector3.LerpUnclamped(
                    escalaObjetivoTexto * escalaInicialTexto,
                    escalaObjetivoTexto,
                    progresoSuave);
            }

            AplicarFundidoAudio(progresoSuave);
            yield return null;
        }

        if (grupoFundido != null)
            grupoFundido.alpha = 1f;

        if (etiquetaFinal != null)
            etiquetaFinal.rectTransform.localScale = escalaObjetivoTexto;

        AplicarFundidoAudio(1f);
    }

    private void AplicarFundidoAudio(float progreso)
    {
        if (!fundirAudio || fuentesAudio == null || volumenesOriginales == null)
            return;

        for (int i = 0; i < fuentesAudio.Length; i++)
        {
            if (fuentesAudio[i] != null)
                fuentesAudio[i].volume = volumenesOriginales[i] * (1f - progreso);
        }
    }

    private IEnumerator CargarEscenaDestino(Jugador jugador, ControladorArmas armas)
    {
        if (string.IsNullOrWhiteSpace(escenaDestino) ||
            !Application.CanStreamedLevelBeLoaded(escenaDestino))
        {
            Debug.LogError(
                $"[FINAL NIVEL] La escena '{escenaDestino}' no existe o no está en Build Settings.");
            RestaurarTrasFallo(jugador, armas);
            yield break;
        }

        DetenerYRestaurarFuentesAudio();
        Time.timeScale = 1f;

        AsyncOperation carga = SceneManager.LoadSceneAsync(escenaDestino);
        if (carga == null)
        {
            Debug.LogError($"[FINAL NIVEL] Unity no pudo iniciar la carga de '{escenaDestino}'.");
            RestaurarTrasFallo(jugador, armas);
            yield break;
        }

        while (!carga.isDone)
            yield return null;
    }

    private void DetenerYRestaurarFuentesAudio()
    {
        if (fuentesAudio == null || volumenesOriginales == null) return;

        for (int i = 0; i < fuentesAudio.Length; i++)
        {
            if (fuentesAudio[i] == null) continue;

            fuentesAudio[i].Stop();
            fuentesAudio[i].volume = volumenesOriginales[i];
        }
    }

    private void RestaurarTrasFallo(Jugador jugador, ControladorArmas armas)
    {
        DetenerYRestaurarFuentesAudio();
        Time.timeScale = 1f;

        if (grupoFundido != null)
            grupoFundido.alpha = 0f;

        if (jugador != null)
        {
            jugador.EstablecerInvulnerabilidadCinematica(false);
            jugador.DescongelarCinematica();
        }

        if (armas != null)
            armas.puedeAtacar = true;

        if (menusPausa != null)
        {
            foreach (MenuPausa menu in menusPausa)
            {
                if (menu != null)
                    menu.enabled = true;
            }
        }

        ejecutandose = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        pausaImpacto = Mathf.Max(0f, pausaImpacto);
        esperaTrasVictoria = Mathf.Max(0f, esperaTrasVictoria);
        duracionFundido = Mathf.Max(0.01f, duracionFundido);
        permanenciaTexto = Mathf.Max(0f, permanenciaTexto);
        tamanoTexto = Mathf.Max(10f, tamanoTexto);
        escalaInicialTexto = Mathf.Clamp(escalaInicialTexto, 0.8f, 1f);
    }
#endif
}
