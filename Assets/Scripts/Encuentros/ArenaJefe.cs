using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona la entrada a la arena y el inicio oficial del combate contra el jefe.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class ArenaJefe : MonoBehaviour
{
    [Header("--- Elementos del Entorno ---")]
    public GameObject puertaEntrada;
    public JefeTanqueController jefeTanque;

    [Header("--- Sistema de Cámaras ---")]
    public GameObject camaraJugador;
    public GameObject camaraArena;

    [Header("--- UI del Jefe ---")]
    [Tooltip("Componente BarraVidaJefe ubicado en Canvas_UIJefe.")]
    public BarraVidaJefe barraVidaJefe;

    [Header("--- Tiempos Cinemáticos ---")]
    public float tiempoTransicionCamara = 2.5f;

    private bool combateIniciado = false;

    private void Start()
    {
        if (puertaEntrada != null) puertaEntrada.SetActive(false);
        if (camaraArena != null) camaraArena.SetActive(false);
        if (camaraJugador != null) camaraJugador.SetActive(true);

        // Refuerzo de seguridad: la UI nunca debe aparecer antes de la batalla.
        if (barraVidaJefe != null)
        {
            barraVidaJefe.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (combateIniciado || !collision.CompareTag("Player"))
        {
            return;
        }

        Jugador scriptJugador = collision.GetComponent<Jugador>();
        ControladorArmas scriptArmas = collision.GetComponentInChildren<ControladorArmas>();

        if (scriptJugador != null)
        {
            StartCoroutine(SecuenciaInicioCombate(scriptJugador, scriptArmas));
        }
    }

    private IEnumerator SecuenciaInicioCombate(Jugador jugador, ControladorArmas armas)
    {
        combateIniciado = true;

        // 1. Bloquear jugador y armas durante la cinemática.
        jugador.CongelarCinematica();

        if (armas != null)
        {
            armas.puedeAtacar = false;
        }

        // 2. Cerrar entrada.
        if (puertaEntrada != null)
        {
            puertaEntrada.SetActive(true);
        }

        // 3. Cambiar cámara.
        if (camaraJugador != null)
        {
            camaraJugador.SetActive(false);
        }

        if (camaraArena != null)
        {
            camaraArena.SetActive(true);
        }

        // 4. Esperar la transición cinematográfica.
        yield return new WaitForSeconds(tiempoTransicionCamara);

        // 5. Inicio oficial de la batalla.
        if (jefeTanque != null)
        {
            SaludJefe saludJefe = jefeTanque.GetComponent<SaludJefe>();

            if (saludJefe != null)
            {
                saludJefe.esVulnerable = true;

                // La barra aparece exactamente cuando el jefe ya puede recibir daño.
                if (barraVidaJefe != null)
                {
                    barraVidaJefe.Mostrar(saludJefe);
                }
            }

            jefeTanque.enabled = true;
        }

        // 6. Devolver control al jugador.
        jugador.DescongelarCinematica();

        if (armas != null)
        {
            armas.puedeAtacar = true;
        }
    }
}