using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jugador : MonoBehaviour
{
    [Header("--- Base del Jugador ---")]
    public float velocidad = 8f;
    private Animator animator;
    public SpriteRenderer spriteRenderer;

    // Cacheamos el Rigidbody aquí para no saturar la memoria en el Update.
    private Rigidbody2D rb;

    [Header("--- Salto Variable ---")]
    public float fuerzaSalto = 16f;
    [Range(0f, 1f)]
    [Tooltip("Reduce la velocidad de subida si sueltas la tecla. 0.5f corta la velocidad a la mitad.")]
    public float multiplicadorCorteSalto = 0.5f; 

    [Header("--- Dash ---")]
    public float velocidadDash = 24f;
    public float tiempoDash = 0.2f;
    public float cooldownDash = 0.75f;

    [Header("--- Detección de Suelo ---")]
    public Transform transformSuelo; // Debes asignar un objeto vacío en los pies del jugador
    public float radioSuelo = 0.2f;
    public LayerMask capaSuelo; // Debes asignar qué layer es considerada "Suelo"

    // Variables internas de control
    private float direccionMirando = 1f; // 1 para derecha, -1 para izquierda
    private bool enSuelo;
    private bool estaDasheando;
    private bool puedeDashear = true;
    private float timerCooldown;

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        // Guardamos el Rigidbody una sola vez al iniciar la escena.
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        // Si el personaje está en pleno dash (frames posteriores), bloqueamos todo
        if (estaDasheando)
        {
            return;
        }

        // Chequeos constantes
        VerificarSuelo();
        ActualizarCooldownDash();
        
        // Mecánicas
        ManejarSalto();
        ManejarDash();

        // Si ManejarDash() acaba de activar el dash en esta misma fracción de segundo,
        // no dejamos que JugadorMovement() sobreescriba nuestra velocidad super sónica.
        if (!estaDasheando)
        {
            JugadorMovement(); 
        }
    }

    void JugadorMovement()
    {
        float movimientoX = Input.GetAxis("Horizontal");
        // float movimientoY = Input.GetAxis("Vertical");

        if (movimientoX > 0)
        {
            animator.SetBool("isWalking", true);
            spriteRenderer.flipX = false;
            direccionMirando = 1f; // Registramos que mira a la derecha para el dash
        }
        else if (movimientoX < 0)
        {
            animator.SetBool("isWalking", true);
            spriteRenderer.flipX = true;
            direccionMirando = -1f; // Registramos que mira a la izquierda para el dash
        }
        else
        {
            animator.SetBool("isWalking", false);
        }

        // Aplicamos el movimiento usando la variable 'rb' que guardamos en el Start.
        rb.velocity = new Vector2(movimientoX * velocidad, rb.velocity.y);
    }

    void ManejarSalto()
    {
        // 1. Iniciar el salto si presionamos ESPACIO y estamos pisando el suelo
        if (Input.GetKeyDown(KeyCode.Space) && enSuelo)
        {
            rb.velocity = new Vector2(rb.velocity.x, fuerzaSalto);
        }

        // 2. Salto Variable: Si soltamos ESPACIO mientras el personaje sigue subiendo, frenamos el impulso
        if (Input.GetKeyUp(KeyCode.Space) && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * multiplicadorCorteSalto);
        }
    }

    void ManejarDash()
    {
        // Iniciar el Dash con SHIFT IZQUIERDO si lo tenemos disponible
        if (Input.GetKeyDown(KeyCode.LeftShift) && puedeDashear)
        {
            StartCoroutine(RutinaDash());
        }
    }

    void VerificarSuelo()
    {
        bool estabaEnSuelo = enSuelo;
        
        // Creamos un círculo de colisión a los pies del jugador para detectar la Layer "Suelo"
        enSuelo = Physics2D.OverlapCircle(transformSuelo.position, radioSuelo, capaSuelo);

        // RESET DEL DASH: Si acabamos de tocar el suelo y el cooldown ya pasó, recuperamos el dash
        if (enSuelo && !estabaEnSuelo && timerCooldown <= 0f)
        {
            puedeDashear = true;
        }
    }

    void ActualizarCooldownDash()
    {
        // Si el cooldown está activo, lo reducimos con el tiempo real
        if (timerCooldown > 0f)
        {
            timerCooldown -= Time.deltaTime;
            
            // Si el tiempo llega a cero y ya estamos en el suelo, recuperamos el dash
            if (timerCooldown <= 0f && enSuelo)
            {
                puedeDashear = true;
            }
        }
    }

    // Corrutina que controla el Dash en el tiempo
    private IEnumerator RutinaDash()
    {
        estaDasheando = true;
        puedeDashear = false;
        timerCooldown = cooldownDash; // Iniciamos la cuenta regresiva de 0.75s

        // Guardamos la gravedad y la desactivamos para no caer en picada mientras dasheamos
        float gravedadOriginal = rb.gravityScale;
        rb.gravityScale = 0f;

        // Disparamos al jugador en línea recta a máxima velocidad
        rb.velocity = new Vector2(direccionMirando * velocidadDash, 0f);

        // Congelamos esta función durante el tiempo que dura el dash (ej: 0.2 segundos)
        yield return new WaitForSeconds(tiempoDash);

        // Restauramos todo a la normalidad
        rb.gravityScale = gravedadOriginal;
        estaDasheando = false;
    }

    // Esto dibujará un círculo rojo en la vista "Scene" de Unity para que veas dónde está el detector de suelo
    private void OnDrawGizmosSelected()
    {
        if (transformSuelo != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transformSuelo.position, radioSuelo);
        }
    }
}