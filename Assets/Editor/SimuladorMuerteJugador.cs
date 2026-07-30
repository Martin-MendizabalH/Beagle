using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Prueba el recorrido Nivel 1 -> muerte -> MenuDerrota -> reinicio.
/// </summary>
[InitializeOnLoad]
public static class SimuladorMuerteJugador
{
    private const string RutaNivel =
        "Assets/Escenas/Niveles/Nivel 1.unity";
    private const string ClaveActiva =
        "Beagle.SimulacionMuerteJugador.Activa";
    private const string ClaveResultado =
        "Beagle.SimulacionMuerteJugador.Resultado";

    static SimuladorMuerteJugador()
    {
        if (!SessionState.GetBool(ClaveActiva, false)) return;
        EditorApplication.playModeStateChanged -= AlCambiarEstado;
        EditorApplication.playModeStateChanged += AlCambiarEstado;
    }

    [MenuItem("Herramientas/Proyecto Beagle/Simular muerte del jugador")]
    public static void Iniciar()
    {
        EditorSceneManager.OpenScene(RutaNivel, OpenSceneMode.Single);
        SessionState.SetBool(ClaveActiva, true);
        SessionState.SetString(ClaveResultado, string.Empty);
        EditorApplication.playModeStateChanged -= AlCambiarEstado;
        EditorApplication.playModeStateChanged += AlCambiarEstado;
        EditorApplication.isPlaying = true;
    }

    public static void Finalizar(bool exito, string mensaje)
    {
        SessionState.SetString(
            ClaveResultado,
            (exito ? "OK|" : "ERROR|") + mensaje);
        EditorApplication.isPlaying = false;
    }

    private static void AlCambiarEstado(PlayModeStateChange estado)
    {
        if (!SessionState.GetBool(ClaveActiva, false)) return;

        if (estado == PlayModeStateChange.EnteredPlayMode)
        {
            GameObject ejecutor = new GameObject("Simulador_Muerte_Jugador");
            Object.DontDestroyOnLoad(ejecutor);
            ejecutor.AddComponent<EjecutorMuerteJugador>();
        }
        else if (estado == PlayModeStateChange.EnteredEditMode)
        {
            string resultado =
                SessionState.GetString(ClaveResultado, string.Empty);
            bool exito = resultado.StartsWith("OK|");
            string mensaje = resultado.Contains("|")
                ? resultado.Substring(resultado.IndexOf('|') + 1)
                : "La simulación terminó sin entregar un resultado.";

            SessionState.SetBool(ClaveActiva, false);
            SessionState.EraseString(ClaveResultado);
            EditorApplication.playModeStateChanged -= AlCambiarEstado;

            if (exito)
                Debug.Log("[SIMULACIÓN MUERTE] OK: " + mensaje);
            else
                Debug.LogError("[SIMULACIÓN MUERTE] " + mensaje);

            if (Application.isBatchMode)
                EditorApplication.Exit(exito ? 0 : 1);
        }
    }
}

public class EjecutorMuerteJugador : MonoBehaviour
{
    private bool finalizado;

    private void OnEnable()
    {
        Application.logMessageReceived += RegistrarError;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= RegistrarError;
    }

    private void Start()
    {
        StartCoroutine(Ejecutar());
    }

    private IEnumerator Ejecutar()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        Jugador jugador = FindObjectOfType<Jugador>();
        if (!Comprobar(jugador != null, "No se encontró al jugador en Nivel 1."))
            yield break;

        jugador.vidas = 1;
        jugador.RecibirDano(1);

        yield return EsperarEscena("MenuDerrota", 5f);
        if (!Comprobar(
            SceneManager.GetActiveScene().name == "MenuDerrota",
            "La muerte no cargó MenuDerrota."))
        {
            yield break;
        }

        yield return new WaitForSecondsRealtime(0.7f);

