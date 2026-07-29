using UnityEngine;

/// <summary>
/// Gestiona la detección de impactos cuerpo a cuerpo (Melee) y la mecánica de Parry.
/// Solo debe estar activo cuando la animación de ataque se está reproduciendo.
/// </summary>
public class AtaqueMelee : MonoBehaviour
{
    [Header("--- Configuración de Daño ---")]
    [Tooltip("Cantidad de daño que inflige el arma cuerpo a cuerpo.")]
    public int danoInfligido = 20;

    [Header("--- Configuración de Parry ---")]
    [Tooltip("Velocidad con la que sale devuelta la bala enemiga.")]
    public float velocidadParry = 25f;
    
    [Tooltip("La etiqueta exacta que tienen las balas enemigas.")]
    public string tagBalaEnemiga = "BalaEnemiga";
    
    [Tooltip("La nueva etiqueta que tendrá la bala tras ser devuelta (para que dañe enemigos).")]
    public string tagBalaAliada = "BalaJugador";

    private Camera camaraPrincipal;

    void Start()
    {
        // Cacheamos la cámara para evitar llamadas costosas en tiempo de ejecución
        camaraPrincipal = Camera.main;
    }

    // Se ejecuta al detectar una colisión con otro Collider[cite: 3]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Detectar impacto a Enemigos
        if (collision.CompareTag("Enemigo"))
        {
            SaludEnemigo salud = collision.GetComponent<SaludEnemigo>();
            if (salud != null)
            {
                salud.RecibirDano(danoInfligido);
            }
        }
        // 2. Detectar colisión con Balas Enemigas para el PARRY[cite: 3]
        else if (collision.CompareTag("BalaEnemigo"))
        {
            EjecutarParry(collision.gameObject);
        }
    }

    /// <summary>
    /// Captura la bala enemiga, cambia su dirección hacia el mouse y la convierte en aliada.
    /// </summary>
    private void EjecutarParry(GameObject balaEnemiga)
    {
        // Obtenemos el cuerpo físico de la bala para alterar su movimiento[cite: 3]
        Rigidbody2D rbBala = balaEnemiga.GetComponent<Rigidbody2D>();
        
        if (rbBala != null)
        {
            // A. Calcular la posición actual del mouse
            Vector3 posicionMouse = camaraPrincipal.ScreenToWorldPoint(Input.mousePosition);
            posicionMouse.z = 0f;

            // B. Calcular el vector de dirección desde la bala hacia el mouse
            Vector3 direccionParry = (posicionMouse - balaEnemiga.transform.position).normalized;

            // C. Alterar la propiedad velocity del proyectil para enviarlo con velocidad constante[cite: 3]
            rbBala.velocity = direccionParry * velocidadParry;

            // D. Rotar la bala visualmente para que apunte hacia donde viaja
            float angulo = Mathf.Atan2(direccionParry.y, direccionParry.x) * Mathf.Rad2Deg;
            balaEnemiga.transform.rotation = Quaternion.Euler(0, 0, angulo);

            // E. Cambiar la etiqueta para que el juego la reconozca como tuya y dañe enemigos
            balaEnemiga.tag = "BalaJugador"; 

            // F. (Opcional - Game Feel) Cambiar el color de la bala para indicar que fue devuelta
            SpriteRenderer srBala = balaEnemiga.GetComponent<SpriteRenderer>();
            if (srBala != null)
            {
                srBala.color = Color.cyan; // Color celeste brillante para el parry
            }

            Debug.Log("¡Parry Exitoso!");
        }
    }
}