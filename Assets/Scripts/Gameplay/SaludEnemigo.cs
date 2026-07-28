using UnityEngine;

/// <summary>
/// Gestiona la vida de cualquier entidad enemiga de forma universal.
/// Al llegar a cero, invoca un prefab de fragmentación (Gibs) y se autodestruye.
/// </summary>
public class SaludEnemigo : MonoBehaviour
{
    [Header("--- Estadísticas ---")]
    [Tooltip("La cantidad de daño máximo que puede soportar el enemigo.")]
    public float vidaMaxima = 100f;
    
    // Variable privada para evitar modificaciones externas no deseadas
    private float vidaActual;

    [Header("--- Efectos de Muerte (Gibs) ---")]
    [Tooltip("El prefab que contiene las partes del enemigo cortadas (Efecto_Muerte_Enemigo).")]
    public GameObject prefabFragmentacion;

    void Start()
    {
        // Inicializamos la salud al máximo apenas el enemigo aparece en escena
        vidaActual = vidaMaxima;
    }

    /// <summary>
    /// Resta la cantidad de daño especificada y evalúa si el enemigo debe morir.
    /// </summary>
    /// <param name="cantidadDano">La cantidad de vida a restar.</param>
    public void RecibirDano(float cantidadDano)
    {
        vidaActual -= cantidadDano;

        // Opcional a futuro: Aquí puedes agregar una rutina para que el sprite parpadee en blanco o rojo.

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    /// <summary>
    /// Ejecuta la secuencia de muerte: Instancia los pedazos y destruye el GameObject base.
    /// </summary>
    private void Morir()
    {
        // 1. Instanciar los pedazos del enemigo (Efecto Gore/Gibs)
        if (prefabFragmentacion != null)
        {
            // Crear el objeto a través de un prefab "plantilla"[cite: 2] en la posición exacta donde murió
            Instantiate(prefabFragmentacion, transform.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("El enemigo murió, pero no tiene un Prefab de Fragmentación asignado en el Inspector.");
        }

        // 2. Destruimos este GameObject. Esto elimina el sprite, las físicas y TODOS los scripts adjuntos (incluyendo al RobotAcosador)[cite: 2]
        Destroy(gameObject);
    }
}