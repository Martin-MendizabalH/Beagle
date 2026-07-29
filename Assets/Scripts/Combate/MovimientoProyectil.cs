using UnityEngine;

/// <summary>
/// Componente modular dedicado exclusivamente a gestionar el movimiento 
/// y la rotación visual dinámica de cualquier proyectil.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))] // Asegura que el GameObject tenga un Rigidbody2D
public class MovimientoProyectil : MonoBehaviour
{
    [Header("--- Configuración Visual ---")]
    [Tooltip("Compensación en grados. Si tu sprite original mira hacia ARRIBA, pon -90. Si mira a la IZQUIERDA, pon 180.")]
    public float compensacionRotacion = 0f;

    [Header("--- Configuración de Movimiento ---")]
    [Tooltip("Velocidad inicial automática. Útil si la bala siempre viaja en línea recta al nacer.")]
    public Vector2 velocidadInicial;
    
    [Tooltip("Si es verdadero, el proyectil usará la 'velocidadInicial' apenas sea creado.")]
    public bool aplicarVelocidadAlInicio = false;

    // Referencia interna al motor de físicas
    private Rigidbody2D rb;

    void Awake()
    {
        // Obtenemos el Rigidbody (cuerpo con físicas) del proyectil creado
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Si queremos que la bala se dispare sola al instanciarse (ej. balas rectas básicas)
        if (aplicarVelocidadAlInicio && rb != null)
        {
            // Aplicar velocidad al Rigidbody[cite: 2]
            rb.velocity = velocidadInicial;
        }
    }

    void Update()
    {
        // El Update se ejecuta constantemente, ideal para actualizar la rotación visual[cite: 1]
        RotarHaciaVelocidad();
    }

    /// <summary>
    /// Lee el vector de velocidad actual y rota el Transform en esa dirección matemática.
    /// </summary>
    private void RotarHaciaVelocidad()
    {
        // sqrMagnitude es más eficiente para el procesador que usar 'magnitude'.
        // Solo rotamos si la bala realmente se está moviendo (velocidad > 0.1).
        if (rb != null && rb.velocity.sqrMagnitude > 0.1f)
        {
            // 1. MAGIA MATEMÁTICA: Mathf.Atan2 calcula el ángulo en radianes usando (Y, X).
            // Luego, Mathf.Rad2Deg lo convierte a grados entendibles por Unity.
            float angulo = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;

            // 2. Aplicamos la rotación matemática al eje Z del Transform (profundidad en 2D),
            // sumando la compensación visual por si el dibujo original estaba rotado.
            transform.rotation = Quaternion.Euler(0f, 0f, angulo + compensacionRotacion);
        }
    }

    /// <summary>
    /// Método público para que un script externo (como el Jefe) pueda 
    /// inyectarle una velocidad calculada (ej. movimiento parabólico).
    /// </summary>
    public void Impulsar(Vector2 nuevaVelocidad)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        // Aplicar velocidad al Rigidbody[cite: 2]
        rb.velocity = nuevaVelocidad;
    }
}