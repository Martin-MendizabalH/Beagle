using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Necesario para manipular componentes Image del Canvas
using TMPro;

/// <summary>
/// Gestiona la lista de armas del jugador, el cambio con la tecla TAB
/// y la visualización temporal del Canvas del Inventario.
/// </summary>
public class InventarioArmas : MonoBehaviour
{
    [Header("--- Inventario ---")]
    public DatosArma[] armasDisponibles;
    
    [Header("--- Referencias Visuales ---")]
    public SpriteRenderer spriteArma;
    
    [Header("--- Interfaz (Canvas MVP) ---")]
    public GameObject canvasInventario;
    public TextMeshProUGUI textoNombreArma;
    
    [Tooltip("El componente Image de la UI donde se mostrará el dibujo del arma")]
    public Image iconoArmaEquipada; // <--- NUEVA VARIABLE PARA LA UI
    
    public float tiempoMostrarCanvas = 1.5f;
    
    private ControladorArmas controladorArmas;
    private int indiceArmaActual = 0;
    private Coroutine rutinaCanvas;

    void Start()
    {
        controladorArmas = GetComponent<ControladorArmas>();

        if (canvasInventario != null) 
        {
            canvasInventario.SetActive(false);
        }

        if (armasDisponibles.Length > 0)
        {
            EquiparArma(0);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CambiarArmaSiguiente();
        }
    }

    void CambiarArmaSiguiente()
    {
        if (armasDisponibles.Length <= 1) return;

        indiceArmaActual++;
        
        if (indiceArmaActual >= armasDisponibles.Length)
        {
            indiceArmaActual = 0;
        }
        
        EquiparArma(indiceArmaActual);
        MostrarUITemporalmente();
    }

    void EquiparArma(int indice)
    {
        if (armasDisponibles.Length == 0 || armasDisponibles[indice] == null) return;

        DatosArma nuevaArma = armasDisponibles[indice];

        // 1. Cambiamos el sprite visual en las manos del Beagle
        if (spriteArma != null)
        {
            spriteArma.sprite = nuevaArma.spriteArma;
        }

        // 2. Le pasamos los datos mecánicos al Controlador de Armas
        if (controladorArmas != null)
        {
            controladorArmas.ActualizarDatosArma(nuevaArma);
        }

        // 3. Actualizamos el texto en el Canvas
        if (textoNombreArma != null)
        {
            textoNombreArma.text = nuevaArma.nombreArma;
        }

        // 4. NUEVA LÓGICA: Actualizamos el icono en el Canvas
        if (iconoArmaEquipada != null)
        {
            iconoArmaEquipada.sprite = nuevaArma.spriteArma;
            
            // Forzamos a Unity a mantener las proporciones originales del Pixel Art
            iconoArmaEquipada.preserveAspect = true; 
            
            // Nos aseguramos de que el color sea totalmente opaco
            iconoArmaEquipada.color = Color.white;
        }
    }

    void MostrarUITemporalmente()
    {
        if (canvasInventario == null) return;
        
        if (rutinaCanvas != null)
        {
            StopCoroutine(rutinaCanvas);
        }
        
        rutinaCanvas = StartCoroutine(RutinaMostrarCanvas());
    }

    private IEnumerator RutinaMostrarCanvas()
    {
        canvasInventario.SetActive(true);
        yield return new WaitForSeconds(tiempoMostrarCanvas);
        canvasInventario.SetActive(false);
    }
}