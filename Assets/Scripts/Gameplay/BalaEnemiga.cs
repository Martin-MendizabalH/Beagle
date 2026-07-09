using UnityEngine;
using UnityEngine.SceneManagement;

public class BalaEnemiga : MonoBehaviour
{
    [Header("Efectos Visuales")]
    public GameObject efectoImpactoPrefab; 

    void Start()
    {
        Destroy(gameObject, 5f); 
    }

    private void OnTriggerEnter2D(Collider2D colision)
    {
        // CUANDO CHOCA CON EL JUGADOR
        if (colision.gameObject.CompareTag("Player")) 
        {
            // Solo destruimos la bala, ¡el script Jugador.cs se encargará del daño!
            Destroy(gameObject); 
        }
        
        // CUANDO CHOCA CON LA PARED
        if (colision.gameObject.CompareTag("Pared")) 
        {
            if (efectoImpactoPrefab != null)
            {
                Vector3 posicionSuelo = transform.position + new Vector3(0f, -0.5f, 0f);
                Instantiate(efectoImpactoPrefab, posicionSuelo, Quaternion.identity);
            }

            Destroy(gameObject); 
        }
    }
}