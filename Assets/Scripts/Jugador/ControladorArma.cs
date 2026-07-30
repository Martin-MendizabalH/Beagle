using UnityEngine;

/// <summary>
/// Gestiona la rotación armónica de los pivotes, el disparo de proyectiles 
/// y la ejecución de animaciones para armas cuerpo a cuerpo.
/// </summary>
public class ControladorArmas : MonoBehaviour
{
    [Header("--- Referencias de Pivotes ---")]
    public Transform cabezaPivot;
    public Transform brazoDerPivot;
    public Transform brazoIzqPivot;
    public Transform puntoDisparo; 
    public Transform transformJugador; 

    [Header("--- Referencias de Animación ---")]
    [Tooltip("El Animator ubicado en el Contenedor_Katana para reproducir el Slash")]
    public Animator animatorArma; // Componente para gatillar animaciones

    [Tooltip("Hitbox de la katana que aplica daño y decide el pogo.")]
    public AtaqueMelee ataqueMelee;

    [Header("--- Audio ---")]
    [SerializeField] private AudioSource fuenteAudioArmas;

    // --- Variables internas inyectadas por el Inventario ---
    private GameObject balaPrefabActual;
    private float fuerzaDisparoActual;
    private float tiempoEntreDisparosActual;
    private bool esAutomaticaActual; 
    private int cantidadPerdigonesActual;
    private float anguloDispersionActual;
    private bool esMeleeActual; // <--- Diferenciador clave
    private AudioClip sonidoUsoActual;
    private float volumenSonidoActual = 0.4f;
    private float variacionTonoActual = 0.035f;
    
    // --- Control interno ---
    private float tiempoSiguienteDisparo = 0f;
    private float anguloApuntadoAbsoluto = 0f; 
    private Camera camaraPrincipal;

    [Header("--- Control de Estado ---")]
    [Tooltip("Determina si el arma puede apuntar y disparar. Útil para bloquearla en cinemáticas.")]
    public bool puedeAtacar = true;

    private void Awake()
    {
        PrepararFuenteAudio();
    }

    void Start()
    {
        camaraPrincipal = Camera.main;
        if (ataqueMelee == null) ataqueMelee = GetComponentInChildren<AtaqueMelee>();
    }

    void Update()
    {
        if (!puedeAtacar) return;

        ApuntarHaciaMouse();
        ManejarInputAtaque();
    }

    /// <summary>
    /// Método público para inyectar los datos del arma seleccionada.
    /// </summary>
    public void ActualizarDatosArma(DatosArma nuevosDatos)
    {
        balaPrefabActual = nuevosDatos.prefabBala;
        fuerzaDisparoActual = nuevosDatos.fuerzaDisparo;
        tiempoEntreDisparosActual = nuevosDatos.tiempoEntreDisparos;
        esAutomaticaActual = nuevosDatos.esAutomatica; 
        cantidadPerdigonesActual = nuevosDatos.cantidadPerdigones;
        anguloDispersionActual = nuevosDatos.anguloDispersion;
        sonidoUsoActual = nuevosDatos.sonidoUso;
        volumenSonidoActual = nuevosDatos.volumenSonido;
        variacionTonoActual = nuevosDatos.variacionTonoSonido;
        
        esMeleeActual = nuevosDatos.esMelee; // Guardamos el tipo de arma
    }

    /// <summary>
    /// Procesa el clic del ratón respetando el enfriamiento (cooldown).
    /// </summary>
    void ManejarInputAtaque()
    {
        if (Time.time < tiempoSiguienteDisparo) return;

        bool intentoAtaque = false;

        if (esAutomaticaActual)
        {
            intentoAtaque = Input.GetButton("Fire1");
        }
        else
        {
            intentoAtaque = Input.GetButtonDown("Fire1");
        }

        if (intentoAtaque)
        {
            EjecutarAccionArma();
            tiempoSiguienteDisparo = Time.time + tiempoEntreDisparosActual;
        }
    }

    /// <summary>
    /// Rota los brazos y la cabeza hacia el cursor del mouse.
    /// </summary>
    void ApuntarHaciaMouse()
    {
        Vector3 posicionMouse = camaraPrincipal.ScreenToWorldPoint(Input.mousePosition);
        posicionMouse.z = 0f; 

        Vector3 direccion = posicionMouse - puntoDisparo.position;
        anguloApuntadoAbsoluto = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;

        Vector3 direccionPivotes = posicionMouse - transform.position;
        float anguloVisual = Mathf.Atan2(direccionPivotes.y, direccionPivotes.x) * Mathf.Rad2Deg;

        if (posicionMouse.x < transformJugador.position.x)
        {
            transformJugador.localScale = new Vector3(-1, 1, 1);
            RotarPivotes(anguloVisual - 180f);
        }
        else
        {
            transformJugador.localScale = new Vector3(1, 1, 1);
            RotarPivotes(anguloVisual);
        }
    }

