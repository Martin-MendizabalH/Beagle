using UnityEngine;

public class Moneda : MonoBehaviour
{
    [SerializeField] private int valor = 5;
    [SerializeField] private AudioClip sonidoColeccion; // Opcional: sonido al agarrarla

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verifica si el objeto que entró en el Trigger es el jugador
        if (collision.CompareTag("Player"))
        {
            // 1. Aquí puedes sumar la moneda al inventario o manager del juego
            // Ejemplo: GameManager.Instance.SumarMonedas(valor);

            // 2. Reproducir sonido si asignaste uno
            if (sonidoColeccion != null)
            {
                AudioSource.PlayClipAtPoint(sonidoColeccion, transform.position);
            }

            // 3. Destruir la moneda del mapa
            Destroy(gameObject);
        }
    }
}