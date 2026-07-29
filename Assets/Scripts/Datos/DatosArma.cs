using UnityEngine;

/// <summary>
/// Contenedor de datos para cada arma.
/// Permite crear y balancear armas directamente desde el menú de Unity.
/// </summary>
[CreateAssetMenu(fileName = "NuevaArma", menuName = "Beagle/Nueva Arma")]
public class DatosArma : ScriptableObject
{
    [Header("--- Información Visual ---")]
    public string nombreArma;
    public Sprite spriteArma;

    [Header("--- Tipo de Combate ---")]
    [Tooltip("Marca esta casilla si el arma es cuerpo a cuerpo (ej. Katana). Déjala desmarcada para armas de fuego.")]
    public bool esMelee;

    [Header("--- Configuración Básica ---")]
    [Tooltip("El prefab de la bala. (Déjalo vacío si es un arma Melee)")]
    public GameObject prefabBala; 
    
    [Tooltip("Fuerza de la bala para armas de fuego, o Fuerza de Empuje (Knockback) para la Katana.")]
    public float fuerzaDisparo = 15f;
    
    [Tooltip("Tiempo en segundos entre cada ataque o disparo.")]
    public float tiempoEntreDisparos = 0.2f;

    [Header("--- Mecánicas Avanzadas ---")]
    [Tooltip("Activar para atacar manteniendo el click (Metralleta/Katana rápida). Desactivar para clics individuales.")]
    public bool esAutomatica;

    [Header("--- Escopeta / Multidisparo ---")]
    [Tooltip("Cantidad de balas que salen. Pon 1 para pistolas/rifles. (Ignorado en Melee)")]
    public int cantidadPerdigones = 1;
    
    [Tooltip("Ángulo de dispersión en grados. Ej: 30 para escopeta. (Ignorado en Melee)")]
    public float anguloDispersion = 0f;
}