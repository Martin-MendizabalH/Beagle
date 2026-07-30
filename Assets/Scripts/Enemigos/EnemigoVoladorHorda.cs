using UnityEngine;

[RequireComponent(typeof(SaludEnemigo))]
public class EnemigoVoladorHorda : MonoBehaviour
{
    [Header("--- Movimiento ---")]
    public float velocidadVuelo = 2.5f;

    [Header("--- Ataque ---")]
    public GameObject balaPrefab;
    public float velocidadBala = 6f;
    public float tiempoEntreDisparos = 2f;

    private Transform jugador;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null) jugador = objJugador.transform;

        // Comienza a disparar automáticamente
        InvokeRepeating("Disparar", 1f, tiempoEntreDisparos);
    }

    void Update()
    {
        if (jugador == null) return;

        // 1. Mirar hacia el jugador
        if (jugador.position.x > transform.position.x)
            spriteRenderer.flipX = false;
        else
            spriteRenderer.flipX = true;

        // 2. Moverse SOLO en el eje X (Horizontal), manteniendo su altura original en Y
        Vector2 posicionObjetivo = new Vector2(jugador.position.x, transform.position.y);
        transform.position = Vector2.MoveTowards(transform.position, posicionObjetivo, velocidadVuelo * Time.deltaTime);
    }

    void Disparar()
    {
        if (jugador == null || balaPrefab == null) return;

        anim.SetTrigger("Disparar");

        // Calcula la dirección de la bala hacia el jugador
        Vector2 direccionHaciaJugador = (jugador.position - transform.position).normalized;
        GameObject proyectil = Instantiate(balaPrefab, transform.position, Quaternion.identity);

        float angulo = Mathf.Atan2(direccionHaciaJugador.y, direccionHaciaJugador.x) * Mathf.Rad2Deg;
        proyectil.transform.rotation = Quaternion.Euler(0, 0, angulo);

        Rigidbody2D rbProyectil = proyectil.GetComponent<Rigidbody2D>();
        if (rbProyectil != null)
        {
            rbProyectil.velocity = direccionHaciaJugador * velocidadBala;
        }
    }

    void OnDestroy()
    {
        CancelInvoke("Disparar");
    }
}