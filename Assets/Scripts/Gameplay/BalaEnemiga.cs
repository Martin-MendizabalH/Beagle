using UnityEngine;

/// <summary>
/// Gestiona las colisiones y el daño de las balas enemigas.
/// </summary>
public class BalaEnemiga : MonoBehaviour
{
    [Header("--- Configuración de Daño ---")]
    [Tooltip("Cantidad de daño que esta bala infligirá al jugador. Editable directamente desde el Inspector.")]
    public int dano = 10; 

    [Header("--- Ciclo de Vida ---")]
    [Tooltip("Tiempo en segundos antes de que la bala se destruya automáticamente para liberar memoria.")]
    public float tiempoVida = 3f;

    void Start()
    {
        // Limpieza de memoria: destruye el GameObject tras el tiempo definido
        Destroy(gameObject, tiempoVida);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // CASO 1: Impacto con el Jugador
        if (collision.CompareTag("Player"))
        {
            // Intentamos obtener el componente de salud del jugador.
            // *NOTA: Si el script de tu jugador se llama distinto (ej. "Jugador"), cámbialo aquí.
            Jugador scriptJugador = collision.GetComponent<Jugador>();

            if (scriptJugador != null)
            {
                // Ejecutamos el método de daño pasándole el valor configurado en el Inspector
                scriptJugador.RecibirDano(dano, transform.position);
            }
            else
            {
                Debug.LogWarning("<color=yellow>[BalaEnemiga] Impacto con Player detectado, pero no se encontró el script de salud.</color>");
            }

            // La bala se destruye a sí misma tras hacer daño[cite: 2]
            Destroy(gameObject);
        }
        // CASO 2: Impacto con el Entorno (Evita que las balas atraviesen paredes)
        else if (collision.CompareTag("Pared"))
        {
            Destroy(gameObject);
        }
    }
}