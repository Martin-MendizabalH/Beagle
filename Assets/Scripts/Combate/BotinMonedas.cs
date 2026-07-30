using UnityEngine;

/// <summary>Genera monedas configurables al morir una entidad, una sola vez.</summary>
public class BotinMonedas : MonoBehaviour
{
    [Header("--- Monedas al morir ---")]
    [SerializeField] private GameObject prefabMoneda;
    [SerializeField, Min(0)] private int cantidadMinima = 2;
    [SerializeField, Min(0)] private int cantidadMaxima = 3;
    [SerializeField, Min(1)] private int valorPorMoneda = 5;
    [SerializeField, Min(0f)] private float dispersionHorizontal = 3.5f;
    [SerializeField, Min(0f)] private float impulsoVertical = 4f;

    private bool botinSoltado;
    public int UltimaCantidadSoltada { get; private set; }

    public void Configurar(GameObject nuevoPrefabMoneda, int nuevaCantidadMinima,
        int nuevaCantidadMaxima, int nuevoValor)
    {
        prefabMoneda = nuevoPrefabMoneda;
        cantidadMinima = Mathf.Max(0, nuevaCantidadMinima);
        cantidadMaxima = Mathf.Max(cantidadMinima, nuevaCantidadMaxima);
        valorPorMoneda = Mathf.Max(1, nuevoValor);
    }

    public void ConfigurarExplosion(float nuevaDispersionHorizontal, float nuevoImpulsoVertical)
    {
        dispersionHorizontal = Mathf.Max(0f, nuevaDispersionHorizontal);
        impulsoVertical = Mathf.Max(0f, nuevoImpulsoVertical);
    }

    public int SoltarMonedas()
    {
        if (botinSoltado) return 0;
        botinSoltado = true;

        if (prefabMoneda == null)
        {
            Debug.LogWarning($"[{name}] No tiene un prefab de moneda asignado.");
            return 0;
        }

        int minimo = Mathf.Min(cantidadMinima, cantidadMaxima);
        int maximo = Mathf.Max(cantidadMinima, cantidadMaxima);
        int cantidad = Random.Range(minimo, maximo + 1);
        UltimaCantidadSoltada = cantidad;

        for (int i = 0; i < cantidad; i++)
        {
            GameObject objetoMoneda = Instantiate(prefabMoneda, transform.position, Quaternion.identity);
            Moneda moneda = objetoMoneda.GetComponent<Moneda>();
            if (moneda == null) moneda = objetoMoneda.AddComponent<Moneda>();

            moneda.ConfigurarValor(valorPorMoneda);
            float direccion = cantidad == 1 ? 0f : Mathf.Lerp(-1f, 1f, i / (float)(cantidad - 1));
            float velocidadHorizontal = direccion * dispersionHorizontal + Random.Range(-0.45f, 0.45f);
            float velocidadVertical = impulsoVertical + Random.Range(-0.5f, 0.5f);
            moneda.Lanzar(new Vector2(velocidadHorizontal, velocidadVertical));
        }

        return cantidad;
    }
}
