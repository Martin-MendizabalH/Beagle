using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuDerrotaManager : MonoBehaviour
{
    // Botón "Reiniciar" - recarga el nivel actual desde 0
    public void Reiniciar()
    {
        Scene escenaActual = SceneManager.GetActiveScene();
        SceneManager.LoadScene(escenaActual.name);
    }

    // Botón "Volver al Selector de Niveles"
    public void VolverASelector()
    {
        SceneManager.LoadScene("SelectorNiveles");
    }

    // Botón "Salir"
    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}