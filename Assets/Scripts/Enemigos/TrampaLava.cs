using UnityEngine;

public class TrampaLava : MonoBehaviour
{
    [Header("Configuración de la Lava")]
    public int dano = 1;
    public float fuerzaRebote = 12f; // Qué tan alto te escupe la lava

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Si el que tocó la lava es el jugador...
        if (collision.CompareTag("Player"))
        {
            // 1. Aplicarle el Daño
            Jugador scriptJugador = collision.GetComponent<Jugador>();
            if (scriptJugador != null)
            {
                // Usamos la misma función de daño de tu Beagle
                scriptJugador.RecibirDano(dano, transform.position);
            }

            // 2. Aplicar el Efecto de Rebote (Knockback hacia arriba)
            Rigidbody2D rbJugador = collision.GetComponent<Rigidbody2D>();
            if (rbJugador != null)
            {
                // Reiniciamos su velocidad vertical para que el rebote siempre sea igual de alto
                rbJugador.velocity = new Vector2(rbJugador.velocity.x, 0);
                
                // Lo empujamos hacia arriba con la fuerza indicada
                rbJugador.AddForce(Vector2.up * fuerzaRebote, ForceMode2D.Impulse);
                
                Debug.Log("¡El jugador cayó en la lava y rebotó!");
            }
        }
    }
}