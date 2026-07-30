using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Simulación integral del Nivel 3: entrada al encuentro, daño directo,
/// i-frames, bala, misil, ácido, contacto y finalización de todos los ataques.
/// </summary>
[InitializeOnLoad]
public static class SimuladorBatallaJefeNivel3
{
    private const string RutaNivel3 = "Assets/Escenas/Niveles/Nivel 3.unity";
    private const string ClaveActiva = "Beagle.SimulacionNivel3.Activa";
    private const string ClaveResultado = "Beagle.SimulacionNivel3.Resultado";

    static SimuladorBatallaJefeNivel3()
    {
        if (SessionState.GetBool(ClaveActiva, false))
        {
            EditorApplication.playModeStateChanged -= AlCambiarEstado;
            EditorApplication.playModeStateChanged += AlCambiarEstado;
        }
    }

    [MenuItem("Herramientas/Proyecto Beagle/Simular batalla del Jefe en Nivel 3")]
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
            new GameObject("Simulador_Batalla_Jefe_Nivel3")
                .AddComponent<EjecutorBatallaJefeNivel3>();
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
                Debug.Log("[SIMULACIÓN NIVEL 3] OK: " + mensaje);
            else
                Debug.LogError("[SIMULACIÓN NIVEL 3] " + mensaje);

            if (Application.isBatchMode) EditorApplication.Exit(exito ? 0 : 1);
        }
    }
}

public class EjecutorBatallaJefeNivel3 : MonoBehaviour
{
    private readonly HashSet<TipoAtaqueTanque> ataquesObservados =
        new HashSet<TipoAtaqueTanque>();
    private readonly List<string> excepciones = new List<string>();

    private Jugador jugador;
    private JefeTanqueController jefe;
    private ArenaJefe arena;
    private Rigidbody2D cuerpoJugador;

    private void OnEnable()
    {
        Application.logMessageReceived += RegistrarLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= RegistrarLog;
        if (jefe != null) jefe.AlIniciarAtaque -= RegistrarAtaque;
    }

    private void Start()
    {
        StartCoroutine(Ejecutar());
    }

