using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Prueba el menú principal, sus dos opciones de audio, la adaptación visual
/// y la transición al selector sin alterar permanentemente las preferencias.
/// </summary>
[InitializeOnLoad]
public static class SimuladorMenuPrincipal
{
    private const string RutaMenu =
        "Assets/Escenas/Menus/MenuPrincipal.unity";
    private const string ClaveActiva =
        "Beagle.SimulacionMenuPrincipal.Activa";
    private const string ClaveResultado =
        "Beagle.SimulacionMenuPrincipal.Resultado";

    static SimuladorMenuPrincipal()
    {
        if (!SessionState.GetBool(ClaveActiva, false)) return;

        EditorApplication.playModeStateChanged -= AlCambiarEstado;
        EditorApplication.playModeStateChanged += AlCambiarEstado;
    }

    [MenuItem("Herramientas/Proyecto Beagle/Simular menú principal")]
    public static void Iniciar()
    {
        EditorSceneManager.OpenScene(RutaMenu, OpenSceneMode.Single);
        SessionState.SetBool(ClaveActiva, true);
        SessionState.SetString(ClaveResultado, string.Empty);
        EditorApplication.playModeStateChanged -= AlCambiarEstado;
        EditorApplication.playModeStateChanged += AlCambiarEstado;
        EditorApplication.isPlaying = true;
    }

    public static void Finalizar(bool exito, string mensaje)
    {
        SessionState.SetString(ClaveResultado, (exito ? "OK|" : "ERROR|") + mensaje);
        EditorApplication.isPlaying = false;
    }

    private static void AlCambiarEstado(PlayModeStateChange estado)
    {
        if (!SessionState.GetBool(ClaveActiva, false)) return;

        if (estado == PlayModeStateChange.EnteredPlayMode)
        {
            GameObject ejecutor = new GameObject("Simulador_Menu_Principal");
            Object.DontDestroyOnLoad(ejecutor);
            ejecutor.AddComponent<EjecutorMenuPrincipal>();
        }
        else if (estado == PlayModeStateChange.EnteredEditMode)
        {
            string resultado = SessionState.GetString(ClaveResultado, string.Empty);
            bool exito = resultado.StartsWith("OK|");
            string mensaje = resultado.Contains("|")
                ? resultado.Substring(resultado.IndexOf('|') + 1)
                : "La simulación terminó sin resultado.";

            SessionState.SetBool(ClaveActiva, false);
            SessionState.EraseString(ClaveResultado);
            EditorApplication.playModeStateChanged -= AlCambiarEstado;

            if (exito)
                Debug.Log("[SIMULACIÓN MENÚ PRINCIPAL] OK: " + mensaje);
            else
                Debug.LogError("[SIMULACIÓN MENÚ PRINCIPAL] " + mensaje);

            if (Application.isBatchMode)
                EditorApplication.Exit(exito ? 0 : 1);
        }
    }
}

public class EjecutorMenuPrincipal : MonoBehaviour
{
    private readonly List<string> excepciones = new List<string>();
    private float musicaOriginal;
    private float efectosOriginal;
    private bool preferenciasRestauradas;

    private void OnEnable()
    {
        Application.logMessageReceived += RegistrarLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= RegistrarLog;
        RestaurarPreferencias();
    }

    private void Start()
    {
        musicaOriginal = ConfiguracionAudio.VolumenMusica;
        efectosOriginal = ConfiguracionAudio.VolumenEfectos;
        StartCoroutine(Ejecutar());
    }

