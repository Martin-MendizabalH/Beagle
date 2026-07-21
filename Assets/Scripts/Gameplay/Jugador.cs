using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Controlador principal del jugador (Beagle).
/// Gestiona movimiento, físicas, salto, dash, consumibles,
/// así como mecánicas de daño, knockback normal, rebote en ácido e I-Frames.
/// </summary>
public class Jugador : MonoBehaviour
{
    [Header("--- Base del Jugador ---")]
    public float velocidad = 8f;
    public SpriteRenderer spriteRenderer;
    private Animator animator;
    private Rigidbody2D rb;

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
    private Color colorOriginal;      

    // Estados para daño
    private bool estaEnKnockback = false;
    private bool esInvulnerable = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        gravedadPorDefecto = rb.gravityScale;
        if (spriteRenderer != null) colorOriginal = spriteRenderer.color;
        if (bordeRojo != null) bordeRojo.SetActive(false);

        ActualizarUIVidasYPociones();
    }

    void Update()
    {
        // 1. Bloqueo de controles por impacto físico o Dash
        if (estaEnKnockback) return;
        if (estaDasheando) return;

        // 2. Inputs directos
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

    void JugadorMovement()
    {   
        float movimientoX = Input.GetAxis("Horizontal");

        if (movimientoX > 0)
        {
            animator.SetBool("isWalking", true);
            spriteRenderer.flipX = false;
            direccionMirando = 1f; 
        }
        else if (movimientoX < 0)
        {
            animator.SetBool("isWalking", true);
            spriteRenderer.flipX = true;
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
        // 1. Aplicamos el rebote físico obligatoriamente
        StartCoroutine(RutinaReboteAcido());

        // 2. Si no es invulnerable, aplicamos el daño y activamos los I-Frames
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

    /// <summary>
    /// Corrutina específica para el rebote en ácido/trampas.
    /// Conserva la inercia en X para permitir maniobrar, pero anula la caída.
    /// </summary>
    private IEnumerator RutinaReboteAcido()
    {
        estaEnKnockback = true;

        // Anulamos la caída (Y=0) pero conservamos la inercia horizontal actual (X)
        rb.velocity = new Vector2(rb.velocity.x, 0f);

        // Impulsamos estrictamente hacia arriba
        rb.AddForce(new Vector2(0f, fuerzaReboteAcido), ForceMode2D.Impulse);

        // Bloqueo de input muy breve para que la física fluya
        yield return new WaitForSeconds(tiempoKnockback);
        estaEnKnockback = false;
    }

    private IEnumerator RutinaIFrames()
    {
        esInvulnerable = true;
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < tiempoInvulnerabilidad)
        {
            float alpha = (spriteRenderer.color.a == 1f) ? 0.3f : 1f;
            spriteRenderer.color = new Color(colorOriginal.r, colorOriginal.g, colorOriginal.b, alpha);
            
            yield return new WaitForSeconds(velocidadParpadeo);
            tiempoTranscurrido += velocidadParpadeo;
        }

        spriteRenderer.color = colorOriginal;
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
        // NUEVA LÓGICA DE ÁCIDO (Como Trigger)
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
        // NUEVA LÓGICA DE ÁCIDO (Como Colisión Sólida)
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