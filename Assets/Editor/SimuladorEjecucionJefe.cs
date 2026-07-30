using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Simulación Play Mode de la entrada, ataques, fase 2 y victoria del Jefe.
/// En batch mode finaliza Unity automáticamente con un código de salida.
/// </summary>
[InitializeOnLoad]
public static class SimuladorEjecucionJefe
{
    private const string ClaveActiva = "Beagle.SimulacionJefe.Activa";
    private const string ClaveResultado = "Beagle.SimulacionJefe.Resultado";
    private const string RutaNivel1 = "Assets/Escenas/Niveles/Nivel 1.unity";

    static SimuladorEjecucionJefe()
    {
        if (SessionState.GetBool(ClaveActiva, false))
        {
            EditorApplication.playModeStateChanged -= AlCambiarEstado;
            EditorApplication.playModeStateChanged += AlCambiarEstado;
        }
    }

    [MenuItem("Herramientas/Proyecto Beagle/Simular pelea del Jefe en Play Mode")]
    public static void Iniciar()
    {
        ConfiguradorPulidoJefe.AplicarConfiguracion();
        EditorSceneManager.OpenScene(RutaNivel1, OpenSceneMode.Single);

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
            GameObject ejecutor = new GameObject("Simulador_Ejecucion_Jefe");
            ejecutor.AddComponent<EjecutorSimulacionJefe>();
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
                Debug.Log("[SIMULACIÓN PLAY JEFE] OK: " + mensaje);
            else
                Debug.LogError("[SIMULACIÓN PLAY JEFE] " + mensaje);

            if (Application.isBatchMode) EditorApplication.Exit(exito ? 0 : 1);
        }
    }
}

public class EjecutorSimulacionJefe : MonoBehaviour
{
    private readonly HashSet<TipoAtaqueTanque> ataquesObservados =
        new HashSet<TipoAtaqueTanque>();

    private ArenaJefe arena;
    private JefeTanqueController jefe;
    private SaludJefe salud;
    private bool transicionCompletada;
    private bool metrallaGeneroProyectiles;
    private float momentoInicioMetralla = -1f;
    private int cantidadMetrallasObservadas;
    private bool pruebaColorCompletada;
    private PoolBalasMetrallaJefe poolMetralla;
    private int cantidadInicialPool;
    private bool estadoBalasRegistrado;
    private bool pruebaMisilCompletada;

    private void Start()
    {
        StartCoroutine(Ejecutar());
    }