        ControladorMenuDerrota menu = FindObjectOfType<ControladorMenuDerrota>();
        Canvas canvas = GameObject.Find("Canvas_MenuDerrota")?.GetComponent<Canvas>();
        Image fondo = canvas != null
            ? canvas.transform.Find("Fondo_MenuDerrota")?.GetComponent<Image>()
            : null;
        CanvasScaler escalador =
            canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
        AspectRatioFitter ajustador =
            fondo != null ? fondo.GetComponent<AspectRatioFitter>() : null;

        if (!Comprobar(menu != null, "No se encontró ControladorMenuDerrota."))
            yield break;
        if (!Comprobar(
            ContextoDerrota.NivelOrigen == "Nivel 1",
            "No se conservó el nivel de origen."))
        {
            yield break;
        }
        if (!Comprobar(
            escalador != null &&
            escalador.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
            Vector2.Distance(
                escalador.referenceResolution,
                new Vector2(2842f, 1467f)) < 0.1f &&
            Mathf.Approximately(escalador.matchWidthOrHeight, 0.5f),
            "El CanvasScaler del menú de derrota no es adaptable."))
        {
            yield break;
        }
        if (!Comprobar(
            fondo != null &&
            fondo.preserveAspect &&
            ajustador != null &&
            ajustador.aspectMode == AspectRatioFitter.AspectMode.FitInParent &&
            Mathf.Abs(ajustador.aspectRatio - 2842f / 1467f) < 0.001f,
            "El fondo no conserva correctamente su proporción."))
        {
            yield break;
        }
        if (!ValidarBoton("BotonReiniciar") ||
            !ValidarBoton("BotonVolver") ||
            !ValidarBoton("BotonSalir"))
        {
            yield break;
        }

        menu.Reiniciar();
        yield return EsperarEscena("Nivel 1", 5f);

        if (!Comprobar(
            SceneManager.GetActiveScene().name == "Nivel 1",
            "Reiniciar no regresó al nivel donde murió el jugador."))
        {
            yield break;
        }

        Finalizar(
            true,
            "muerte, fundidos, menú adaptable y reinicio del nivel verificados.");
    }

    private static IEnumerator EsperarEscena(string escena, float limite)
    {
        float transcurrido = 0f;
        while (SceneManager.GetActiveScene().name != escena &&
               transcurrido < limite)
        {
            transcurrido += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private bool ValidarBoton(string nombre)
    {
        Button boton = GameObject.Find(nombre)?.GetComponent<Button>();
        Image imagen = boton != null ? boton.GetComponent<Image>() : null;
        RectTransform rect = boton != null
            ? boton.GetComponent<RectTransform>()
            : null;

        return Comprobar(
            boton != null &&
            boton.interactable &&
            imagen != null &&
            imagen.raycastTarget &&
            imagen.color.a <= 0.001f &&
            rect != null &&
            rect.parent.name == "Fondo_MenuDerrota" &&
            rect.anchorMin.x >= 0f &&
            rect.anchorMin.y >= 0f &&
            rect.anchorMax.x <= 1f &&
            rect.anchorMax.y <= 1f &&
            rect.anchorMin.x < rect.anchorMax.x &&
            rect.anchorMin.y < rect.anchorMax.y,
            $"El botón '{nombre}' no está anclado correctamente al arte.");
    }

    private bool Comprobar(bool condicion, string mensaje)
    {
        if (condicion) return true;
        Finalizar(false, mensaje);
        return false;
    }

    private void RegistrarError(
        string condicion,
        string traza,
        LogType tipo)
    {
        if (finalizado ||
            (tipo != LogType.Exception && tipo != LogType.Assert))
        {
            return;
        }

        Finalizar(false, "Excepción durante la simulación: " + condicion);
    }

    private void Finalizar(bool exito, string mensaje)
    {
        if (finalizado) return;
        finalizado = true;
        StopAllCoroutines();
        SimuladorMuerteJugador.Finalizar(exito, mensaje);
    }
}
