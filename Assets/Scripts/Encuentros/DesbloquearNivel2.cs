using UnityEngine;
using UnityEngine.SceneManagement;

public class DesbloquearNivel2 : MonoBehaviour
{
    [Tooltip("Escribe el nombre exacto de tu escena del mapamundi")]
    public string nombreSelector = "SelectorNiveles";

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Guardamos usando la clave exacta que lee MapMovement.cs (1 significa que completaste el nivel 1 y desbloqueaste el 2)
            PlayerPrefs.SetInt("NivelMaximoDesbloqueado", 1);
            PlayerPrefs.Save();

            // Regresamos al selector de niveles
            ControladorPantallaCarga.CargarNivelConCarga(nombreSelector);
        }
    }
}