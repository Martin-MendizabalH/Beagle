using UnityEngine;

[RequireComponent(typeof(SaludEnemigo))] // Usa el mismo sistema de vida profesional
public class EnemigoVoladorIA : MonoBehaviour
{
    [Header("--- Movimiento Vertical ---")]
    public float velocidadVuelo = 2f;
    public float distanciaMovimiento = 2.5f; // Cuántos cuadros sube y baja desde su centro
    
    private Vector2 posicionInicial;
    private bool moviendoArriba = true;

    [Header("--- Ataque ---")]
    public GameObject balaPrefab; // Aquí pondrás tu NUEVA bala
    public float velocidadBala = 6f;
    public float tiempoEntreDisparos = 2f;

    private Transform jugador;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // Guardamos el punto exacto donde lo pusiste en el mapa para que suba y baje desde ahí
        posicionInicial = transform.position; 
        
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Busca al Beagle
        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null) jugador = objJugador.transform;

        // Comienza a disparar en bucle infinito
        InvokeRepeating("Disparar", 1f, tiempoEntreDisparos);
    }

    void Update()
    {
        if (jugador == null) return;

        // 1. Mirar siempre hacia el jugador
        if (jugador.position.x > transform.position.x)
            spriteRenderer.flipX = false;
        else
            spriteRenderer.flipX = true;

        // 2. Movimiento Vertical (Tipo Paratroopa)
        float limiteArriba = posicionInicial.y + distanciaMovimiento;
        float limiteAbajo = posicionInicial.y - distanciaMovimiento;

        if (moviendoArriba)
        {
            // Sube
            transform.Translate(Vector2.up * velocidadVuelo * Time.deltaTime);
            if (transform.position.y >= limiteArriba) moviendoArriba = false; // Cambia de dirección
        }
        else
        {
            // Baja
            transform.Translate(Vector2.down * velocidadVuelo * Time.deltaTime);
            if (transform.position.y <= limiteAbajo) moviendoArriba = true; // Cambia de dirección
        }
    }

    void Disparar()
    {
        if (jugador == null || balaPrefab == null) return;

        // Activa tu animación de disparo
        anim.SetTrigger("Disparar");

        // Calcula la dirección hacia el jugador
        Vector2 direccionHaciaJugador = (jugador.position - transform.position).normalized;

        // Instancia la nueva bala
        GameObject proyectil = Instantiate(balaPrefab, transform.position, Quaternion.identity);

        // Rotar la bala (opcional, por si la bala tiene forma de flecha o algo así)
        float angulo = Mathf.Atan2(direccionHaciaJugador.y, direccionHaciaJugador.x) * Mathf.Rad2Deg;
        proyectil.transform.rotation = Quaternion.Euler(0, 0, angulo);

        // Le da la velocidad a la bala
        Rigidbody2D rbProyectil = proyectil.GetComponent<Rigidbody2D>();
        if (rbProyectil != null)
        {
            rbProyectil.velocity = direccionHaciaJugador * velocidadBala;
        }
    }

    void OnDestroy()
    {
        // Evita errores cuando matas al enemigo
        CancelInvoke("Disparar");
    }
}