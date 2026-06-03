using UnityEngine;

public class NPC_Tienda : MonoBehaviour
{
    [Header("Configuración de Interfaz")]
    public GameObject panelTienda; 

    private bool jugadorCerca = false;

    void Start()
    {
        
        if (panelTienda != null)
        {
            panelTienda.SetActive(false);
        }
    }

    void Update()
    {
        // Si el jugador esta cerca y apreta e, abre la tienda
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            AbrirCerrarTienda();
        }
    }

    private void AbrirCerrarTienda()
    {
        
        bool estadoActual = panelTienda.activeSelf;
        panelTienda.SetActive(!estadoActual);

        
        Time.timeScale = panelTienda.activeSelf ? 0f : 1f;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugadorCerca = true;
           
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugadorCerca = false;
            panelTienda.SetActive(false); 
            Time.timeScale = 1f; 
        }
    }
}
