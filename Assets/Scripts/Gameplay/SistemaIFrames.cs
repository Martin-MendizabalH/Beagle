using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona los cuadros de invulnerabilidad (i-frames) y el parpadeo visual
/// para personajes compuestos por múltiples sprites.
/// </summary>
public class SistemaIFrames : MonoBehaviour
{
    [Header("--- Configuración de i-frames ---")]
    [Tooltip("Duración total en segundos de la invulnerabilidad tras recibir daño.")]
    public float duracionInvulnerabilidad = 1.5f;
    
    [Tooltip("Qué tan rápido parpadea el personaje (en segundos).")]
    public float intervaloParpadeo = 0.1f;

    // Variable pública (pero de solo lectura externa) para que tu script de vida
    // sepa si el jugador puede o no recibir daño en este momento.
    public bool esInvulnerable { get; private set; }

    // Array para almacenar todos los pedazos del cuerpo del Beagle
    private SpriteRenderer[] todosLosSprites;

    void Start()
    {
        // En lugar de buscar un solo componente[cite: 1], 
        // buscamos TODOS los componentes de tipo SpriteRenderer en este GameObject y en todos sus hijos.
        todosLosSprites = GetComponentsInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// Método público para gatillar el inicio de los i-frames.
    /// </summary>
    public void ActivarIFrames()
    {
        if (!esInvulnerable)
        {
            // Iniciamos la corrutina que se ejecutará en paralelo al flujo normal del juego
            StartCoroutine(RutinaParpadeo());
        }
    }

    /// <summary>
    /// Corrutina que maneja el tiempo y el efecto visual de encender/apagar los sprites.
    /// </summary>
    private IEnumerator RutinaParpadeo()
    {
        // 1. Hacemos al jugador invulnerable
        esInvulnerable = true;

        float tiempoTranscurrido = 0f;

        // 2. Bucle que se ejecuta mientras dure la invulnerabilidad
        while (tiempoTranscurrido < duracionInvulnerabilidad)
        {
            // Recorremos cada pedacito del Beagle (brazos, cabeza, etc.)
            foreach (SpriteRenderer sr in todosLosSprites)
            {
                // Invertimos su estado visual (si está encendido, se apaga, y viceversa)
                sr.enabled = !sr.enabled;
            }

            // Esperamos una fracción de segundo antes del siguiente ciclo
            yield return new WaitForSeconds(intervaloParpadeo);
            tiempoTranscurrido += intervaloParpadeo;
        }

        // 3. Limpieza de seguridad: Forzamos a que todos los sprites queden encendidos al final
        foreach (SpriteRenderer sr in todosLosSprites)
        {
            sr.enabled = true;
        }

        // 4. El jugador vuelve a ser vulnerable
        esInvulnerable = false;
    }
}