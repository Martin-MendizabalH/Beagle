using UnityEngine;

public class GolemIA : MonoBehaviour
{
    [Header("Configuración de IA")]
    public float velocidad = 2f;
    public float distanciaDeteccion = 8f; 
    public float distanciaAtaque = 1.2f;  
    public int danoAtaque = 1;

    private Transform jugador;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Busca automáticamente al Beagle por su Tag
        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null)
        {
            jugador = objJugador.transform;
        }
        else
        {
            Debug.LogError("¡No se encontró ningún objeto con el Tag 'Player' en la escena!");
        }
    }

    void Update()
    {
        if (jugador == null) return;

        float distancia = Vector2.Distance(transform.position, jugador.position);

        if (distancia <= distanciaDeteccion && distancia > distanciaAtaque)
        {
            PerseguirJugador();
        }
        else if (distancia <= distanciaAtaque)
        {
            AtacarJugador();
        }
        else
        {
            anim.SetBool("isWalking", false);
        }
    }

    void PerseguirJugador()
    {
        anim.SetBool("isWalking", true);

        if (jugador.position.x > transform.position.x)
            spriteRenderer.flipX = false; 
        else
            spriteRenderer.flipX = true;  

        Vector2 posicionObjetivo = new Vector2(jugador.position.x, transform.position.y);
        transform.position = Vector2.MoveTowards(transform.position, posicionObjetivo, velocidad * Time.deltaTime);
    }

    void AtacarJugador()
    {
        anim.SetBool("isWalking", false);
        anim.SetTrigger("Atacar");
    }

    // Detecta el golpe físico contra el jugador
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("¡El Golem chocó con el Jugador!");

            // Empuje (Knockback)
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 direccionEmpuje = (collision.transform.position - transform.position).normalized;
                playerRb.AddForce(direccionEmpuje * 5f, ForceMode2D.Impulse);
            }

            // Aquí puedes activar tu función de daño al jugador si ya la tienes creada
            // collision.gameObject.GetComponent<VidaJugador>().RecibirDaño(danoAtaque);
        }
    }
}