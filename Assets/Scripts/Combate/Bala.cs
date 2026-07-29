using UnityEngine;

/// <summary>
/// Gestiona exclusivamente las colisiones y el daño infligido por las balas del jugador.
/// </summary>
public class BalaJugador : MonoBehaviour
{
    [Header("--- Propiedades de Daño ---")]
    [Tooltip("Daño base que inflige esta bala a los enemigos.")]
    public int danoInfligido = 10;
    
    [Tooltip("Tiempo en segundos antes de que la bala se destruya sola para liberar memoria.")]
    public float tiempoDeVida = 2f;

    void Start()
    {
        // Limpieza de memoria: destruye el GameObject tras un tiempo determinado
        Destroy(gameObject, tiempoDeVida);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // CASO 1: Impacto en Punto Crítico
        if (collision.gameObject.CompareTag("PuntoCritico"))
        {
            PuntoCritico critico = collision.gameObject.GetComponent<PuntoCritico>();
            if (critico != null)
            {
                critico.ImpactoCritico(danoInfligido);
            }
            DestruirProyectil();
        }
        // CASO 2: Impacto en Enemigo estándar o Jefe
        else if (collision.gameObject.CompareTag("Enemigo"))
        {
            SaludJefe saludJefe = collision.gameObject.GetComponent<SaludJefe>();
            
            if (saludJefe != null)
            {
                saludJefe.RecibirDano(danoInfligido);
            }
            else
            {
                SaludEnemigo saludNormal = collision.gameObject.GetComponent<SaludEnemigo>();
                if (saludNormal != null)
                {
                    saludNormal.RecibirDano(danoInfligido);
                }
            }
            DestruirProyectil();
        }
        // CASO 3: Choca con el entorno (Suelo/Pared)
        else if (collision.gameObject.CompareTag("Pared"))
        {
            DestruirProyectil();
        }
    }

    /// <summary>
    /// Centraliza la destrucción del proyectil para facilitar futuras adiciones (ej. partículas o sonido).
    /// </summary>
    private void DestruirProyectil()
    {
        // Se destruye a sí mismo al impactar
        Destroy(gameObject);
    }
}