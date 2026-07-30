using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Prueba integral del cierre temporal: muerte del Jefe, ausencia de botín,
/// bloqueo del Jugador, fundido, texto y regreso al menú principal.
/// </summary>
[InitializeOnLoad]
public static class SimuladorFinalTemporalNivel3
{
    private const string RutaNivel3 = "Assets/Escenas/Niveles/Nivel 3.unity";
    private const string ClaveActiva = "Beagle.SimulacionFinalTemporal.Activa";
    private const string ClaveResultado = "Beagle.SimulacionFinalTemporal.Resultado";

    static SimuladorFinalTemporalNivel3()
    {
        if (!SessionState.GetBool(ClaveActiva, false)) return;

        EditorApplication.playModeStateChanged -= AlCambiarEstado;
        EditorApplication.playModeStateChanged += AlCambiarEstado;
    }

    [MenuItem("Herramientas/Proyecto Beagle/Simular final temporal del Nivel 3")]
    public static void Iniciar()
    {
        EditorSceneManager.OpenScene(RutaNivel3, OpenSceneMode.Single);
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
            GameObject ejecutor = new GameObject("Simulador_Final_Temporal_Nivel3");
            Object.DontDestroyOnLoad(ejecutor);
            ejecutor.AddComponent<EjecutorFinalTemporalNivel3>();
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
                Debug.Log("[SIMULACIÓN FINAL TEMPORAL] OK: " + mensaje);
            else
                Debug.LogError("[SIMULACIÓN FINAL TEMPORAL] " + mensaje);

            if (Application.isBatchMode)
                EditorApplication.Exit(exito ? 0 : 1);
        }
    }
}

public class EjecutorFinalTemporalNivel3 : MonoBehaviour
{
    private readonly List<string> excepciones = new List<string>();

    private void OnEnable()
    {
        Application.logMessageReceived += RegistrarLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= RegistrarLog;
    }

    private void Start()
    {
        StartCoroutine(Ejecutar());
    }

    private IEnumerator Ejecutar()
    {
        yield return null;

        ArenaJefe arena = FindObjectOfType<ArenaJefe>(true);
        Jugador jugador = FindObjectOfType<Jugador>(true);

        if (!Comprobar(arena != null, "No se encontró ArenaJefe.")) yield break;
        if (!Comprobar(jugador != null, "No se encontró el Jugador.")) yield break;
        if (!Comprobar(arena.secuenciaFinal != null,
            "ArenaJefe no tiene asignada la secuencia final.")) yield break;
        if (!Comprobar(arena.secuenciaFinal.Activa,
            "La secuencia final está desactivada.")) yield break;
        if (!Comprobar(!arena.soltarBotinJefe,
            "El botín del Jefe continúa habilitado en esta batalla final.")) yield break;

        JefeTanqueController jefe = arena.jefeTanque;
        SaludJefe salud = jefe != null ? jefe.GetComponent<SaludJefe>() : null;
        ControladorArmas armas = jugador.GetComponentInChildren<ControladorArmas>(true);

        if (!Comprobar(jefe != null && salud != null,
            "El Jefe o su componente de salud no están disponibles.")) yield break;
        if (!Comprobar(armas != null,
            "No se encontró el controlador de armas del Jugador.")) yield break;

        jugador.vidas = Mathf.Max(jugador.vidas, 10);
        jugador.transform.position = arena.transform.position;
        Physics2D.SyncTransforms();

        float limiteInicio = Time.realtimeSinceStartup + 7f;
        while ((!jefe.enabled || !salud.esVulnerable) &&
               Time.realtimeSinceStartup < limiteInicio)
        {
            yield return null;
        }

        if (!Comprobar(jefe.enabled && salud.esVulnerable,
            "El trigger no inició correctamente la batalla.")) yield break;

        int monedasAntes = FindObjectsOfType<Moneda>(true).Length;
        salud.RecibirDano(salud.vidaActual);
        yield return null;

        if (!Comprobar(arena.secuenciaFinal.EstaEjecutandose,
            "La muerte del Jefe no inició la secuencia final.")) yield break;
        if (!Comprobar(jugador.EsInvulnerable,
            "El Jugador quedó vulnerable durante el final.")) yield break;
        if (!Comprobar(!armas.puedeAtacar,
            "Las armas siguen habilitadas durante el final.")) yield break;

        yield return new WaitForSecondsRealtime(0.3f);
        int monedasDespues = FindObjectsOfType<Moneda>(true).Length;
        if (!Comprobar(monedasDespues == monedasAntes,
            "El Jefe soltó monedas aunque el botín estaba desactivado.")) yield break;

        GameObject canvasFinal = GameObject.Find("Canvas_FinalTemporal");
        if (!Comprobar(canvasFinal != null,
            "No se creó el Canvas del final temporal.")) yield break;

        CanvasGroup grupo = canvasFinal.GetComponent<CanvasGroup>();
        TextMeshProUGUI texto = canvasFinal.GetComponentInChildren<TextMeshProUGUI>(true);
        if (!Comprobar(grupo != null && texto != null,
            "La interfaz final no contiene CanvasGroup y texto.")) yield break;
        if (!Comprobar(texto.text == "Continuará...",
            "El texto final no coincide con la configuración.")) yield break;

        float limiteFundido = Time.realtimeSinceStartup + 4f;
        while (grupo != null && grupo.alpha < 0.95f &&
               Time.realtimeSinceStartup < limiteFundido)
        {
            yield return null;
        }

        if (!Comprobar(grupo != null && grupo.alpha >= 0.95f,
            "El fundido negro no alcanzó su opacidad final.")) yield break;

        float limiteCarga = Time.realtimeSinceStartup + 5f;
        while (SceneManager.GetActiveScene().name != "MenuPrincipal" &&
               Time.realtimeSinceStartup < limiteCarga)
        {
            yield return null;
        }

        if (!Comprobar(SceneManager.GetActiveScene().name == "MenuPrincipal",
            "La secuencia no regresó al menú principal.")) yield break;
        if (!Comprobar(Mathf.Approximately(Time.timeScale, 1f),
            "Time.timeScale no fue restaurado antes de cargar el menú.")) yield break;
        if (!Comprobar(excepciones.Count == 0,
            "Se registraron excepciones: " + string.Join(" | ", excepciones))) yield break;

        SimuladorFinalTemporalNivel3.Finalizar(
            true,
            "muerte, botín desactivado, invulnerabilidad, bloqueo de armas, " +
            "fundido, texto y carga de MenuPrincipal verificados.");
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

        SimuladorFinalTemporalNivel3.Finalizar(false, mensaje);
        return false;
    }
}
