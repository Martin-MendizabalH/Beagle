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

    // Eventos para que la interfaz reaccione sin depender de la IA del jefe.
    public event Action<int, int> AlCambiarVida;
    public event Action AlMorir;

    private SpriteRenderer spriteRenderer;
    private bool estaMuerto = false;

    private void Start()
    {
        vidaActual = vidaMaxima;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Recibe daño solo cuando la arena ya inició el combate.
    /// </summary>
    public void RecibirDano(int cantidad)
    {
        if (!esVulnerable || estaMuerto || cantidad <= 0)
        {
            return;
        }

        vidaActual = Mathf.Max(vidaActual - cantidad, 0);

        Debug.Log($"[JEFE] Daño recibido: {cantidad}. Vida restante: {vidaActual}");

        // La UI roja se actualiza inmediatamente.
        AlCambiarVida?.Invoke(vidaActual, vidaMaxima);

        EvaluarFase();

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

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.6f, 0.6f);
            }
        }
    }

    private void Morir()
    {
        if (estaMuerto) return;

        estaMuerto = true;
        Debug.Log("[JEFE] Unidad destruida. ¡Victoria!");

        // Oculta la barra antes de destruir el objeto.
        AlMorir?.Invoke();

        if (prefabFragmentacion != null)
        {
            Instantiate(prefabFragmentacion, transform.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("[JEFE] No hay prefab de fragmentación asignado.");
        }

        Destroy(gameObject);
    }
}