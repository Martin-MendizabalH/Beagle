using UnityEngine;

/// <summary>
/// Gestiona la física y seguimiento de un misil teledirigido.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MisilTeledirigido : MonoBehaviour
{
    [Header("--- Vuelo y Físicas ---")]
    [Tooltip("Velocidad de avance constante del misil.")]
    public float velocidad = 4f;
    [Tooltip("Qué tan rápido puede girar. Un valor bajo hace que le cueste dar curvas cerradas.")]
    public float velocidadRotacion = 150f; 

    [Header("--- Daño y Vida ---")]
    public int danoAlJugador = 1;
    [Tooltip("Tiempo máximo de vuelo antes de quedarse sin combustible.")]
    public float tiempoDeVida = 7f;

    private Transform jugador;
    private Rigidbody2D rb;

    void Start()
    {
        // Obtener Rigidbody (cuerpo con físicas) del proyectil creado
        rb = GetComponent<Rigidbody2D>();
        
        // Se destruye el proyectil al cabo de los segundos para liberar memoria[cite: 3]
        Destroy(gameObject, tiempoDeVida);

        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null) jugador = objJugador.transform;
    }

    void FixedUpdate()
    {
        if (jugador != null)
        {
            // 1. Calculamos el vector de dirección hacia el jugador
            Vector2 direccion = (Vector2)jugador.position - rb.position;
            direccion.Normalize();

            // 2. Calculamos hacia qué lado debe girar usando Producto Cruz
            // Asumimos que el misil apunta hacia la DERECHA ("transform.right") en su sprite original
            float cantidadGiro = Vector3.Cross(transform.right, direccion).z;

            // 3. Aplicamos la rotación física limitando la curva
            rb.angularVelocity = cantidadGiro * velocidadRotacion;

            // 4. Aplicar velocidad al Rigidbody[cite: 3] empujando siempre "hacia el frente" del misil
            rb.velocity = transform.right * velocidad;
        }
    }

    // Detecta la colision con algun gameObject[cite: 3]
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Dañamos al jugador (Utilizamos la función que creamos en Jugador.cs)
            Jugador scriptJugador = collision.GetComponent<Jugador>();
            if (scriptJugador != null) scriptJugador.RecibirDano(danoAlJugador, transform.position);
            
            Explotar();
        }
        // Si choca con el mapa, se destruye ("torear" al misil)
        else if (collision.gameObject.CompareTag("Pared"))
        {
            Explotar();
        }
    }

    private void Explotar()
    {
        // TODO: Instanciar prefab de explosión visual/sonido aquí.
        Destroy(gameObject); // Se destruye a si mismo[cite: 3]
    }
}