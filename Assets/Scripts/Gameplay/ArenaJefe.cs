using System.Collections;
using UnityEngine;

/// <summary>
/// Funciona como un Game Manager local exclusivo para la arena del jefe.
/// Controla la transición de cámaras, la puerta, el congelamiento perfecto del jugador y el inicio del combate.
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

    [Header("--- Tiempos Cinemáticos ---")]
    public float tiempoTransicionCamara = 2.5f;

    private bool combateIniciado = false;

    void Start()
    {
        if (puertaEntrada != null) puertaEntrada.SetActive(false);
        if (camaraArena != null) camaraArena.SetActive(false);
        if (camaraJugador != null) camaraJugador.SetActive(true);
    }

    // Se ejecuta al colisionar con un trigger[cite: 2]
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!combateIniciado && collision.CompareTag("Player"))
        {
            // Extraemos los scripts directamente de la colisión del jugador
            Jugador scriptJugador = collision.GetComponent<Jugador>();
            
            // Buscamos el ControladorArmas en el propio jugador o en cualquiera de sus sub-objetos (brazos/pivotes)
            ControladorArmas scriptArmas = collision.GetComponentInChildren<ControladorArmas>();
            
            if (scriptJugador != null)
            {
                StartCoroutine(SecuenciaInicioCombate(scriptJugador, scriptArmas));
            }
        }
    }

    private IEnumerator SecuenciaInicioCombate(Jugador jugador, ControladorArmas armas)
    {
        combateIniciado = true;

        // 1. BLOQUEO CINEMÁTICO (Efecto Megaman)
        jugador.CongelarCinematica();
        if (armas != null) armas.puedeAtacar = false; // Desactivamos el disparo y el giro del arma

        // 2. ENTORNO
        if (puertaEntrada != null) puertaEntrada.SetActive(true);

        // 3. CÁMARAS
        if (camaraJugador != null) camaraJugador.SetActive(false);
        if (camaraArena != null) camaraArena.SetActive(true);

        // 4. SUSPENSO
        yield return new WaitForSeconds(tiempoTransicionCamara);

        // 5. INICIO OFICIAL DEL COMBATE
        if (jefeTanque != null)
        {
            SaludJefe saludJefe = jefeTanque.GetComponent<SaludJefe>();
            if (saludJefe != null) saludJefe.esVulnerable = true;
            
            jefeTanque.enabled = true;
        }

        // 6. LIBERACIÓN
        jugador.DescongelarCinematica();
        if (armas != null) armas.puedeAtacar = true; // El arma vuelve a responder
    }
}