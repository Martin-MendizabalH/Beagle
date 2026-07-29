using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Controlador principal del jugador (Beagle).
/// Gestiona movimiento, físicas, salto, dash, consumibles,
/// así como mecánicas de daño, knockback normal, rebote en ácido e I-Frames para múltiples sprites.
/// </summary>
public class Jugador : MonoBehaviour
{
    [Header("--- Base del Jugador ---")]
    public float velocidad = 8f;
    private Animator animator;
    private Rigidbody2D rb;

    // Arreglo para guardar todas las partes visuales del jugador (cabeza, brazos, cuerpo, etc.)
    private SpriteRenderer[] todosLosSprites;

    [Header("--- Salto Variable ---")]
    public float fuerzaSalto = 16f;
    [Range(0f, 1f)]
    public float multiplicadorCorteSalto = 0.5f; 

    [Header("--- Dash ---")]
    public float velocidadDash = 24f;
    public float tiempoDash = 0.2f;
    public float cooldownDash = 0.75f;

    [Header("--- Detección de Suelo ---")]
    public Transform transformSuelo; 
    public float radioSuelo = 0.2f;
    public LayerMask capaSuelo; 

    [Header("--- Sistema de Vidas y UI ---")]
    public int vidas = 3;
    public int vidasMaximas = 3; 
    public Image[] beaglesUI; 
    public GameObject bordeRojo; 

    [Header("--- Sistema de Consumibles ---")]
    public int cantidadPociones = 0;
    public TextMeshProUGUI textoContadorPociones; 

    [Header("--- Knockback e I-Frames ---")]
    public float fuerzaKnockbackX = 10f;
    public float fuerzaKnockbackY = 5f;
    [Tooltip("Fuerza pura hacia arriba al caer en ácido (Tag: Finish)")]
    public float fuerzaReboteAcido = 18f; 
    public float tiempoKnockback = 0.25f;
    public float tiempoInvulnerabilidad = 1.5f;
    public float velocidadParpadeo = 0.1f;

    // Variables internas de control de estado
    private float direccionMirando = 1f; 
    private bool enSuelo;
    private bool estaDasheando;
    private bool puedeDashear = true;
    private float timerCooldown;
    private float gravedadPorDefecto;       

    // Estados para daño
    private bool estaEnKnockback = false;
    private bool esInvulnerable = false;

    [Header("--- Control de Estado ---")]
    [Tooltip("Determina si el jugador puede moverse y actuar. Se desactiva durante cinemáticas.")]
    public bool puedeControlar = true;

    private RigidbodyType2D tipoCuerpoOriginal;

    void Start()
    {
        // Obtenemos los componentes nativos del GameObject
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            tipoCuerpoOriginal = rb.bodyType;
        }

        // Buscamos TODOS los SpriteRenderers en este GameObject y en sus hijos (para el Beagle fragmentado)
        todosLosSprites = GetComponentsInChildren<SpriteRenderer>();

        gravedadPorDefecto = rb.gravityScale;
        if (bordeRojo != null) bordeRojo.SetActive(false);

