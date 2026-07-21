using UnityEngine;

public class BalaEnemigo : MonoBehaviour
{
    public float dano = 10f;
    public float tiempoVida = 3f;

    void Start()
    {
        Destroy(gameObject, tiempoVida); // Se destruye automáticamente tras unos segundos
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Opción A: Si tu jugador tiene un método de vida
            /*
            SaludJugador jugador = collision.GetComponent<SaludJugador>();
            if (jugador != null)
            {
                jugador.RecibirDano(dano);
            }
            */

            // Opción B: Reiniciar la escena o matar directamente al jugador al ser alcanzado
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );

            Destroy(gameObject); // Destruir la bala tras hacer daño
        }
    }
}