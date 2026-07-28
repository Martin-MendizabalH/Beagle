using UnityEngine;

/// <summary>
/// Se adjunta al Prefab padre que contiene todos los pedacitos (Gibs).
/// Al instanciarse, empuja todos sus hijos en direcciones aleatorias.
/// </summary>
public class EfectoFragmentacion : MonoBehaviour
{
    [Header("--- Configuración de la Explosión ---")]
    [Tooltip("Fuerza mínima con la que saldrán volando los pedazos.")]
    public float fuerzaMinima = 5f;
    
    [Tooltip("Fuerza máxima con la que saldrán volando los pedazos.")]
    public float fuerzaMaxima = 12f;
    
    [Tooltip("Tiempo en segundos antes de que los pedazos desaparezcan del mapa para optimizar memoria.")]
    public float tiempoDeVida = 4f;

    void Start()
    {
        // 1. Ejecutamos la matemática para que las partes salgan volando
        ExplotarPedazos();

        // 2. Programamos la autodestrucción del contenedor al cabo de 'tiempoDeVida' segundos[cite: 2]
        Destroy(gameObject, tiempoDeVida);
    }

    private void ExplotarPedazos()
    {
        // Recorremos todos los objetos hijos (los 9 pedacitos) que están dentro de este GameObject padre
        foreach (Transform pedazo in transform)
        {
            // Intentamos obtener el Rigidbody (cuerpo con físicas) del pedacito evaluado[cite: 2]
            Rigidbody2D rb = pedazo.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                // A. Calculamos una dirección aleatoria en 2D (con tendencia a ir hacia arriba y a los lados)
                Vector2 direccionAleatoria = new Vector2(Random.Range(-1f, 1f), Random.Range(0.5f, 1f)).normalized;

                // B. Calculamos una fuerza aleatoria entre los límites que definimos
                float fuerzaAleatoria = Random.Range(fuerzaMinima, fuerzaMaxima);

                // C. Aplicamos la velocidad al Rigidbody[cite: 2] para que salga volando en esa dirección
                rb.velocity = direccionAleatoria * fuerzaAleatoria;

                // D. (Juice) Le damos un pequeño torque (giro) aleatorio para que se vea más dinámico y caótico
                rb.AddTorque(Random.Range(-15f, 15f));
            }
        }
    }
}