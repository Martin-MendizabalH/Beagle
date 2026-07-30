using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Control principal del Jefe Tanque. Coordina movimiento, fases y ejecución
/// de ataques, delegando la selección y los efectos a componentes especializados.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(DirectorAtaquesJefe))]
[RequireComponent(typeof(EfectosVisualesJefeTanque))]
[RequireComponent(typeof(SacudidaCamaraJefe))]
[RequireComponent(typeof(EstadoVisualJefe))]
[RequireComponent(typeof(PoolBalasMetrallaJefe))]
[RequireComponent(typeof(SonidosJefeTanque))]
public class JefeTanqueController : MonoBehaviour
{
    [Header("--- Puntos de Disparo ---")]
    public Transform puntoDisparoCanon;
    public Transform puntoDisparoMetralla;
    public LineRenderer lineaLaser;

    [Header("--- Prefabs de Ataque ---")]
    public GameObject balaMetrallaPrefab;
    public GameObject misilPrefab;

    [Header("--- Movimiento ---")]
    [Min(0f)] public float velocidadMovimiento = 3f;
    [Min(0f)] public float distanciaMinimaPersecucion = 3.2f;
    [Min(0f)] public float tiempoEntreAtaques = 2.5f;
    [Min(0.1f)] public float multiplicadorVelocidadFase2 = 1.25f;
    [Range(0.2f, 1f)] public float multiplicadorEsperaFase2 = 0.72f;

    [Header("--- Telegrafiado General ---")]
    [Min(0.1f)] public float tiempoTelegrafiado = 1f;
    [Min(0.02f)] public float intervaloParpadeo = 0.1f;
    [Min(0f)] public float pausaAntesDelImpacto = 0.15f;
    [Range(0.5f, 1f)] public float multiplicadorTelegrafiadoFase2 = 0.9f;

    [Header("--- Lluvia de Metralla ---")]
    [Min(1)] public int cantidadBalasMetralla = 5;
    [Tooltip("Respaldo utilizado mientras no exista un LimitesArenaJefe asignado.")]
    [Min(2f)] public float anchoDeLaArena = 23f;
    [Min(0.1f)] public float fuerzaSaltoMetralla = 12f;
    [Min(0.1f)] public float radioMarcadorMetralla = 0.42f;
    [Min(0f)] public float intervaloLanzamientoMetralla = 0.04f;
    [Min(0f)] public float recuperacionMetralla = 0.45f;

    [Header("--- Embestida ---")]
    [Min(0f)] public float velocidadAnticipacionEmbestida = 4f;
    [Min(0f)] public float velocidadEmbestida = 20f;
    [Min(0.1f)] public float duracionMaximaEmbestida = 0.75f;
    [Min(0f)] public float recuperacionEmbestida = 1.05f;
    public LayerMask mascaraEntorno;

    [Header("--- Láser ---")]
    [Min(1)] public int danoLaser = 1;
    [Min(0.05f)] public float tiempoMantenimientoLaser = 0.5f;
    [Min(0f)] public float recuperacionLaser = 0.65f;
    [Min(0f)] public float grosorGuiaLaser = 0.12f;
    [Min(0f)] public float intensidadSacudidaLaser = 0.22f;
    [Min(0.05f)] public float duracionSacudidaLaser = 0.35f;

    [Header("--- Misil ---")]
    [Min(1)] public int maximoMisilesActivos = 1;
    [Min(0f)] public float recuperacionMisil = 0.45f;

    [Header("--- Transición de Fase ---")]
    [Min(0.2f)] public float duracionTransicionFase2 = 1.25f;
    [Min(0f)] public float intensidadSacudidaFase2 = 0.32f;

    [Header("--- Colores de Telegrafiado ---")]
    public Color colorAvisoLaser = Color.red;
    public Color colorAvisoMetralla = Color.yellow;
    public Color colorAvisoEmbestida = Color.gray;
    public Color colorAvisoMisil = Color.magenta;
    public Color colorBaseFase2 = new Color(1f, 0.6f, 0.6f);