    private IEnumerator Ejecutar()
    {
        yield return null;

        ArenaJefe[] arenas = FindObjectsOfType<ArenaJefe>(true);
        if (!Comprobar(arenas.Length > 0, "No se encontró ArenaJefe en Nivel 1.")) yield break;

        arena = arenas[0];
        jefe = arena.jefeTanque;
        if (!Comprobar(jefe != null, "ArenaJefe no tiene Jefe asignado.")) yield break;

        salud = jefe.GetComponent<SaludJefe>();
        if (!Comprobar(salud != null, "El Jefe no tiene SaludJefe.")) yield break;
        poolMetralla = jefe.GetComponent<PoolBalasMetrallaJefe>();
        if (!Comprobar(poolMetralla != null, "El Jefe no tiene pool de metralla.")) yield break;

        jefe.AlIniciarAtaque += RegistrarAtaque;
        jefe.AlCompletarTransicionFase2 += RegistrarTransicion;

        GameObject objetoJugador = GameObject.FindGameObjectWithTag("Player");
        if (!Comprobar(objetoJugador != null, "No se encontró el jugador.")) yield break;

        Jugador jugador = objetoJugador.GetComponent<Jugador>();
        jugador.vidas = 999;
        jugador.vidasMaximas = 999;
        objetoJugador.transform.position = arena.transform.position;

        float esperaInicio = 0f;
        while (!jefe.enabled && esperaInicio < 6f)
        {
            esperaInicio += Time.deltaTime;
            yield return null;
        }

        if (!Comprobar(jefe.enabled && salud.esVulnerable,
            "La batalla no comenzó después de entrar al trigger.")) yield break;

        jefe.ForzarSiguienteAtaqueParaDepuracion(TipoAtaqueTanque.Laser);
        jefe.ForzarSiguienteAtaqueParaDepuracion(TipoAtaqueTanque.Metralla);
        jefe.ForzarSiguienteAtaqueParaDepuracion(TipoAtaqueTanque.Metralla);
        jefe.ForzarSiguienteAtaqueParaDepuracion(TipoAtaqueTanque.Metralla);
        jefe.ForzarSiguienteAtaqueParaDepuracion(TipoAtaqueTanque.Laser);
        cantidadInicialPool = poolMetralla.CantidadTotal;

        Rigidbody2D cuerpoJugador = objetoJugador.GetComponent<Rigidbody2D>();
        if (cuerpoJugador != null)
        {
            cuerpoJugador.velocity = Vector2.zero;
            cuerpoJugador.bodyType = RigidbodyType2D.Kinematic;
        }
        jugador.puedeControlar = false;
        objetoJugador.transform.position = new Vector3(
            arena.limitesArena.Centro, jefe.transform.position.y + 1.5f, 0f);

        float esperaAtaques = 0f;
        while ((cantidadMetrallasObservadas < 3 ||
            !metrallaGeneroProyectiles ||
            !pruebaColorCompletada ||
            Time.time < momentoInicioMetralla + 3f ||
            (poolMetralla.CantidadActivas > 0 &&
                Time.time < momentoInicioMetralla + 5f)) &&
            esperaAtaques < 42f)
        {
            esperaAtaques += Time.deltaTime;

            if (momentoInicioMetralla >= 0f &&
                Time.time >= momentoInicioMetralla + 1.35f &&
                FindObjectsOfType<BalaEnemiga>().Length > 0)
            {
                metrallaGeneroProyectiles = true;
            }

            if (!estadoBalasRegistrado &&
                cantidadMetrallasObservadas >= 3 &&
                Time.time >= momentoInicioMetralla + 3f)
            {
                estadoBalasRegistrado = true;
                Debug.Log("[SIMULACIÓN PLAY JEFE] Estado de balas a los 3 s: " +
                    DescribirBalasActivas());

                if (!Comprobar(ValidarTransformBalas(out string errorTransform),
                    errorTransform)) yield break;
            }

            yield return null;
        }

        if (!Comprobar(ataquesObservados.Count >= 2,
            "La IA no utilizó suficiente variedad de ataques. " +
            $"Observados={string.Join(", ", ataquesObservados)}; " +
            $"habilitado={jefe.enabled}; activo={jefe.gameObject.activeInHierarchy}; " +
            $"atacando={jefe.EstaAtacando}; timeScale={Time.timeScale}; " +
            $"tiempoJuego={Time.time:0.00}.")) yield break;
        if (!Comprobar(ataquesObservados.Contains(TipoAtaqueTanque.Metralla),
            "La IA no llegó a ejecutar la metralla.")) yield break;
        if (!Comprobar(metrallaGeneroProyectiles,
            "La metralla no generó proyectiles balísticos.")) yield break;
        if (!Comprobar(pruebaColorCompletada,
            "No se completó la prueba de color durante el láser.")) yield break;
        if (!Comprobar(cantidadMetrallasObservadas >= 3,
            "No se pudieron repetir tres lluvias de metralla.")) yield break;
        if (!Comprobar(poolMetralla.CantidadTotal == cantidadInicialPool,
            "El pool creó o perdió proyectiles durante las lluvias repetidas.")) yield break;
        if (!Comprobar(poolMetralla.CantidadActivas == 0,
            "Quedaron balas activas después de esperar sus impactos: " +
            DescribirBalasActivas())) yield break;

        int danoParaFase2 = Mathf.Max(1, salud.vidaActual -
            Mathf.FloorToInt(salud.vidaMaxima * salud.umbralFase2));
        salud.RecibirDano(danoParaFase2);

        float esperaFase = 0f;
        while (!transicionCompletada && esperaFase < 7f)
        {
            esperaFase += Time.deltaTime;
            yield return null;
        }

        if (!Comprobar(transicionCompletada && salud.estaEnFase2 && salud.esVulnerable,
            "La transición a fase 2 no se completó correctamente.")) yield break;

        float esperaMisil = 0f;
        while ((!ataquesObservados.Contains(TipoAtaqueTanque.Misil) ||
            !pruebaMisilCompletada) && esperaMisil < 9f)
        {
            esperaMisil += Time.deltaTime;
            yield return null;
        }

        if (!Comprobar(ataquesObservados.Contains(TipoAtaqueTanque.Misil),
            "La fase 2 no ejecutó el misil forzado.")) yield break;
        if (!Comprobar(pruebaMisilCompletada,
            "No se completó la prueba de salida y parry del misil.")) yield break;

        salud.RecibirDano(99999);
        yield return new WaitForSeconds(arena.tiempoAntesDeAbrirSalida + 0.5f);

        if (!Comprobar(arena.puertaEntrada == null || !arena.puertaEntrada.activeSelf,
            "La puerta no se abrió después de derrotar al Jefe.")) yield break;
        if (!Comprobar(arena.camaraArena == null || !arena.camaraArena.activeSelf,
            "La cámara de arena siguió activa tras la victoria.")) yield break;
        if (!Comprobar(arena.camaraJugador == null || arena.camaraJugador.activeSelf,
            "La cámara del jugador no fue restaurada.")) yield break;
        if (!Comprobar(FindObjectsOfType<BalaEnemiga>().Length == 0 &&
            FindObjectsOfType<MisilTeledirigido>().Length == 0,
            "Quedaron proyectiles hostiles después de la victoria.")) yield break;

        Desvincular();
        SimuladorEjecucionJefe.Finalizar(
            true,
            $"ataques observados={ataquesObservados.Count}, metralla, fase 2, misil y victoria verificados.");
    }

