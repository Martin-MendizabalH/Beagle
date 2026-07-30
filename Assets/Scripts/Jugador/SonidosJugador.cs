using UnityEngine;

/// <summary>
/// Reproduce los efectos de locomoción del jugador sin depender de eventos de
/// animación. La cadencia de pasos sigue el desplazamiento físico real.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SonidosJugador : MonoBehaviour
{
    [Header("--- Fuente ---")]
    [SerializeField] private AudioSource fuenteEfectos;

    [Header("--- Pasos ---")]
    [SerializeField] private AudioClip[] sonidosPasos;
    [SerializeField, Range(0f, 1f)] private float volumenPasos = 0.2f;
    [SerializeField, Min(0.08f)] private float intervaloPasos = 0.28f;
    [SerializeField, Min(0f)] private float velocidadMinimaPasos = 0.35f;
    [SerializeField, Range(0f, 0.2f)] private float variacionTonoPasos = 0.055f;

    [Header("--- Acciones ---")]
    [SerializeField] private AudioClip sonidoSalto;
    [SerializeField, Range(0f, 1f)] private float volumenSalto = 0.32f;
    [SerializeField] private AudioClip sonidoDash;
    [SerializeField, Range(0f, 1f)] private float volumenDash = 0.38f;
    [SerializeField, Range(0f, 0.2f)] private float variacionTonoAcciones = 0.025f;

    private Jugador jugador;
    private Rigidbody2D cuerpoFisico;
    private float siguientePaso;
    private int indicePaso;

    public AudioSource FuenteEfectos => fuenteEfectos;

    private void Awake()
    {
        jugador = GetComponent<Jugador>();
        cuerpoFisico = GetComponent<Rigidbody2D>();
        PrepararFuente();
    }

    private void Update()
    {
        ActualizarPasos();
    }

    public void ReproducirSalto()
    {
        Reproducir(sonidoSalto, volumenSalto, variacionTonoAcciones);
        siguientePaso = Time.time + intervaloPasos * 0.65f;
    }

    public void ReproducirDash()
    {
        Reproducir(sonidoDash, volumenDash, variacionTonoAcciones);
        siguientePaso = Time.time + intervaloPasos;
    }

    private void ActualizarPasos()
    {
        bool puedeCaminar =
            jugador != null &&
            cuerpoFisico != null &&
            jugador.PuedeReproducirPasos &&
            Mathf.Abs(cuerpoFisico.velocity.x) >= velocidadMinimaPasos;

        if (!puedeCaminar)
        {
            siguientePaso = Mathf.Min(
                siguientePaso,
                Time.time + intervaloPasos * 0.35f);
            return;
        }

        if (Time.time < siguientePaso || sonidosPasos == null || sonidosPasos.Length == 0)
        {
            return;
        }

        AudioClip paso = ObtenerSiguientePaso();
        Reproducir(paso, volumenPasos, variacionTonoPasos);

        float proporcionVelocidad = Mathf.Clamp(
            Mathf.Abs(cuerpoFisico.velocity.x) / Mathf.Max(0.01f, jugador.velocidad),
            0.75f,
            1.35f);
        siguientePaso = Time.time + intervaloPasos / proporcionVelocidad;
    }

    private AudioClip ObtenerSiguientePaso()
    {
        for (int i = 0; i < sonidosPasos.Length; i++)
        {
            int indice = (indicePaso + i) % sonidosPasos.Length;
            if (sonidosPasos[indice] == null) continue;

            indicePaso = (indice + 1) % sonidosPasos.Length;
            return sonidosPasos[indice];
        }

        return null;
    }

    private void PrepararFuente()
    {
        if (fuenteEfectos == null)
        {
            fuenteEfectos = GetComponent<AudioSource>();
        }

        if (fuenteEfectos == null)
        {
            fuenteEfectos = gameObject.AddComponent<AudioSource>();
        }

        fuenteEfectos.playOnAwake = false;
        fuenteEfectos.loop = false;
        fuenteEfectos.spatialBlend = 0f;
    }

    private void Reproducir(AudioClip clip, float volumen, float variacionTono)
    {
        if (clip == null) return;
        if (fuenteEfectos == null) PrepararFuente();
        if (fuenteEfectos == null) return;

        fuenteEfectos.pitch = 1f + Random.Range(-variacionTono, variacionTono);
        fuenteEfectos.PlayOneShot(
            clip,
            ConfiguracionAudio.AplicarEfectos(volumen));
    }
}
