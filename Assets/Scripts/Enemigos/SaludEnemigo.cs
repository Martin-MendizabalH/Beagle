using UnityEngine;

/// <summary>Vida reutilizable para enemigos normales, con feedback y botin.</summary>
public class SaludEnemigo : MonoBehaviour
{
    [Header("--- Estadisticas ---")]
    public float vidaMaxima = 100f;

    [Header("--- Efectos de muerte ---")]
    public GameObject prefabFragmentacion;

    private float vidaActual;
    private RetroalimentacionDanio retroalimentacionDanio;
    private BotinMonedas botinMonedas;
    private bool estaMuerto;
    private bool estaInicializada;

    private void Awake()
    {
        InicializarVida();
    }

    private void InicializarVida()
    {
        if (estaInicializada) return;

        vidaActual = vidaMaxima;
        retroalimentacionDanio = GetComponent<RetroalimentacionDanio>();
        botinMonedas = GetComponent<BotinMonedas>();
        estaInicializada = true;
    }

    public void RecibirDano(float cantidadDano)
    {
        InicializarVida();
        if (estaMuerto || cantidadDano <= 0f) return;

        if (retroalimentacionDanio == null) retroalimentacionDanio = GetComponent<RetroalimentacionDanio>();
        if (botinMonedas == null) botinMonedas = GetComponent<BotinMonedas>();

        vidaActual -= cantidadDano;
        retroalimentacionDanio?.MostrarDanio();

        if (vidaActual <= 0f) Morir();
    }

    private void Morir()
    {
        if (estaMuerto) return;
        estaMuerto = true;

        botinMonedas?.SoltarMonedas();

        if (prefabFragmentacion != null)
        {
            Instantiate(prefabFragmentacion, transform.position, Quaternion.identity);
        }

        DestruirEntidad();
    }

    private void DestruirEntidad()
    {
        if (Application.isPlaying) Destroy(gameObject);
        else DestroyImmediate(gameObject);
    }
}
