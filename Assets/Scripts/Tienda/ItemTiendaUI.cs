using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemTiendaUI : MonoBehaviour
{
    // [SerializedField] es un atributo que permite que una variable privada sea visible y editable en el Inspector de Unity
    public ObjetoTienda objeto; // Referencia al ScriptableObject del objeto
    public TextMeshProUGUI textoNombre; // Referencia al componente de texto para mostrar el nombre del objeto
    public TextMeshProUGUI textoPrecio; // Referencia al componente de texto para mostrar el precio del objeto
    public Image imagenSprite; // Referencia al componente de imagen para mostrar el icono del objeto
    public Button botonComprar; // Referencia al botón de compra


    private void OnEnable()
    {
        botonComprar.onClick.AddListener(RealizarCompra); // Agrega el método RealizarCompra como listener al evento onClick del botón
    }

    private void OnDisable()
    {
        botonComprar.onClick.RemoveListener(RealizarCompra); // Elimina el método RealizarCompra como listener al evento onClick del botón
    }


    // Inicializa el item con los datos del objeto
    public void Inicializar(ObjetoTienda nuevoObjeto)
    {
        objeto = nuevoObjeto;
        
        // Actualiza los textos e imagen
        textoNombre.text = objeto.nombreObjeto;
        textoPrecio.text = "$" + objeto.precio.ToString();
        imagenSprite.sprite = objeto.icono;
    }

    private void RealizarCompra()
    {
        if (objeto == null) return;

        // Intenta comprar el objeto
        if (Tienda.Instancia.IntentarCompra(objeto.precio))
        {
            Debug.Log($"¡{objeto.nombreObjeto} comprado exitosamente!");
            
            if (objeto.prefabObjeto != null)
            {
                Instantiate(objeto.prefabObjeto);
            }
        }
        else
        {
            Debug.Log("Dinero insuficiente");
        }
    }
}
