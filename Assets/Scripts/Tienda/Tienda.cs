using System;
using UnityEngine;

/// <summary>
/// Monedero de la partida y punto único para validar pagos.
/// La entrega del artículo corresponde a cada ItemTiendaUI.
/// </summary>
public class Tienda : MonoBehaviour
{
    public static Tienda Instancia;

    [Header("Configuración")]
    [Min(0)]
    public int dineroJugador = 100;

    public event Action<int> DineroCambiado;

    public int DineroJugador => dineroJugador;

    private void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
            // La interfaz que contiene esta tienda debe mantenerse al pasar
            // desde el Nivel 1 a los niveles 2 y 3.
            DontDestroyOnLoad(gameObject);
        }
        else if (Instancia != this)
        {
            // Las tiendas incluidas en los otros niveles son solo respaldo si
            // se inicia directamente en ellos; si ya existe la del Nivel 1,
            // no debe reemplazarse.
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        DineroCambiado?.Invoke(dineroJugador);
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    public void AgregarMonedas(int cantidad)
    {
        if (cantidad <= 0) return;

        dineroJugador += cantidad;
        DineroCambiado?.Invoke(dineroJugador);
    }

    public bool PuedePagar(int precioObjeto)
    {
        return precioObjeto >= 0 && dineroJugador >= precioObjeto;
    }

    public bool IntentarCompra(int precioObjeto)
    {
        if (!PuedePagar(precioObjeto))
        {
            Debug.Log("Dinero insuficiente.");
            return false;
        }

        dineroJugador -= precioObjeto;
        DineroCambiado?.Invoke(dineroJugador);
        Debug.Log($"¡Compra exitosa! Saldo restante: {dineroJugador}");
        return true;
    }

    public void Reembolsar(int cantidad)
    {
        AgregarMonedas(cantidad);
    }
}
