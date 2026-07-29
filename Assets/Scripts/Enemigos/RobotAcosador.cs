using UnityEngine;

/// <summary>
/// Gestiona la entrada cinemática, la persecución y el disparo cíclico del Robot.
/// Delega la gestión de vida y muerte al componente SaludEnemigo.
/// </summary>
[RequireComponent(typeof(SaludEnemigo))] // Fuerza a Unity a añadir el script de vida si no está
public class RobotAcosador : MonoBehaviour
{
    [Header("--- Referencias ---")]
    [Tooltip("El transform del jugador al que el robot va a seguir.")]
    public Transform jugador; 
    
    [Header("--- Movimiento y Posicionamiento ---")]
    [Tooltip("Distancia en el eje X que mantendrá a la espalda del jugador.")]
    public float distanciaAtras = 3f; 
    [Tooltip("Qué tan rápido entra volando a la pantalla en su fase inicial.")]
    public float velocidadEntrada = 6f; 
    
    [Header("--- Ataque ---")]
    [Tooltip("El prefab de la bala que disparará el robot.")]
    public GameObject balaPrefab; 
    [Tooltip("La velocidad a la que viajará la bala.")]
    public float velocidadBala = 8f; 
    [Tooltip("Tiempo en segundos entre cada disparo una vez que esté en posición.")]
    public float tiempoEntreDisparos = 1.5f; 

    // Variables de control interno
    private bool enPosicion = false; // Controla si ya llegó a la espalda del jugador

    void Start()
    {
        // Medida de seguridad: Si olvidaste asignar al jugador en el Inspector, lo busca automáticamente
        if (jugador == null)
        {
            GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
            if (objJugador != null) jugador = objJugador.transform;
        }
    }

    void Update()
    {
        if (jugador == null) return;

        // Calculamos el punto exacto donde el robot DEBE quedarse (a la espalda del jugador)
        float posicionObjetivoX = jugador.position.x - distanciaAtras;

        // FASE 1: Haciendo su entrada épica
        if (!enPosicion) 
        {
            // Movemos al robot hacia la derecha de forma suave usando MoveTowards
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
            // Se "ancla" matemáticamente a la espalda del jugador en el eje X
            transform.position = new Vector2(posicionObjetivoX, transform.position.y);
        }
    }

    /// <summary>
    /// Instancia una bala y la dispara en dirección al jugador.
    /// </summary>
    void Disparar()
    {
        if (jugador == null || balaPrefab == null) return; 

        Vector3 posicionInicial = transform.position;
        Vector2 direccionHaciaJugador = (jugador.position - transform.position).normalized;

        // Crear proyectil a lanzar a través de un prefab "plantilla"
        GameObject proyectil = Instantiate(balaPrefab, posicionInicial, Quaternion.identity);

        // Rotamos visualmente la bala para que apunte hacia donde viaja
        float angulo = Mathf.Atan2(direccionHaciaJugador.y, direccionHaciaJugador.x) * Mathf.Rad2Deg;
        proyectil.transform.rotation = Quaternion.Euler(0, 0, angulo);

        // Obtener Rigidbody (cuerpo con físicas) del proyectil creado[cite: 2]
        Rigidbody2D rbProyectil = proyectil.GetComponent<Rigidbody2D>();
        
        if (rbProyectil != null)
        {
            // Aplicar velocidad al Rigidbody[cite: 2]
            rbProyectil.velocity = direccionHaciaJugador * velocidadBala; 
        }
    }

    /// <summary>
    /// Se ejecuta automáticamente cuando este GameObject es destruido por el script SaludEnemigo.
    /// </summary>
    void OnDestroy()
    {
        // Limpiamos los procesos en segundo plano para evitar errores de memoria (NullReference)
        CancelInvoke("Disparar");
    }
}