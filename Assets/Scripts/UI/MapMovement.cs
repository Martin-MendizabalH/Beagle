using UnityEngine;
using UnityEngine.SceneManagement;

public class MapMovement : MonoBehaviour
{
    [Header("Puntos del Mapa (Waypoints)")]
    public Transform[] puntosNiveles; // Arrastra aquí PuntoNivel1, PuntoNivel2, PuntoNivel3
    
    [Header("Configuración de Movimiento")]
    public float velocidad = 5f;

    private int indiceActual = 0; // 0 = Nivel 1, 1 = Nivel 2, 2 = Nivel 3
    private bool estaMoviendose = false;
    private Transform objetivoMovimiento;

    void Start()
    {
        // Al iniciar el selector, posicionamos al beagle instantáneamente en el Nivel 1
        if (puntosNiveles.Length > 0)
        {
            transform.position = puntosNiveles[0].position;
        }
    }

    void Update()
    {
        // Si ya se está moviendo hacia un punto, interpolamos su posición
        if (estaMoviendose)
        {
            transform.position = Vector3.MoveTowards(transform.position, objetivoMovimiento.position, velocidad * Time.deltaTime);

            // Si llega al destino
            if (Vector3.Distance(transform.position, objetivoMovimiento.position) < 0.001f)
            {
                transform.position = objetivoMovimiento.position;
                estaMoviendose = false;
            }
            return; // No recibe comandos mientras se mueve
        }

        // Movimiento hacia la Derecha (Teclas D o Flecha Derecha)
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (indiceActual < puntosNiveles.Length - 1)
            {
                // Validar si el siguiente nivel está desbloqueado
                // Nivel 2 (índice 1) se desbloquea con PlayerPrefs "Nivel1Completado" == 1
                if (indiceActual == 0 && PlayerPrefs.GetInt("Nivel1Completado", 0) == 0)
                {
                    Debug.Log("¡El Nivel 2 está bloqueado! Debes completar el Nivel 1 primero.");
                    return;
                }

                // Nivel 3 (índice 2) se desbloquea con PlayerPrefs "Nivel2Completado" == 1
                if (indiceActual == 1 && PlayerPrefs.GetInt("Nivel2Completado", 0) == 0)
                {
                    Debug.Log("¡El Nivel 3 está bloqueado! Debes completar el Nivel 2 primero.");
                    return;
                }

                indiceActual++;
                IniciarMovimiento(puntosNiveles[indiceActual]);
            }
        }

        // Movimiento hacia la Izquierda (Teclas S, A o Flecha Izquierda)
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (indiceActual > 0)
            {
                indiceActual--;
                IniciarMovimiento(puntosNiveles[indiceActual]);
            }
        }

        // Entrar al nivel seleccionado al presionar la Barra Espaciadora
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CargarNivelActual();
        }
    }

    void IniciarMovimiento(Transform nuevoObjetivo)
    {
        objetivoMovimiento = nuevoObjetivo;
        estaMoviendose = true;
    }

    void CargarNivelActual()
    {
        // Carga la escena según el índice actual
        if (indiceActual == 0) SceneManager.LoadScene("Nivel1");
        else if (indiceActual == 1) SceneManager.LoadScene("Nivel2");
        else if (indiceActual == 2) SceneManager.LoadScene("Nivel3");
    }
}