    void RotarPivotes(float angulo)
    {
        Quaternion rotacion = Quaternion.Euler(0, 0, angulo);
        if (cabezaPivot != null) cabezaPivot.rotation = rotacion;
        if (brazoDerPivot != null) brazoDerPivot.rotation = rotacion;
        if (brazoIzqPivot != null) brazoIzqPivot.rotation = rotacion;
    }

    /// <summary>
    /// Decide si el ataque es a distancia o cuerpo a cuerpo.
    /// </summary>
    void EjecutarAccionArma()
    {
        if (esMeleeActual)
        {
            // Ejecutar el ataque de la Katana
            AtaqueCuerpoACuerpo();
        }
        else
        {
            // Ejecutar el disparo de proyectiles
            DispararProyectiles();
        }
    }

    /// <summary>
    /// Gatilla la animación del corte de la espada.
    /// </summary>
    void AtaqueCuerpoACuerpo()
    {
        if (ataqueMelee != null)
        {
            // Se captura el ángulo actual: el slash no cambia de dirección a mitad del corte.
            float anguloRadianes = anguloApuntadoAbsoluto * Mathf.Deg2Rad;
            Vector2 direccionCorte = new Vector2(Mathf.Cos(anguloRadianes), Mathf.Sin(anguloRadianes));
            ataqueMelee.PrepararAtaque(direccionCorte);
        }

        if (animatorArma != null)
        {
            // Enviamos la señal al Animator para transicionar al estado de ataque[cite: 2]
            animatorArma.SetTrigger("Atacar");
        }
        else
        {
            Debug.LogWarning("Falta asignar el Animator del arma en el ControladorArmas.");
        }

        ReproducirSonidoUso();
    }

    /// <summary>
    /// Instancia una o varias balas aplicando la matemática del cono de dispersión.
    /// </summary>
    void DispararProyectiles()
    {
        if (balaPrefabActual == null || puntoDisparo == null) return;

        float anguloInicial = 0f;
        float incrementoAngulo = 0f;

        if (cantidadPerdigonesActual > 1)
        {
            anguloInicial = -anguloDispersionActual / 2f;
            incrementoAngulo = anguloDispersionActual / (cantidadPerdigonesActual - 1);
        }

        for (int i = 0; i < cantidadPerdigonesActual; i++)
        {
            float desviacion = anguloInicial + (incrementoAngulo * i);
            float anguloFinalBala = anguloApuntadoAbsoluto + desviacion;
            Quaternion rotacionBala = Quaternion.Euler(0f, 0f, anguloFinalBala);

            // Instanciar el GameObject[cite: 3]
            GameObject bala = Instantiate(balaPrefabActual, puntoDisparo.position, rotacionBala);

            // Añadir velocidad al Rigidbody2D[cite: 3]
            Rigidbody2D rbBala = bala.GetComponent<Rigidbody2D>();
            if (rbBala != null)
            {
                rbBala.velocity = bala.transform.right * fuerzaDisparoActual;
            }
        }

        ReproducirSonidoUso();
    }

    private void PrepararFuenteAudio()
    {
        if (fuenteAudioArmas == null)
        {
            fuenteAudioArmas = GetComponent<AudioSource>();
        }

        if (fuenteAudioArmas == null)
        {
            fuenteAudioArmas = gameObject.AddComponent<AudioSource>();
        }

        fuenteAudioArmas.playOnAwake = false;
        fuenteAudioArmas.loop = false;
        fuenteAudioArmas.spatialBlend = 0f;
    }

    private void ReproducirSonidoUso()
    {
        if (sonidoUsoActual == null) return;
        if (fuenteAudioArmas == null) PrepararFuenteAudio();
        if (fuenteAudioArmas == null) return;

        fuenteAudioArmas.pitch = 1f + Random.Range(-variacionTonoActual, variacionTonoActual);
        fuenteAudioArmas.PlayOneShot(
            sonidoUsoActual,
            ConfiguracionAudio.AplicarEfectos(volumenSonidoActual));
    }
}
