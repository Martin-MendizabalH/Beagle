using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Construye y controla la presentación completa del menú principal conservando
/// la ilustración original. Todos los elementos adicionales usan UI nativa para
/// adaptarse a distintas resoluciones.
/// </summary>
[DisallowMultipleComponent]
public class ControladorMenuPrincipal : MonoBehaviour
{
    private static readonly Vector2 ResolucionArte = new Vector2(1328f, 768f);

    [Header("--- Navegación ---")]
    [SerializeField] private string escenaSelectorNiveles = "SelectorNiveles";

    [Header("--- Música ---")]
    [SerializeField] private AudioClip musicaMenu;
    [SerializeField, Range(0f, 1f)] private float volumenBaseMusica = 0.65f;

    [Header("--- Sonidos UI Opcionales ---")]
    [SerializeField] private AudioClip sonidoSeleccion;
    [SerializeField] private AudioClip sonidoConfirmacion;

    [Header("--- Transiciones ---")]
    [SerializeField, Min(0.05f)] private float duracionFundido = 0.45f;

    private Canvas canvasPrincipal;
    private Image fondoMenu;
    private Button botonJugar;
    private Button botonOpciones;
    private Button botonSalir;
    private EventSystem sistemaEventos;

    private GameObject panelOpciones;
    private GameObject panelConfirmarSalida;
    private Slider sliderMusica;
    private Slider sliderEfectos;
    private TextMeshProUGUI porcentajeMusica;
    private TextMeshProUGUI porcentajeEfectos;
    private CanvasGroup grupoFundido;

    private AudioSource fuenteMusica;
    private AudioSource fuenteInterfaz;
    private TMP_FontAsset fuenteTitulos;
    private Sprite spriteInterfaz;
    private Sprite spriteFondoControl;
    private Sprite spriteManejador;

    private bool cambiandoEscena;
    private float multiplicadorFundidoMusica = 1f;

    private void Awake()
    {
        Time.timeScale = 1f;
        CargarRecursosInterfaz();
        LocalizarEscena();
        ConfigurarEscaladoYFondo();
        ConfigurarBotonesPrincipales();
        CrearPanelOpciones();
        CrearPanelConfirmacion();
        CrearFundido();
        ConfigurarAudio();
    }

    private void OnEnable()
    {
        ConfiguracionAudio.AlCambiarMusica += AlCambiarVolumenMusica;
    }

    private void OnDisable()
    {
        ConfiguracionAudio.AlCambiarMusica -= AlCambiarVolumenMusica;
    }

    private void Start()
    {
        if (fuenteMusica != null && musicaMenu != null)
        {
            fuenteMusica.clip = musicaMenu;
            fuenteMusica.Play();
        }

        AplicarVolumenMusica();
        StartCoroutine(FundidoDeEntrada());
    }

    private void Update()
    {
        if (cambiandoEscena || !Input.GetKeyDown(KeyCode.Escape)) return;

        if (panelOpciones != null && panelOpciones.activeSelf)
            CerrarOpciones();
        else if (panelConfirmarSalida != null && panelConfirmarSalida.activeSelf)
            CerrarConfirmacionSalida();
        else
            AbrirConfirmacionSalida();
    }

    private void CargarRecursosInterfaz()
    {
        fuenteTitulos = Resources.Load<TMP_FontAsset>(
            "Fonts & Materials/Bangers SDF");
        if (fuenteTitulos == null)
            fuenteTitulos = TMP_Settings.defaultFontAsset;

        spriteInterfaz = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        spriteFondoControl =
            Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        spriteManejador = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
    }

    private void LocalizarEscena()
    {
        canvasPrincipal = FindObjectOfType<Canvas>();
        sistemaEventos = EventSystem.current;

        fondoMenu = BuscarComponente<Image>("Fondo_MenuPrincipal", "Image");
        botonJugar = BuscarComponente<Button>("Boton_Jugar", "Jugar");
        botonOpciones = BuscarComponente<Button>("Boton_Opciones", "Opciones");
        botonSalir = BuscarComponente<Button>("Boton_Salir", "Salir");

        if (canvasPrincipal == null || fondoMenu == null ||
            botonJugar == null || botonOpciones == null || botonSalir == null)
        {
            Debug.LogError(
                "[MENÚ PRINCIPAL] La escena no contiene su Canvas, fondo o botones base.");
        }
    }

