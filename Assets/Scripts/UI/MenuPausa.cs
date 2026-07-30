using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla la pausa y mantiene su ilustración adaptada a cualquier pantalla.
/// La imagen base contiene el arte y los textos; los botones son zonas de interacción
/// transparentes que se anclan sobre ella.
/// </summary>
[DisallowMultipleComponent]
public class MenuPausa : MonoBehaviour
{
    [Header("Referencia de interfaz")]
    [SerializeField] private GameObject ImagenMenu;

    private bool juegoPausado;

    private void Awake()
    {
        ConfigurarInterfazResponsive();
        Reanudar();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (juegoPausado)
            Reanudar();
        else
            Pausar();
    }

    public void Reanudar()
    {
        if (ImagenMenu != null) ImagenMenu.SetActive(false);
        Time.timeScale = 1f;
        juegoPausado = false;
    }

    public void Pausar()
    {
        if (ImagenMenu != null) ImagenMenu.SetActive(true);
        Time.timeScale = 0f;
        juegoPausado = true;
    }

    public void CargarMenuPrincipal(string nombreEscena)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscena);
    }

    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }

    private void ConfigurarInterfazResponsive()
    {
        Canvas canvas = GetComponent<Canvas>();
        CanvasScaler escalador = GetComponent<CanvasScaler>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000;
        }

        if (escalador != null)
        {
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(2816f, 1459f);
            escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escalador.matchWidthOrHeight = 0.5f;
        }

        if (ImagenMenu == null) return;

        Image fondo = ImagenMenu.GetComponent<Image>();
        RectTransform rectFondo = ImagenMenu.GetComponent<RectTransform>();
        if (fondo != null)
        {
            fondo.preserveAspect = true;
            fondo.raycastTarget = false;
        }

        if (rectFondo != null)
        {
            rectFondo.anchorMin = new Vector2(0.5f, 0.5f);
            rectFondo.anchorMax = new Vector2(0.5f, 0.5f);
            rectFondo.pivot = new Vector2(0.5f, 0.5f);
            rectFondo.anchoredPosition = Vector2.zero;
            rectFondo.sizeDelta = new Vector2(2816f, 1459f);

            AspectRatioFitter ajustador = ImagenMenu.GetComponent<AspectRatioFitter>();
            if (ajustador == null) ajustador = ImagenMenu.AddComponent<AspectRatioFitter>();
            ajustador.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            ajustador.aspectRatio = 2816f / 1459f;
        }

        ConfigurarZonaInteractiva("BotonReanudar", 0.39f, 0.615f, 0.61f, 0.735f);
        ConfigurarZonaInteractiva("BotonAjustes", 0.39f, 0.445f, 0.61f, 0.565f);
        ConfigurarZonaInteractiva("BotonSalir", 0.39f, 0.28f, 0.61f, 0.40f);
        ConfigurarZonaInteractiva("BotonSelectorNiveles", 0.34f, 0.035f, 0.67f, 0.14f);
    }

    private void ConfigurarZonaInteractiva(
        string nombre,
        float minimoX,
        float minimoY,
        float maximoX,
        float maximoY)
    {
        Transform zona = ImagenMenu.transform.Find(nombre);
        if (zona == null) return;

        RectTransform rect = zona.GetComponent<RectTransform>();
        Image imagen = zona.GetComponent<Image>();
        Button boton = zona.GetComponent<Button>();
        if (rect == null || imagen == null || boton == null) return;

        rect.anchorMin = new Vector2(minimoX, minimoY);
        rect.anchorMax = new Vector2(maximoX, maximoY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        imagen.color = new Color(1f, 1f, 1f, 0f);
        imagen.raycastTarget = true;
        boton.targetGraphic = imagen;
    }
}