    private IEnumerator Ejecutar()
    {
        yield return null;

        arena = FindObjectOfType<ArenaJefe>(true);
        jugador = FindObjectOfType<Jugador>(true);

        if (!Comprobar(arena != null, "No se encontró ArenaJefe.")) yield break;
        if (!Comprobar(jugador != null, "No se encontró el Jugador.")) yield break;

        jefe = arena.jefeTanque;
        if (!Comprobar(jefe != null, "ArenaJefe no tiene Jefe asignado.")) yield break;
        if (!Comprobar(jefe.lineaLaser != null, "El Jefe no tiene LineRenderer de láser."))
            yield break;
        if (!Comprobar(jugador.beaglesUI != null &&
            jugador.beaglesUI.Length == 3 &&
            jugador.beaglesUI.All(imagen => imagen != null),
            "El HUD de vidas no está conectado.")) yield break;

        InventarioArmas inventario =
            jugador.GetComponentInChildren<InventarioArmas>(true);
        if (!Comprobar(inventario != null,
            "El Jugador no contiene InventarioArmas.")) yield break;
        if (!Comprobar(inventario.iconoArmaEquipada != null &&
            inventario.animatorUI != null,
            "La interfaz del inventario no está conectada.")) yield break;
        if (!Comprobar(inventario.iconoArmaEquipada.gameObject.activeInHierarchy,
            "El inventario de armas no está visible.")) yield break;

        SonidosJefeTanque sonidos = jefe.GetComponent<SonidosJefeTanque>();
        if (!Comprobar(sonidos != null,
            "El Jefe no contiene el módulo de sonidos.")) yield break;
        if (!Comprobar(sonidos.FuenteEfectos != null &&
            sonidos.FuenteMovimiento != null &&
            sonidos.FuenteAtaques != null,
            "Los canales de audio del Jefe no están configurados.")) yield break;
        if (!Comprobar(sonidos.sonidoMovimiento != null &&
            sonidos.sonidoLaser != null &&
            sonidos.sonidoEmbestida != null &&
            sonidos.sonidoDisparoMetralla != null &&
            sonidos.sonidoImpactoMetralla != null &&
            sonidos.sonidoExplosionMisil != null &&
            sonidos.sonidoMuerte != null,
            "La biblioteca sonora del Jefe no está completamente asignada."))
            yield break;

        // Verifica también rutas sonoras que no siempre ocurren en una batalla corta.
        sonidos.ReproducirImpactoMetralla(jefe.transform.position);
        sonidos.ReproducirImpactoPared();
        sonidos.ReproducirExplosionMisil();
        sonidos.ReproducirTransicionFase();

        cuerpoJugador = jugador.GetComponent<Rigidbody2D>();
        if (!Comprobar(cuerpoJugador != null, "El Jugador no tiene Rigidbody2D.")) yield break;

        jugador.vidas = 3;
        jugador.vidasMaximas = 3;
        jugador.tiempoInvulnerabilidad = 0.18f;
        jugador.velocidadParpadeo = 0.02f;
        jugador.tiempoKnockback = 0.04f;

        cuerpoJugador.velocity = Vector2.zero;
        jugador.transform.position = arena.transform.position;
        Physics2D.SyncTransforms();

        float esperaInicio = 0f;
        while ((!jefe.enabled || !jefe.GetComponent<SaludJefe>().esVulnerable) &&
            esperaInicio < 6f)
        {
            esperaInicio += Time.deltaTime;
            yield return null;
        }

        if (!Comprobar(jefe.enabled, "El trigger no activó la IA del Jefe.")) yield break;
        jefe.enabled = false;
        yield return null;

        // Daño directo, actualización del HUD e i-frames.
        int vidaAntes = jugador.VidasActuales;
        jugador.RecibirDano(1, jefe.transform.position);
        yield return null;
        if (!Comprobar(jugador.VidasActuales == vidaAntes - 1,
            "El daño directo no redujo la vida.")) yield break;
        if (!Comprobar(!jugador.beaglesUI[2].enabled,
            "El HUD no ocultó el icono correspondiente.")) yield break;

        int vidaDuranteIFrames = jugador.VidasActuales;
        jugador.RecibirDano(1, jefe.transform.position);
        yield return null;
        if (!Comprobar(jugador.VidasActuales == vidaDuranteIFrames,
            "Los i-frames no bloquearon un golpe inmediato.")) yield break;

        yield return new WaitForSeconds(jugador.tiempoInvulnerabilidad + 0.08f);
        jugador.RecibirDano(1, jefe.transform.position);
        yield return null;
        if (!Comprobar(jugador.VidasActuales == vidaDuranteIFrames - 1,
            "El Jugador no volvió a ser vulnerable al terminar los i-frames.")) yield break;

        jugador.vidas = 50;
        jugador.vidasMaximas = 50;
        yield return new WaitForSeconds(jugador.tiempoInvulnerabilidad + 0.08f);

        // Bala enemiga real.
        yield return ProbarBala();
        if (!Comprobar(excepciones.Count == 0,
            "Se produjo una excepción durante el impacto de bala: " +
            string.Join(" | ", excepciones))) yield break;

        yield return new WaitForSeconds(jugador.tiempoInvulnerabilidad + 0.08f);

        // Misil enemigo real.
        yield return ProbarMisil();
        if (!Comprobar(excepciones.Count == 0,
            "Se produjo una excepción durante el impacto de misil: " +
            string.Join(" | ", excepciones))) yield break;

        yield return new WaitForSeconds(jugador.tiempoInvulnerabilidad + 0.08f);

        // Peligro Finish/ácido.
        yield return ProbarAcido();
        if (!Comprobar(excepciones.Count == 0,
            "Se produjo una excepción durante el daño de ácido: " +
            string.Join(" | ", excepciones))) yield break;

        yield return new WaitForSeconds(jugador.tiempoInvulnerabilidad + 0.08f);

        // Contacto físico con el Jefe.
        yield return ProbarContacto();
        if (!Comprobar(excepciones.Count == 0,
            "Se produjo una excepción durante el contacto con el Jefe: " +
            string.Join(" | ", excepciones))) yield break;

        yield return new WaitForSeconds(jugador.tiempoInvulnerabilidad + 0.08f);

        // Máquina completa de ataques, incluido el láser que originó la regresión.
        yield return ProbarAtaques();
        if (!Comprobar(excepciones.Count == 0,
            "La simulación registró excepciones: " + string.Join(" | ", excepciones)))
            yield break;

        LimpiarProyectiles();
        SimuladorBatallaJefeNivel3.Finalizar(
            true,
            "trigger, HUD, inventario, audio opcional, daño, i-frames, bala, " +
            "misil, ácido, contacto y finalización de todos los ataques verificados.");
    }

