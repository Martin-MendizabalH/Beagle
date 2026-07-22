using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPausa : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameObject ImagenMenu; // Aquí asignas 'ImagenMenu'

    private bool juegoPausado = false;

    void Start()
    {
        // Asegura que el juego empiece sin el menú y con tiempo normal
        Reanudar();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (juegoPausado)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    public void Reanudar()
    {
        ImagenMenu.SetActive(false);
        Time.timeScale = 1f;
        juegoPausado = false;
    }

    public void Pausar()
    {
        ImagenMenu.SetActive(true);
        Time.timeScale = 0f;
        juegoPausado = true;
    }

    public void CargarMenuPrincipal(string nombreEscena)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscena);
    }

    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}