using UnityEngine;

/// <summary>
/// Gestiona el daño y las colisiones de una bala enemiga, incluyendo su estado
/// después de ser desviada mediante un parry.
/// </summary>
public class BalaEnemiga : MonoBehaviour
{
    [Header("--- Configuración de Daño ---")]
    [Tooltip("Cantidad de daño que esta bala inflige al jugador.")]
    public int dano = 10;

    [Tooltip("Daño que inflige a un enemigo después de ser desviada mediante parry.")]
    [Min(1)] public int danoAlSerDesviada = 10;

    [Header("--- Ciclo de Vida ---")]
    [Tooltip("Tiempo en segundos antes de que la bala se destruya automáticamente.")]
    public float tiempoVida = 3f;

    private bool fueDesviada;
    private bool impactoProcesado;

    public bool FueDesviada => fueDesviada;

    private void Start()
    {
        Destroy(gameObject, tiempoVida);
    }

    /// <summary>
    /// Convierte la bala enemiga en un proyectil capaz de dañar enemigos.
    /// </summary>
    public void Desviar()
    {
        if (fueDesviada) return;

        fueDesviada = true;
        gameObject.tag = "BalaJugador";

        SpriteRenderer spriteBala = GetComponent<SpriteRenderer>();
        if (spriteBala != null) spriteBala.color = Color.cyan;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (fueDesviada)
        {
            ProcesarImpactoDesviado(collision);
            return;
        }

        if (collision.CompareTag("Player"))
        {
            Jugador jugador = collision.GetComponent<Jugador>();
            if (jugador != null)
            {
                jugador.RecibirDano(dano, transform.position);
            }
            else
            {
                Debug.LogWarning(
                    "<color=yellow>[BalaEnemiga] Impacto con Player detectado, " +
                    "pero no se encontró el script Jugador.</color>");
            }

            Destroy(gameObject);
        }
        else if (collision.CompareTag("Pared"))
        {
            Destroy(gameObject);
        }
    }

    private void ProcesarImpactoDesviado(Collider2D collision)
    {
        if (impactoProcesado) return;

        // El proyectil reflejado ya no puede herir al jugador que hizo el parry.
        if (collision.CompareTag("Player")) return;

        PuntoCritico puntoCritico = collision.GetComponent<PuntoCritico>();
        if (puntoCritico != null)
        {
            impactoProcesado = true;
            puntoCritico.ImpactoCritico(danoAlSerDesviada);
            Destroy(gameObject);
            return;
        }

        SaludJefe saludJefe = collision.GetComponentInParent<SaludJefe>();
        SaludEnemigo saludEnemigo = collision.GetComponentInParent<SaludEnemigo>();
        SoldadoEnemigo soldado = collision.GetComponentInParent<SoldadoEnemigo>();

        if (saludJefe != null)
        {
            impactoProcesado = true;
            saludJefe.RecibirDano(danoAlSerDesviada);
            Destroy(gameObject);
        }
        else if (saludEnemigo != null)
        {
            impactoProcesado = true;
            saludEnemigo.RecibirDano(danoAlSerDesviada);
            Destroy(gameObject);
        }
        else if (soldado != null)
        {
            impactoProcesado = true;
            soldado.RecibirDano(danoAlSerDesviada);
            Destroy(gameObject);
        }
        else if (collision.CompareTag("Pared"))
        {
            impactoProcesado = true;
            Destroy(gameObject);
        }
    }
}
