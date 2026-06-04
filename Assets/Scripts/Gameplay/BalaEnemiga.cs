using UnityEngine;
using UnityEngine.SceneManagement;

public class BalaEnemiga : MonoBehaviour
{
    [Header("Efectos Visuales")]
    // Casilla para arrastrar tu nuevo prefab de explosión
    public GameObject efectoImpactoPrefab; 

    void Start()
    {
        Destroy(gameObject, 5f); 
    }

    private void OnTriggerEnter2D(Collider2D colision)
    {
        if (colision.gameObject.CompareTag("Player")) 
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
        }
        
        // Si choca con el suelo o las paredes
        if (colision.gameObject.CompareTag("Pared")) 
        {
            // 1. Instanciamos el efecto visual un poco más abajo del centro de la bala (Opción B)
            if (efectoImpactoPrefab != null)
            {
                // Calculamos una posición restando 0.5 en el eje Y
                Vector3 posicionSuelo = transform.position + new Vector3(0f, -0.5f, 0f);
                
                // Instanciamos la explosión en esa nueva posición más baja
                Instantiate(efectoImpactoPrefab, posicionSuelo, Quaternion.identity);
            }

            // 2. Destruimos la bala
            Destroy(gameObject); 
        }
    }
}