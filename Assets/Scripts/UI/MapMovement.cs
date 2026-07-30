using UnityEngine;
using UnityEngine.SceneManagement;

public class MapMovement : MonoBehaviour
{
    [Header("Configuración de Puntos y Escenas")]
    public Transform[] puntosNiveles; // Arrastra PuntoNivel1, PuntoNivel2 y PuntoNivel3
    public string[] nombresEscenas = { "Nivel 1", "Nivel 2", "Nivel 3" };
    public float velocidadMovimiento = 5f;

    [Header("Indicadores de Bloqueo (Candados)")]
    public GameObject[] candadosNiveles; // Arrastra las imágenes de candado para Nivel 2 y Nivel 3

    private int indiceActual = 0;
    private int nivelMaximoDesbloqueado;

    // Componentes de animación y renderizado
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Lógica de desbloqueo: El Nivel 1 siempre está abierto (índice 0). 
        // Si pasas el Nivel 1, se guarda que el nivel máximo alcanzado es 1 (Nivel 2 desbloqueado).
        // Si pasas el Nivel 2, se guarda 2 (Nivel 3 desbloqueado).
        // Si no hay datos guardados, por defecto inicia en 0 (solo Nivel 1 disponible).
        nivelMaximoDesbloqueado = PlayerPrefs.GetInt("NivelMaximoDesbloqueado", 0);

        // Posicionar al beagle en el punto inicial al cargar
        if (puntosNiveles.Length > 0)
        {
            transform.position = puntosNiveles[indiceActual].position;
        }

        ActualizarCandadosVisuales();
    }

    void Update()
    {
        Vector3 destino = puntosNiveles[indiceActual].position;
        float distancia = Vector3.Distance(transform.position, destino);

        // Verificar si se está moviendo hacia el destino
        if (distancia > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, destino, velocidadMovimiento * Time.deltaTime);

            if (animator != null)
            {
                animator.SetBool("isWalking", true);
            }

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
            if (animator != null)
            {
                animator.SetBool("isWalking", false);
            }

            // Tecla D: Avanzar al siguiente nivel (Solo si el nivel al que quiere ir está desbloqueado)
            if (Input.GetKeyDown(KeyCode.D) && indiceActual < puntosNiveles.Length - 1 && indiceActual < nivelMaximoDesbloqueado)
            {
                indiceActual++;
            }
            // Tecla A: Retroceder al nivel anterior
            else if (Input.GetKeyDown(KeyCode.A) && indiceActual > 0)
            {
                indiceActual--;
            }

            // Entrar al nivel seleccionado con Espacio (Solo si ya está desbloqueado)
            if (Input.GetKeyDown(KeyCode.Space) && indiceActual <= nivelMaximoDesbloqueado)
            {
                ControladorPantallaCarga.CargarNivelConCarga(nombresEscenas[indiceActual]);
            }
        }
    }

    void ActualizarCandadosVisuales()
    {
        // Recorre los candados y los apaga si el nivel ya fue desbloqueado
        for (int i = 0; i < candadosNiveles.Length; i++)
        {
            if (candadosNiveles[i] != null)
            {
                // Si el índice del nivel (i + 1) es menor o igual al máximo desbloqueado, ocultamos el candado
                if ((i + 1) <= nivelMaximoDesbloqueado)
                {
                    candadosNiveles[i].SetActive(false);
                }
                else
                {
                    candadosNiveles[i].SetActive(true);
                }
            }
        }
    }
}