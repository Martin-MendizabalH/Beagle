using UnityEngine;

public class Tienda : MonoBehaviour
{
    public static Tienda Instancia;

    [Header("Configuración")]
    public int dineroJugador = 100;

    private void Awake()
    {
        // Implementacion del Singleton
        if (Instancia == null)
        {
            Instancia = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Verifica si se puede comprar un objeto y realiza la compra si es posible
    public bool IntentarCompra(int precioObjeto)
    {
        if (dineroJugador >= precioObjeto)
        {   
            // Ejemplo de cómo deberías llamarlo desde tu Tienda.cs:
            Jugador scriptJugador = FindObjectOfType<Jugador>();
            if (scriptJugador != null)
            {
                scriptJugador.AgregarPocion(1); // Le suma 1 poción al inventario
            }
            dineroJugador -= precioObjeto;
            Debug.Log($"¡Compra exitosa! Saldo restante: {dineroJugador}");
            // Aqui se realiza la compra
            return true;
        }
        else
        {
            Debug.Log("Dinero insuficiente.");
            return false;
        }
    }
}