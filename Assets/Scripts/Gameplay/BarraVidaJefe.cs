using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla la vida visual del jefe:
/// - Rojo: vida actual, baja inmediatamente.
/// - Blanco: daño reciente, espera y luego alcanza a la barra roja.
/// </summary>
public class BarraVidaJefe : MonoBehaviour
{
    [Header("--- Referencias UI ---")]
    [SerializeField] private Image barraRoja;
    [SerializeField] private Image barraBlanca;

    [Header("--- Indicador de Daño ---")]
    [Tooltip("Segundos que la barra blanca espera tras el último golpe antes de moverse.")]
    [SerializeField] private float retrasoAntesDeDeslizar = 1f;

    [Tooltip("Velocidad con la que la barra blanca alcanza a la roja.")]
    [SerializeField] private float velocidadDeslizamientoBlanco = 0.75f;

    private SaludJefe saludJefeActual;

    // Vida objetivo de la barra roja, expresada entre 0 y 1.
    private float porcentajeVidaObjetivo = 1f;

    // Momento en que el jefe recibió el último golpe.
    private float momentoUltimoDano;

    /// <summary>
    /// Muestra la UI y se conecta al jefe activo.
    /// </summary>
    public void Mostrar(SaludJefe jefe)
    {
        if (jefe == null)
        {
            Debug.LogWarning("[UI JEFE] Se intentó mostrar la barra sin asignar un SaludJefe.");
            return;
        }

        gameObject.SetActive(true);

        DesvincularJefe();

        saludJefeActual = jefe;
        saludJefeActual.AlCambiarVida += ActualizarBarra;
        saludJefeActual.AlMorir += Ocultar;

        // El combate siempre comienza con ambas barras llenas.
        porcentajeVidaObjetivo = 1f;
        momentoUltimoDano = Time.time;

        if (barraRoja != null)
        {
            barraRoja.fillAmount = 1f;
        }

        if (barraBlanca != null)
        {
            barraBlanca.fillAmount = 1f;
        }

        // Seguridad: si el jefe tuviera vida distinta por alguna razón,
        // sincroniza el valor real al aparecer.
        ActualizarBarra(saludJefeActual.vidaActual, saludJefeActual.vidaMaxima);
    }

    /// <summary>
    /// La barra roja baja de inmediato. La blanca queda donde estaba.
    /// </summary>
    private void ActualizarBarra(int vidaActual, int vidaMaxima)
    {
        if (vidaMaxima <= 0) return;

        porcentajeVidaObjetivo = Mathf.Clamp01((float)vidaActual / vidaMaxima);

        // La vida actual siempre se comunica instantáneamente en rojo.
        if (barraRoja != null)
        {
            barraRoja.fillAmount = porcentajeVidaObjetivo;
        }

        // Cada golpe reinicia la espera de la barra blanca.
        momentoUltimoDano = Time.time;

        // Preparado para una curación futura:
        // si la vida subiera, la blanca no debería quedarse por debajo de la roja.
        if (barraBlanca != null && barraBlanca.fillAmount < porcentajeVidaObjetivo)
        {
            barraBlanca.fillAmount = porcentajeVidaObjetivo;
        }
    }

    private void Update()
    {
        if (saludJefeActual == null || barraBlanca == null)
        {
            return;
        }

        // La barra blanca espera el tiempo definido después del último golpe.
        if (Time.time < momentoUltimoDano + retrasoAntesDeDeslizar)
        {
            return;
        }

        // Después, se desliza suavemente hasta alcanzar a la barra roja.
        barraBlanca.fillAmount = Mathf.MoveTowards(
            barraBlanca.fillAmount,
            porcentajeVidaObjetivo,
            velocidadDeslizamientoBlanco * Time.deltaTime
        );
    }

    /// <summary>
    /// Desconecta los eventos del jefe y oculta la interfaz.
    /// </summary>
    public void Ocultar()
    {
        DesvincularJefe();
        gameObject.SetActive(false);
    }

    private void DesvincularJefe()
    {
        if (saludJefeActual != null)
        {
            saludJefeActual.AlCambiarVida -= ActualizarBarra;
            saludJefeActual.AlMorir -= Ocultar;
            saludJefeActual = null;
        }
    }

    private void OnDisable()
    {
        DesvincularJefe();
    }
}