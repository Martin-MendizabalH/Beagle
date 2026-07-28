using UnityEngine;

/// <summary>
/// Gestiona el daño de la bala y su autodestrucción al impactar o tras un tiempo.
/// </summary>
public class Proyectil : MonoBehaviour
{
    [Header("--- Propiedades de la Bala ---")]
    [Tooltip("Daño que inflige esta bala específica.")]
    public int danoInfligido = 10;
    
    [Tooltip("Tiempo en segundos antes de que la bala se destruya sola (para no saturar memoria).")]
    public float tiempoDeVida = 2f;

    void Start()
    {
        // Se destruye el proyectil al cabo de los segundos definidos para liberar recursos[cite: 3]
        Destroy(gameObject, tiempoDeVida);
    }

    // Detecta la colisión con algún GameObject usando triggers[cite: 3]
    void OnTriggerEnter2D(Collider2D collision)
    {
        // Si colisiona con algún gameObject que tenga el tag "Enemigo"[cite: 3]
        if (collision.gameObject.CompareTag("Enemigo"))
        {
            // Intentamos obtener el script 'SaludEnemigo' del objeto con el que chocamos
            SaludEnemigo salud = collision.gameObject.GetComponent<SaludEnemigo>();

            // Buena práctica: Siempre verificar que el componente no sea nulo antes de usarlo
            if (salud != null)
            {
                // Le indicamos al enemigo que reste vida
                salud.RecibirDano(danoInfligido);
            }

            // Independientemente de si hizo daño o no, la bala se destruye a sí misma tras chocar con el enemigo[cite: 3]
            Destroy(gameObject);
        }
        // Opcional: Destruir la bala si choca contra las paredes o el suelo (suponiendo que tienen el tag "Entorno" o "Suelo")
        else if (collision.gameObject.CompareTag("Pared"))
        {
            Destroy(gameObject);
        }
    }
}