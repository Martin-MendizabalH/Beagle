using UnityEngine;

public class BalaEnemigoMover : MonoBehaviour
{
    public float velocidad = 6f;
    private int direccion = 1;

    void Start()
    {
        // Se destruye sola en 3 segundos para no gastar memoria
        Destroy(gameObject, 3f);

        // Busca al Beagle apenas nace
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null)
        {
            // Si el Beagle está a la izquierda, la bala viaja a la izquierda
            if (jugador.transform.position.x < transform.position.x)
            {
                direccion = -1;
                
                // Opcional: Voltea el dibujo de la bala si es necesario
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.flipX = true;
            }
        }
    }

    void Update()
    {
        // Vuela hacia adelante a toda velocidad
        transform.Translate(Vector2.right * velocidad * direccion * Time.deltaTime);
    }
}