using System;
using UnityEngine;

/// <summary>
/// Gestiona la vida, fases y muerte del jefe.
/// También notifica a la UI cada vez que cambia su vida.
/// </summary>
public class SaludJefe : MonoBehaviour
{
    [Header("--- Estadísticas Base ---")]
    [Tooltip("Salud total con la que inicia el jefe.")]
    public int vidaMaxima = 150;

    public int vidaActual { get; private set; }

    [Header("--- Control de Fases ---")]
    [Tooltip("Porcentaje de vida en el que el jefe entra en Fase 2.")]
    [Range(0.1f, 0.9f)]
    public float umbralFase2 = 0.5f;

    public bool estaEnFase2 { get; private set; }

    [Header("--- Estado de Combate ---")]
    [Tooltip("El jefe solo puede recibir daño cuando la batalla comienza oficialmente.")]
    public bool esVulnerable = false;

    [Header("--- Efectos de Muerte ---")]
    public GameObject prefabFragmentacion;

    [Tooltip("Permite que cada encuentro decida si el jefe deja caer su botín al morir.")]
    public bool soltarBotinAlMorir = true;

    // Eventos para que la interfaz reaccione sin depender de la IA del jefe.
    public event Action<int, int> AlCambiarVida;
    public event Action AlEntrarFase2;
    public event Action AlMorir;

    private RetroalimentacionDanio retroalimentacionDanio;
    private BotinMonedas botinMonedas;
    private bool estaMuerto = false;
    private bool estaInicializada;

    private void Awake()
    {
        InicializarVida();
    }

    private void InicializarVida()
    {
        if (estaInicializada) return;

        vidaActual = vidaMaxima;
        retroalimentacionDanio = GetComponent<RetroalimentacionDanio>();
        botinMonedas = GetComponent<BotinMonedas>();
        estaInicializada = true;
    }

    /// <summary>
    /// Recibe daño solo cuando la arena ya inició el combate.
    /// </summary>
    public void RecibirDano(int cantidad)
    {
        InicializarVida();
        if (!esVulnerable || estaMuerto || cantidad <= 0)
        {
            return;
        }

        if (retroalimentacionDanio == null) retroalimentacionDanio = GetComponent<RetroalimentacionDanio>();
        if (botinMonedas == null) botinMonedas = GetComponent<BotinMonedas>();

        vidaActual = Mathf.Max(vidaActual - cantidad, 0);

        Debug.Log($"[JEFE] Daño recibido: {cantidad}. Vida restante: {vidaActual}");

        // La UI roja se actualiza inmediatamente.
        AlCambiarVida?.Invoke(vidaActual, vidaMaxima);

        EvaluarFase();
        retroalimentacionDanio?.MostrarDanio();

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    private void EvaluarFase()
    {
        if (!estaEnFase2 && vidaActual <= vidaMaxima * umbralFase2)
        {
            estaEnFase2 = true;
            Debug.Log("[JEFE] ¡ALERTA! Iniciando Protocolo de Erradicación (Fase 2).");

            AlEntrarFase2?.Invoke();
        }
    }

    private void Morir()
    {
        if (estaMuerto) return;

        estaMuerto = true;
        esVulnerable = false;
        Debug.Log("[JEFE] Unidad destruida. ¡Victoria!");

        // Oculta la barra antes de destruir el objeto.
        AlMorir?.Invoke();
        if (soltarBotinAlMorir)
            botinMonedas?.SoltarMonedas();

        if (prefabFragmentacion != null)
        {
            Instantiate(prefabFragmentacion, transform.position, Quaternion.identity);
        }
        else if (Application.isPlaying)
        {
            Debug.LogWarning("[JEFE] No hay prefab de fragmentación asignado.");
        }

        DestruirEntidad();
    }

    private void DestruirEntidad()
    {
        if (Application.isPlaying) Destroy(gameObject);
        else DestroyImmediate(gameObject);
    }
}
