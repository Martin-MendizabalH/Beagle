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

    [Header("--- Tienda ---")]
    [Min(0)]
    [Tooltip("Precio de compra. La tienda lee este valor directamente para evitar precios duplicados en la interfaz.")]
    public int precio = 50;

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

    [Header("--- Sonido ---")]
    [Tooltip("Sonido que se reproduce una vez por ataque, no por cada proyectil generado.")]
    public AudioClip sonidoUso;

    [Range(0f, 1f)]
    [Tooltip("Volumen base del arma antes de aplicar el volumen global de efectos.")]
    public float volumenSonido = 0.4f;

    [Range(0f, 0.2f)]
    [Tooltip("Variación aleatoria suave del tono para evitar que ataques repetidos resulten monótonos.")]
    public float variacionTonoSonido = 0.035f;

    [Header("--- Mecánicas Avanzadas ---")]
    [Tooltip("Activar para atacar manteniendo el click (Metralleta/Katana rápida). Desactivar para clics individuales.")]
    public bool esAutomatica;

    [Header("--- Escopeta / Multidisparo ---")]
    [Tooltip("Cantidad de balas que salen. Pon 1 para pistolas/rifles. (Ignorado en Melee)")]
    public int cantidadPerdigones = 1;
    
    [Tooltip("Ángulo de dispersión en grados. Ej: 30 para escopeta. (Ignorado en Melee)")]
    public float anguloDispersion = 0f;
}