    private void RegistrarAtaque(TipoAtaqueTanque ataque)
    {
        ataquesObservados.Add(ataque);
        Debug.Log($"[SIMULACIÓN PLAY JEFE] Ataque observado: {ataque}.");
        if (ataque == TipoAtaqueTanque.Metralla)
        {
            cantidadMetrallasObservadas++;
            momentoInicioMetralla = Time.time;
        }
        else if (ataque == TipoAtaqueTanque.Laser && !pruebaColorCompletada)
        {
            StartCoroutine(ProbarColorDuranteAtaque());
        }
        else if (ataque == TipoAtaqueTanque.Misil && !pruebaMisilCompletada)
        {
            StartCoroutine(ProbarMisil());
        }
    }

    private IEnumerator ProbarColorDuranteAtaque()
    {
        EstadoVisualJefe estadoVisual = jefe.GetComponent<EstadoVisualJefe>();
        SpriteRenderer sprite = jefe.GetComponent<SpriteRenderer>();
        RetroalimentacionDanio destello = jefe.GetComponent<RetroalimentacionDanio>();

        if (!Comprobar(estadoVisual != null && sprite != null && destello != null,
            "Faltan componentes para comprobar el estado visual.")) yield break;

        yield return new WaitForSeconds(0.12f);
        for (int i = 0; i < 5; i++)
        {
            salud.RecibirDano(1);
            yield return new WaitForSeconds(0.025f);
        }

        yield return new WaitForSeconds(0.15f);
        float diferenciaColor =
            Vector4.Distance(sprite.color, estadoVisual.ColorActualEsperado);
        if (!Comprobar(diferenciaColor < 0.002f,
            $"El destello restauró un color incorrecto. Diferencia={diferenciaColor}.")) yield break;

        bool siluetaSigueActiva =
            sprite.sharedMaterial != null &&
            sprite.sharedMaterial.shader != null &&
            sprite.sharedMaterial.shader.name == "Beagle/SiluetaDano";
        if (!Comprobar(!siluetaSigueActiva,
            "El material de silueta quedó activo después del destello.")) yield break;

        pruebaColorCompletada = true;
    }

