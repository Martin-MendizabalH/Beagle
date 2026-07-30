using UnityEngine;

/// <summary>
/// Centraliza todos los sonidos del Jefe Tanque. Los clips son opcionales:
/// dejar cualquier campo vacío nunca interrumpe la IA ni los ataques.
/// </summary>
[DisallowMultipleComponent]
public class SonidosJefeTanque : MonoBehaviour
{
    [Header("--- Fuentes de Audio ---")]
    [Tooltip("Canal para disparos, impactos y otros sonidos que pueden superponerse.")]
    [SerializeField] private AudioSource fuenteEfectos;

    [Tooltip("Canal exclusivo para el movimiento continuo del tanque.")]
    [SerializeField] private AudioSource fuenteMovimiento;

    [Tooltip("Canal exclusivo para sonidos continuos como el láser o la embestida.")]
    [SerializeField] private AudioSource fuenteAtaques;

    [Header("--- Movimiento ---")]
    public AudioClip sonidoMovimiento;

    [Header("--- Lluvia de Metralla ---")]
    public AudioClip sonidoAnticipoMetralla;
    public AudioClip sonidoDisparoMetralla;
    public AudioClip sonidoImpactoMetralla;

    [Header("--- Láser ---")]
    public AudioClip sonidoAnticipoLaser;
    public AudioClip sonidoLaser;
    public AudioClip sonidoFinLaser;

    [Header("--- Embestida ---")]
    public AudioClip sonidoAnticipoEmbestida;
    public AudioClip sonidoEmbestida;
    public AudioClip sonidoImpactoPared;

    [Header("--- Misil ---")]
    public AudioClip sonidoAnticipoMisil;
    public AudioClip sonidoLanzamientoMisil;
    public AudioClip sonidoExplosionMisil;

    [Header("--- Estado del Jefe ---")]
    public AudioClip sonidoActivacion;
    public AudioClip sonidoRecibirDano;
    public AudioClip sonidoTransicionFase;
    public AudioClip sonidoMuerte;

    [Header("--- Mezcla ---")]
    [Range(0f, 1f)] public float volumenMovimiento = 0.75f;
    [Range(0f, 1f)] public float volumenAtaques = 1f;
    [Range(0f, 1f)] public float volumenEfectos = 1f;
    [Range(0f, 1f)]
    [Tooltip("0 reproduce audio 2D; 1 aplica distancia y posición tridimensional.")]
    public float mezclaEspacial = 0f;

    private Rigidbody2D cuerpo;
    private JefeTanqueController controlador;
    private SaludJefe salud;
    private bool movimientoSolicitado;

