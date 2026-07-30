using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestiona el inventario de armas del jugador con un enfoque de UI Minimalista.
/// </summary>
public class InventarioArmas : MonoBehaviour
{
    [Header("--- Inventario ---")]
    public DatosArma[] armasDisponibles;
    
    [Header("--- Referencias del Jugador ---")]
    public SpriteRenderer spriteArma;
    
    [Header("--- Interfaz Minimalista ---")]
    [Tooltip("El componente Image donde se muestra el Sprite del arma equipada")]
    public Image iconoArmaEquipada;
    
    [Tooltip("El Animator ubicado en el marco de la UI para el efecto visual")]
    public Animator animatorUI; 
    
    private ControladorArmas controladorArmas;
    private int indiceArmaActual = 0;

    void Start()
    {
        // Obtenemos el script de comportamiento de nuestro GameObject
        controladorArmas = GetComponent<ControladorArmas>();

        // Equipamos el arma inicial al partir el juego
        if (armasDisponibles != null && armasDisponibles.Length > 0)
        {
            EquiparArma(0);
        }
    }

    void Update()
    {
        // Detectamos el cambio de arma con TAB
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CambiarArmaSiguiente();
        }
    }

    void CambiarArmaSiguiente()
    {
        if (armasDisponibles == null || armasDisponibles.Length <= 1) return;

        indiceArmaActual++;
        
        if (indiceArmaActual >= armasDisponibles.Length)
        {
            indiceArmaActual = 0;
        }
        
        EquiparArma(indiceArmaActual);
    }

    void EquiparArma(int indice)
    {
        if (armasDisponibles == null ||
            indice < 0 ||
            indice >= armasDisponibles.Length ||
            armasDisponibles[indice] == null)
        {
            return;
        }

        DatosArma nuevaArma = armasDisponibles[indice];

        // 1. Actualizar el Sprite en la mano del jugador
        if (spriteArma != null)
        {
            spriteArma.sprite = nuevaArma.spriteArma;
        }

        // 2. Actualizar las mecánicas (ControladorArmas)
        if (controladorArmas != null)
        {
            controladorArmas.ActualizarDatosArma(nuevaArma);
        }

        // 3. Actualizar el ícono en la Interfaz
        if (iconoArmaEquipada != null)
        {
            iconoArmaEquipada.sprite = nuevaArma.spriteArma;
            iconoArmaEquipada.preserveAspect = true;
            iconoArmaEquipada.color = Color.white;
        }

        // 4. Gatillar el "Juice" visual en el marco de la UI
        if (animatorUI != null)
        {
            // Enviamos la señal al Animator para que ejecute la animación de destello/salto
            animatorUI.SetTrigger("CambioArma");
        }
    }
}
