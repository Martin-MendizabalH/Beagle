using UnityEngine;

/// <summary>
/// Mantiene un SpriteRenderer de fondo cubriendo la cámara sin deformar su arte.
/// Se recalcula cuando cambia la resolución o la cámara objetivo.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class AjustarFondo : MonoBehaviour
{
    [SerializeField] private Camera camaraObjetivo;

    private SpriteRenderer renderizador;
    private Vector3 escalaInicial;
    private int anchoAnterior;
    private int altoAnterior;

    private void Awake()
    {
        renderizador = GetComponent<SpriteRenderer>();
        escalaInicial = transform.localScale;
    }

    private void OnEnable()
    {
        ActualizarTamano();
    }

    private void Start()
    {
        ActualizarTamano();
    }

    private void Update()
    {
        if (Screen.width == anchoAnterior && Screen.height == altoAnterior) return;
        ActualizarTamano();
    }

    /// <summary>
    /// Recalcula manualmente la cobertura; útil si la cámara cambia de tamaño.
    /// </summary>
    public void ActualizarTamano()
    {
        if (renderizador == null) renderizador = GetComponent<SpriteRenderer>();
        if (renderizador == null || renderizador.sprite == null) return;

        Camera camara = camaraObjetivo != null ? camaraObjetivo : Camera.main;
        if (camara == null || !camara.orthographic) return;

        float altoCamara = camara.orthographicSize * 2f;
        float anchoCamara = altoCamara * camara.aspect;

        Vector2 tamanoSprite = renderizador.sprite.bounds.size;
        float anchoBase = tamanoSprite.x * Mathf.Abs(escalaInicial.x);
        float altoBase = tamanoSprite.y * Mathf.Abs(escalaInicial.y);
        if (anchoBase <= 0f || altoBase <= 0f) return;

        float multiplicador = Mathf.Max(anchoCamara / anchoBase, altoCamara / altoBase);
        transform.localScale = new Vector3(
            escalaInicial.x * multiplicador,
            escalaInicial.y * multiplicador,
            escalaInicial.z);

        anchoAnterior = Screen.width;
        altoAnterior = Screen.height;
    }
}