    private Rigidbody2D rb;
    private Collider2D colliderPrincipal;
    private SpriteRenderer spriteRenderer;
    private SaludJefe saludJefe;
    private DirectorAtaquesJefe directorAtaques;
    private EfectosVisualesJefeTanque efectosVisuales;
    private SacudidaCamaraJefe sacudidaCamara;
    private EstadoVisualJefe estadoVisual;
    private PoolBalasMetrallaJefe poolMetralla;
    private SonidosJefeTanque sonidos;
    private Transform jugador;
    private LimitesArenaJefe limitesArena;
    private Color colorOriginal;
    private Vector3 escalaOriginal;
    private Coroutine cicloCombate;
    private bool estaAtacando;
    private bool transicionFase2Pendiente;
    private bool combateDetenido;
    private bool forzarMisilFase2;
    private readonly Queue<TipoAtaqueTanque> ataquesForzadosDepuracion =
        new Queue<TipoAtaqueTanque>();

    public bool EstaAtacando => estaAtacando;
    public int CantidadBalasMetrallaActivas =>
        poolMetralla != null ? poolMetralla.CantidadActivas : 0;
    public event System.Action<TipoAtaqueTanque> AlIniciarAtaque;
    public event System.Action AlCompletarTransicionFase2;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        colliderPrincipal = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        saludJefe = GetComponent<SaludJefe>();
        directorAtaques = GetComponent<DirectorAtaquesJefe>();
        if (directorAtaques == null) directorAtaques = gameObject.AddComponent<DirectorAtaquesJefe>();

        efectosVisuales = GetComponent<EfectosVisualesJefeTanque>();
        if (efectosVisuales == null)
            efectosVisuales = gameObject.AddComponent<EfectosVisualesJefeTanque>();

        sacudidaCamara = GetComponent<SacudidaCamaraJefe>();
        if (sacudidaCamara == null)
            sacudidaCamara = gameObject.AddComponent<SacudidaCamaraJefe>();

        estadoVisual = GetComponent<EstadoVisualJefe>();
        if (estadoVisual == null)
            estadoVisual = gameObject.AddComponent<EstadoVisualJefe>();

        poolMetralla = GetComponent<PoolBalasMetrallaJefe>();
        if (poolMetralla == null)
            poolMetralla = gameObject.AddComponent<PoolBalasMetrallaJefe>();

        sonidos = GetComponent<SonidosJefeTanque>();
        if (sonidos == null)
            sonidos = gameObject.AddComponent<SonidosJefeTanque>();

        colorOriginal = spriteRenderer != null ? spriteRenderer.color : Color.white;
        estadoVisual.Inicializar(colorOriginal, colorBaseFase2);
        poolMetralla.Preparar(
            balaMetrallaPrefab, cantidadBalasMetralla * 2 + 2);
        efectosVisuales.PrepararMarcadoresMetralla(cantidadBalasMetralla);
        escalaOriginal = new Vector3(
            Mathf.Abs(transform.localScale.x),
            Mathf.Abs(transform.localScale.y),
            Mathf.Abs(transform.localScale.z));

        if (mascaraEntorno.value == 0)
        {
            int capaSuelo = LayerMask.NameToLayer("Suelo");
            if (capaSuelo >= 0) mascaraEntorno = 1 << capaSuelo;
        }

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    private void OnEnable()
    {
        combateDetenido = false;
        transicionFase2Pendiente = false;
        forzarMisilFase2 = false;
        estaAtacando = false;

        if (lineaLaser != null) lineaLaser.enabled = false;
        estadoVisual?.EstablecerFase(saludJefe != null && saludJefe.estaEnFase2);
        estadoVisual?.OcultarAviso();
        sonidos?.ReproducirActivacion();
        if (saludJefe != null) saludJefe.AlEntrarFase2 += SolicitarTransicionFase2;
        directorAtaques?.Reiniciar();

        if (cicloCombate == null) cicloCombate = StartCoroutine(CicloDeCombate());
    }

