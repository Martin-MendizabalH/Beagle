using UnityEngine;

/// <summary>
/// Gestiona la vida total del jefe, el cambio de fases y su destrucción.
/// Diseño modular: Recibe daño tanto del chasis como de la torreta.
/// </summary>
public class SaludJefe : MonoBehaviour
{
    [Header("--- Estadísticas Base ---")]
    [Tooltip("Salud total con la que inicia el jefe.")]
    public int vidaMaxima = 150;
    
    // Propiedad pública de solo lectura para evitar modificaciones externas accidentales
    public int vidaActual { get; private set; }

    [Header("--- Control de Fases ---")]
    [Tooltip("Porcentaje de vida (ej. 0.5 = 50%) en el que el jefe entra en Modo Erradicación.")]
    [Range(0.1f, 0.9f)]
    public float umbralFase2 = 0.5f;
    
    // Bandera de control de estado
    public bool estaEnFase2 { get; private set; } = false;

    [Header("--- Estado de Combate ---")]
    [Tooltip("Determina si el jefe puede recibir daño. Se activa cuando el jugador entra a la arena.")]
    public bool esVulnerable = false; // Inicia en FALSE para que nazca siendo inmortal

    [Header("--- Efectos de Muerte (Gibs) ---")]
    [Tooltip("El prefab que contiene las partes del enemigo cortadas (Efecto_Muerte_Enemigo).")]
    public GameObject prefabFragmentacion;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        vidaActual = vidaMaxima;
        spriteRenderer = GetComponent<SpriteRenderer>(); // Obtenemos el componente visual[cite: 3]
    }

    /// <summary>
    /// Método centralizado para recibir daño.
    /// </summary>
    public void RecibirDano(int cantidad)
    {   
        // 1. ESCUDO CINEMÁTICO: Si no es vulnerable, ignoramos el impacto por completo
        if (!esVulnerable) return;

        vidaActual -= cantidad;
        Debug.Log($"[JEFE] Daño recibido: {cantidad}. Vida restante: {vidaActual}");

        EvaluarFase();

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    /// <summary>
    /// Comprueba si la vida ha caído por debajo del umbral matemático para activar la Fase 2.
    /// </summary>
    private void EvaluarFase()
    {
        // Evaluamos si cruzó el umbral y nos aseguramos de que el código solo se ejecute una vez
        if (!estaEnFase2 && vidaActual <= (vidaMaxima * umbralFase2))
        {
            estaEnFase2 = true;
            Debug.Log("[JEFE] ¡ALERTA! Iniciando Protocolo de Erradicación (Fase 2).");
            
            // Feedback Visual: Tintamos el tanque de rojo para avisar al jugador
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.6f, 0.6f); // Tono rojizo
            }
        }
    }

    private void Morir()
    {
        Debug.Log("[JEFE] Unidad Destruida. ¡Victoria!");

        // 1. Instanciar los pedazos del enemigo (Efecto Gore/Gibs)
        if (prefabFragmentacion != null)
        {
            // Crear el objeto a través de un prefab "plantilla"[cite: 2] en la posición exacta donde murió
            Instantiate(prefabFragmentacion, transform.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("El enemigo murió, pero no tiene un Prefab de Fragmentación asignado en el Inspector.");
        }
        
        // Aquí a futuro llamaremos a GameManager.Instance.BossDerrotado() o similar
        
        Destroy(gameObject); // Destruye el GameObject principal y todos sus hijos anidados
    }
}