using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Conserva el nivel desde el que murió el jugador durante el cambio de escena.
/// </summary>
public static class ContextoDerrota
{
    private static string nivelOrigen;

    public static string NivelOrigen => nivelOrigen;

    public static void RegistrarNivelOrigen(string nombreEscena)
    {
        if (!string.IsNullOrWhiteSpace(nombreEscena) &&
            nombreEscena != "MenuDerrota")
        {
            nivelOrigen = nombreEscena;
        }
    }
}

/// <summary>
/// Controla el menú de derrota, sus transiciones y su adaptación a cualquier
/// resolución sin deformar la ilustración original.
/// </summary>
[DisallowMultipleComponent]
public class ControladorMenuDerrota : MonoBehaviour
{
    private static readonly Vector2 ResolucionArte = new Vector2(2842f, 1467f);

    [Header("--- Navegación ---")]
    [SerializeField] private string escenaSelectorNiveles = "SelectorNiveles";

    [Header("--- Transiciones ---")]
    [SerializeField, Min(0.05f)] private float duracionFundido = 0.45f;

    private Canvas canvas;
    private Image fondo;
    private Button botonReiniciar;
    private Button botonVolver;
    private Button botonSalir;
    private FundidoPantalla fundido;
    private bool cambiandoEscena;

    private void Awake()
    {
        Time.timeScale = 1f;
        LocalizarElementos();
        ConfigurarInterfazResponsive();
        ConfigurarBotones();
        AsegurarSistemaEventos();

        fundido = FundidoPantalla.Crear(
            "Transicion_Menu_Derrota",
            32000,
            1f);
    }

    private IEnumerator Start()
    {
        yield return fundido.CambiarOpacidad(0f, duracionFundido);
        fundido.BloquearInteraccion(false);

        if (EventSystem.current != null && botonReiniciar != null)
            EventSystem.current.SetSelectedGameObject(botonReiniciar.gameObject);
    }

    private void Update()
    {
        if (!cambiandoEscena && Input.GetKeyDown(KeyCode.Escape))
            VolverASelector();
    }

    private void LocalizarElementos()
    {
        canvas = FindObjectOfType<Canvas>();
        botonReiniciar = BuscarBoton("BotonReiniciar");
        botonVolver = BuscarBoton("BotonVolver");
        botonSalir = BuscarBoton("BotonSalir");

        if (canvas != null)
        {
            Transform candidato = canvas.transform.Find("Fondo_MenuDerrota");
            if (candidato == null)
                candidato = canvas.transform.Find("Image");
            if (candidato != null) fondo = candidato.GetComponent<Image>();
        }

        if (canvas == null || fondo == null ||
            botonReiniciar == null || botonVolver == null || botonSalir == null)
        {
            Debug.LogError(
                "[MENÚ DERROTA] Faltan el Canvas, el fondo o uno de los botones.");
        }
    }

    private static Button BuscarBoton(string nombre)
    {
        GameObject objeto = GameObject.Find(nombre);
        return objeto != null ? objeto.GetComponent<Button>() : null;
    }

    private void ConfigurarInterfazResponsive()
    {
        if (canvas == null || fondo == null) return;

        canvas.gameObject.name = "Canvas_MenuDerrota";
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler escalador = canvas.GetComponent<CanvasScaler>();
        if (escalador == null)
            escalador = canvas.gameObject.AddComponent<CanvasScaler>();

        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = ResolucionArte;
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;

        CrearFondoParaBandas();

        fondo.gameObject.name = "Fondo_MenuDerrota";
        RectTransform rectFondo = fondo.rectTransform;
        rectFondo.anchorMin = new Vector2(0.5f, 0.5f);
        rectFondo.anchorMax = new Vector2(0.5f, 0.5f);
        rectFondo.pivot = new Vector2(0.5f, 0.5f);
        rectFondo.anchoredPosition = Vector2.zero;
        rectFondo.sizeDelta = ResolucionArte;
        rectFondo.localScale = Vector3.one;

        fondo.preserveAspect = true;
        fondo.raycastTarget = false;

        AspectRatioFitter ajustador = fondo.GetComponent<AspectRatioFitter>();
        if (ajustador == null)
            ajustador = fondo.gameObject.AddComponent<AspectRatioFitter>();

        ajustador.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        ajustador.aspectRatio = ResolucionArte.x / ResolucionArte.y;
    }

