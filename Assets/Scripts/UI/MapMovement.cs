using UnityEngine;
using UnityEngine.SceneManagement;

public class MapMovement : MonoBehaviour
{
    [Header("Configuración de Puntos y Escenas")]
    public Transform[] puntosNiveles; // Arrastra PuntoNivel1, PuntoNivel2 y PuntoNivel3
    public string[] nombresEscenas = { "Nivel1", "Nivel2", "Nivel3" };
    public float velocidadMovimiento = 5f;

    private int indiceActual = 0;
    private int nivelMaximoDesbloqueado;

    // Componentes de animación y renderizado
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // Obtener componentes
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Progreso desbloqueado por PlayerPrefs
        nivelMaximoDesbloqueado = PlayerPrefs.GetInt("NivelDesbloqueado", 2);

        // Posicionar al beagle en el punto inicial al cargar
        if (puntosNiveles.Length > 0)
        {
            transform.position = puntosNiveles[indiceActual].position;
        }
    }

    void Update()
    {
        Vector3 destino = puntosNiveles[indiceActual].position;
        float distancia = Vector3.Distance(transform.position, destino);

        // Verificar si se está moviendo hacia el destino
        if (distancia > 0.01f)
        {
            // Moverse hacia el punto
            transform.position = Vector3.MoveTowards(transform.position, destino, velocidadMovimiento * Time.deltaTime);

            // Activar la animación usando 'isWalking'
            if (animator != null)
            {
                animator.SetBool("isWalking", true);
            }

            // Voltear el sprite según la dirección del movimiento (Derecha o Izquierda)
            if (spriteRenderer != null)
            {
                if (destino.x < transform.position.x)
                {
                    spriteRenderer.flipX = true; // Mira a la izquierda
                }
                else if (destino.x > transform.position.x)
                {
                    spriteRenderer.flipX = false; // Mira a la derecha
                }
            }
        }
        else
        {
            // Ya llegó al destino: Desactivar la animación con 'isWalking'
            if (animator != null)
            {
                animator.SetBool("isWalking", false);
            }

            // Tecla D: Avanzar al siguiente nivel (hacia la derecha/adelante)
            if (Input.GetKeyDown(KeyCode.D) && indiceActual < puntosNiveles.Length - 1 && indiceActual < nivelMaximoDesbloqueado)
            {
                indiceActual++;
            }
            // Tecla A: Retroceder al nivel anterior (hacia la izquierda/atrás)
            else if (Input.GetKeyDown(KeyCode.A) && indiceActual > 0)
            {
                indiceActual--;
            }

            // Entrar al nivel seleccionado con Espacio
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SceneManager.LoadScene(nombresEscenas[indiceActual]);
            }
        }
    }
}