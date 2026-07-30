using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ControladorPantallaCarga : MonoBehaviour
{
    [Header("--- Arrastra tus 6 imágenes aquí ---")]
    public Sprite[] pantallasDeCarga; // Aquí colocaras las 6 imágenes en el Inspector

    [Header("--- Referencia UI ---")]
    public Image contenedorImagen; // Arrastra el objeto ImagenPantallaCarga de la UI

    [Header("--- Tiempos ---")]
    public float tiempoDeEspera = 3f; // Segundos que dura la pantalla antes de entrar al nivel

    private static string escenaDestino = "Nivel1"; // Escena a la que queremos ir

    // Método estático para que cualquier botón o script le avise a dónde queremos ir
    public static void CargarNivelConCarga(string nombreNivel)
    {
        escenaDestino = nombreNivel;
        SceneManager.LoadScene("PantallaCarga");
    }

    void Start()
    {
        MostrarImagenAleatoria();
        
        // Inicia la corrutina para esperar unos segundos y cambiar al nivel real
        Invoke("CambiarAEscenaDestino", tiempoDeEspera);
    }

    void MostrarImagenAleatoria()
    {
        if (pantallasDeCarga.Length > 0 && contenedorImagen != null)
        {
            // Genera un número aleatorio entre 0 y 5 (dándole exactamente la misma probabilidad a las 6 imágenes)
            int indiceAleatorio = Random.Range(0, pantallasDeCarga.Length);
            
            // Asigna esa imagen al componente UI Image
            contenedorImagen.sprite = pantallasDeCarga[indiceAleatorio];
        }
    }

    void CambiarAEscenaDestino()
    {
        SceneManager.LoadScene(escenaDestino);
    }
}