    private void OnDisable()
    {
        if (saludJefe != null) saludJefe.AlEntrarFase2 -= SolicitarTransicionFase2;
        if (lineaLaser != null) lineaLaser.enabled = false;
        estadoVisual?.OcultarAviso();
        if (rb != null) rb.velocity = Vector2.zero;

        StopAllCoroutines();
        sonidos?.DetenerTodos();
        cicloCombate = null;
        estaAtacando = false;
    }

    private void FixedUpdate()
    {
        if (combateDetenido || jugador == null || estaAtacando) return;

        MirarAlJugador();
        MoverHaciaJugador();
    }

    public void ConfigurarEncuentro(LimitesArenaJefe nuevosLimites)
    {
        limitesArena = nuevosLimites;
        poolMetralla?.Preparar(
            balaMetrallaPrefab, cantidadBalasMetralla * 2 + 2);
        efectosVisuales?.PrepararMarcadoresMetralla(cantidadBalasMetralla);
    }

    public void DetenerCombate()
    {
        combateDetenido = true;
        estaAtacando = true;
        if (lineaLaser != null) lineaLaser.enabled = false;
        if (rb != null) rb.velocity = Vector2.zero;
        poolMetralla?.RetirarActivas();
        StopAllCoroutines();
        sonidos?.DetenerTodos();
        cicloCombate = null;
    }

    /// <summary>
    /// Encola un ataque para pruebas desde herramientas de Editor.
    /// El combate normal no utiliza esta cola.
    /// </summary>
    public void ForzarSiguienteAtaqueParaDepuracion(TipoAtaqueTanque ataque)
    {
        ataquesForzadosDepuracion.Enqueue(ataque);
    }

    private void SolicitarTransicionFase2()
    {
        transicionFase2Pendiente = true;
        if (saludJefe != null) saludJefe.esVulnerable = false;
    }

    private IEnumerator CicloDeCombate()
    {
        yield return new WaitForSeconds(0.5f);

        while (!combateDetenido)
        {
            BuscarJugadorSiHaceFalta();
            if (jugador == null)
            {
                yield return null;
                continue;
            }

            if (transicionFase2Pendiente)
            {
                yield return StartCoroutine(TransicionFase2());
                if (combateDetenido) break;
            }

            TipoAtaqueTanque ataque = ElegirSiguienteAtaque();
            yield return StartCoroutine(EjecutarAtaque(ataque));

            if (transicionFase2Pendiente)
            {
                yield return StartCoroutine(TransicionFase2());
                if (combateDetenido) break;
            }

            float multiplicadorEspera =
                saludJefe != null && saludJefe.estaEnFase2 ? multiplicadorEsperaFase2 : 1f;
            float espera = tiempoEntreAtaques * multiplicadorEspera;
            float transcurrido = 0f;

            while (!combateDetenido && transcurrido < espera && !transicionFase2Pendiente)
            {
                transcurrido += Time.deltaTime;
                yield return null;
            }
        }

        cicloCombate = null;
    }

    private TipoAtaqueTanque ElegirSiguienteAtaque()
    {
        if (ataquesForzadosDepuracion.Count > 0)
            return ataquesForzadosDepuracion.Dequeue();

        if (forzarMisilFase2 && PuedeLanzarMisil())
        {
            forzarMisilFase2 = false;
            return TipoAtaqueTanque.Misil;
        }

        bool fase2 = saludJefe != null && saludJefe.estaEnFase2;
        return directorAtaques.ElegirAtaque(
            transform, jugador, fase2, fase2 && PuedeLanzarMisil(), limitesArena);
    }