        ActualizarUIVidasYPociones();
    }

    void Update()
    {   
        // 1. CERRADURA: Si el jugador no tiene el control, detenemos todo.
        // Ahora el Update es limpio. Solo bloquea las acciones habituales.
        if (!puedeControlar) return;

        // 2. Bloqueo de controles por impacto físico o Dash
        if (estaEnKnockback) return;
        if (estaDasheando) return;

        // 3. Inputs directos
        if (Input.GetKeyDown(KeyCode.Q))
        {
            UsarPocion();
        }

        VerificarSuelo();
        ActualizarCooldownDash();
        
        ManejarSalto();
        ManejarDash();

        if (!estaDasheando)
        {
            JugadorMovement(); 
        }
    }

    /// <summary>
    /// Frena al jugador en seco y anula su gravedad para suspenderlo en el aire.
    /// </summary>
    /// <summary>
    /// Frena al jugador en seco y lo convierte en un objeto intocable (Kinematic)
    /// para suspenderlo perfectamente en el aire estilo Megaman.
    /// </summary>
    public void CongelarCinematica()
    {
        puedeControlar = false;
        
        Rigidbody2D rb = GetComponent<Rigidbody2D>(); //[cite: 2]
        if (rb != null)
        {
            rb.velocity = Vector2.zero; // Frenamos instantáneamente la inercia
            
            // MAGIA AQUÍ: Convertimos el cuerpo en Cinemático. 
            // Ya no le afectará la gravedad ni ninguna fuerza física externa.
            rb.bodyType = RigidbodyType2D.Kinematic; 
        }

        // Forzamos la animación a su estado de reposo (Idle)
        if (animator != null)
        {
            animator.SetBool("runningX", false);
            animator.SetBool("runningY", false);
        }
    }

    /// <summary>
    /// Devuelve el control y restaura la física normal (Dynamic) del jugador.
    /// </summary>
    public void DescongelarCinematica()
    {
        puedeControlar = true;
        
        Rigidbody2D rb = GetComponent<Rigidbody2D>(); //[cite: 2]
        if (rb != null)
        {
            // Le devolvemos su estado original para que vuelva a caer y reaccionar al entorno
            rb.bodyType = tipoCuerpoOriginal; 
        }
    }

    void JugadorMovement()
    {   
        float movimientoX = Input.GetAxis("Horizontal");

        if (movimientoX > 0)
        {
            animator.SetBool("isWalking", true);
            direccionMirando = 1f; 
        }
        else if (movimientoX < 0)
        {
            animator.SetBool("isWalking", true);
            direccionMirando = -1f; 
        }
        else
        {
            animator.SetBool("isWalking", false);
        }

        rb.velocity = new Vector2(movimientoX * velocidad, rb.velocity.y);
    }

    void ManejarSalto()
    {
        if (Input.GetKeyDown(KeyCode.Space) && enSuelo)
        {
            rb.velocity = new Vector2(rb.velocity.x, fuerzaSalto);
        }

        if (Input.GetKeyUp(KeyCode.Space) && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * multiplicadorCorteSalto);
        }
    }

    void ManejarDash()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && puedeDashear)
        {
            StartCoroutine(RutinaDash());
        }
    }

    void VerificarSuelo()
    {
        bool estabaEnSuelo = enSuelo;
        enSuelo = Physics2D.OverlapCircle(transformSuelo.position, radioSuelo, capaSuelo);

        if (enSuelo && !estabaEnSuelo && timerCooldown <= 0f)
        {
            puedeDashear = true;
        }
    }

    void ActualizarCooldownDash()
    {
        if (timerCooldown > 0f)
        {
            timerCooldown -= Time.deltaTime;
            if (timerCooldown <= 0f && enSuelo) puedeDashear = true;
        }
    }

    private IEnumerator RutinaDash()
    {
        estaDasheando = true;
        puedeDashear = false;
        timerCooldown = cooldownDash; 

        rb.gravityScale = 0f;
        rb.velocity = new Vector2(direccionMirando * velocidadDash, 0f);

        yield return new WaitForSeconds(tiempoDash);

        rb.gravityScale = gravedadPorDefecto;
        estaDasheando = false;
    }

    // =========================================================================
    // SISTEMA DE CONSUMIBLES (POCIONES)
    // =========================================================================

    private void UsarPocion()
    {
        if (cantidadPociones > 0 && vidas < vidasMaximas)
        {
            cantidadPociones--;
            vidas++;
            ActualizarUIVidasYPociones();
        }
    }

    public void AgregarPocion(int cantidad)
    {
        cantidadPociones += cantidad;
        ActualizarUIVidasYPociones();
    }

    private void ActualizarUIVidasYPociones()
    {
        for (int i = 0; i < beaglesUI.Length; i++)
        {
            beaglesUI[i].enabled = (i < vidas);  
        }

        if (textoContadorPociones != null)
        {
            textoContadorPociones.text = cantidadPociones.ToString();
        }
    }

    // =========================================================================
    // SISTEMA DE DAÑO, KNOCKBACK, ÁCIDO E I-FRAMES
    // =========================================================================

    public void RecibirDano(int cantidad)
    {
        ProcesarDanoBase(cantidad);
    }

    public void RecibirDano(int cantidad, Vector2 posicionAtacante)
    {
        if (esInvulnerable) return;

        if (ProcesarDanoBase(cantidad))
        {
            StartCoroutine(RutinaKnockback(posicionAtacante));
            StartCoroutine(RutinaIFrames());
        }
    }

    /// <summary>
    /// Gestiona de forma independiente el empuje por entorno (Tag: Finish).
    /// El jugador SIEMPRE rebotará (incluso en I-Frames), pero solo recibirá daño si no es invulnerable.
    /// </summary>
    private void RebotePorAcido(int cantidadDano)
    {
        StartCoroutine(RutinaReboteAcido());

        if (!esInvulnerable)
        {
            if (ProcesarDanoBase(cantidadDano))
            {
                StartCoroutine(RutinaIFrames());
            }
        }
    }

    private bool ProcesarDanoBase(int cantidad)
    {
        if (esInvulnerable) return false;

        vidas -= cantidad;
        
        if (estaDasheando)
        {
            estaDasheando = false;
            rb.gravityScale = gravedadPorDefecto;
        }

        ActualizarUIVidasYPociones();

        if (vidas <= 0)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return false; 
        }
        else
        {
            StartCoroutine(EfectoBordeRojo());
            return true; 
        }
    }

    private IEnumerator RutinaKnockback(Vector2 posicionAtacante)
    {
        estaEnKnockback = true;
        rb.velocity = Vector2.zero;

        float direccionEmpuje = transform.position.x < posicionAtacante.x ? -1f : 1f;
        Vector2 fuerzaEmpuje = new Vector2(direccionEmpuje * fuerzaKnockbackX, fuerzaKnockbackY);
        rb.AddForce(fuerzaEmpuje, ForceMode2D.Impulse);

        yield return new WaitForSeconds(tiempoKnockback);
        estaEnKnockback = false;
    }

    private IEnumerator RutinaReboteAcido()
    {
        estaEnKnockback = true;
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(new Vector2(0f, fuerzaReboteAcido), ForceMode2D.Impulse);

        yield return new WaitForSeconds(tiempoKnockback);
        estaEnKnockback = false;
    }

    /// <summary>
    /// Corrutina actualizada: Recorre todos los sprites del jugador y los hace parpadear.
    /// </summary>
    private IEnumerator RutinaIFrames()
    {
        esInvulnerable = true;
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < tiempoInvulnerabilidad)
        {
            // Apagamos y encendemos todos los pedazos del cuerpo en sincronía
            foreach (SpriteRenderer sr in todosLosSprites)
            {
                sr.enabled = !sr.enabled;
            }
            
            yield return new WaitForSeconds(velocidadParpadeo);
            tiempoTranscurrido += velocidadParpadeo;
        }

        // Medida de seguridad: Garantizamos que todos los pedazos queden visibles al terminar
        foreach (SpriteRenderer sr in todosLosSprites)
        {
            sr.enabled = true;
        }
        
        esInvulnerable = false;
    }

    IEnumerator EfectoBordeRojo()
    {
        if (bordeRojo != null) bordeRojo.SetActive(true); 
        yield return new WaitForSeconds(0.2f); 
        if (bordeRojo != null) bordeRojo.SetActive(false); 
    }

    // =========================================================================
    // DETECCIÓN DE COLISIONES
    // =========================================================================

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Vacio"))
        {
            RecibirDano(3); 
        }
        else if (collider.gameObject.CompareTag("BalaEnemiga"))
        {
            RecibirDano(1, collider.transform.position);
            Destroy(collider.gameObject); 
        }
        else if (collider.gameObject.CompareTag("Finish"))
        {
            RebotePorAcido(1);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemigo"))
        {
            RecibirDano(1, collision.transform.position);
        }
        else if (collision.gameObject.CompareTag("Finish"))
        {
            RebotePorAcido(1);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (transformSuelo != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transformSuelo.position, radioSuelo);
        }
    }
}