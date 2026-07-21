using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; // IMPORTANTE: Necesario para usar TextMeshPro

/// <summary>
/// Controlador principal del jugador (Beagle).
/// Gestiona movimiento, físicas, salto, dash, consumibles,
/// así como mecánicas de daño, knockback e I-Frames.
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
    public int vidasMaximas = 3; // Límite para no curar de más
    public Image[] beaglesUI; 
    public GameObject bordeRojo; 

    [Header("--- Sistema de Consumibles ---")]
    public int cantidadPociones = 0;
    public TextMeshProUGUI textoContadorPociones; // Arrastrar aquí el texto TMP del Canvas

    [Header("--- Knockback e I-Frames ---")]
    public float fuerzaKnockbackX = 10f;
    public float fuerzaKnockbackY = 5f;
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

        // Actualizamos la UI al iniciar para asegurar que todo cuadra
        ActualizarUIVidasYPociones();
    }

    void Update()
    {
        if (estaEnKnockback) return;
        if (estaDasheando) return;

        // INPUT DE CONSUMIBLES
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

    /// <summary>
    /// Intenta consumir una poción para curar una vida.
    /// </summary>
    private void UsarPocion()
    {
        if (cantidadPociones > 0 && vidas < vidasMaximas)
        {
            cantidadPociones--;
            vidas++;
            
            // Opcional: Aquí podrías reproducir un sonido o partículas de curación
            Debug.Log("Poción usada. Vidas actuales: " + vidas);
            
            ActualizarUIVidasYPociones();
        }
        else if (vidas >= vidasMaximas)
        {
            Debug.Log("Salud al máximo. No se gastó la poción.");
        }
        else
        {
            Debug.Log("No tienes pociones en el inventario.");
        }
    }

    /// <summary>
    /// Método público para que la Tienda o GameManager llamen al comprar/recolectar.
    /// </summary>
    public void AgregarPocion(int cantidad)
    {
        cantidadPociones += cantidad;
        ActualizarUIVidasYPociones();
        Debug.Log("Compraste pociones. Total: " + cantidadPociones);
    }

    /// <summary>
    /// Centraliza la actualización de la interfaz gráfica (Vidas y Contador).
    /// </summary>
    private void ActualizarUIVidasYPociones()
    {
        // Actualizamos las caritas del Beagle
        for (int i = 0; i < beaglesUI.Length; i++)
        {
            beaglesUI[i].enabled = (i < vidas);  
        }

        // Actualizamos el número de pociones
        if (textoContadorPociones != null)
        {
            textoContadorPociones.text = cantidadPociones.ToString();
        }
    }

    // =========================================================================
    // SISTEMA DE DAÑO, KNOCKBACK E I-FRAMES
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

    private bool ProcesarDanoBase(int cantidad)
    {
        if (esInvulnerable) return false;

        vidas -= cantidad;
        
        if (estaDasheando)
        {
            estaDasheando = false;
            rb.gravityScale = gravedadPorDefecto;
        }

        // Usamos nuestro método unificado
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
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemigo"))
        {
            RecibirDano(1, collision.transform.position);
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