    private IEnumerator EjecutarAtaque(TipoAtaqueTanque tipoAtaque)
    {
        estaAtacando = true;
        MirarAlJugador();
        AlIniciarAtaque?.Invoke(tipoAtaque);

        try
        {
            switch (tipoAtaque)
            {
                case TipoAtaqueTanque.Laser:
                    yield return StartCoroutine(AtaqueLaser());
                    break;
                case TipoAtaqueTanque.Metralla:
                    yield return StartCoroutine(AtaqueMetralla());
                    break;
                case TipoAtaqueTanque.Embestida:
                    yield return StartCoroutine(AtaqueEmbestida());
                    break;
                case TipoAtaqueTanque.Misil:
                    yield return StartCoroutine(AtaqueMisilTeledirigido());
                    break;
            }
        }
        finally
        {
            // Un fallo en un efecto o en el receptor de daño no debe dejar la
            // máquina de estados bloqueada permanentemente en "atacando".
            if (!combateDetenido) estadoVisual?.OcultarAviso();
            estaAtacando = false;
        }
    }

    private void BuscarJugadorSiHaceFalta()
    {
        if (jugador != null) return;
        GameObject objetoJugador = GameObject.FindGameObjectWithTag("Player");
        if (objetoJugador != null) jugador = objetoJugador.transform;
    }

    private void MirarAlJugador()
    {
        if (jugador == null) return;

        float signoX = jugador.position.x > transform.position.x ? -1f : 1f;
        transform.localScale = new Vector3(
            signoX * escalaOriginal.x,
            escalaOriginal.y,
            escalaOriginal.z);
    }

    private void MoverHaciaJugador()
    {
        float diferenciaX = jugador.position.x - transform.position.x;
        if (Mathf.Abs(diferenciaX) <= distanciaMinimaPersecucion)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        float direccion = Mathf.Sign(diferenciaX);
        if (limitesArena != null &&
            limitesArena.EstaCercaDelLimite(transform.position.x, direccion, 0.15f))
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        float multiplicador =
            saludJefe != null && saludJefe.estaEnFase2 ? multiplicadorVelocidadFase2 : 1f;
        rb.velocity = new Vector2(direccion * velocidadMovimiento * multiplicador, rb.velocity.y);
    }

    private IEnumerator RutinaTelegrafiado(Color colorAviso, bool aplicarRetroceso = false)
    {
        float duracion = ObtenerDuracionTelegrafiado();

        if (aplicarRetroceso)
        {
            float direccion = ObtenerDireccionMirada();
            bool puedeRetroceder = limitesArena == null ||
                !limitesArena.EstaCercaDelLimite(
                    transform.position.x, -direccion, distanciaMinimaPersecucion);

            rb.velocity = new Vector2(
                puedeRetroceder ? -direccion * velocidadAnticipacionEmbestida : 0f,
                rb.velocity.y);
            efectosVisuales?.EmitirPreparacionEmbestida();
        }
        else
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        float transcurrido = 0f;
        bool usarAviso = false;
        while (transcurrido < duracion)
        {
            if (usarAviso) estadoVisual?.MostrarAviso(colorAviso);
            else estadoVisual?.OcultarAviso();

            usarAviso = !usarAviso;
            yield return new WaitForSeconds(intervaloParpadeo);
            transcurrido += intervaloParpadeo;
        }

        estadoVisual?.MostrarAviso(colorAviso);
        rb.velocity = new Vector2(0f, rb.velocity.y);
        yield return new WaitForSeconds(pausaAntesDelImpacto);
    }