    private IEnumerator ProbarMisil()
    {
        MisilTeledirigido misil = null;
        float esperaCreacion = 0f;
        while (misil == null && esperaCreacion < 2f)
        {
            misil = FindObjectOfType<MisilTeledirigido>();
            esperaCreacion += Time.deltaTime;
            yield return null;
        }

        if (!Comprobar(misil != null, "El ataque no llegó a crear el misil.")) yield break;

        Rigidbody2D cuerpoMisil = misil.GetComponent<Rigidbody2D>();
        Collider2D hitboxMisil = misil.GetComponent<Collider2D>();
        if (!Comprobar(cuerpoMisil != null && hitboxMisil != null,
            "El misil no tiene Rigidbody2D o Collider2D.")) yield break;

        if (!Comprobar(misil.EstaEnSalidaVertical &&
            cuerpoMisil.velocity.y > 0f &&
            Mathf.Abs(cuerpoMisil.velocity.x) < 0.05f,
            $"El misil no salió verticalmente. Velocidad={cuerpoMisil.velocity}.")) yield break;

        Collider2D[] hitboxesJefe = jefe.GetComponentsInChildren<Collider2D>(true);
        bool ignoraAlJefe = true;
        foreach (Collider2D hitboxJefe in hitboxesJefe)
        {
            if (hitboxJefe != null &&
                !Physics2D.GetIgnoreCollision(hitboxMisil, hitboxJefe))
            {
                ignoraAlJefe = false;
                break;
            }
        }

        if (!Comprobar(ignoraAlJefe,
            "El misil hostil no estaba ignorando los colliders de su emisor.")) yield break;

        int vidaAntesParry = salud.vidaActual;
        Vector2 direccionAlJefe =
            ((Vector2)jefe.transform.position - cuerpoMisil.position).normalized;
        misil.Desviar(direccionAlJefe, 18f);

        bool colisionReactivada = true;
        foreach (Collider2D hitboxJefe in hitboxesJefe)
        {
            if (hitboxJefe != null &&
                Physics2D.GetIgnoreCollision(hitboxMisil, hitboxJefe))
            {
                colisionReactivada = false;
                break;
            }
        }

        if (!Comprobar(colisionReactivada,
            "El parry no reactivó la colisión del misil contra el Jefe.")) yield break;

        float esperaImpacto = 0f;
        while (misil != null && salud.vidaActual >= vidaAntesParry && esperaImpacto < 2f)
        {
            esperaImpacto += Time.deltaTime;
            yield return null;
        }

        if (!Comprobar(salud.vidaActual < vidaAntesParry,
            "El misil desviado no dañó al Jefe.")) yield break;

        pruebaMisilCompletada = true;
    }

    private void RegistrarTransicion()
    {
        transicionCompletada = true;
    }

    private bool Comprobar(bool condicion, string mensaje)
    {
        if (condicion) return true;

        Desvincular();
        SimuladorEjecucionJefe.Finalizar(false, mensaje);
        return false;
    }

    private void Desvincular()
    {
        if (jefe == null) return;
        jefe.AlIniciarAtaque -= RegistrarAtaque;
        jefe.AlCompletarTransicionFase2 -= RegistrarTransicion;
    }

    private static string DescribirBalasActivas()
    {
        var descripcion = new StringBuilder();
        foreach (BalaEnemiga bala in FindObjectsOfType<BalaEnemiga>())
        {
            if (bala == null || !bala.gameObject.activeInHierarchy) continue;
            Rigidbody2D cuerpo = bala.GetComponent<Rigidbody2D>();
            if (descripcion.Length > 0) descripcion.Append("; ");
            descripcion.Append(bala.name)
                .Append(" pos=")
                .Append(bala.transform.position.ToString("F2"))
                .Append(" vel=")
                .Append(cuerpo != null ? cuerpo.velocity.ToString("F2") : "sin Rigidbody");
        }
        return descripcion.Length > 0 ? descripcion.ToString() : "ninguna";
    }

    private bool ValidarTransformBalas(out string error)
    {
        Vector3 escalaEsperada = jefe.balaMetrallaPrefab.transform.localScale;
        foreach (BalaEnemiga bala in FindObjectsOfType<BalaEnemiga>())
        {
            if (bala == null || !bala.name.Contains("_Reutilizable")) continue;

            Vector3 escala = bala.transform.localScale;
            if (Vector3.Distance(escala, escalaEsperada) > 0.001f)
            {
                error =
                    $"Una bala reutilizada cambió de tamaño. " +
                    $"Esperada={escalaEsperada}, actual={escala}.";
                return false;
            }

            Rigidbody2D cuerpo = bala.GetComponent<Rigidbody2D>();
            if (cuerpo == null || cuerpo.velocity.sqrMagnitude < 0.1f) continue;
            float alineacion =
                Vector2.Dot(bala.transform.right, cuerpo.velocity.normalized);
            if (alineacion < 0.92f)
            {
                error =
                    $"Una bala no apunta hacia su velocidad. Alineación={alineacion:0.000}.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }
}
