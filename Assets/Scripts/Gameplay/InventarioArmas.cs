using System.Collections;
using UnityEngine;
using TMPro; // Importamos TextMesh Pro para la UI
using UnityEngine.UI;

/// <summary>
/// Gestiona el inventario activo del jugador, permitiendo ciclar entre armas
/// mediante la tecla TAB y mostrando un feedback visual temporal en la UI.
/// </summary>
public class InventarioArmas : MonoBehaviour
{
    [Header("Configuración del Inventario")]
    [SerializeField] private DatosArma[] armasDisponibles; // Lista de armas (Pistola, Metralleta, Katana)
    private int indiceArmaActual = 0;

    [Header("Referencias de UI")]
    [SerializeField] private GameObject panelUI_Inventario; // El contenedor de la UI
    [SerializeField] private TextMeshProUGUI textoNombreArma; // Texto TMP para el nombre
    [SerializeField] private Image imagenIconoArma;         // Imagen para el icono
    [SerializeField] private float tiempoVisibleUI = 1.5f;  // Cuánto tiempo se muestra en pantalla

    private Coroutine rutinaOcultarUI;

    private void Start()
    {
        // Aseguramos que la UI inicie oculta al cargar la escena
        if (panelUI_Inventario != null)
        {
            panelUI_Inventario.SetActive(false);
        }

        // Equipar el arma inicial por defecto
        EquiparArma(indiceArmaActual, false); 
    }

    private void Update()
    {
        // Detectar la tecla TAB para alternar armas (Input clásico de Cuphead)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            AlternarSiguienteArma();
        }
    }

    /// <summary>
    /// Cambia el índice actual para apuntar al siguiente elemento del arreglo de forma cíclica.
    /// </summary>
    private void AlternarSiguienteArma()
    {
        if (armasDisponibles.Length == 0) return;

        // Avanzar en el índice. El operador módulo (%) asegura que vuelva a 0 al pasar el límite
        indiceArmaActual = (indiceArmaActual + 1) % armasDisponibles.Length;

        // Equipar la nueva arma y activar la UI temporal
        EquiparArma(indiceArmaActual, true);
    }

    /// <summary>
    /// Aplica los cambios lógicos del arma actual y maneja la visualización de la UI.
    /// </summary>
    private void EquiparArma(int indice, bool mostrarUI)
    {
        DatosArma armaEquipada = armasDisponibles[indice];
        
        // NOTA DE DESARROLLO: Aquí conectarás esto con tu script DisparoBeagle.cs o Jugador.cs
        // Ejemplo: miScriptDisparo.CambiarConfiguracionArma(armaEquipada);
        Debug.Log($"Beagle ha equipado: {armaEquipada.nombreArma}");

        if (mostrarUI)
        {
            // Si ya hay una rutina corriendo para ocultar la UI, la detenemos para resetear el tiempo
            if (rutinaOcultarUI != null)
            {
                StopCoroutine(rutinaOcultarUI);
            }

            // Iniciamos la corrutina que muestra la UI y la oculta tras unos segundos
            rutinaOcultarUI = StartCoroutine(MostrarUI_Temporal(armaEquipada));
        }
    }

    /// <summary>
    /// Corrutina que maneja el Pop-up temporal de la UI estilo Cuphead.
    /// </summary>
    private IEnumerator MostrarUI_Temporal(DatosArma arma)
    {
        // Actualizar datos de la UI
        textoNombreArma.text = arma.nombreArma.ToUpper(); // En mayúsculas para diseño Run 'n' Gun
        imagenIconoArma.sprite = arma.iconoArma;

        // Mostrar el panel
        panelUI_Inventario.SetActive(true);

        // Esperar el tiempo configurado en el Inspector
        yield return new WaitForSeconds(tiempoVisibleUI);

        // Ocultar el panel de forma limpia
        panelUI_Inventario.SetActive(false);
    }
}
