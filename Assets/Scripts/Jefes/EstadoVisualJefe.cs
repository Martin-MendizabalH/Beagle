using UnityEngine;

/// <summary>
/// Fuente única de verdad para el color del Jefe. La fase define el color base
/// y los ataques pueden superponer temporalmente un color de aviso.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class EstadoVisualJefe : MonoBehaviour
{
    private SpriteRenderer sprite;
    private Color colorFase1 = Color.white;
    private Color colorFase2 = new Color(1f, 0.6f, 0.6f);
    private Color colorAviso;
    private bool avisoActivo;
    private bool fase2;
    private bool inicializado;

    public Color ColorActualEsperado =>
        avisoActivo ? colorAviso : (fase2 ? colorFase2 : colorFase1);

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        if (!inicializado)
        {
            colorFase1 = sprite.color;
            Aplicar();
        }
    }

    private void OnEnable()
    {
        Aplicar();
    }

    public void Inicializar(Color nuevoColorFase1, Color nuevoColorFase2)
    {
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        colorFase1 = nuevoColorFase1;
        colorFase2 = nuevoColorFase2;
        inicializado = true;
        Aplicar();
    }

    public void EstablecerFase(bool nuevaFase2)
    {
        fase2 = nuevaFase2;
        Aplicar();
    }

    public void MostrarAviso(Color nuevoColor)
    {
        colorAviso = nuevoColor;
        avisoActivo = true;
        Aplicar();
    }

    public void OcultarAviso()
    {
        avisoActivo = false;
        Aplicar();
    }

    public void RestaurarEstado()
    {
        Aplicar();
    }

    private void Aplicar()
    {
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.color = ColorActualEsperado;
    }
}
