using UnityEngine;

public class SoldadoEnemigo : MonoBehaviour
{
    [Header("Estadísticas del Soldado")]
    public float vida = 30f;
    public float velocidad = 2f;

    [Header("Patrulla y Giro")]
    public float tiempoCambioDireccion = 3f;
    private float temporizadorGiro;
    private bool mirandoDerecha = true;

    [Header("Detección y Disparo")]
    public Transform jugador;
    public float distanciaDeteccion = 6f;
    public float cadenciaDisparo = 1.5f;
    private float temporizadorDisparo;
    public GameObject prefabBalaEnemigo;
    public Transform puntoDisparo;

    // Referencias a Componentes
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>(); // Obtenemos el Animator del soldado

        temporizadorGiro = tiempoCambioDireccion;

        if (jugador == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                jugador = playerObj.transform;
        }
    }

    void Update()
    {
        if (jugador != null)
        {
            float distanciaAlJugador = Vector2.Distance(transform.position, jugador.position);

            if (distanciaAlJugador <= distanciaDeteccion)
            {
                ApuntarAlJugador();
                ManejarDisparo();
            }
            else
            {
                ManejarPatrulla();
            }
        }
        else
        {
            ManejarPatrulla();
        }
    }

    void ManejarPatrulla()
    {
        float direccionX = mirandoDerecha ? 1f : -1f;
        rb.velocity = new Vector2(direccionX * velocidad, rb.velocity.y);

        temporizadorGiro -= Time.deltaTime;
        if (temporizadorGiro <= 0)
        {
            Girar();
            temporizadorGiro = tiempoCambioDireccion;
        }
    }

    void ApuntarAlJugador()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);

        if (jugador.position.x > transform.position.x && !mirandoDerecha)
        {
            Girar();
        }
        else if (jugador.position.x < transform.position.x && mirandoDerecha)
        {
            Girar();
        }
    }

    void Girar()
    {
        mirandoDerecha = !mirandoDerecha;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !mirandoDerecha;
        }
    }

    void ManejarDisparo()
    {
        temporizadorDisparo -= Time.deltaTime;

        if (temporizadorDisparo <= 0)
        {
            Disparar();
            temporizadorDisparo = cadenciaDisparo;
        }
    }

    void Disparar()
    {
        // 1. Disparamos el Trigger para reproducir SoldierShooting
        if (animator != null)
        {
            animator.SetTrigger("disparar");
        }

        // 2. Instanciamos la bala
        if (prefabBalaEnemigo != null)
        {
            Vector3 posicionSpawn = (puntoDisparo != null) ? puntoDisparo.position : transform.position;
            GameObject bala = Instantiate(prefabBalaEnemigo, posicionSpawn, Quaternion.identity);

            Rigidbody2D rbBala = bala.GetComponent<Rigidbody2D>();
            if (rbBala != null)
            {
                float direccionDisparo = mirandoDerecha ? 1f : -1f;
                rbBala.velocity = new Vector2(direccionDisparo * 8f, 0);
            }
        }
    }

    public void RecibirDano(float cantidad)
    {
        vida -= cantidad;
        if (vida <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("BalaJugador"))
        {
            RecibirDano(10f);
            Destroy(collision.gameObject);
        }
    }
}