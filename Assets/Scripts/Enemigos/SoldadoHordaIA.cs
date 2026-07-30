using UnityEngine;

public class SoldadoHordaIA : MonoBehaviour
{
    [Header("--- Horda y Combate ---")]
    public float distanciaParaFrenar = 5f; // Camina hacia ti y se frena a los 5 metros
    public float velocidad = 2.5f;
    public GameObject prefabBala;
    public Transform puntoDisparo;
    public float tiempoEntreDisparos = 1.5f;
    private float temporizadorDisparo;

    [Header("--- Vida ---")]
    public int vida = 3;

    private Transform jugador;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        temporizadorDisparo = tiempoEntreDisparos;

        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null) jugador = objJugador.transform;
    }

    void Update()
    {
        if (jugador == null) return;

        float distancia = Vector2.Distance(transform.position, jugador.position);
        MirarAlJugador();

        // Si estás lejos, no dispara, se acerca a ti
        if (distancia > distanciaParaFrenar)
        {
            // Camina hacia el jugador solo en X (para no salir volando hacia arriba)
            Vector2 objetivo = new Vector2(jugador.position.x, transform.position.y);
            transform.position = Vector2.MoveTowards(transform.position, objetivo, velocidad * Time.deltaTime);
        }
        else
        {
            // Cuando ya está a 5 metros o menos, se frena y empieza a disparar
            ManejarDisparo();
        }
    }

    void MirarAlJugador()
    {
        bool mirarIzquierda = jugador.position.x < transform.position.x;
        spriteRenderer.flipX = mirarIzquierda;
        
        float posX = Mathf.Abs(puntoDisparo.localPosition.x);
        puntoDisparo.localPosition = new Vector2(mirarIzquierda ? -posX : posX, puntoDisparo.localPosition.y);
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

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("BalaJugador"))
        {
            vida--;
            Destroy(collision.gameObject); // Destruye tu bala

            if (vida <= 0)
            {
                GestorHordaNivel1.enemigosMuertos++; // Le avisa al Gestor
                Destroy(gameObject); // El soldado muere
            }
        }
    }
}