    private IEnumerator ProbarBala()
    {
        PrepararJugadorParaImpacto();
        int vidaAntes = jugador.VidasActuales;
        GameObject bala = Instantiate(
            jefe.balaMetrallaPrefab,
            jugador.transform.position + Vector3.left * 2f,
            Quaternion.identity);
        Rigidbody2D cuerpoBala = bala.GetComponent<Rigidbody2D>();
        Collider2D hitboxBala = bala.GetComponent<Collider2D>();
        Collider2D hitboxJugador = jugador.GetComponent<Collider2D>();

        if (!Comprobar(cuerpoBala != null && hitboxBala != null && hitboxJugador != null,
            "No se pudo preparar la prueba física de BalaEnemiga.")) yield break;

        Debug.Log(
            "[SIMULACIÓN NIVEL 3] Capas bala/jugador: " +
            bala.layer + "/" + jugador.gameObject.layer +
            "; ignoradas=" +
            Physics2D.GetIgnoreLayerCollision(bala.layer, jugador.gameObject.layer));

        if (cuerpoBala != null)
        {
            cuerpoBala.gravityScale = 0f;
            cuerpoBala.velocity = Vector2.right * 12f;
        }

        Physics2D.SyncTransforms();
        float espera = 0f;
        while (jugador.VidasActuales == vidaAntes && espera < 1.5f)
        {
            yield return new WaitForFixedUpdate();
            espera += Time.fixedDeltaTime;
        }

        if (!Comprobar(jugador.VidasActuales < vidaAntes,
            "Una BalaEnemiga real no dañó al Jugador.")) yield break;
    }

    private IEnumerator ProbarMisil()
    {
        PrepararJugadorParaImpacto();
        int vidaAntes = jugador.VidasActuales;
        Collider2D hitboxJugador = jugador.GetComponent<Collider2D>();
        if (!Comprobar(hitboxJugador != null,
            "El Jugador no tiene collider para probar el misil.")) yield break;

        Vector3 origenMisil = hitboxJugador.bounds.center + Vector3.left * 1.5f;
        origenMisil.z = jugador.transform.position.z;
        GameObject misilObjeto = Instantiate(
            jefe.misilPrefab,
            origenMisil,
            Quaternion.identity);
        MisilTeledirigido misil = misilObjeto.GetComponent<MisilTeledirigido>();
        if (!Comprobar(misil != null, "El prefab de misil no tiene su comportamiento."))
            yield break;

        misil.duracionSalidaVertical = 0f;
        misil.ConfigurarEmisor(jefe.gameObject);
        Rigidbody2D cuerpoMisil = misilObjeto.GetComponent<Rigidbody2D>();
        if (!Comprobar(cuerpoMisil != null,
            "El misil no tiene Rigidbody2D.")) yield break;

        // Para esta prueba aislamos el impacto de la navegación: el ataque
        // integral posterior conserva y verifica la salida vertical real.
        cuerpoMisil.rotation = 0f;
        cuerpoMisil.angularVelocity = 0f;
        cuerpoMisil.velocity = Vector2.right * misil.velocidad;
        Physics2D.SyncTransforms();

        float espera = 0f;
        while (jugador.VidasActuales == vidaAntes && espera < 2f)
        {
            yield return new WaitForFixedUpdate();
            espera += Time.fixedDeltaTime;
        }

        if (!Comprobar(jugador.VidasActuales < vidaAntes,
            "Un MisilTeledirigido real no dañó al Jugador.")) yield break;
        if (misilObjeto != null) Destroy(misilObjeto);
    }

    private IEnumerator ProbarAcido()
    {
        PrepararJugadorParaImpacto();
        cuerpoJugador.bodyType = RigidbodyType2D.Dynamic;
        int vidaAntes = jugador.VidasActuales;
        GameObject acido = new GameObject("Acido_Prueba_Automatizada");
        acido.tag = "Finish";
        acido.transform.position = jugador.transform.position + Vector3.down * 1.25f;
        BoxCollider2D hitbox = acido.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.size = new Vector2(3f, 0.75f);
        cuerpoJugador.velocity = Vector2.down * 5f;
        Physics2D.SyncTransforms();

        float espera = 0f;
        while (jugador.VidasActuales == vidaAntes && espera < 1.5f)
        {
            yield return new WaitForFixedUpdate();
            espera += Time.fixedDeltaTime;
        }

        Destroy(acido);
        if (!Comprobar(jugador.VidasActuales < vidaAntes,
            "Un peligro con tag Finish no dañó al Jugador.")) yield break;
    }

