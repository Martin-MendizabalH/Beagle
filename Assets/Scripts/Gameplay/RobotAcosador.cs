using UnityEngine;

public class RobotAcosador : MonoBehaviour
{
    public Transform jugador; 
    public float distanciaAtras = 3f; 
    
    public GameObject balaPrefab; 
    public float velocidadBala = 8f; 
    public float tiempoEntreDisparos = 1.5f; 

    [Header("--- Animación de Entrada ---")]
    public float velocidadEntrada = 6f; // Qué tan rápido entra volando a la pantalla
    private bool enPosicion = false; // Controla si ya llegó a la espalda del jugador

    // NOTA: Borramos el método Start() porque ya no queremos que dispare al inicio.

    void Update()
    {
        if (jugador == null) return;

        // Calculamos el punto exacto donde el robot DEBE quedarse (a la espalda del jugador)
        float posicionObjetivoX = jugador.position.x - distanciaAtras;

        // FASE 1: Haciendo su entrada épica
        if (!enPosicion) 
        {
            // Movemos al robot hacia la derecha de forma suave usando MoveTowards y Time.deltaTime
            float nuevaX = Mathf.MoveTowards(transform.position.x, posicionObjetivoX, velocidadEntrada * Time.deltaTime);
            transform.position = new Vector2(nuevaX, transform.position.y);

            // Si su posición ya alcanzó el objetivo, cambiamos de fase
            if (transform.position.x >= posicionObjetivoX)
            {
                enPosicion = true; // Ya llegó a su posición
                
                // RECIÉN AHORA le ordenamos que empiece a disparar cíclicamente
                InvokeRepeating("Disparar", 0.5f, tiempoEntreDisparos);
            }
        }
        // FASE 2: Ya está en posición, comportamiento normal de persecución
        else 
        {
            transform.position = new Vector2(posicionObjetivoX, transform.position.y);
        }
    }

    void Disparar()
    {
        if (jugador == null) return; 

        Vector3 posicionInicial = transform.position;
        Vector2 direccionHaciaJugador = (jugador.position - transform.position).normalized;

        GameObject proyectil = Instantiate(balaPrefab, posicionInicial, Quaternion.identity);

        float angulo = Mathf.Atan2(direccionHaciaJugador.y, direccionHaciaJugador.x) * Mathf.Rad2Deg;
        proyectil.transform.rotation = Quaternion.Euler(0, 0, angulo);

        Rigidbody2D rbProyectil = proyectil.GetComponent<Rigidbody2D>();
        rbProyectil.velocity = direccionHaciaJugador * velocidadBala; 
    }
}