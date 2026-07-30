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
        // Siempre prioriza la tienda persistente del Nivel 1. Así, los NPC de
        // los niveles 2 y 3 abren el mismo Canvas y conservan su estado.
        if (Tienda.Instancia != null)
        {
            panelTienda = Tienda.Instancia.gameObject;
        }

        // Si se inicia directamente en otro nivel, recupera su tienda local
        // aunque el Canvas comience desactivado.
        if (panelTienda == null)
        {
            Tienda tiendaEnEscena = FindObjectOfType<Tienda>(true);

            if (tiendaEnEscena != null)
            {
                panelTienda = tiendaEnEscena.gameObject;
            }
            else
            {
                Debug.LogError("No se encontró una Tienda en la escena actual.");
                return;
            }
        }

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

            // ESCUDO 2: Solo apaga la tienda si la referencia aún existe
            if (panelTienda != null)
            {
                panelTienda.SetActive(false);
            }

            Time.timeScale = 1f; // Siempre devolvemos el tiempo a la normalidad al salir
        }
    }
}
