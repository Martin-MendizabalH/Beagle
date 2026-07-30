using UnityEngine;
using UnityEngine.UI;
using Cinemachine; // ¡Esta línea es la clave para controlar el CinemachineBrain!

public class GestorHordaNivel1 : MonoBehaviour
{
    [Header("--- Cámara (Cinemachine) ---")]
    public CinemachineBrain cerebroCamara; // Aquí arrastraremos la Main Camera

    [Header("--- Horda ---")]
    public GameObject soldadoHordaPrefab;
    public Transform puntoSpawnIzq;
    public Transform puntoSpawnDer;
    public float tiempoEntreSpawns = 2f;
    public int totalEnemigos = 10;

    [Header("--- UI y Recompensa ---")]
    public Slider barraProgreso;
    public GameObject llaveNivel1;

    private int enemigosSpawneados = 0;
    public static int enemigosMuertos = 0; // Se irá llenando cuando mates soldados
    private bool hordaActiva = false;

    void Start()
    {
        enemigosMuertos = 0;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !hordaActiva)
        {
            EmpezarHorda();
        }
    }

    void EmpezarHorda()
    {
        hordaActiva = true;

        // ¡Apagamos el cerebro de Cinemachine para congelar la pantalla!
        if (cerebroCamara != null)
        {
            cerebroCamara.enabled = false;
        }

        // Activamos la barra de progreso
        if (barraProgreso != null)
        {
            barraProgreso.gameObject.SetActive(true);
            barraProgreso.maxValue = totalEnemigos;
            barraProgreso.value = 0;
        }

        // Empezar a soltar soldados de izquierda a derecha
        InvokeRepeating("SpawnearSoldado", 1f, tiempoEntreSpawns);
    }

    void SpawnearSoldado()
    {
        if (enemigosSpawneados >= totalEnemigos)
        {
            CancelInvoke("SpawnearSoldado");
            return;
        }

        // Elige un lado al azar (50% izquierda, 50% derecha)
        Transform puntoElegido = (Random.Range(0, 2) == 0) ? puntoSpawnIzq : puntoSpawnDer;
        Instantiate(soldadoHordaPrefab, puntoElegido.position, Quaternion.identity);
        
        enemigosSpawneados++;
    }

    void Update()
    {
        if (!hordaActiva) return;

        // Actualizamos la barra
        if (barraProgreso != null)
        {
            barraProgreso.value = enemigosMuertos;
        }

        // Si ya mataste a todos, ganaste
        if (enemigosMuertos >= totalEnemigos)
        {
            TerminarHorda();
        }
    }

    void TerminarHorda()
    {
        hordaActiva = false;
        
        if (llaveNivel1 != null) llaveNivel1.SetActive(true); // Cae la llave
        if (barraProgreso != null) barraProgreso.gameObject.SetActive(false); // Se oculta la barra

        Destroy(gameObject); // Destruimos el gatillo para que no se repita
    }
}