    private IEnumerator AtaqueLaser()
    {
        if (puntoDisparoCanon == null || lineaLaser == null) yield break;

        float grosorOriginal = lineaLaser.widthMultiplier;
        bool fase2 = saludJefe != null && saludJefe.estaEnFase2;
        float direccionX = ObtenerDireccionMirada();
        Vector2 direccionBase = new Vector2(direccionX, 0f);
        Vector2 direccionDisparo = direccionBase;

        if (!fase2 && jugador != null)
        {
            direccionDisparo =
                ((Vector2)jugador.position - (Vector2)puntoDisparoCanon.position).normalized;
        }
        else if (fase2)
        {
            direccionDisparo = RotarDireccion(direccionBase, -45f * direccionX);
        }

        try
        {
            yield return StartCoroutine(TelegrafiarLaser(direccionDisparo));
            if (combateDetenido) yield break;

            lineaLaser.widthMultiplier = grosorOriginal;
            lineaLaser.enabled = true;
            sonidos?.IniciarLaser();
            sacudidaCamara?.Sacudir(intensidadSacudidaLaser, duracionSacudidaLaser);

            bool jugadorDanado = false;
            float duracionDisparo = Mathf.Max(0.05f, tiempoMantenimientoLaser);
            float tiempoRestante = duracionDisparo;

            while (!combateDetenido && tiempoRestante > 0f)
            {
                Vector2 direccionActual = direccionDisparo;
                if (fase2)
                {
                    float progreso = 1f - tiempoRestante / duracionDisparo;
                    float angulo = Mathf.Lerp(-45f, 45f, progreso) * direccionX;
                    direccionActual = RotarDireccion(direccionBase, angulo);
                }

                ActualizarLineaLaser(direccionActual, true, ref jugadorDanado);
                tiempoRestante -= Time.deltaTime;
                yield return null;
            }

            sonidos?.FinalizarLaser();
            float duracionDesvanecimiento = 0.3f;
            float tiempo = 0f;
            while (!combateDetenido && tiempo < duracionDesvanecimiento)
            {
                tiempo += Time.deltaTime;
                lineaLaser.widthMultiplier =
                    Mathf.Lerp(grosorOriginal, 0f, tiempo / duracionDesvanecimiento);
                yield return null;
            }

            efectosVisuales?.EmitirSobrecalentamiento();
            yield return new WaitForSeconds(recuperacionLaser);
        }
        finally
        {
            // Siempre se restaura el LineRenderer, incluso si otro componente
            // lanza una excepción mientras el rayo intenta aplicar daño.
            sonidos?.DetenerBucleAtaque();
            lineaLaser.enabled = false;
            lineaLaser.widthMultiplier = grosorOriginal;
        }
    }

    private IEnumerator TelegrafiarLaser(Vector2 direccion)
    {
        float duracion = ObtenerDuracionTelegrafiado();
        float grosorOriginal = lineaLaser.widthMultiplier;
        try
        {
            sonidos?.ReproducirAnticipoLaser();
            lineaLaser.enabled = true;
            lineaLaser.widthMultiplier = grosorGuiaLaser;

            float transcurrido = 0f;
            bool usarAviso = false;
            while (transcurrido < duracion)
            {
                bool dummy = true;
                ActualizarLineaLaser(direccion, false, ref dummy);

                if (usarAviso) estadoVisual?.MostrarAviso(colorAvisoLaser);
                else estadoVisual?.OcultarAviso();

                usarAviso = !usarAviso;
                yield return new WaitForSeconds(intervaloParpadeo);
                transcurrido += intervaloParpadeo;
            }

            estadoVisual?.MostrarAviso(colorAvisoLaser);
            yield return new WaitForSeconds(pausaAntesDelImpacto);
        }
        finally
        {
            lineaLaser.enabled = false;
            lineaLaser.widthMultiplier = grosorOriginal;
        }
    }

    private void ActualizarLineaLaser(Vector2 direccion, bool aplicarDano, ref bool jugadorDanado)
    {
        Vector2 origen = puntoDisparoCanon.position;
        Vector2 puntoFinal = origen + direccion * 50f;
        RaycastHit2D[] impactos = Physics2D.RaycastAll(origen, direccion, 50f);
        System.Array.Sort(impactos, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit2D impacto in impactos)
        {
            if (impacto.collider == null || impacto.collider.transform.root == transform.root)
                continue;

            Jugador jugadorImpactado = impacto.collider.GetComponentInParent<Jugador>();
            if (jugadorImpactado != null)
            {
                if (aplicarDano && !jugadorDanado)
                {
                    jugadorImpactado.RecibirDano(danoLaser, puntoDisparoCanon.position);
                    jugadorDanado = true;
                }
                continue;
            }

            if (EsEntorno(impacto.collider))
            {
                puntoFinal = impacto.point;
                break;
            }
        }

        lineaLaser.SetPosition(0, origen);
        lineaLaser.SetPosition(1, puntoFinal);
    }

