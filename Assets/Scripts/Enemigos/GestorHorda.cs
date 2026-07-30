using UnityEngine;
using UnityEngine.UI;

public class GestorHorda : MonoBehaviour
{
    [Header("--- Control de Cámara ---")]
    [Tooltip("El script que mueve tu cámara. Al entrar, se apagará.")]
    public MonoBehaviour scriptCamara; 

    [Header("--- Enemigos y Spawns ---")]
    public GameObject[] enemigosPrefabs; // Arrastra el Golem y el Volador
    public Transform[] puntosSpawn;      // Arrastra los puntos vacíos de spawn a distintas alturas
    public float tiempoEntreSpawns = 1.5f;
    
    [Header("--- Progreso ---")]
    public int enemigosParaGanar = 10;
    public Slider barraProgreso;
    public GameObject llaveFinal;

    private int enemigosDerrotados = 0;
    private bool hordaActiva = false;

    void Start()
    {
        if (barraProgreso != null)
        {
            barraProgreso.maxValue = enemigosParaGanar;
            barraProgreso.value = 0;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("¡Algo tocó la arena de la horda! Fue: " + collision.gameObject.name);

        if (collision.CompareTag("Player") && !hordaActiva)
        {
            EmpezarEvento();
        }
    }

    void EmpezarEvento()
    {
        hordaActiva = true;

        // VAMOS A VER SI ENCUENTRA EL SCRIPT DE LA CÁMARA
        if (scriptCamara != null) 
        {
            scriptCamara.enabled = false;
            Debug.Log("¡Acabo de apagar la cámara exitosamente!"); 
        }
        else 
        {
            Debug.Log("¡ALERTA! El script de la cámara está en NULL. No tengo nada que apagar.");
        }

        // Muestra la barra de progreso
        if (barraProgreso != null) barraProgreso.gameObject.SetActive(true);

        // Empieza a crear enemigos cíclicamente
        InvokeRepeating("SpawnearEnemigo", 1f, tiempoEntreSpawns);
    }

    void SpawnearEnemigo()
    {
        if (enemigosDerrotados >= enemigosParaGanar) return; 

        // Elige un punto de aparición al azar y un enemigo al azar
        int spawnIndex = Random.Range(0, puntosSpawn.Length);
        int enemyIndex = Random.Range(0, enemigosPrefabs.Length);

        Instantiate(enemigosPrefabs[enemyIndex], puntosSpawn[spawnIndex].position, Quaternion.identity);
    }

    // 2. ESTA FUNCIÓN LA LLAMAREMOS CADA VEZ QUE MUERA UN ENEMIGO
    public void RegistrarMuerteEnemigo()
    {
        if (!hordaActiva) return;

        enemigosDerrotados++;
        if (barraProgreso != null) barraProgreso.value = enemigosDerrotados;

        if (enemigosDerrotados >= enemigosParaGanar)
        {
            GanarEvento();
        }
    }

    // 3. AL LLENAR LA BARRA
    void GanarEvento()
    {
        hordaActiva = false;
        CancelInvoke("SpawnearEnemigo"); // Detiene la creación de enemigos

        // Oculta la barra y hace aparecer la llave
        if (barraProgreso != null) barraProgreso.gameObject.SetActive(false);
        if (llaveFinal != null) llaveFinal.SetActive(true);

        // Apaga la caja de colisión de este evento para que no se repita
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }
}