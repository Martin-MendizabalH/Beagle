using UnityEngine;
using UnityEngine.SceneManagement; // Para cambiar escenas

public class FinDeNivel : MonoBehaviour
{
    [Tooltip("El nombre exacto de la escena del Selector de Niveles")]
    public string nombreSelector = "SelectorNiveles";
    
    [Tooltip("Ej: Nivel2. Para guardar el progreso")]
    public string nivelADesbloquear = "Nivel2"; 

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 1. Guardamos el progreso (Usando PlayerPrefs como vieron en la ayudantía)
            PlayerPrefs.SetInt(nivelADesbloquear + "Desbloqueado", 1);
            PlayerPrefs.Save();

            // 2. Desaparecemos al jugador y cambiamos de escena
            Destroy(collision.gameObject);
            SceneManager.LoadScene(nombreSelector);
        }
    }
}