    private IEnumerator Ejecutar()
    {
        yield return new WaitForSecondsRealtime(0.8f);

        ControladorMenuPrincipal controlador =
            FindObjectOfType<ControladorMenuPrincipal>(true);
        Canvas canvas = FindObjectOfType<Canvas>(true);
        CanvasScaler escalador =
            canvas != null ? canvas.GetComponent<CanvasScaler>() : null;

        if (!Comprobar(controlador != null,
            "No se encontró ControladorMenuPrincipal.")) yield break;
        if (!Comprobar(canvas != null && escalador != null,
            "El menú no contiene Canvas y CanvasScaler.")) yield break;
        if (!Comprobar(
            escalador.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
            escalador.referenceResolution == new Vector2(1328f, 768f),
            "El Canvas no utiliza la resolución adaptable esperada.")) yield break;

        Image fondo = GameObject.Find("Fondo_MenuPrincipal")?.GetComponent<Image>();
        if (!Comprobar(
            fondo != null &&
            fondo.preserveAspect &&
            fondo.GetComponent<AspectRatioFitter>() != null,
            "El fondo no conserva su proporción.")) yield break;

        Button jugar = GameObject.Find("Boton_Jugar")?.GetComponent<Button>();
        Button opciones = GameObject.Find("Boton_Opciones")?.GetComponent<Button>();
        Button salir = GameObject.Find("Boton_Salir")?.GetComponent<Button>();
        if (!Comprobar(jugar != null && opciones != null && salir != null,
            "Falta uno de los botones principales.")) yield break;
        if (!Comprobar(
            jugar.transform.parent == fondo.transform &&
            opciones.transform.parent == fondo.transform &&
            salir.transform.parent == fondo.transform,
            "Los botones no están anclados al arte del menú.")) yield break;
        if (!Comprobar(
            jugar.navigation.selectOnDown == opciones &&
            opciones.navigation.selectOnDown == salir &&
            salir.navigation.selectOnDown == jugar,
            "La navegación de teclado o mando no es circular.")) yield break;

        AudioSource musica = controlador.GetComponents<AudioSource>()
            .FirstOrDefault(fuente => fuente.clip != null);
        if (!Comprobar(
            musica != null && musica.loop &&
            Mathf.Approximately(musica.spatialBlend, 0f),
            "La música del menú no está configurada en bucle y 2D.")) yield break;

        controlador.Opciones();
        yield return null;

        GameObject panelOpciones = GameObject.Find("Panel_Opciones");
        if (!Comprobar(panelOpciones != null && panelOpciones.activeSelf,
            "El botón Opciones no abrió su panel.")) yield break;

        Slider[] sliders = panelOpciones.GetComponentsInChildren<Slider>(true);
        if (!Comprobar(sliders.Length == 2,
            "El panel debe contener únicamente Música y Efectos.")) yield break;

        Slider sliderMusica =
            sliders.FirstOrDefault(slider => slider.name == "Slider_Musica");
        Slider sliderEfectos =
            sliders.FirstOrDefault(slider => slider.name == "Slider_Efectos");
        if (!Comprobar(sliderMusica != null && sliderEfectos != null,
            "No se encontraron ambos controles de volumen.")) yield break;

        TextMeshProUGUI[] textos =
            panelOpciones.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (!Comprobar(textos.Any(texto => texto.font != null &&
            texto.font.name.Contains("Bangers")),
            "El panel no está utilizando la tipografía prevista.")) yield break;

        sliderMusica.value = 0.32f;
        sliderEfectos.value = 0.46f;
        yield return null;

        if (!Comprobar(
            Mathf.Approximately(ConfiguracionAudio.VolumenMusica, 0.32f) &&
            Mathf.Approximately(ConfiguracionAudio.VolumenEfectos, 0.46f),
            "Los sliders no actualizaron ambos canales de audio.")) yield break;
        if (!Comprobar(Mathf.Approximately(musica.volume, 0.65f * 0.32f),
            "El volumen musical no se aplicó a la fuente del menú.")) yield break;

        Button volver = GameObject.Find("Boton_Volver")?.GetComponent<Button>();
        if (!Comprobar(volver != null, "El panel Opciones no tiene botón Volver."))
            yield break;
        if (!Comprobar(
            sliderMusica.navigation.selectOnDown == sliderEfectos &&
            sliderEfectos.navigation.selectOnDown == volver &&
            volver.navigation.selectOnDown == sliderMusica,
            "La navegación de Opciones no es circular.")) yield break;
        volver.onClick.Invoke();
        yield return null;
        if (!Comprobar(!panelOpciones.activeSelf,
            "Volver no cerró el panel Opciones.")) yield break;

        controlador.Salir();
        yield return null;
        GameObject confirmacion = GameObject.Find("Panel_ConfirmarSalida");
        Button cancelar =
            GameObject.Find("Boton_CancelarSalir")?.GetComponent<Button>();
        if (!Comprobar(confirmacion != null && confirmacion.activeSelf &&
            cancelar != null,
            "Salir no abrió una confirmación cancelable.")) yield break;
        cancelar.onClick.Invoke();
        yield return null;
        if (!Comprobar(!confirmacion.activeSelf,
            "No se pudo cancelar la salida.")) yield break;

        RestaurarPreferencias();
        controlador.Jugar();

        float limiteCarga = Time.realtimeSinceStartup + 5f;
        while (SceneManager.GetActiveScene().name != "SelectorNiveles" &&
               Time.realtimeSinceStartup < limiteCarga)
        {
            yield return null;
        }

        if (!Comprobar(SceneManager.GetActiveScene().name == "SelectorNiveles",
            "Jugar no cargó SelectorNiveles.")) yield break;
        if (!Comprobar(Mathf.Approximately(Time.timeScale, 1f),
            "Time.timeScale no quedó restaurado.")) yield break;
        if (!Comprobar(excepciones.Count == 0,
            "Se registraron excepciones: " + string.Join(" | ", excepciones)))
            yield break;

        SimuladorMenuPrincipal.Finalizar(
            true,
            "arte adaptable, navegación, música, opciones, persistencia temporal, " +
            "confirmación de salida y carga del selector verificadas.");
    }

    private void RestaurarPreferencias()
    {
        if (preferenciasRestauradas) return;

        ConfiguracionAudio.VolumenMusica = musicaOriginal;
        ConfiguracionAudio.VolumenEfectos = efectosOriginal;
        ConfiguracionAudio.Guardar();
        preferenciasRestauradas = true;
    }

    private void RegistrarLog(string condicion, string traza, LogType tipo)
    {
        if (tipo == LogType.Exception ||
            (tipo == LogType.Error &&
             (condicion.Contains("NullReferenceException") ||
              condicion.Contains("MissingReferenceException"))))
        {
            excepciones.Add(condicion);
        }
    }

    private bool Comprobar(bool condicion, string mensaje)
    {
        if (condicion) return true;

        RestaurarPreferencias();
        SimuladorMenuPrincipal.Finalizar(false, mensaje);
        return false;
    }
}