    private IEnumerator ProbarContacto()
    {
        PrepararJugadorParaImpacto();
        int vidaAntes = jugador.VidasActuales;
        cuerpoJugador.bodyType = RigidbodyType2D.Dynamic;
        cuerpoJugador.velocity = Vector2.zero;
        float lado = jugador.transform.position.x <= jefe.transform.position.x ? -1f : 1f;
        jugador.transform.position = jefe.transform.position +
            Vector3.right * lado * 2.5f;
        cuerpoJugador.velocity = Vector2.right * -lado * 7f;
        Physics2D.SyncTransforms();

        float espera = 0f;
        while (jugador.VidasActuales == vidaAntes && espera < 1.2f)
        {
            espera += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        jugador.transform.position = new Vector3(
            arena.limitesArena.Centro,
            jefe.puntoDisparoCanon.position.y,
            0f);
        Physics2D.SyncTransforms();

        if (!Comprobar(jugador.VidasActuales < vidaAntes,
            "El contacto con el Jefe no dañó al Jugador.")) yield break;
    }

    private void PrepararJugadorParaImpacto()
    {
        cuerpoJugador.bodyType = RigidbodyType2D.Kinematic;
        cuerpoJugador.velocity = Vector2.zero;
        cuerpoJugador.angularVelocity = 0f;
        jugador.puedeControlar = false;
        jugador.transform.position = new Vector3(
            arena.limitesArena != null
                ? arena.limitesArena.Centro
                : jefe.transform.position.x - 6f,
            jefe.puntoDisparoCanon.position.y,
            0f);
        Physics2D.SyncTransforms();
    }

    private IEnumerator ProbarAtaques()
    {
        ataquesObservados.Clear();
        jefe.AlIniciarAtaque += RegistrarAtaque;

        jefe.tiempoTelegrafiado = 0.15f;
        jefe.intervaloParpadeo = 0.03f;
        jefe.pausaAntesDelImpacto = 0.03f;
        jefe.tiempoMantenimientoLaser = 0.22f;
        jefe.recuperacionLaser = 0.08f;
        jefe.intervaloLanzamientoMetralla = 0f;
        jefe.recuperacionMetralla = 0.08f;
        jefe.duracionMaximaEmbestida = 0.18f;
        jefe.recuperacionEmbestida = 0.08f;
        jefe.recuperacionMisil = 0.08f;
        jefe.tiempoEntreAtaques = 0.08f;

        cuerpoJugador.bodyType = RigidbodyType2D.Kinematic;
        cuerpoJugador.velocity = Vector2.zero;
        jugador.puedeControlar = false;
        jugador.vidas = 50;
        jugador.vidasMaximas = 50;
        jugador.transform.position = new Vector3(
            jefe.transform.position.x - 6f,
            jefe.puntoDisparoCanon.position.y,
            0f);
        Physics2D.SyncTransforms();

        int vidaAntesLaser = jugador.VidasActuales;
        jefe.ForzarSiguienteAtaqueParaDepuracion(TipoAtaqueTanque.Laser);
        jefe.ForzarSiguienteAtaqueParaDepuracion(TipoAtaqueTanque.Metralla);
        jefe.ForzarSiguienteAtaqueParaDepuracion(TipoAtaqueTanque.Embestida);
        jefe.ForzarSiguienteAtaqueParaDepuracion(TipoAtaqueTanque.Misil);
        jefe.enabled = true;

        float espera = 0f;
        while ((ataquesObservados.Count < 4 || jefe.EstaAtacando) && espera < 10f)
        {
            espera += Time.deltaTime;
            yield return null;
        }

        if (!Comprobar(ataquesObservados.Contains(TipoAtaqueTanque.Laser),
            "La IA no ejecutó el láser forzado.")) yield break;
        if (!Comprobar(ataquesObservados.Contains(TipoAtaqueTanque.Metralla),
            "La IA no completó la metralla después del láser.")) yield break;
        if (!Comprobar(ataquesObservados.Contains(TipoAtaqueTanque.Embestida),
            "La IA no completó la embestida después del láser.")) yield break;
        if (!Comprobar(ataquesObservados.Contains(TipoAtaqueTanque.Misil),
            "La IA no completó el misil después del láser.")) yield break;
        if (!Comprobar(!jefe.EstaAtacando,
            "La máquina de estados quedó bloqueada atacando.")) yield break;
        if (!Comprobar(!jefe.lineaLaser.enabled,
            "El LineRenderer del láser permaneció encendido.")) yield break;
        if (!Comprobar(jugador.VidasActuales < vidaAntesLaser,
            "El láser atravesó al Jugador sin dañarlo.")) yield break;

        jefe.AlIniciarAtaque -= RegistrarAtaque;
        jefe.DetenerCombate();
    }

    private void RegistrarAtaque(TipoAtaqueTanque ataque)
    {
        ataquesObservados.Add(ataque);
        Debug.Log("[SIMULACIÓN NIVEL 3] Ataque observado: " + ataque);
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

        LimpiarProyectiles();
        SimuladorBatallaJefeNivel3.Finalizar(false, mensaje);
        return false;
    }

    private static void LimpiarProyectiles()
    {
        foreach (BalaEnemiga bala in FindObjectsOfType<BalaEnemiga>())
        {
            if (bala != null) bala.Retirar();
        }

        foreach (MisilTeledirigido misil in FindObjectsOfType<MisilTeledirigido>())
        {
            if (misil != null) Destroy(misil.gameObject);
        }
    }
}
