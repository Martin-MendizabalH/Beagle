using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mantiene el catálogo y las armas realmente adquiridas por el jugador.
/// </summary>
public class InventarioArmas : MonoBehaviour
{
    [Header("--- Catálogo de armas ---")]
    [Tooltip("Todas las armas que pueden formar parte del inventario. No significa que estén adquiridas.")]
    public DatosArma[] armasDisponibles;

    [Tooltip("Armas que posee el jugador al comenzar. Si queda vacío, se usa la primera arma del catálogo.")]
    [SerializeField] private DatosArma[] armasIniciales;

    [Header("--- Referencias del Jugador ---")]
    public SpriteRenderer spriteArma;

    [Header("--- Interfaz Minimalista ---")]
    [Tooltip("El componente Image donde se muestra el Sprite del arma equipada")]
    public Image iconoArmaEquipada;

    [Tooltip("El Animator ubicado en el marco de la UI para el efecto visual")]
    public Animator animatorUI;

    private readonly List<DatosArma> armasAdquiridas = new List<DatosArma>();
    private ControladorArmas controladorArmas;
    private int indiceArmaActual;

    public event Action InventarioActualizado;

    public IReadOnlyList<DatosArma> ArmasAdquiridas => armasAdquiridas;
    public DatosArma ArmaEquipada =>
        indiceArmaActual >= 0 && indiceArmaActual < armasAdquiridas.Count
            ? armasAdquiridas[indiceArmaActual]
            : null;

    private void Start()
    {
        controladorArmas = GetComponent<ControladorArmas>();
        InicializarInventario();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CambiarArmaSiguiente();
        }
    }

    private void InicializarInventario()
    {
        armasAdquiridas.Clear();

        if (armasIniciales != null && armasIniciales.Length > 0)
        {
            foreach (DatosArma arma in armasIniciales)
            {
                ProgresoArmasSesion.Registrar(arma);
                AgregarSinNotificar(arma);
            }
        }
        else if (armasDisponibles != null)
        {
            foreach (DatosArma arma in armasDisponibles)
            {
                if (arma == null) continue;
                ProgresoArmasSesion.Registrar(arma);
                AgregarSinNotificar(arma);
                break;
            }
        }

        if (armasDisponibles != null)
        {
            foreach (DatosArma arma in armasDisponibles)
            {
                if (ProgresoArmasSesion.Posee(arma))
                {
                    AgregarSinNotificar(arma);
                }
            }
        }

        indiceArmaActual = armasAdquiridas.Count > 0 ? 0 : -1;
        if (indiceArmaActual >= 0)
        {
            AplicarArmaEquipada();
        }

        InventarioActualizado?.Invoke();
    }

    public void CambiarArmaSiguiente()
    {
        if (armasAdquiridas.Count <= 1) return;

        indiceArmaActual++;
        if (indiceArmaActual >= armasAdquiridas.Count)
        {
            indiceArmaActual = 0;
        }

        AplicarArmaEquipada();
    }

    public bool PoseeArma(DatosArma arma)
    {
        return arma != null && armasAdquiridas.Contains(arma);
    }

    public bool AdquirirArma(DatosArma arma, bool equiparAlComprar = true)
    {
        if (arma == null || PoseeArma(arma) || !PerteneceAlCatalogo(arma))
        {
            return false;
        }

        ProgresoArmasSesion.Registrar(arma);
        armasAdquiridas.Add(arma);
        if (equiparAlComprar)
        {
            indiceArmaActual = armasAdquiridas.Count - 1;
            AplicarArmaEquipada();
        }

        InventarioActualizado?.Invoke();
        return true;
    }

    public bool EquiparArma(DatosArma arma)
    {
        int indice = armasAdquiridas.IndexOf(arma);
        if (indice < 0) return false;

        indiceArmaActual = indice;
        AplicarArmaEquipada();
        InventarioActualizado?.Invoke();
        return true;
    }

    private void AplicarArmaEquipada()
    {
        DatosArma nuevaArma = ArmaEquipada;
        if (nuevaArma == null) return;

        if (spriteArma != null)
        {
            spriteArma.sprite = nuevaArma.spriteArma;
        }

        if (controladorArmas != null)
        {
            controladorArmas.ActualizarDatosArma(nuevaArma);
        }

        if (iconoArmaEquipada != null)
        {
            iconoArmaEquipada.sprite = nuevaArma.spriteArma;
            iconoArmaEquipada.preserveAspect = true;
            iconoArmaEquipada.color = Color.white;
        }

        if (animatorUI != null)
        {
            animatorUI.SetTrigger("CambioArma");
        }
    }

    private bool PerteneceAlCatalogo(DatosArma arma)
    {
        if (armasDisponibles == null) return false;

        foreach (DatosArma armaDisponible in armasDisponibles)
        {
            if (armaDisponible == arma) return true;
        }

        return false;
    }

    private void AgregarSinNotificar(DatosArma arma)
    {
        if (arma != null && !armasAdquiridas.Contains(arma))
        {
            armasAdquiridas.Add(arma);
        }
    }
}
