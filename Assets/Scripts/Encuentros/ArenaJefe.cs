using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona el inicio, los límites y la finalización del encuentro contra el Jefe.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class ArenaJefe : MonoBehaviour
{
    [Header("--- Elementos del Encuentro ---")]
    public GameObject puertaEntrada;
    public JefeTanqueController jefeTanque;
    public LimitesArenaJefe limitesArena;

    [Header("--- Sistema de Cámaras ---")]
    public GameObject camaraJugador;
    public GameObject camaraArena;

    [Header("--- UI del Jefe ---")]
    public BarraVidaJefe barraVidaJefe;

    [Header("--- Resultado del Encuentro ---")]
    [Tooltip("Si se asigna y está activa, reemplaza la salida normal por la secuencia final.")]
    public SecuenciaFinalNivel secuenciaFinal;

    [Tooltip("Permite decidir el botín por encuentro sin modificar el prefab del Jefe.")]
    public bool soltarBotinJefe = true;

    [Header("--- Tiempos Cinemáticos ---")]
    [Min(0f)] public float tiempoTransicionCamara = 2.5f;
    [Min(0f)] public float tiempoAntesDeAbrirSalida = 0.8f;

    private bool combateIniciado;
    private bool combateFinalizado;
    private Jugador jugadorActual;
    private ControladorArmas armasActuales;
    private SaludJefe saludJefe;

    private void Start()
    {
        if (puertaEntrada != null) puertaEntrada.SetActive(false);
        if (camaraArena != null) camaraArena.SetActive(false);
        if (camaraJugador != null) camaraJugador.SetActive(true);
        if (barraVidaJefe != null) barraVidaJefe.gameObject.SetActive(false);

        if (limitesArena == null && camaraArena != null)
            limitesArena = camaraArena.GetComponent<LimitesArenaJefe>();

        SacudidaCamaraJefe.PrepararReceptor(camaraArena);
        SacudidaCamaraJefe.PrepararReceptor(camaraJugador);

        if (jefeTanque != null)
        {
            jefeTanque.ConfigurarEncuentro(limitesArena);
            saludJefe = jefeTanque.GetComponent<SaludJefe>();
            if (saludJefe != null)
            {
                saludJefe.soltarBotinAlMorir = soltarBotinJefe;
                saludJefe.AlMorir += FinalizarCombate;
            }
        }

        if (secuenciaFinal == null)
            secuenciaFinal = GetComponent<SecuenciaFinalNivel>();
    }

    private void OnDestroy()
    {
        if (saludJefe != null) saludJefe.AlMorir -= FinalizarCombate;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (combateIniciado || combateFinalizado || !collision.CompareTag("Player")) return;

        jugadorActual = collision.GetComponent<Jugador>();
        armasActuales = collision.GetComponentInChildren<ControladorArmas>();
        if (jugadorActual != null)
            StartCoroutine(SecuenciaInicioCombate());
    }

    private IEnumerator SecuenciaInicioCombate()
    {
        combateIniciado = true;
        jugadorActual.CongelarCinematica();
        if (armasActuales != null) armasActuales.puedeAtacar = false;

        if (puertaEntrada != null) puertaEntrada.SetActive(true);
        if (camaraJugador != null) camaraJugador.SetActive(false);
        if (camaraArena != null) camaraArena.SetActive(true);

        yield return new WaitForSeconds(tiempoTransicionCamara);

        if (jefeTanque != null)
        {
            jefeTanque.ConfigurarEncuentro(limitesArena);
            saludJefe = jefeTanque.GetComponent<SaludJefe>();

            if (saludJefe != null)
            {
                saludJefe.esVulnerable = true;
                if (barraVidaJefe != null) barraVidaJefe.Mostrar(saludJefe);
            }

            jefeTanque.enabled = true;
        }

        jugadorActual.DescongelarCinematica();
        if (armasActuales != null) armasActuales.puedeAtacar = true;
    }

    private void FinalizarCombate()
    {
        if (combateFinalizado) return;

        combateFinalizado = true;
        jefeTanque?.DetenerCombate();
        jefeTanque?.GetComponent<SacudidaCamaraJefe>()?.Sacudir(0.38f, 0.5f);
        LimpiarProyectiles();

        if (secuenciaFinal != null &&
            secuenciaFinal.Activa &&
            secuenciaFinal.Iniciar(jugadorActual, armasActuales))
        {
            return;
        }

        StartCoroutine(SecuenciaVictoria());
    }

    private IEnumerator SecuenciaVictoria()
    {
        yield return new WaitForSeconds(tiempoAntesDeAbrirSalida);

        LimpiarProyectiles();
        if (puertaEntrada != null) puertaEntrada.SetActive(false);

        if (camaraArena != null) camaraArena.SetActive(false);
        if (camaraJugador != null) camaraJugador.SetActive(true);

        if (jugadorActual != null) jugadorActual.DescongelarCinematica();
        if (armasActuales != null) armasActuales.puedeAtacar = true;
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