    private void CrearFondoParaBandas()
    {
        Transform existente = canvas.transform.Find("Fondo_Bandas");
        if (existente != null) return;

        GameObject objeto = new GameObject(
            "Fondo_Bandas",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        objeto.transform.SetParent(canvas.transform, false);
        objeto.transform.SetAsFirstSibling();

        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image imagen = objeto.GetComponent<Image>();
        imagen.color = new Color(0.015f, 0.018f, 0.028f, 1f);
        imagen.raycastTarget = false;
    }

    private void ConfigurarBotones()
    {
        if (fondo == null) return;

        ConfigurarZona(
            botonReiniciar,
            new Vector2(0.515f, 0.585f),
            new Vector2(0.845f, 0.755f));
        ConfigurarZona(
            botonVolver,
            new Vector2(0.515f, 0.39f),
            new Vector2(0.845f, 0.56f));
        ConfigurarZona(
            botonSalir,
            new Vector2(0.515f, 0.195f),
            new Vector2(0.845f, 0.37f));

        if (botonReiniciar != null)
        {
            botonReiniciar.onClick = new Button.ButtonClickedEvent();
            botonReiniciar.onClick.AddListener(Reiniciar);
        }

        if (botonVolver != null)
        {
            botonVolver.onClick = new Button.ButtonClickedEvent();
            botonVolver.onClick.AddListener(VolverASelector);
        }

        if (botonSalir != null)
        {
            botonSalir.onClick = new Button.ButtonClickedEvent();
            botonSalir.onClick.AddListener(Salir);
        }

        ConfigurarNavegacion();
    }

    private void ConfigurarZona(Button boton, Vector2 anclaMinima, Vector2 anclaMaxima)
    {
        if (boton == null) return;

        boton.transform.SetParent(fondo.transform, false);

        RectTransform rect = boton.GetComponent<RectTransform>();
        rect.anchorMin = anclaMinima;
        rect.anchorMax = anclaMaxima;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        Image imagen = boton.GetComponent<Image>();
        if (imagen == null) imagen = boton.gameObject.AddComponent<Image>();

        imagen.color = new Color(1f, 1f, 1f, 0f);
        imagen.raycastTarget = true;
        imagen.preserveAspect = true;

        boton.targetGraphic = imagen;
        boton.transition = Selectable.Transition.ColorTint;

        ColorBlock colores = ColorBlock.defaultColorBlock;
        colores.normalColor = new Color(1f, 1f, 1f, 0f);
        colores.highlightedColor = new Color(0.35f, 0.9f, 1f, 0.2f);
        colores.pressedColor = new Color(1f, 0.55f, 0.25f, 0.3f);
        colores.selectedColor = new Color(0.35f, 0.9f, 1f, 0.24f);
        colores.disabledColor = new Color(1f, 1f, 1f, 0f);
        colores.fadeDuration = 0.08f;
        boton.colors = colores;

        foreach (Graphic grafico in boton.GetComponentsInChildren<Graphic>(true))
        {
            if (grafico != imagen) grafico.raycastTarget = false;
        }
    }

    private void ConfigurarNavegacion()
    {
        if (botonReiniciar == null || botonVolver == null || botonSalir == null)
            return;

        botonReiniciar.navigation = CrearNavegacion(botonSalir, botonVolver);
        botonVolver.navigation = CrearNavegacion(botonReiniciar, botonSalir);
        botonSalir.navigation = CrearNavegacion(botonVolver, botonReiniciar);
    }

    private static Navigation CrearNavegacion(Selectable arriba, Selectable abajo)
    {
        return new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = arriba,
            selectOnDown = abajo
        };
    }

    private static void AsegurarSistemaEventos()
    {
        if (EventSystem.current != null) return;

        new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));
    }

    public void Reiniciar()
    {
        string nivel = ContextoDerrota.NivelOrigen;
        if (string.IsNullOrWhiteSpace(nivel))
        {
            Debug.LogWarning(
                "[MENÚ DERROTA] No existe un nivel de origen; se volverá al selector.");
            nivel = escenaSelectorNiveles;
        }

        SolicitarCambioEscena(nivel);
    }

    public void VolverASelector()
    {
        SolicitarCambioEscena(escenaSelectorNiveles);
    }

    public void Salir()
    {
        if (cambiandoEscena) return;
        StartCoroutine(SecuenciaSalir());
    }

    private void SolicitarCambioEscena(string nombreEscena)
    {
        if (cambiandoEscena) return;

        if (!Application.CanStreamedLevelBeLoaded(nombreEscena))
        {
            Debug.LogError(
                $"[MENÚ DERROTA] La escena '{nombreEscena}' no está disponible.");
            return;
        }

        StartCoroutine(CambiarEscena(nombreEscena));
    }

    private IEnumerator CambiarEscena(string nombreEscena)
    {
        cambiandoEscena = true;
        CambiarInteraccion(false);
        fundido.BloquearInteraccion(true);
        yield return fundido.CambiarOpacidad(1f, duracionFundido);

        Time.timeScale = 1f;
        AsyncOperation carga = SceneManager.LoadSceneAsync(nombreEscena);

        if (carga == null)
        {
            Debug.LogError(
                $"[MENÚ DERROTA] No se pudo cargar la escena '{nombreEscena}'.");
            cambiandoEscena = false;
            CambiarInteraccion(true);
            yield return fundido.CambiarOpacidad(0f, duracionFundido);
            fundido.BloquearInteraccion(false);
        }
    }

    private IEnumerator SecuenciaSalir()
    {
        cambiandoEscena = true;
        CambiarInteraccion(false);
        fundido.BloquearInteraccion(true);
        yield return fundido.CambiarOpacidad(1f, duracionFundido);

        Debug.Log("[MENÚ DERROTA] Saliendo del juego...");
        Application.Quit();

#if UNITY_EDITOR
        cambiandoEscena = false;
        CambiarInteraccion(true);
        yield return fundido.CambiarOpacidad(0f, duracionFundido);
        fundido.BloquearInteraccion(false);
#endif
    }

    private void CambiarInteraccion(bool activa)
    {
        if (botonReiniciar != null) botonReiniciar.interactable = activa;
        if (botonVolver != null) botonVolver.interactable = activa;
        if (botonSalir != null) botonSalir.interactable = activa;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        duracionFundido = Mathf.Max(0.05f, duracionFundido);
    }
#endif
}
