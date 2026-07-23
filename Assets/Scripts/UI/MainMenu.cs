using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void Jugar()
    {
        SceneManager.LoadScene("SelectorNiveles");
    }

    // Método para el botón Opciones (puedes crear una escena de opciones o dejarlo preparado)
    public void Opciones()
    {
        Debug.Log("Abriendo opciones...");
        // SceneManager.LoadScene("Opciones");
    }

    // Método para el botón Salir del juego
    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}