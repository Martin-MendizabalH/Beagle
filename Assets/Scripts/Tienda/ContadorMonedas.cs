using TMPro;
using UnityEngine;

public class ContadorMonedas : MonoBehaviour
{
    private TextMeshProUGUI textoMonedas;
    private Tienda tiendaSuscrita;

    private void OnEnable()
    {
        textoMonedas = GetComponent<TextMeshProUGUI>();
        IntentarSuscribirse();
    }

    private void Start()
    {
        // En Start ya se ejecutaron todos los Awake de la escena.
        IntentarSuscribirse();
    }

    private void OnDisable()
    {
        if (tiendaSuscrita != null)
        {
            tiendaSuscrita.DineroCambiado -= ActualizarTexto;
            tiendaSuscrita = null;
        }
    }

    private void IntentarSuscribirse()
    {
        if (tiendaSuscrita != null || Tienda.Instancia == null) return;

        tiendaSuscrita = Tienda.Instancia;
        tiendaSuscrita.DineroCambiado += ActualizarTexto;
        ActualizarTexto(tiendaSuscrita.DineroJugador);
    }

    private void ActualizarTexto(int cantidad)
    {
        if (textoMonedas != null) textoMonedas.text = cantidad.ToString();
    }
}