    private IEnumerator AtaqueMetralla()
    {
        if (balaMetrallaPrefab == null || puntoDisparoMetralla == null) yield break;

        sonidos?.ReproducirAnticipoMetralla();
        List<Vector2> objetivos = CalcularObjetivosMetralla();
        float duracionAviso = ObtenerDuracionTelegrafiado() + pausaAntesDelImpacto;
        foreach (Vector2 objetivo in objetivos)
            efectosVisuales?.CrearMarcadorSuelo(objetivo, radioMarcadorMetralla, duracionAviso);

        yield return StartCoroutine(RutinaTelegrafiado(colorAvisoMetralla));
        if (combateDetenido) yield break;

        foreach (Vector2 objetivo in objetivos)
        {
            GameObject bala = poolMetralla != null
                ? poolMetralla.Obtener(puntoDisparoMetralla.position, Quaternion.identity)
                : Instantiate(
                    balaMetrallaPrefab, puntoDisparoMetralla.position, Quaternion.identity);
            if (bala == null) continue;

            Rigidbody2D cuerpoBala = bala.GetComponent<Rigidbody2D>();

            if (cuerpoBala == null)
            {
                Debug.LogWarning("[JEFE] La bala de metralla no posee Rigidbody2D.");
                BalaEnemiga comportamientoSinFisica = bala.GetComponent<BalaEnemiga>();
                if (comportamientoSinFisica != null) comportamientoSinFisica.Retirar();
                else Destroy(bala);
                continue;
            }

            BalaEnemiga comportamiento = bala.GetComponent<BalaEnemiga>();
            comportamiento?.ConfigurarNotificacionImpactoEntorno(
                sonidos != null
                    ? new System.Action<Vector2>(sonidos.ReproducirImpactoMetralla)
                    : null);

            if (cuerpoBala.gravityScale <= 0.01f) cuerpoBala.gravityScale = 1.5f;

            Vector2 velocidad = CalcularVelocidadBalistica(
                puntoDisparoMetralla.position, objetivo, cuerpoBala);
            MovimientoProyectil movimiento = bala.GetComponent<MovimientoProyectil>();

            if (movimiento != null) movimiento.Impulsar(velocidad);
            else cuerpoBala.velocity = velocidad;
            sonidos?.ReproducirDisparoMetralla();

            float esperaLanzamiento = 0f;
            while (esperaLanzamiento < intervaloLanzamientoMetralla)
            {
                yield return new WaitForFixedUpdate();
                esperaLanzamiento += Time.fixedDeltaTime;
            }
        }

        yield return new WaitForSeconds(recuperacionMetralla);
    }

    private List<Vector2> CalcularObjetivosMetralla()
    {
        int cantidad = Mathf.Max(1, cantidadBalasMetralla);
        int cantidadEspacios = cantidad + 2;
        int huecoInicial = Random.Range(1, Mathf.Max(2, cantidadEspacios - 2));

        float izquierda = limitesArena != null
            ? limitesArena.Izquierda
            : transform.position.x - anchoDeLaArena * 0.5f;
        float derecha = limitesArena != null
            ? limitesArena.Derecha
            : transform.position.x + anchoDeLaArena * 0.5f;

        var objetivos = new List<Vector2>(cantidad);
        for (int i = 0; i < cantidadEspacios && objetivos.Count < cantidad; i++)
        {
            if (i == huecoInicial || i == huecoInicial + 1) continue;

            float t = cantidadEspacios <= 1 ? 0.5f : i / (float)(cantidadEspacios - 1);
            float x = Mathf.Lerp(izquierda, derecha, t);
            objetivos.Add(EncontrarPuntoSuelo(x));
        }

        return objetivos;
    }

