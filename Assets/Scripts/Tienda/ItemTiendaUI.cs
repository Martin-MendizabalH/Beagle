using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tarjeta reutilizable para vender un objeto o un arma.
/// </summary>
public class ItemTiendaUI : MonoBehaviour
{
    [Header("--- Artículo ---")]
    [Tooltip("Objeto consumible o genérico. Déjalo vacío cuando esta tarjeta venda un arma.")]
    public ObjetoTienda objeto;

    [Tooltip("Arma vendida por esta tarjeta. Su precio se lee directamente desde DatosArma.")]
    public DatosArma arma;

    [Header("--- Interfaz ---")]
    public TextMeshProUGUI textoNombre;
    public TextMeshProUGUI textoPrecio;
    public Image imagenSprite;
    public Button botonComprar;

    [Header("--- Compra de arma ---")]
    [SerializeField] private bool equiparArmaAlComprar = true;
    [SerializeField] private string textoArmaAdquirida = "ADQUIRIDA";

    private InventarioArmas inventarioArmas;
    private Tienda tiendaSuscrita;
    private bool inventarioSuscrito;

    private void OnEnable()
    {
        ResolverReferencias();
        if (botonComprar != null) botonComprar.onClick.AddListener(RealizarCompra);
        SuscribirseAEventos();
        ActualizarVisual();
    }

    private void Start()
    {
        // Cubre escenas donde esta tarjeta se habilita antes del Awake de Tienda.
        SuscribirseAEventos();
        ActualizarVisual();
    }

    private void OnDisable()
    {
        if (botonComprar != null) botonComprar.onClick.RemoveListener(RealizarCompra);
        DesuscribirseDeEventos();
    }

    public void Inicializar(ObjetoTienda nuevoObjeto)
    {
        objeto = nuevoObjeto;
        arma = null;
        ActualizarVisual();
    }

    public void Inicializar(DatosArma nuevaArma)
    {
        arma = nuevaArma;
        objeto = null;
        ActualizarVisual();
    }

    private void RealizarCompra()
    {
        if (Tienda.Instancia == null)
        {
            Debug.LogWarning("No existe una Tienda activa para procesar la compra.");
            return;
        }

        if (arma != null)
        {
            ComprarArma();
        }
        else if (objeto != null)
        {
            ComprarObjeto();
        }
    }

    private void ComprarArma()
    {
        ResolverInventario();
        if (inventarioArmas == null)
        {
            Debug.LogWarning($"No se encontró el inventario del jugador; no se pudo comprar {arma.nombreArma}.");
            return;
        }

        if (inventarioArmas.PoseeArma(arma))
        {
            ActualizarVisual();
            return;
        }

        if (!Tienda.Instancia.IntentarCompra(arma.precio)) return;

        if (!inventarioArmas.AdquirirArma(arma, equiparArmaAlComprar))
        {
            Tienda.Instancia.Reembolsar(arma.precio);
            Debug.LogWarning($"No se pudo añadir {arma.nombreArma} al inventario. La compra fue reembolsada.");
            return;
        }

        Debug.Log($"¡{arma.nombreArma} comprada y añadida al inventario!");
        ActualizarVisual();
    }

    private void ComprarObjeto()
    {
        Jugador jugador = FindObjectOfType<Jugador>();
        if (jugador == null)
        {
            Debug.LogWarning($"No se encontró al jugador; no se pudo comprar {objeto.nombreObjeto}.");
            return;
        }

        if (!Tienda.Instancia.IntentarCompra(objeto.precio)) return;

        jugador.AgregarPocion(1);
        if (objeto.prefabObjeto != null)
        {
            Instantiate(objeto.prefabObjeto);
        }

        Debug.Log($"¡{objeto.nombreObjeto} comprado exitosamente!");
        ActualizarVisual();
    }

    public void ActualizarVisual()
    {
        ResolverReferencias();
        ResolverInventario();

        bool esArma = arma != null;
        bool esObjeto = !esArma && objeto != null;
        bool adquirida = esArma && inventarioArmas != null && inventarioArmas.PoseeArma(arma);
        int precio = esArma ? arma.precio : esObjeto ? objeto.precio : 0;

        if (textoNombre != null)
        {
            textoNombre.text = esArma ? arma.nombreArma : esObjeto ? objeto.nombreObjeto : string.Empty;
        }

        if (textoPrecio != null)
        {
            textoPrecio.text = adquirida ? textoArmaAdquirida : precio.ToString();
        }

        if (imagenSprite != null)
        {
            Sprite icono = esArma ? arma.spriteArma : esObjeto ? objeto.icono : null;
            if (icono != null)
            {
                imagenSprite.sprite = icono;
                imagenSprite.preserveAspect = true;
            }
        }

        if (botonComprar != null)
        {
            bool hayArticulo = esArma || esObjeto;
            bool puedePagar = Tienda.Instancia != null && Tienda.Instancia.PuedePagar(precio);
            botonComprar.interactable = hayArticulo && !adquirida && puedePagar;
        }
    }

    private void ResolverReferencias()
    {
        if (botonComprar == null)
        {
            botonComprar = GetComponentInChildren<Button>(true);
        }

        TextMeshProUGUI[] textos = GetComponentsInChildren<TextMeshProUGUI>(true);
        if (textoPrecio == null || textoPrecio == textoNombre)
        {
            foreach (TextMeshProUGUI texto in textos)
            {
                if (texto.gameObject.name.ToLowerInvariant().Contains("precio"))
                {
                    textoPrecio = texto;
                    break;
                }
            }
        }

        if (textoNombre == null || textoNombre == textoPrecio)
        {
            foreach (TextMeshProUGUI texto in textos)
            {
                if (texto != textoPrecio &&
                    texto.gameObject.name.ToLowerInvariant().Contains("texto"))
                {
                    textoNombre = texto;
                    break;
                }
            }
        }
    }

    private void ResolverInventario()
    {
        if (inventarioArmas == null)
        {
            inventarioArmas = FindObjectOfType<InventarioArmas>();
        }
    }

    private void SuscribirseAEventos()
    {
        ResolverInventario();
        if (tiendaSuscrita == null && Tienda.Instancia != null)
        {
            tiendaSuscrita = Tienda.Instancia;
            tiendaSuscrita.DineroCambiado += AlCambiarDinero;
        }

        if (!inventarioSuscrito && inventarioArmas != null)
        {
            inventarioArmas.InventarioActualizado += ActualizarVisual;
            inventarioSuscrito = true;
        }
    }

    private void DesuscribirseDeEventos()
    {
        if (tiendaSuscrita != null)
        {
            tiendaSuscrita.DineroCambiado -= AlCambiarDinero;
            tiendaSuscrita = null;
        }

        if (inventarioSuscrito && inventarioArmas != null)
        {
            inventarioArmas.InventarioActualizado -= ActualizarVisual;
            inventarioSuscrito = false;
        }
    }

    private void AlCambiarDinero(int _) => ActualizarVisual();
}