    public AudioSource FuenteEfectos => fuenteEfectos;
    public AudioSource FuenteMovimiento => fuenteMovimiento;
    public AudioSource FuenteAtaques => fuenteAtaques;

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        controlador = GetComponent<JefeTanqueController>();
        salud = GetComponent<SaludJefe>();
        AsegurarFuentes();
    }

    private void OnEnable()
    {
        ConfiguracionAudio.AlCambiarEfectos -= AlCambiarVolumenEfectos;
        ConfiguracionAudio.AlCambiarEfectos += AlCambiarVolumenEfectos;

        if (salud == null) salud = GetComponent<SaludJefe>();
        if (salud != null)
        {
            salud.AlCambiarVida -= AlCambiarVida;
            salud.AlCambiarVida += AlCambiarVida;
            salud.AlMorir -= AlMorir;
            salud.AlMorir += AlMorir;
        }
    }

    private void OnDisable()
    {
        ConfiguracionAudio.AlCambiarEfectos -= AlCambiarVolumenEfectos;

        if (salud != null)
        {
            salud.AlCambiarVida -= AlCambiarVida;
            salud.AlMorir -= AlMorir;
        }

        DetenerTodos();
    }

    private void Update()
    {
        bool debeSonarMovimiento =
            controlador != null &&
            controlador.enabled &&
            cuerpo != null &&
            Mathf.Abs(cuerpo.velocity.x) > 0.05f;

        if (debeSonarMovimiento)
            IniciarMovimiento();
        else
            DetenerMovimiento();
    }

    /// <summary>
    /// Permite que el configurador de Editor deje tres canales explícitos
    /// y fáciles de inspeccionar en el prefab.
    /// </summary>
    public void ConfigurarFuentes(
        AudioSource nuevaFuenteEfectos,
        AudioSource nuevaFuenteMovimiento,
        AudioSource nuevaFuenteAtaques)
    {
        fuenteEfectos = nuevaFuenteEfectos;
        fuenteMovimiento = nuevaFuenteMovimiento;
        fuenteAtaques = nuevaFuenteAtaques;
        ConfigurarFuente(fuenteEfectos, false);
        ConfigurarFuente(fuenteMovimiento, true);
        ConfigurarFuente(fuenteAtaques, true);
    }

    public void ReproducirAnticipoMetralla()
    {
        ReproducirEfecto(sonidoAnticipoMetralla);
    }

    public void ReproducirDisparoMetralla()
    {
        ReproducirEfecto(sonidoDisparoMetralla);
    }

    public void ReproducirImpactoMetralla(Vector2 posicion)
    {
        ReproducirEfecto(sonidoImpactoMetralla);
    }

    public void ReproducirAnticipoLaser()
    {
        ReproducirEfecto(sonidoAnticipoLaser);
    }

    public void IniciarLaser()
    {
        IniciarBucleAtaque(sonidoLaser);
    }

    public void FinalizarLaser()
    {
        DetenerBucleAtaque();
        ReproducirEfecto(sonidoFinLaser);
    }

    public void ReproducirAnticipoEmbestida()
    {
        ReproducirEfecto(sonidoAnticipoEmbestida);
    }

    public void IniciarEmbestida()
    {
        IniciarBucleAtaque(sonidoEmbestida);
    }

    public void FinalizarEmbestida()
    {
        DetenerBucleAtaque();
    }

    public void ReproducirImpactoPared()
    {
        ReproducirEfecto(sonidoImpactoPared);
    }

    public void ReproducirAnticipoMisil()
    {
        ReproducirEfecto(sonidoAnticipoMisil);
    }

    public void ReproducirLanzamientoMisil()
    {
        ReproducirEfecto(sonidoLanzamientoMisil);
    }

    public void ReproducirExplosionMisil()
    {
        ReproducirEfecto(sonidoExplosionMisil);
    }

    public void ReproducirTransicionFase()
    {
        ReproducirEfecto(sonidoTransicionFase);
    }

    public void ReproducirActivacion()
    {
        ReproducirEfecto(sonidoActivacion);
    }

    public void DetenerBucleAtaque()
    {
        if (fuenteAtaques != null) fuenteAtaques.Stop();
    }

    public void DetenerTodos()
    {
        movimientoSolicitado = false;
        if (fuenteMovimiento != null) fuenteMovimiento.Stop();
        if (fuenteAtaques != null) fuenteAtaques.Stop();
        if (fuenteEfectos != null) fuenteEfectos.Stop();
    }

    private void IniciarMovimiento()
    {
        if (movimientoSolicitado) return;
        movimientoSolicitado = true;
        ReproducirBucle(fuenteMovimiento, sonidoMovimiento, volumenMovimiento);
    }

    private void DetenerMovimiento()
    {
        if (!movimientoSolicitado &&
            (fuenteMovimiento == null || !fuenteMovimiento.isPlaying))
        {
            return;
        }

        movimientoSolicitado = false;
        if (fuenteMovimiento != null) fuenteMovimiento.Stop();
    }

    private void IniciarBucleAtaque(AudioClip clip)
    {
        ReproducirBucle(fuenteAtaques, clip, volumenAtaques);
    }

    private void ReproducirBucle(AudioSource fuente, AudioClip clip, float volumen)
    {
        if (fuente == null || clip == null || !isActiveAndEnabled) return;
        if (fuente.isPlaying && fuente.clip == clip) return;

        fuente.Stop();
        fuente.clip = clip;
        fuente.volume = ConfiguracionAudio.AplicarEfectos(volumen);
        fuente.loop = true;
        fuente.Play();
    }

    private void ReproducirEfecto(AudioClip clip)
    {
        if (fuenteEfectos == null || clip == null || !isActiveAndEnabled) return;
        fuenteEfectos.PlayOneShot(
            clip,
            ConfiguracionAudio.AplicarEfectos(volumenEfectos));
    }

    private void AlCambiarVida(int vidaActual, int vidaMaxima)
    {
        if (vidaActual > 0) ReproducirEfecto(sonidoRecibirDano);
    }

    private void AlMorir()
    {
        DetenerTodos();
        ReproducirIndependiente(sonidoMuerte, volumenEfectos);
    }

    private void ReproducirIndependiente(AudioClip clip, float volumen)
    {
        if (clip == null) return;

        GameObject emisor = new GameObject("Sonido_Muerte_Jefe");
        emisor.transform.position = transform.position;
        AudioSource fuente = emisor.AddComponent<AudioSource>();
        ConfigurarFuente(fuente, false);
        fuente.volume = ConfiguracionAudio.AplicarEfectos(volumen);
        fuente.clip = clip;
        fuente.Play();
        Destroy(emisor, clip.length + 0.1f);
    }

    private void AsegurarFuentes()
    {
        AudioSource[] fuentes = GetComponents<AudioSource>();
        int indice = 0;

        if (fuenteEfectos == null && indice < fuentes.Length)
            fuenteEfectos = fuentes[indice++];
        if (fuenteMovimiento == null && indice < fuentes.Length)
            fuenteMovimiento = fuentes[indice++];
        if (fuenteAtaques == null && indice < fuentes.Length)
            fuenteAtaques = fuentes[indice++];

        if (fuenteEfectos == null) fuenteEfectos = gameObject.AddComponent<AudioSource>();
        if (fuenteMovimiento == null) fuenteMovimiento = gameObject.AddComponent<AudioSource>();
        if (fuenteAtaques == null) fuenteAtaques = gameObject.AddComponent<AudioSource>();

        ConfigurarFuente(fuenteEfectos, false);
        ConfigurarFuente(fuenteMovimiento, true);
        ConfigurarFuente(fuenteAtaques, true);
    }

    private void ConfigurarFuente(AudioSource fuente, bool bucle)
    {
        if (fuente == null) return;
        fuente.playOnAwake = false;
        fuente.loop = bucle;
        fuente.spatialBlend = mezclaEspacial;
        fuente.dopplerLevel = 0f;
    }

    private void AlCambiarVolumenEfectos(float nuevoVolumen)
    {
        if (fuenteMovimiento != null)
        {
            fuenteMovimiento.volume =
                ConfiguracionAudio.AplicarEfectos(volumenMovimiento);
        }

        if (fuenteAtaques != null)
        {
            fuenteAtaques.volume =
                ConfiguracionAudio.AplicarEfectos(volumenAtaques);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ConfigurarFuente(fuenteEfectos, false);
        ConfigurarFuente(fuenteMovimiento, true);
        ConfigurarFuente(fuenteAtaques, true);
    }
#endif
}