    private Vector2 EncontrarPuntoSuelo(float x)
    {
        float alturaInicio = Mathf.Max(transform.position.y, puntoDisparoMetralla.position.y) + 6f;
        RaycastHit2D[] impactos =
            Physics2D.RaycastAll(new Vector2(x, alturaInicio), Vector2.down, 30f);
        System.Array.Sort(impactos, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit2D impacto in impactos)
        {
            if (impacto.collider == null || impacto.collider.transform.root == transform.root)
                continue;
            if (EsSuperficieParaMetralla(impacto)) return impacto.point;
        }

        Debug.LogWarning(
            $"[JEFE] No se encontró una superficie física bajo x={x:0.00}; " +
            "se utilizará la altura de respaldo de la metralla.");
        return new Vector2(x, transform.position.y - 1.2f);
    }

    private Vector2 CalcularVelocidadBalistica(
        Vector2 origen, Vector2 destino, Rigidbody2D cuerpoBala)
    {
        float gravedad = Mathf.Abs(Physics2D.gravity.y * cuerpoBala.gravityScale);
        if (gravedad <= 0.01f) gravedad = Mathf.Abs(Physics2D.gravity.y);

        float desplazamientoY = destino.y - origen.y;
        float discriminante =
            fuerzaSaltoMetralla * fuerzaSaltoMetralla - 2f * gravedad * desplazamientoY;
        float tiempoVuelo = discriminante > 0f
            ? (fuerzaSaltoMetralla + Mathf.Sqrt(discriminante)) / gravedad
            : Mathf.Max(0.35f, 2f * fuerzaSaltoMetralla / gravedad);

        float velocidadX = (destino.x - origen.x) / Mathf.Max(0.1f, tiempoVuelo);
        return new Vector2(velocidadX, fuerzaSaltoMetralla);
    }

    private IEnumerator AtaqueEmbestida()
    {
        sonidos?.ReproducirAnticipoEmbestida();
        yield return StartCoroutine(RutinaTelegrafiado(colorAvisoEmbestida, true));
        if (combateDetenido) yield break;

        MirarAlJugador();
        float direccion = jugador != null
            ? Mathf.Sign(jugador.position.x - transform.position.x)
            : ObtenerDireccionMirada();
        if (Mathf.Approximately(direccion, 0f)) direccion = ObtenerDireccionMirada();

        float tiempo = 0f;
        bool impactoPared = false;

        sonidos?.IniciarEmbestida();
        try
        {
            while (!combateDetenido && tiempo < duracionMaximaEmbestida)
            {
                if (limitesArena != null &&
                    limitesArena.EstaCercaDelLimite(transform.position.x, direccion, 0.2f))
                {
                    impactoPared = true;
                    break;
                }

                if (DetectarParedEmbestida(direccion))
                {
                    impactoPared = true;
                    break;
                }

                rb.velocity = new Vector2(direccion * velocidadEmbestida, rb.velocity.y);
                tiempo += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }
        finally
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            sonidos?.FinalizarEmbestida();
        }

        if (impactoPared)
        {
            Vector2 puntoImpacto = (Vector2)transform.position +
                Vector2.right * direccion * colliderPrincipal.bounds.extents.x;
            efectosVisuales?.EmitirImpactoEmbestida(puntoImpacto);
            sonidos?.ReproducirImpactoPared();
            sacudidaCamara?.Sacudir(0.16f, 0.22f);
        }

        yield return new WaitForSeconds(recuperacionEmbestida);
    }

