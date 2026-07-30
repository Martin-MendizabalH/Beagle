using UnityEngine;

public class SoldadoIA : MonoBehaviour
{
    [Header("--- Combate y Visión ---")]
    public float distanciaDeteccion = 8f;
    public GameObject prefabBala;
    public Transform puntoDisparo;
    public float tiempoEntreDisparos = 1.5f;
    private float temporizadorDisparo;

    [Header("--- Movimiento (Patrulla) ---")]
    public float velocidad = 2f;
    public float distanciaPatrulla = 3f; // Cuánto camina antes de darse la vuelta
    private float posicionInicialX;
    private int direccionPatrulla = 1; // 1 es derecha, -1 es izquierda

    [Header("--- Vida ---")]
    public int vida = 3; // Cantidad de disparos que aguanta

    private Transform jugador;
    private bool jugadorDetectado = false;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Guarda en qué parte del mapa empezó para patrullar desde ahí
        posicionInicialX = transform.position.x;
        temporizadorDisparo = tiempoEntreDisparos;

        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null) jugador = objJugador.transform;
    }

    void Update()
    {
        if (jugador == null) return;

        float distanciaAlJugador = Vector2.Distance(transform.position, jugador.position);

        if (distanciaAlJugador <= distanciaDeteccion)
        {
            jugadorDetectado = true;
            MirarAlJugador();
            ManejarDisparo();
        }
        else
        {
            jugadorDetectado = false;
            Patrullar();
        }
    }

    // --- NUEVO: SISTEMA DE MOVIMIENTO ---
    void Patrullar()
    {
        // Mueve al soldado de un lado a otro
        transform.Translate(Vector2.right * velocidad * direccionPatrulla * Time.deltaTime);

        // Si se aleja mucho a la derecha, date la vuelta
        if (transform.position.x > posicionInicialX + distanciaPatrulla)
        {
            direccionPatrulla = -1;
            Voltear(true); // Mirar a la izquierda
        }
        // Si se aleja mucho a la izquierda, date la vuelta
        else if (transform.position.x < posicionInicialX - distanciaPatrulla)
        {
            direccionPatrulla = 1;
            Voltear(false); // Mirar a la derecha
        }
    }

    void Voltear(bool mirarIzquierda)
    {
        spriteRenderer.flipX = mirarIzquierda;
        // Acomoda el punto de disparo al lado correcto del arma
        float posX = Mathf.Abs(puntoDisparo.localPosition.x);
        puntoDisparo.localPosition = new Vector2(mirarIzquierda ? -posX : posX, puntoDisparo.localPosition.y);
    }

    void MirarAlJugador()
    {
        Voltear(jugador.position.x < transform.position.x);
    }

    void ManejarDisparo()
    {
        temporizadorDisparo -= Time.deltaTime;
        if (temporizadorDisparo <= 0f)
        {
            anim.SetTrigger("Disparar");
            if (prefabBala != null && puntoDisparo != null)
            {
                Instantiate(prefabBala, puntoDisparo.position, puntoDisparo.rotation);
            }
            temporizadorDisparo = tiempoEntreDisparos;
        }
    }

    // --- NUEVO: SISTEMA DE DAÑO ---
    void OnTriggerEnter2D(Collider2D collision)
    {
        // Si lo que me choca es la bala del Beagle (Asegúrate de que tu bala tenga este Tag)
        if (collision.CompareTag("BalaJugador"))
        {
            vida--; // Pierde 1 de vida
            Destroy(collision.gameObject); // Destruye la bala del jugador para que no lo atraviese

            if (vida <= 0)
            {
                Destroy(gameObject); // El soldado muere y desaparece
            }
        }
    }
}