    private static T BuscarComponente<T>(params string[] nombres) where T : Component
    {
        foreach (string nombre in nombres)
        {
            GameObject objeto = GameObject.Find(nombre);
            if (objeto == null) continue;

            T componente = objeto.GetComponent<T>();
            if (componente != null) return componente;
        }

        return null;
    }

    private void ConfigurarEscaladoYFondo()
    {
        if (canvasPrincipal == null || fondoMenu == null) return;

        canvasPrincipal.gameObject.name = "Canvas_MenuPrincipal";

        CanvasScaler escalador = canvasPrincipal.GetComponent<CanvasScaler>();
        if (escalador == null)
            escalador = canvasPrincipal.gameObject.AddComponent<CanvasScaler>();

        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = ResolucionArte;
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;

        RectTransform rectCanvas = canvasPrincipal.GetComponent<RectTransform>();
        GameObject fondoBase = new GameObject(
            "Fondo_Base",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        fondoBase.transform.SetParent(rectCanvas, false);
        fondoBase.transform.SetAsFirstSibling();
        RectTransform rectFondoBase = fondoBase.GetComponent<RectTransform>();
        Estirar(rectFondoBase);
        Image imagenBase = fondoBase.GetComponent<Image>();
        imagenBase.color = new Color(0.035f, 0.047f, 0.1f, 1f);
        imagenBase.raycastTarget = false;

        fondoMenu.gameObject.name = "Fondo_MenuPrincipal";
        fondoMenu.raycastTarget = false;
        fondoMenu.preserveAspect = true;

        RectTransform rectFondo = fondoMenu.rectTransform;
        rectFondo.anchorMin = new Vector2(0.5f, 0.5f);
        rectFondo.anchorMax = new Vector2(0.5f, 0.5f);
        rectFondo.pivot = new Vector2(0.5f, 0.5f);
        rectFondo.anchoredPosition = Vector2.zero;
        rectFondo.sizeDelta = ResolucionArte;

        AspectRatioFitter ajustador = fondoMenu.GetComponent<AspectRatioFitter>();
        if (ajustador == null)
            ajustador = fondoMenu.gameObject.AddComponent<AspectRatioFitter>();

        ajustador.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        ajustador.aspectRatio = ResolucionArte.x / ResolucionArte.y;
    }

    private void ConfigurarBotonesPrincipales()
    {
        if (fondoMenu == null ||
            botonJugar == null || botonOpciones == null || botonSalir == null)
        {
            return;
        }

        ConfigurarBotonSobreArte(
            botonJugar,
            "Boton_Jugar",
            new Vector2(0.55f, 0.66f),
            new Vector2(0.935f, 0.9f));
        ConfigurarBotonSobreArte(
            botonOpciones,
            "Boton_Opciones",
            new Vector2(0.55f, 0.368f),
            new Vector2(0.935f, 0.607f));
        ConfigurarBotonSobreArte(
            botonSalir,
            "Boton_Salir",
            new Vector2(0.55f, 0.075f),
            new Vector2(0.935f, 0.32f));

        botonJugar.onClick = new Button.ButtonClickedEvent();
        botonOpciones.onClick = new Button.ButtonClickedEvent();
        botonSalir.onClick = new Button.ButtonClickedEvent();
        botonJugar.onClick.AddListener(Jugar);
        botonOpciones.onClick.AddListener(Opciones);
        botonSalir.onClick.AddListener(Salir);

        Navigation navegacionJugar = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = botonSalir,
            selectOnDown = botonOpciones
        };
        Navigation navegacionOpciones = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = botonJugar,
            selectOnDown = botonSalir
        };
        Navigation navegacionSalir = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = botonOpciones,
            selectOnDown = botonJugar
        };

        botonJugar.navigation = navegacionJugar;
        botonOpciones.navigation = navegacionOpciones;
        botonSalir.navigation = navegacionSalir;
    }

    private void ConfigurarBotonSobreArte(
        Button boton,
        string nombre,
        Vector2 anclaMinima,
        Vector2 anclaMaxima)
    {
        boton.gameObject.name = nombre;
        boton.transform.SetParent(fondoMenu.transform, false);

        RectTransform rect = boton.GetComponent<RectTransform>();
        rect.anchorMin = anclaMinima;
        rect.anchorMax = anclaMaxima;
        rect.offsetMin = new Vector2(8f, 8f);
        rect.offsetMax = new Vector2(-8f, -8f);
        rect.localScale = Vector3.one;

        Image imagen = boton.GetComponent<Image>();
        if (imagen == null) imagen = boton.gameObject.AddComponent<Image>();
        if (imagen.sprite == null) imagen.sprite = spriteInterfaz;
        imagen.type = Image.Type.Sliced;
        imagen.raycastTarget = true;
        imagen.color = new Color(1f, 1f, 1f, 0f);

        boton.targetGraphic = imagen;
        boton.transition = Selectable.Transition.ColorTint;
        boton.colors = CrearColores(
            new Color(1f, 1f, 1f, 0f),
            new Color(0.15f, 0.85f, 1f, 0.18f),
            new Color(1f, 0.45f, 0.08f, 0.3f),
            new Color(0.15f, 0.85f, 1f, 0.22f));

        RetroalimentacionBotonMenu retroalimentacion =
            boton.GetComponent<RetroalimentacionBotonMenu>();
        if (retroalimentacion == null)
            retroalimentacion = boton.gameObject.AddComponent<RetroalimentacionBotonMenu>();
        retroalimentacion.Configurar(this);
    }

    private static ColorBlock CrearColores(
        Color normal,
        Color resaltado,
        Color presionado,
        Color seleccionado)
    {
        ColorBlock colores = ColorBlock.defaultColorBlock;
        colores.normalColor = normal;
        colores.highlightedColor = resaltado;
        colores.pressedColor = presionado;
        colores.selectedColor = seleccionado;
        colores.disabledColor = new Color(0.25f, 0.3f, 0.35f, 0.08f);
        colores.colorMultiplier = 1f;
        colores.fadeDuration = 0.08f;
        return colores;
    }

    private void CrearPanelOpciones()
    {
        if (canvasPrincipal == null) return;

        panelOpciones = CrearCortina("Panel_Opciones");
        GameObject panel = CrearPanelMetalico(
            "Marco_Opciones",
            panelOpciones.transform,
            new Vector2(620f, 430f));

        CrearRemaches(panel.transform, new Vector2(286f, 190f));

        CrearTexto(
            "Titulo_Opciones",
            panel.transform,
            "OPCIONES",
            new Vector2(0f, 150f),
            new Vector2(480f, 80f),
            58f,
            new Color(1f, 0.58f, 0.08f, 1f));

        CrearTexto(
            "Etiqueta_Musica",
            panel.transform,
            "MÚSICA",
            new Vector2(-180f, 45f),
            new Vector2(190f, 55f),
            36f,
            new Color(1f, 0.69f, 0.12f, 1f));
        sliderMusica = CrearSlider(
            "Slider_Musica",
            panel.transform,
            new Vector2(55f, 45f));
        porcentajeMusica = CrearTexto(
            "Porcentaje_Musica",
            panel.transform,
            string.Empty,
            new Vector2(245f, 45f),
            new Vector2(90f, 50f),
            28f,
            new Color(0.35f, 0.9f, 1f, 1f));

        CrearTexto(
            "Etiqueta_Efectos",
            panel.transform,
            "EFECTOS",
            new Vector2(-180f, -55f),
            new Vector2(190f, 55f),
            36f,
            new Color(0.4f, 0.82f, 1f, 1f));
        sliderEfectos = CrearSlider(
            "Slider_Efectos",
            panel.transform,
            new Vector2(55f, -55f));
        porcentajeEfectos = CrearTexto(
            "Porcentaje_Efectos",
            panel.transform,
            string.Empty,
            new Vector2(245f, -55f),
            new Vector2(90f, 50f),
            28f,
            new Color(0.35f, 0.9f, 1f, 1f));

        Button volver = CrearBotonMetalico(
            "Boton_Volver",
            panel.transform,
            "VOLVER",
            new Vector2(0f, -150f),
            new Vector2(230f, 64f),
            new Color(0.36f, 0.8f, 1f, 1f));
        volver.onClick.AddListener(CerrarOpciones);

        Navigation navegacionMusica = sliderMusica.navigation;
        navegacionMusica.mode = Navigation.Mode.Explicit;
        navegacionMusica.selectOnUp = volver;
        navegacionMusica.selectOnDown = sliderEfectos;
        sliderMusica.navigation = navegacionMusica;

        Navigation navegacionEfectos = sliderEfectos.navigation;
        navegacionEfectos.mode = Navigation.Mode.Explicit;
        navegacionEfectos.selectOnUp = sliderMusica;
        navegacionEfectos.selectOnDown = volver;
        sliderEfectos.navigation = navegacionEfectos;

        Navigation navegacionVolver = volver.navigation;
        navegacionVolver.mode = Navigation.Mode.Explicit;
        navegacionVolver.selectOnUp = sliderEfectos;
        navegacionVolver.selectOnDown = sliderMusica;
        volver.navigation = navegacionVolver;

        sliderMusica.SetValueWithoutNotify(ConfiguracionAudio.VolumenMusica);
        sliderEfectos.SetValueWithoutNotify(ConfiguracionAudio.VolumenEfectos);
        sliderMusica.onValueChanged.AddListener(CambiarVolumenMusica);
        sliderEfectos.onValueChanged.AddListener(CambiarVolumenEfectos);
        ActualizarPorcentajes();

        panelOpciones.SetActive(false);
    }

    private void CrearPanelConfirmacion()
    {
        if (canvasPrincipal == null) return;

        panelConfirmarSalida = CrearCortina("Panel_ConfirmarSalida");
        GameObject panel = CrearPanelMetalico(
            "Marco_ConfirmarSalida",
            panelConfirmarSalida.transform,
            new Vector2(590f, 290f));

        CrearRemaches(panel.transform, new Vector2(270f, 120f));
        CrearTexto(
            "Pregunta_Salir",
            panel.transform,
            "¿SALIR DEL JUEGO?",
            new Vector2(0f, 58f),
            new Vector2(500f, 85f),
            48f,
            new Color(1f, 0.66f, 0.1f, 1f));

        Button confirmar = CrearBotonMetalico(
            "Boton_ConfirmarSalir",
            panel.transform,
            "SÍ",
            new Vector2(-125f, -70f),
            new Vector2(190f, 65f),
            new Color(1f, 0.45f, 0.12f, 1f));
        Button cancelar = CrearBotonMetalico(
            "Boton_CancelarSalir",
            panel.transform,
            "NO",
            new Vector2(125f, -70f),
            new Vector2(190f, 65f),
            new Color(0.35f, 0.84f, 1f, 1f));

        confirmar.onClick.AddListener(ConfirmarSalida);
        cancelar.onClick.AddListener(CerrarConfirmacionSalida);

        Navigation navegacionConfirmar = confirmar.navigation;
        navegacionConfirmar.mode = Navigation.Mode.Explicit;
        navegacionConfirmar.selectOnLeft = cancelar;
        navegacionConfirmar.selectOnRight = cancelar;
        confirmar.navigation = navegacionConfirmar;

        Navigation navegacionCancelar = cancelar.navigation;
        navegacionCancelar.mode = Navigation.Mode.Explicit;
        navegacionCancelar.selectOnLeft = confirmar;
        navegacionCancelar.selectOnRight = confirmar;
        cancelar.navigation = navegacionCancelar;

        panelConfirmarSalida.SetActive(false);
    }

    private GameObject CrearCortina(string nombre)
    {
        GameObject cortina = new GameObject(
            nombre,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        cortina.transform.SetParent(canvasPrincipal.transform, false);
        Estirar(cortina.GetComponent<RectTransform>());

        Image imagen = cortina.GetComponent<Image>();
        imagen.color = new Color(0.015f, 0.02f, 0.055f, 0.86f);
        imagen.raycastTarget = true;
        return cortina;
    }

    private GameObject CrearPanelMetalico(
        string nombre,
        Transform padre,
        Vector2 tamano)
    {
        GameObject panel = new GameObject(
            nombre,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline));
        panel.transform.SetParent(padre, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        Posicionar(rect, Vector2.zero, tamano);

        Image imagen = panel.GetComponent<Image>();
        imagen.sprite = spriteInterfaz;
        imagen.type = Image.Type.Sliced;
        imagen.color = new Color(0.3f, 0.38f, 0.44f, 0.98f);

        Outline borde = panel.GetComponent<Outline>();
        borde.effectColor = new Color(0.02f, 0.06f, 0.1f, 1f);
        borde.effectDistance = new Vector2(7f, -7f);

        GameObject interior = new GameObject(
            "Interior_Azul",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline));
        interior.transform.SetParent(panel.transform, false);
        RectTransform rectInterior = interior.GetComponent<RectTransform>();
        Estirar(rectInterior);
        rectInterior.offsetMin = new Vector2(16f, 16f);
        rectInterior.offsetMax = new Vector2(-16f, -16f);

        Image imagenInterior = interior.GetComponent<Image>();
        imagenInterior.sprite = spriteInterfaz;
        imagenInterior.type = Image.Type.Sliced;
        imagenInterior.color = new Color(0.075f, 0.12f, 0.2f, 0.97f);

        Outline bordeInterior = interior.GetComponent<Outline>();
        bordeInterior.effectColor = new Color(0.1f, 0.72f, 0.9f, 0.85f);
        bordeInterior.effectDistance = new Vector2(2f, -2f);
        interior.transform.SetAsFirstSibling();

        return panel;
    }

    private void CrearRemaches(Transform padre, Vector2 desplazamiento)
    {
        Vector2[] posiciones =
        {
            new Vector2(-desplazamiento.x, desplazamiento.y),
            new Vector2(desplazamiento.x, desplazamiento.y),
            new Vector2(-desplazamiento.x, -desplazamiento.y),
            new Vector2(desplazamiento.x, -desplazamiento.y)
        };

        foreach (Vector2 posicion in posiciones)
        {
            GameObject remache = new GameObject(
                "Remache",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            remache.transform.SetParent(padre, false);
            Posicionar(remache.GetComponent<RectTransform>(), posicion, new Vector2(14f, 14f));
            Image imagen = remache.GetComponent<Image>();
            imagen.sprite = spriteManejador != null ? spriteManejador : spriteInterfaz;
            imagen.color = new Color(0.16f, 0.2f, 0.23f, 1f);
            imagen.raycastTarget = false;
        }
    }

    private Slider CrearSlider(string nombre, Transform padre, Vector2 posicion)
    {
        GameObject objetoSlider = new GameObject(nombre, typeof(RectTransform), typeof(Slider));
        objetoSlider.transform.SetParent(padre, false);
        RectTransform rectSlider = objetoSlider.GetComponent<RectTransform>();
        Posicionar(rectSlider, posicion, new Vector2(285f, 38f));

        GameObject fondo = new GameObject(
            "Fondo",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        fondo.transform.SetParent(objetoSlider.transform, false);
        RectTransform rectFondo = fondo.GetComponent<RectTransform>();
        Estirar(rectFondo);
        rectFondo.offsetMin = new Vector2(0f, 8f);
        rectFondo.offsetMax = new Vector2(0f, -8f);
        Image imagenFondo = fondo.GetComponent<Image>();
        imagenFondo.sprite = spriteFondoControl != null ? spriteFondoControl : spriteInterfaz;
        imagenFondo.type = Image.Type.Sliced;
        imagenFondo.color = new Color(0.025f, 0.04f, 0.075f, 1f);

        GameObject areaRelleno = new GameObject("Area_Relleno", typeof(RectTransform));
        areaRelleno.transform.SetParent(objetoSlider.transform, false);
        RectTransform rectAreaRelleno = areaRelleno.GetComponent<RectTransform>();
        Estirar(rectAreaRelleno);
        rectAreaRelleno.offsetMin = new Vector2(8f, 11f);
        rectAreaRelleno.offsetMax = new Vector2(-8f, -11f);

        GameObject relleno = new GameObject(
            "Relleno",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        relleno.transform.SetParent(areaRelleno.transform, false);
        RectTransform rectRelleno = relleno.GetComponent<RectTransform>();
        Estirar(rectRelleno);
        Image imagenRelleno = relleno.GetComponent<Image>();
        imagenRelleno.sprite = spriteInterfaz;
        imagenRelleno.type = Image.Type.Sliced;
        imagenRelleno.color = new Color(0.08f, 0.72f, 0.94f, 1f);

        GameObject areaManejador = new GameObject("Area_Manejador", typeof(RectTransform));
        areaManejador.transform.SetParent(objetoSlider.transform, false);
        RectTransform rectAreaManejador = areaManejador.GetComponent<RectTransform>();
        Estirar(rectAreaManejador);
        rectAreaManejador.offsetMin = new Vector2(12f, 0f);
        rectAreaManejador.offsetMax = new Vector2(-12f, 0f);

        GameObject manejador = new GameObject(
            "Manejador",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        manejador.transform.SetParent(areaManejador.transform, false);
        RectTransform rectManejador = manejador.GetComponent<RectTransform>();
        rectManejador.sizeDelta = new Vector2(30f, 42f);
        Image imagenManejador = manejador.GetComponent<Image>();
        imagenManejador.sprite = spriteManejador != null ? spriteManejador : spriteInterfaz;
        imagenManejador.color = new Color(0.8f, 0.9f, 0.94f, 1f);

        Slider slider = objetoSlider.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.fillRect = rectRelleno;
        slider.handleRect = rectManejador;
        slider.targetGraphic = imagenManejador;
        slider.transition = Selectable.Transition.ColorTint;
        slider.colors = CrearColores(
            new Color(0.8f, 0.9f, 0.94f, 1f),
            new Color(0.35f, 0.9f, 1f, 1f),
            new Color(1f, 0.55f, 0.12f, 1f),
            new Color(0.35f, 0.9f, 1f, 1f));
        return slider;
    }

    private Button CrearBotonMetalico(
        string nombre,
        Transform padre,
        string texto,
        Vector2 posicion,
        Vector2 tamano,
        Color colorTexto)
    {
        GameObject objeto = new GameObject(
            nombre,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(Outline));
        objeto.transform.SetParent(padre, false);
        Posicionar(objeto.GetComponent<RectTransform>(), posicion, tamano);

        Image imagen = objeto.GetComponent<Image>();
        imagen.sprite = spriteInterfaz;
        imagen.type = Image.Type.Sliced;
        imagen.color = new Color(0.42f, 0.5f, 0.55f, 1f);

        Outline borde = objeto.GetComponent<Outline>();
        borde.effectColor = new Color(0.025f, 0.06f, 0.09f, 1f);
        borde.effectDistance = new Vector2(4f, -4f);

        Button boton = objeto.GetComponent<Button>();
        boton.targetGraphic = imagen;
        boton.colors = CrearColores(
            new Color(0.42f, 0.5f, 0.55f, 1f),
            new Color(0.55f, 0.68f, 0.74f, 1f),
            new Color(0.26f, 0.35f, 0.42f, 1f),
            new Color(0.55f, 0.68f, 0.74f, 1f));

        TextMeshProUGUI etiqueta = CrearTexto(
            "Texto",
            objeto.transform,
            texto,
            Vector2.zero,
            tamano,
            35f,
            colorTexto);
        Estirar(etiqueta.rectTransform);

        RetroalimentacionBotonMenu retroalimentacion =
            objeto.AddComponent<RetroalimentacionBotonMenu>();
        retroalimentacion.Configurar(this);
        return boton;
    }

    private TextMeshProUGUI CrearTexto(
        string nombre,
        Transform padre,
        string contenido,
        Vector2 posicion,
        Vector2 tamano,
        float tamanoFuente,
        Color color)
    {
        GameObject objeto = new GameObject(
            nombre,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        objeto.transform.SetParent(padre, false);
        Posicionar(objeto.GetComponent<RectTransform>(), posicion, tamano);

        TextMeshProUGUI texto = objeto.GetComponent<TextMeshProUGUI>();
        if (fuenteTitulos != null) texto.font = fuenteTitulos;
        texto.text = contenido;
        texto.fontSize = tamanoFuente;
        texto.color = color;
        texto.alignment = TextAlignmentOptions.Center;
        texto.enableWordWrapping = false;
        texto.raycastTarget = false;
        texto.outlineColor = new Color32(8, 12, 20, 255);
        texto.outlineWidth = 0.22f;
        return texto;
    }

    private void CrearFundido()
    {
        if (canvasPrincipal == null) return;

        GameObject objeto = new GameObject(
            "Transicion_Negra",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));
        objeto.transform.SetParent(canvasPrincipal.transform, false);
        objeto.transform.SetAsLastSibling();
        Estirar(objeto.GetComponent<RectTransform>());

        Image imagen = objeto.GetComponent<Image>();
        imagen.color = Color.black;
        imagen.raycastTarget = true;

        grupoFundido = objeto.GetComponent<CanvasGroup>();
        grupoFundido.alpha = 1f;
        grupoFundido.blocksRaycasts = true;
        grupoFundido.interactable = true;
    }

    private void ConfigurarAudio()
    {
        fuenteMusica = gameObject.AddComponent<AudioSource>();
        fuenteMusica.playOnAwake = false;
        fuenteMusica.loop = true;
        fuenteMusica.spatialBlend = 0f;
        fuenteMusica.dopplerLevel = 0f;

        fuenteInterfaz = gameObject.AddComponent<AudioSource>();
        fuenteInterfaz.playOnAwake = false;
        fuenteInterfaz.loop = false;
        fuenteInterfaz.spatialBlend = 0f;
        fuenteInterfaz.dopplerLevel = 0f;
    }

    public void Jugar()
    {
        if (cambiandoEscena) return;
        ReproducirConfirmacion();
        StartCoroutine(CargarSelectorNiveles());
    }

    public void Opciones()
    {
        AbrirOpciones();
    }

    public void Salir()
    {
        AbrirConfirmacionSalida();
    }

    private void AbrirOpciones()
    {
        if (cambiandoEscena || panelOpciones == null) return;

        ReproducirConfirmacion();
        panelConfirmarSalida?.SetActive(false);
        panelOpciones.SetActive(true);
        CambiarInteraccionBotonesPrincipales(false);
        sliderMusica.SetValueWithoutNotify(ConfiguracionAudio.VolumenMusica);
        sliderEfectos.SetValueWithoutNotify(ConfiguracionAudio.VolumenEfectos);
        ActualizarPorcentajes();
        Seleccionar(sliderMusica.gameObject);
    }

    private void CerrarOpciones()
    {
        if (panelOpciones == null) return;

        ReproducirConfirmacion();
        ConfiguracionAudio.Guardar();
        panelOpciones.SetActive(false);
        CambiarInteraccionBotonesPrincipales(true);
        Seleccionar(botonOpciones.gameObject);
    }

    private void AbrirConfirmacionSalida()
    {
        if (cambiandoEscena || panelConfirmarSalida == null) return;

        ReproducirConfirmacion();
        panelOpciones?.SetActive(false);
        panelConfirmarSalida.SetActive(true);
        CambiarInteraccionBotonesPrincipales(false);

        Button botonNo = BuscarComponente<Button>("Boton_CancelarSalir");
        Seleccionar(botonNo != null ? botonNo.gameObject : panelConfirmarSalida);
    }

    private void CerrarConfirmacionSalida()
    {
        if (panelConfirmarSalida == null) return;

        ReproducirConfirmacion();
        panelConfirmarSalida.SetActive(false);
        CambiarInteraccionBotonesPrincipales(true);
        Seleccionar(botonSalir.gameObject);
    }

    private void ConfirmarSalida()
    {
        ReproducirConfirmacion();
        ConfiguracionAudio.Guardar();
        Debug.Log("[MENÚ PRINCIPAL] Saliendo del juego...");
        Application.Quit();
    }

    private void CambiarVolumenMusica(float valor)
    {
        ConfiguracionAudio.VolumenMusica = valor;
        ActualizarPorcentajes();
    }

    private void CambiarVolumenEfectos(float valor)
    {
        ConfiguracionAudio.VolumenEfectos = valor;
        ActualizarPorcentajes();
    }

    private void ActualizarPorcentajes()
    {
        if (porcentajeMusica != null)
            porcentajeMusica.text = Mathf.RoundToInt(
                ConfiguracionAudio.VolumenMusica * 100f) + "%";
        if (porcentajeEfectos != null)
            porcentajeEfectos.text = Mathf.RoundToInt(
                ConfiguracionAudio.VolumenEfectos * 100f) + "%";
    }

    private void AlCambiarVolumenMusica(float valor)
    {
        AplicarVolumenMusica();
    }

    private void AplicarVolumenMusica()
    {
        if (fuenteMusica == null) return;

        fuenteMusica.volume =
            ConfiguracionAudio.AplicarMusica(volumenBaseMusica) *
            multiplicadorFundidoMusica;
    }

    private IEnumerator FundidoDeEntrada()
    {
        if (grupoFundido == null) yield break;

        grupoFundido.alpha = 1f;
        grupoFundido.blocksRaycasts = true;
        yield return null;

        float transcurrido = 0f;
        while (transcurrido < duracionFundido)
        {
            transcurrido += Time.unscaledDeltaTime;
            grupoFundido.alpha = 1f - Mathf.Clamp01(transcurrido / duracionFundido);
            yield return null;
        }

        grupoFundido.alpha = 0f;
        grupoFundido.blocksRaycasts = false;
        grupoFundido.interactable = false;
        Seleccionar(botonJugar != null ? botonJugar.gameObject : null);
    }

    private IEnumerator CargarSelectorNiveles()
    {
        cambiandoEscena = true;
        CambiarInteraccionBotonesPrincipales(false);

        if (!Application.CanStreamedLevelBeLoaded(escenaSelectorNiveles))
        {
            Debug.LogError(
                $"[MENÚ PRINCIPAL] La escena '{escenaSelectorNiveles}' no está disponible.");
            cambiandoEscena = false;
            CambiarInteraccionBotonesPrincipales(true);
            yield break;
        }

        if (grupoFundido != null)
        {
            grupoFundido.blocksRaycasts = true;
            grupoFundido.interactable = true;
        }

        float transcurrido = 0f;
        while (transcurrido < duracionFundido)
        {
            transcurrido += Time.unscaledDeltaTime;
            float progreso = Mathf.Clamp01(transcurrido / duracionFundido);

            if (grupoFundido != null)
                grupoFundido.alpha = Mathf.SmoothStep(0f, 1f, progreso);

            multiplicadorFundidoMusica = 1f - progreso;
            AplicarVolumenMusica();
            yield return null;
        }

        ConfiguracionAudio.Guardar();
        Time.timeScale = 1f;
        AsyncOperation carga =
            SceneManager.LoadSceneAsync(escenaSelectorNiveles);

        if (carga == null)
        {
            Debug.LogError("[MENÚ PRINCIPAL] No se pudo iniciar la carga del selector.");
            cambiandoEscena = false;
            multiplicadorFundidoMusica = 1f;
            AplicarVolumenMusica();
            CambiarInteraccionBotonesPrincipales(true);
            yield break;
        }

        while (!carga.isDone)
            yield return null;
    }

    private void CambiarInteraccionBotonesPrincipales(bool interactivos)
    {
        if (botonJugar != null) botonJugar.interactable = interactivos;
        if (botonOpciones != null) botonOpciones.interactable = interactivos;
        if (botonSalir != null) botonSalir.interactable = interactivos;
    }

    private void Seleccionar(GameObject objeto)
    {
        if (sistemaEventos == null) sistemaEventos = EventSystem.current;
        if (sistemaEventos == null || objeto == null) return;

        sistemaEventos.SetSelectedGameObject(null);
        sistemaEventos.SetSelectedGameObject(objeto);
    }

    public void ReproducirSeleccion()
    {
        if (fuenteInterfaz == null || sonidoSeleccion == null) return;
        fuenteInterfaz.PlayOneShot(
            sonidoSeleccion,
            ConfiguracionAudio.AplicarEfectos(1f));
    }

    private void ReproducirConfirmacion()
    {
        if (fuenteInterfaz == null || sonidoConfirmacion == null) return;
        fuenteInterfaz.PlayOneShot(
            sonidoConfirmacion,
            ConfiguracionAudio.AplicarEfectos(1f));
    }

    private static void Estirar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void Posicionar(
        RectTransform rect,
        Vector2 posicion,
        Vector2 tamano)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
        rect.localScale = Vector3.one;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        volumenBaseMusica = Mathf.Clamp01(volumenBaseMusica);
        duracionFundido = Mathf.Max(0.05f, duracionFundido);
    }
#endif
}

/// <summary>
/// Comunica selección por mouse, teclado o mando sin acoplar cada botón
/// a clips concretos.
/// </summary>
public class RetroalimentacionBotonMenu :
    MonoBehaviour,
    IPointerEnterHandler,
    ISelectHandler
{
    private ControladorMenuPrincipal controlador;

    public void Configurar(ControladorMenuPrincipal nuevoControlador)
    {
        controlador = nuevoControlador;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        controlador?.ReproducirSeleccion();
    }

    public void OnSelect(BaseEventData eventData)
    {
        controlador?.ReproducirSeleccion();
    }
}