    private bool DetectarParedEmbestida(float direccion)
    {
        if (colliderPrincipal == null) return false;

        float distancia = velocidadEmbestida * Time.fixedDeltaTime + 0.12f;
        Bounds limitesCollider = colliderPrincipal.bounds;
        Vector2 origenFrontal = (Vector2)limitesCollider.center +
            Vector2.right * direccion * (limitesCollider.extents.x * 0.95f);

        float[] alturas =
        {
            -limitesCollider.extents.y * 0.62f,
            0f,
            limitesCollider.extents.y * 0.62f
        };

        foreach (float altura in alturas)
        {
            Vector2 origen = origenFrontal + Vector2.up * altura;
            RaycastHit2D[] impactos = Physics2D.RaycastAll(
                origen, Vector2.right * direccion, distancia, mascaraEntorno);

            foreach (RaycastHit2D impacto in impactos)
            {
                if (impacto.collider == null ||
                    impacto.collider.transform.root == transform.root)
                {
                    continue;
                }

                if (Mathf.Abs(impacto.normal.x) > 0.45f ||
                    impacto.collider.CompareTag("Pared"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private IEnumerator AtaqueMisilTeledirigido()
    {
        sonidos?.ReproducirAnticipoMisil();
        yield return StartCoroutine(RutinaTelegrafiado(colorAvisoMisil));
        if (combateDetenido || misilPrefab == null || puntoDisparoMetralla == null) yield break;

        if (PuedeLanzarMisil())
        {
            GameObject objetoMisil =
                Instantiate(misilPrefab, puntoDisparoMetralla.position, Quaternion.identity);
            objetoMisil.GetComponent<MisilTeledirigido>()?.ConfigurarEmisor(gameObject);
            efectosVisuales?.EmitirExplosionEn(puntoDisparoMetralla.position);
            sonidos?.ReproducirLanzamientoMisil();
        }

        yield return new WaitForSeconds(recuperacionMisil);
    }

    private bool PuedeLanzarMisil()
    {
        return FindObjectsOfType<MisilTeledirigido>().Length < maximoMisilesActivos;
    }

    private IEnumerator TransicionFase2()
    {
        transicionFase2Pendiente = false;
        estaAtacando = true;
        rb.velocity = Vector2.zero;
        if (lineaLaser != null) lineaLaser.enabled = false;

        LimpiarProyectilesHostiles();
        efectosVisuales?.EmitirTransicionFase();
        sonidos?.ReproducirTransicionFase();
        sacudidaCamara?.Sacudir(intensidadSacudidaFase2, 0.5f);

        float tiempo = 0f;
        bool alternar = false;
        while (!combateDetenido && tiempo < duracionTransicionFase2)
        {
            estadoVisual?.MostrarAviso(alternar ? Color.white : colorAvisoLaser);

            alternar = !alternar;
            tiempo += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (combateDetenido) yield break;

        estadoVisual?.EstablecerFase(true);
        estadoVisual?.OcultarAviso();
        efectosVisuales?.ActivarHumoFase2();

        if (saludJefe != null) saludJefe.esVulnerable = true;
        forzarMisilFase2 = true;
        estaAtacando = false;
        AlCompletarTransicionFase2?.Invoke();
    }

    private void LimpiarProyectilesHostiles()
    {
        foreach (BalaEnemiga bala in FindObjectsOfType<BalaEnemiga>())
        {
            if (bala != null && !bala.FueDesviada) bala.Retirar();
        }

        foreach (MisilTeledirigido misil in FindObjectsOfType<MisilTeledirigido>())
        {
            if (misil != null) Destroy(misil.gameObject);
        }
    }

    private bool EsEntorno(Collider2D collider)
    {
        if (collider.CompareTag("Pared")) return true;
        int capaSuelo = LayerMask.NameToLayer("Suelo");
        return capaSuelo >= 0 && collider.gameObject.layer == capaSuelo;
    }

    private bool EsSuperficieParaMetralla(RaycastHit2D impacto)
    {
        Collider2D collider = impacto.collider;
        if (collider == null || collider.isTrigger) return false;
        if (EsEntorno(collider)) return true;
        if (impacto.normal.y < 0.35f) return false;

        Rigidbody2D cuerpoImpactado = collider.attachedRigidbody;
        return cuerpoImpactado == null ||
            cuerpoImpactado.bodyType == RigidbodyType2D.Static;
    }

    private float ObtenerDireccionMirada()
    {
        return transform.localScale.x > 0f ? -1f : 1f;
    }

    private float ObtenerDuracionTelegrafiado()
    {
        bool fase2 = saludJefe != null && saludJefe.estaEnFase2;
        return tiempoTelegrafiado * (fase2 ? multiplicadorTelegrafiadoFase2 : 1f);
    }

    private static Vector2 RotarDireccion(Vector2 direccion, float angulo)
    {
        return Quaternion.Euler(0f, 0f, angulo) * direccion;
    }
}
