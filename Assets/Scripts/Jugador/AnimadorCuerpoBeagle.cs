using UnityEngine;

/// <summary>
/// Anima exclusivamente el cuerpo del Beagle mediante transformaciones suaves.
/// La cabeza, los brazos, el arma y sus pivotes permanecen independientes.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Jugador), typeof(Rigidbody2D))]
public class AnimadorCuerpoBeagle : MonoBehaviour
{
    [Header("--- Referencia ---")]
    [SerializeField] private Transform cuerpo;

    [Header("--- Caminar ---")]
    [SerializeField, Min(0.1f)] private float velocidadCiclo = 10f;
    [SerializeField, Min(0f)] private float elevacionPaso = 0.085f;
    [SerializeField, Range(0f, 0.15f)] private float compresionPaso = 0.06f;
    [SerializeField, Range(0f, 8f)] private float inclinacionPaso = 4f;
    [SerializeField, Min(0f)] private float velocidadMinima = 0.15f;

    [Header("--- Salto y caída ---")]
    [SerializeField, Range(0f, 0.2f)] private float estiramientoSalto = 0.14f;
    [SerializeField, Range(0f, 0.2f)] private float compresionCaida = 0.075f;
    [SerializeField, Range(0.02f, 0.4f)] private float duracionAterrizaje = 0.12f;
    [SerializeField, Range(0f, 0.2f)] private float compresionAterrizaje = 0.13f;

    [Header("--- Suavizado ---")]
    [SerializeField, Min(1f)] private float rapidezSuavizado = 18f;

    private Jugador jugador;
    private Rigidbody2D cuerpoFisico;
    private Vector3 posicionBase;
    private Vector3 escalaBase;
    private Quaternion rotacionBase;
    private float fasePaso;
    private float tiempoAterrizaje;
    private bool estabaEnSuelo;

    private void Awake()
    {
        jugador = GetComponent<Jugador>();
        cuerpoFisico = GetComponent<Rigidbody2D>();

        if (cuerpo == null)
        {
            Transform[] partes = GetComponentsInChildren<Transform>(true);
            foreach (Transform parte in partes)
            {
                if (parte.name != "Cuerpo") continue;
                cuerpo = parte;
                break;
            }
        }

        if (cuerpo == null)
        {
            Debug.LogWarning("[Jugador] No se encontró el objeto Cuerpo para animar.");
            enabled = false;
            return;
        }

        posicionBase = cuerpo.localPosition;
        escalaBase = cuerpo.localScale;
        rotacionBase = cuerpo.localRotation;
        estabaEnSuelo = jugador.EstaEnSuelo;
    }

    private void LateUpdate()
    {
        if (cuerpo == null || jugador == null || cuerpoFisico == null) return;

        bool enSuelo = jugador.EstaEnSuelo;
        if (enSuelo && !estabaEnSuelo)
            tiempoAterrizaje = duracionAterrizaje;

        Vector3 posicionObjetivo = posicionBase;
        Vector3 escalaObjetivo = escalaBase;
        Quaternion rotacionObjetivo = rotacionBase;

        if (!enSuelo)
        {
            AplicarPoseAerea(ref posicionObjetivo, ref escalaObjetivo);
        }
        else if (tiempoAterrizaje > 0f)
        {
            AplicarAterrizaje(ref posicionObjetivo, ref escalaObjetivo);
        }
        else if (Mathf.Abs(cuerpoFisico.velocity.x) > velocidadMinima &&
                 !jugador.EstaDasheando)
        {
            AplicarCaminata(ref posicionObjetivo, ref escalaObjetivo,
                ref rotacionObjetivo);
        }
        else
        {
            fasePaso = 0f;
        }

        float mezcla = 1f - Mathf.Exp(-rapidezSuavizado * Time.deltaTime);
        cuerpo.localPosition = Vector3.Lerp(cuerpo.localPosition,
            posicionObjetivo, mezcla);
        cuerpo.localScale = Vector3.Lerp(cuerpo.localScale,
            escalaObjetivo, mezcla);
        cuerpo.localRotation = Quaternion.Slerp(cuerpo.localRotation,
            rotacionObjetivo, mezcla);

        estabaEnSuelo = enSuelo;
    }

    private void AplicarCaminata(ref Vector3 posicion, ref Vector3 escala,
        ref Quaternion rotacion)
    {
        float proporcionVelocidad = Mathf.Clamp01(
            Mathf.Abs(cuerpoFisico.velocity.x) / Mathf.Max(0.01f, jugador.velocidad));
        fasePaso += Time.deltaTime * velocidadCiclo *
            Mathf.Lerp(0.7f, 1.15f, proporcionVelocidad);

        float onda = Mathf.Sin(fasePaso);
        float rebote = Mathf.Abs(onda);
        posicion.y += rebote * elevacionPaso;
        escala.x *= 1f + rebote * compresionPaso;
        escala.y *= 1f - rebote * compresionPaso;
        rotacion *= Quaternion.Euler(0f, 0f, onda * inclinacionPaso);
    }

    private void AplicarPoseAerea(ref Vector3 posicion, ref Vector3 escala)
    {
        fasePaso = 0f;
        float direccionVertical = Mathf.Clamp(cuerpoFisico.velocity.y / 8f,
            -1f, 1f);

        if (direccionVertical > 0f)
        {
            float intensidad = estiramientoSalto * direccionVertical;
            escala.x *= 1f - intensidad;
            escala.y *= 1f + intensidad;
            posicion.y += intensidad * 0.2f;
        }
        else
        {
            float intensidad = compresionCaida * -direccionVertical;
            escala.x *= 1f + intensidad;
            escala.y *= 1f - intensidad;
            posicion.y -= intensidad * 0.12f;
        }
    }

    private void AplicarAterrizaje(ref Vector3 posicion, ref Vector3 escala)
    {
        tiempoAterrizaje = Mathf.Max(0f, tiempoAterrizaje - Time.deltaTime);
        float progreso = tiempoAterrizaje / duracionAterrizaje;
        float intensidad = Mathf.Sin(progreso * Mathf.PI) *
            compresionAterrizaje;

        escala.x *= 1f + intensidad;
        escala.y *= 1f - intensidad;
        posicion.y -= intensidad * 0.25f;
    }

    private void OnDisable()
    {
        if (cuerpo == null) return;
        cuerpo.localPosition = posicionBase;
        cuerpo.localScale = escalaBase;
        cuerpo.localRotation = rotacionBase;
    }
}
