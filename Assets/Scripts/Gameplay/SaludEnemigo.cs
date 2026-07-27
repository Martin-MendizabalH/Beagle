using UnityEngine;

/// <summary>
/// Gestiona la vida del enemigo y su destrucción al llegar a 0.
/// </summary>
public class SaludEnemigo : MonoBehaviour
{
    [Header("--- Atributos del Enemigo ---")]
    [Tooltip("Cantidad de vida máxima del enemigo.")]
    public int saludMaxima = 30;
    
    // Variable privada para controlar la vida actual sin que otros scripts la modifiquen directamente
    private int saludActual;

    void Start()
    {
        // Inicializamos la salud al máximo cuando el enemigo aparece en la escena
        saludActual = saludMaxima;
    }

    /// <summary>
    /// Método público para restar vida al enemigo. 
    /// Será llamado por las balas al momento del impacto.
    /// </summary>
    /// <param name="cantidadDano">La cantidad de daño que inflige el proyectil.</param>
    public void RecibirDano(int cantidadDano)
    {
        saludActual -= cantidadDano;
        
        Debug.Log("Enemigo recibió " + cantidadDano + " de daño. Salud restante: " + saludActual);

        // Comprobamos si la salud ha llegado a 0 o menos
        if (saludActual <= 0)
        {
            Morir();
        }
    }

    /// <summary>
    /// Gestiona la lógica de destrucción del enemigo.
    /// </summary>
    private void Morir()
    {
        // Aquí a futuro puedes instanciar un prefab de explosión, reproducir un sonido o soltar monedas.
        
        // Destruimos el GameObject de este enemigo del mapa
        Destroy(gameObject);
    }
}